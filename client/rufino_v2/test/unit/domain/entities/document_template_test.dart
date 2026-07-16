import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/document_template.dart';

void main() {
  group('DocumentTemplate computed properties', () {
    const full = DocumentTemplate(
      id: '1',
      name: 'Contrato',
      description: 'Template de contrato',
      policies: TemplatePolicies(
        expiration: ExpirationRule(durationInDays: 365),
        workload: WorkloadRule(hours: 8),
      ),
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
      acceptsSignature: false,
    );

    test('hasDocumentGroup returns true when group id is not empty', () {
      expect(full.hasDocumentGroup, isTrue);
      expect(empty.hasDocumentGroup, isFalse);
    });

    test('hasValidity returns true when the expiration rule is active', () {
      expect(full.hasValidity, isTrue);
      expect(empty.hasValidity, isFalse);
    });

    test('hasWorkload returns true when the workload rule is active', () {
      expect(full.hasWorkload, isTrue);
      expect(empty.hasWorkload, isFalse);
    });

    test('validityInDays derives from the expiration rule', () {
      expect(full.validityInDays, 365);
      expect(empty.validityInDays, isNull);
    });

    test('workload derives from the workload rule', () {
      expect(full.workload, 8);
      expect(empty.workload, isNull);
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

  group('PlaceSignatureData.validateType', () {
    test('returns error when null or empty, since type is mandatory', () {
      expect(PlaceSignatureData.validateType(null), isNotNull);
      expect(PlaceSignatureData.validateType(''), isNotNull);
      expect(PlaceSignatureData.validateType('   '), isNotNull);
    });

    test('returns null when a type is selected', () {
      expect(PlaceSignatureData.validateType('2'), isNull);
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

  group('TemplatePolicies', () {
    test('isEmpty is true when no rule is active', () {
      expect(const TemplatePolicies().isEmpty, isTrue);
    });

    test('isEmpty is false when any rule is active', () {
      const policies =
          TemplatePolicies(workload: WorkloadRule(hours: 8));

      expect(policies.isEmpty, isFalse);
    });

    test('copyWith replaces a rule and keeps the others', () {
      const policies = TemplatePolicies(
        expiration: ExpirationRule(durationInDays: 365),
        workload: WorkloadRule(hours: 8),
      );

      final updated =
          policies.copyWith(expiration: const ExpirationRule(durationInDays: 30));

      expect(updated.expiration!.durationInDays, 30);
      expect(updated.workload!.hours, 8);
    });

    test('copyWith removes a rule when the clear flag is set', () {
      const policies = TemplatePolicies(
        expiration: ExpirationRule(durationInDays: 365),
        workload: WorkloadRule(hours: 8),
      );

      final updated = policies.copyWith(clearExpiration: true);

      expect(updated.expiration, isNull);
      expect(updated.workload!.hours, 8);
    });

    test('isEmpty is false when only the period rule is active', () {
      const policies = TemplatePolicies(
        period: PeriodRule(granularity: PeriodGranularity.monthly),
      );

      expect(policies.isEmpty, isFalse);
    });
  });

  group('DocumentTemplate competência', () {
    test('usePreviousPeriod derives from the period rule', () {
      const withRetroactive = DocumentTemplate(
        id: '1',
        name: 'X',
        description: '',
        policies: TemplatePolicies(
          period: PeriodRule(
              granularity: PeriodGranularity.monthly, usePreviousPeriod: true),
        ),
        acceptsSignature: false,
      );

      expect(withRetroactive.usePreviousPeriod, isTrue);
      expect(withRetroactive.hasPeriod, isTrue);
    });

    test('usePreviousPeriod is false when there is no period rule', () {
      const noPeriod = DocumentTemplate(
        id: '1',
        name: 'X',
        description: '',
        acceptsSignature: false,
      );

      expect(noPeriod.usePreviousPeriod, isFalse);
      expect(noPeriod.hasPeriod, isFalse);
    });
  });

  group('PeriodGranularity', () {
    test('fromId returns the matching granularity', () {
      expect(PeriodGranularity.fromId(3), PeriodGranularity.monthly);
    });

    test('fromId returns null for an unknown id', () {
      expect(PeriodGranularity.fromId(99), isNull);
    });

    test('ids match the backend PeriodType smart enum', () {
      expect(PeriodGranularity.daily.id, 1);
      expect(PeriodGranularity.weekly.id, 2);
      expect(PeriodGranularity.monthly.id, 3);
      expect(PeriodGranularity.yearly.id, 4);
    });
  });

  group('ExpirationRule.validateDuration', () {
    test('returns error when empty, since an active rule needs a duration', () {
      expect(ExpirationRule.validateDuration(null), isNotNull);
      expect(ExpirationRule.validateDuration(''), isNotNull);
    });

    test('returns error for zero, which the API rejects as a rule', () {
      expect(ExpirationRule.validateDuration('0'), isNotNull);
    });

    test('returns error for out of range value', () {
      expect(ExpirationRule.validateDuration('-1'), isNotNull);
      expect(ExpirationRule.validateDuration('1000'), isNotNull);
      expect(ExpirationRule.validateDuration('abc'), isNotNull);
    });

    test('returns null for valid value in 1-999 range', () {
      expect(ExpirationRule.validateDuration('1'), isNull);
      expect(ExpirationRule.validateDuration('365'), isNull);
      expect(ExpirationRule.validateDuration('999'), isNull);
    });
  });

  group('WorkloadRule.validateHours', () {
    test('returns error when empty, since an active rule needs hours', () {
      expect(WorkloadRule.validateHours(null), isNotNull);
      expect(WorkloadRule.validateHours(''), isNotNull);
    });

    test('returns error for zero, which the API rejects as a rule', () {
      expect(WorkloadRule.validateHours('0'), isNotNull);
    });

    test('returns null for valid value in 1-999 range', () {
      expect(WorkloadRule.validateHours('1'), isNull);
      expect(WorkloadRule.validateHours('8'), isNull);
      expect(WorkloadRule.validateHours('999'), isNull);
    });
  });
}
