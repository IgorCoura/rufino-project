import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/document_group.dart';

void main() {
  group('DocumentGroup computed properties', () {
    test('hasDescription returns true when description is not empty', () {
      const group = DocumentGroup(
        id: '1',
        name: 'Admissão',
        description: 'Documentos de admissão',
      );
      expect(group.hasDescription, isTrue);
    });

    test('hasDescription returns false when description is empty', () {
      const group = DocumentGroup(id: '1', name: 'Admissão', description: '');
      expect(group.hasDescription, isFalse);
    });
  });

  group('DocumentGroup.validateName', () {
    test('returns error when null or empty', () {
      expect(DocumentGroup.validateName(null), isNotNull);
      expect(DocumentGroup.validateName(''), isNotNull);
      expect(DocumentGroup.validateName('   '), isNotNull);
    });

    test('returns error when exceeds 100 characters', () {
      expect(DocumentGroup.validateName('A' * 101), isNotNull);
    });

    test('returns null for valid name', () {
      expect(DocumentGroup.validateName('Admissão'), isNull);
    });
  });

  group('DocumentGroup.validateDescription', () {
    test('returns error when null or empty', () {
      expect(DocumentGroup.validateDescription(null), isNotNull);
      expect(DocumentGroup.validateDescription(''), isNotNull);
      expect(DocumentGroup.validateDescription('   '), isNotNull);
    });

    test('returns error when exceeds 1000 characters', () {
      expect(DocumentGroup.validateDescription('A' * 1001), isNotNull);
    });

    test('returns null for valid description', () {
      expect(
        DocumentGroup.validateDescription('Documentos necessários para admissão.'),
        isNull,
      );
    });
  });
}
