import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/core/utils/fuzzy_name_matcher.dart';

void main() {
  group('FuzzyNameMatcher.normalize', () {
    test('converts to lowercase and removes diacritics', () {
      expect(
        FuzzyNameMatcher.normalize('João Ávila Çésar'),
        equals('joao avila cesar'),
      );
    });

    test('collapses whitespace and strips punctuation', () {
      expect(
        FuzzyNameMatcher.normalize('  Maria   da   Silva!  '),
        equals('maria da silva'),
      );
    });

    test('returns empty string for empty input', () {
      expect(FuzzyNameMatcher.normalize(''), isEmpty);
    });
  });

  group('FuzzyNameMatcher.jaroWinkler', () {
    test('returns 1.0 for identical strings', () {
      expect(FuzzyNameMatcher.jaroWinkler('joao', 'joao'), equals(1.0));
    });

    test('returns 0.0 for empty strings', () {
      expect(FuzzyNameMatcher.jaroWinkler('', 'abc'), equals(0.0));
      expect(FuzzyNameMatcher.jaroWinkler('abc', ''), equals(0.0));
    });

    test('returns high score for similar strings', () {
      final score = FuzzyNameMatcher.jaroWinkler('joao silva', 'joao silvo');
      expect(score, greaterThan(0.9));
    });

    test('returns low score for completely different strings', () {
      final score =
          FuzzyNameMatcher.jaroWinkler('joao silva', 'pedro ferreira');
      expect(score, lessThan(0.7));
    });
  });

  group('FuzzyNameMatcher.score', () {
    test('returns high score for exact normalized match', () {
      final s = FuzzyNameMatcher.score('joao silva santos', 'joao silva santos');
      expect(s, greaterThanOrEqualTo(0.95));
    });

    test('returns high score when names differ only by accents', () {
      final nameA = FuzzyNameMatcher.normalize('João Silva Santos');
      final nameB = FuzzyNameMatcher.normalize('Joao Silva Santos');
      final s = FuzzyNameMatcher.score(nameA, nameB);
      expect(s, greaterThanOrEqualTo(0.95));
    });

    test('handles reversed name order with high score', () {
      final nameA = FuzzyNameMatcher.normalize('Santos João Silva');
      final nameB = FuzzyNameMatcher.normalize('João Silva Santos');
      final s = FuzzyNameMatcher.score(nameA, nameB);
      expect(s, greaterThan(0.6));
    });

    test('handles missing middle name with medium-to-high score', () {
      final nameA = FuzzyNameMatcher.normalize('João Santos');
      final nameB = FuzzyNameMatcher.normalize('João Silva Santos');
      final s = FuzzyNameMatcher.score(nameA, nameB);
      expect(s, greaterThan(0.5));
    });

    test('handles Portuguese connectors without penalty', () {
      final nameA = FuzzyNameMatcher.normalize('Maria Silva');
      final nameB = FuzzyNameMatcher.normalize('Maria da Silva');
      final s = FuzzyNameMatcher.score(nameA, nameB);
      expect(s, greaterThan(0.75));
    });

    test('returns low score for completely unrelated names', () {
      final nameA = FuzzyNameMatcher.normalize('Carlos Eduardo Pereira');
      final nameB = FuzzyNameMatcher.normalize('Ana Beatriz Oliveira');
      final s = FuzzyNameMatcher.score(nameA, nameB);
      expect(s, lessThan(0.45));
    });
  });

  group('FuzzyNameMatcher.findBestMatch', () {
    final candidates = [
      const NameCandidate(
        documentUnitId: '1',
        employeeId: 'e1',
        documentId: 'd1',
        name: 'João Silva Santos',
      ),
      const NameCandidate(
        documentUnitId: '2',
        employeeId: 'e2',
        documentId: 'd2',
        name: 'Maria Aparecida de Souza',
      ),
      const NameCandidate(
        documentUnitId: '3',
        employeeId: 'e3',
        documentId: 'd3',
        name: 'Carlos Eduardo Ferreira',
      ),
    ];

    test('matches exact name in text after label', () {
      const text = 'Documento de admissão\nNome: João Silva Santos\nData: 01/01/2026';
      final result = FuzzyNameMatcher.findBestMatch(text, candidates);

      expect(result, isNotNull);
      expect(result!.candidate.employeeId, equals('e1'));
      expect(result.confidenceLevel, equals(MatchConfidenceLevel.high));
    });

    test('matches name with accent variations', () {
      const text = 'Nome: Joao Silva Santos';
      final result = FuzzyNameMatcher.findBestMatch(text, candidates);

      expect(result, isNotNull);
      expect(result!.candidate.employeeId, equals('e1'));
      expect(result.confidence, greaterThan(0.85));
    });

    test('matches name found as capitalised word sequence', () {
      const text =
          'Este documento pertence a Maria Aparecida de Souza, emitido em 2026.';
      final result = FuzzyNameMatcher.findBestMatch(text, candidates);

      expect(result, isNotNull);
      expect(result!.candidate.employeeId, equals('e2'));
    });

    test('returns null for empty text', () {
      final result = FuzzyNameMatcher.findBestMatch('', candidates);
      expect(result, isNull);
    });

    test('returns null for empty candidates list', () {
      final result = FuzzyNameMatcher.findBestMatch('Nome: João Silva', []);
      expect(result, isNull);
    });

    test('returns low confidence for weak match', () {
      const text = 'Nome: Pedro Henrique Almeida';
      final result = FuzzyNameMatcher.findBestMatch(text, candidates);

      if (result != null) {
        expect(
          result.confidenceLevel,
          isNot(equals(MatchConfidenceLevel.high)),
        );
      }
    });

    test('picks the best match among multiple candidates', () {
      const text = 'Funcionário: Carlos Eduardo Ferreira\nSetor: TI';
      final result = FuzzyNameMatcher.findBestMatch(text, candidates);

      expect(result, isNotNull);
      expect(result!.candidate.employeeId, equals('e3'));
      expect(result.confidenceLevel, equals(MatchConfidenceLevel.high));
    });

    test('matches when name is embedded in running text without label', () {
      const text =
          'Declaramos que Carlos Eduardo Ferreira exerceu suas funções.';
      final result = FuzzyNameMatcher.findBestMatch(text, candidates);

      expect(result, isNotNull);
      expect(result!.candidate.employeeId, equals('e3'));
    });
  });

  group('MatchConfidenceLevel', () {
    test('high is assigned for scores >= 0.85', () {
      final result = FuzzyNameMatcher.findBestMatch(
        'Nome: João Silva Santos',
        [
          const NameCandidate(
            documentUnitId: '1',
            employeeId: 'e1',
            documentId: 'd1',
            name: 'João Silva Santos',
          ),
        ],
      );
      expect(result!.confidenceLevel, equals(MatchConfidenceLevel.high));
    });
  });
}
