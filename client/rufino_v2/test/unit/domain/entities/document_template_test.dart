import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/document_template.dart';

void main() {
  group('DocumentTemplate computed properties', () {
    const full = DocumentTemplate(
      id: '1',
      name: 'Contrato',
      description: 'Template de contrato',
      validityInDays: 365,
      workload: 8,
      usePreviousPeriod: false,
      acceptsSignature: true,
      bodyFileName: 'body.html',
      documentGroupId: 'g1',
      documentGroupName: 'Admissão',
      placeSignatures: [PlaceSignatureData(typeSignatureId: '1')],
    );

    const empty = DocumentTemplate(
      id: '2',
      name: 'Empty',
      description: '',
      validityInDays: null,
      workload: null,
      usePreviousPeriod: false,
      acceptsSignature: false,
    );

    test('hasDocumentGroup returns true when group id is not empty', () {
      expect(full.hasDocumentGroup, isTrue);
      expect(empty.hasDocumentGroup, isFalse);
    });

    test('hasValidity returns true when validityInDays is positive', () {
      expect(full.hasValidity, isTrue);
      expect(empty.hasValidity, isFalse);
    });

    test('hasWorkload returns true when workload is positive', () {
      expect(full.hasWorkload, isTrue);
      expect(empty.hasWorkload, isFalse);
    });

    test('hasFileConfiguration returns true when any file name is set', () {
      expect(full.hasFileConfiguration, isTrue);
      expect(empty.hasFileConfiguration, isFalse);
    });

    test('hasSignaturePlacements returns true when placements exist', () {
      expect(full.hasSignaturePlacements, isTrue);
      expect(empty.hasSignaturePlacements, isFalse);
    });

    test('requiresSignatureSetup reflects acceptsSignature', () {
      expect(full.requiresSignatureSetup, isTrue);
      expect(empty.requiresSignatureSetup, isFalse);
    });
  });

  group('DocumentTemplate.validateName', () {
    test('returns error when null or empty', () {
      expect(DocumentTemplate.validateName(null), isNotNull);
      expect(DocumentTemplate.validateName(''), isNotNull);
      expect(DocumentTemplate.validateName('   '), isNotNull);
    });

    test('returns error when exceeds 100 characters', () {
      expect(DocumentTemplate.validateName('A' * 101), isNotNull);
    });

    test('returns null for valid name', () {
      expect(DocumentTemplate.validateName('Contrato CLT'), isNull);
    });
  });

  group('DocumentTemplate.validateDescription', () {
    test('returns error when null or empty', () {
      expect(DocumentTemplate.validateDescription(null), isNotNull);
      expect(DocumentTemplate.validateDescription(''), isNotNull);
    });

    test('returns error when exceeds 500 characters', () {
      expect(DocumentTemplate.validateDescription('A' * 501), isNotNull);
    });

    test('returns null for valid description', () {
      expect(DocumentTemplate.validateDescription('Descrição válida'), isNull);
    });
  });

  group('DocumentTemplate.validateValidity', () {
    test('returns null when empty (optional field)', () {
      expect(DocumentTemplate.validateValidity(null), isNull);
      expect(DocumentTemplate.validateValidity(''), isNull);
    });

    test('returns error for out of range value', () {
      expect(DocumentTemplate.validateValidity('-1'), isNotNull);
      expect(DocumentTemplate.validateValidity('1000'), isNotNull);
      expect(DocumentTemplate.validateValidity('abc'), isNotNull);
    });

    test('returns null for valid value in 0-999 range', () {
      expect(DocumentTemplate.validateValidity('0'), isNull);
      expect(DocumentTemplate.validateValidity('365'), isNull);
      expect(DocumentTemplate.validateValidity('999'), isNull);
    });
  });

  group('DocumentTemplate.validateWorkload', () {
    test('returns null when empty (optional field)', () {
      expect(DocumentTemplate.validateWorkload(null), isNull);
      expect(DocumentTemplate.validateWorkload(''), isNull);
    });

    test('returns error for out of range value', () {
      expect(DocumentTemplate.validateWorkload('-1'), isNotNull);
      expect(DocumentTemplate.validateWorkload('1000'), isNotNull);
    });

    test('returns null for valid value', () {
      expect(DocumentTemplate.validateWorkload('8'), isNull);
    });
  });

  group('DocumentTemplate.validateFileName', () {
    test('returns null when empty (optional field)', () {
      expect(DocumentTemplate.validateFileName(null), isNull);
      expect(DocumentTemplate.validateFileName(''), isNull);
    });

    test('returns error when exceeds 20 characters', () {
      expect(DocumentTemplate.validateFileName('A' * 21), isNotNull);
    });

    test('returns error when not ending with .html', () {
      expect(DocumentTemplate.validateFileName('file.txt'), isNotNull);
    });

    test('returns null for valid .html file name', () {
      expect(DocumentTemplate.validateFileName('body.html'), isNull);
    });
  });

  group('PlaceSignatureData computed properties', () {
    test('hasType returns true when typeSignatureId is not empty', () {
      const sig = PlaceSignatureData(typeSignatureId: '1');
      expect(sig.hasType, isTrue);
      expect(const PlaceSignatureData().hasType, isFalse);
    });

    test('isComplete returns true when all fields have values', () {
      const sig = PlaceSignatureData(
        typeSignatureId: '1',
        page: '1',
        positionBottom: '50',
        positionLeft: '50',
        sizeX: '30',
        sizeY: '10',
      );
      expect(sig.isComplete, isTrue);
    });

    test('isComplete returns false when any field is empty', () {
      const sig = PlaceSignatureData(typeSignatureId: '1', page: '1');
      expect(sig.isComplete, isFalse);
    });
  });

  group('PlaceSignatureData.validateField', () {
    test('returns error when empty', () {
      expect(PlaceSignatureData.validateField(null, 'Página'), isNotNull);
      expect(PlaceSignatureData.validateField('', 'Página'), isNotNull);
    });

    test('returns error for out of range value', () {
      expect(PlaceSignatureData.validateField('-1', 'Página'), isNotNull);
      expect(PlaceSignatureData.validateField('101', 'Página'), isNotNull);
      expect(PlaceSignatureData.validateField('abc', 'Página'), isNotNull);
    });

    test('returns null for valid value in 0-100 range', () {
      expect(PlaceSignatureData.validateField('0', 'Página'), isNull);
      expect(PlaceSignatureData.validateField('50', 'Página'), isNull);
      expect(PlaceSignatureData.validateField('100', 'Página'), isNull);
    });

    test('includes label in error message', () {
      final error = PlaceSignatureData.validateField(null, 'Página');
      expect(error, contains('Página'));
    });
  });
}
