import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/models/document_template_api_model.dart';
import 'package:rufino_v2/domain/entities/document_template.dart';

void main() {
  group('DocumentTemplateApiModel', () {
    test('fromJson parses validity and workload when present', () {
      final json = {
        'id': 'tpl-1',
        'name': 'NR01',
        'description': 'Descrição',
        'documentValidityDurationInDays': 365,
        'workloadInHours': 8,
      };

      final model = DocumentTemplateApiModel.fromJson(json);

      expect(model.validityDurationInDays, 365);
      expect(model.workloadInHours, 8);
    });

    test('fromJson keeps validity and workload null when the API omits them',
        () {
      final json = {'id': 'tpl-1', 'name': 'NR01', 'description': 'Descrição'};

      final model = DocumentTemplateApiModel.fromJson(json);

      expect(model.validityDurationInDays, isNull);
      expect(model.workloadInHours, isNull);
    });

    test('toEntity maps a present validity and workload to the entity', () {
      const model = DocumentTemplateApiModel(
        id: 'tpl-1',
        name: 'NR01',
        description: 'Descrição',
        validityDurationInDays: 365,
        workloadInHours: 8,
        usePreviousPeriod: false,
        acceptsSignature: false,
      );

      final entity = model.toEntity();

      expect(entity.validityInDays, 365);
      expect(entity.workload, 8);
    });

    test('toEntity maps an absent validity and workload to null', () {
      const model = DocumentTemplateApiModel(
        id: 'tpl-1',
        name: 'NR01',
        description: 'Descrição',
        validityDurationInDays: null,
        workloadInHours: null,
        usePreviousPeriod: false,
        acceptsSignature: false,
      );

      final entity = model.toEntity();

      expect(entity.validityInDays, isNull);
      expect(entity.workload, isNull);
    });

    test('toEntity maps a zeroed validity and workload to null', () {
      const model = DocumentTemplateApiModel(
        id: 'tpl-1',
        name: 'NR01',
        description: 'Descrição',
        validityDurationInDays: 0,
        workloadInHours: 0,
        usePreviousPeriod: false,
        acceptsSignature: false,
      );

      final entity = model.toEntity();

      expect(entity.validityInDays, isNull);
      expect(entity.workload, isNull);
    });

    test('toCreateJson sends null instead of zero when there is no rule', () {
      const model = DocumentTemplateApiModel(
        id: '',
        name: 'NR01',
        description: 'Descrição',
        validityDurationInDays: null,
        workloadInHours: null,
        usePreviousPeriod: false,
        acceptsSignature: false,
      );

      final json = model.toCreateJson();

      expect(json['documentValidityDurationInDays'], isNull);
      expect(json['workloadInHours'], isNull);
    });

    test('toJson sends null instead of zero when there is no rule', () {
      const model = DocumentTemplateApiModel(
        id: 'tpl-1',
        name: 'NR01',
        description: 'Descrição',
        validityDurationInDays: null,
        workloadInHours: null,
        usePreviousPeriod: false,
        acceptsSignature: false,
      );

      final json = model.toJson();

      expect(json['documentValidityDurationInDays'], isNull);
      expect(json['workloadInHours'], isNull);
    });

    test('toJson forwards validity and workload when a rule is configured', () {
      const model = DocumentTemplateApiModel(
        id: 'tpl-1',
        name: 'NR01',
        description: 'Descrição',
        validityDurationInDays: 365,
        workloadInHours: 8,
        usePreviousPeriod: false,
        acceptsSignature: false,
      );

      final json = model.toJson();

      expect(json['documentValidityDurationInDays'], 365);
      expect(json['workloadInHours'], 8);
    });
  });

  group('DocumentTemplateApiModel policies', () {
    test('fromJson parses the policies block into rules', () {
      final json = {
        'id': 'tpl-1',
        'name': 'NR01',
        'description': 'Descrição',
        'policies': {
          'expiration': {'durationInDays': 365},
          'workload': {'hours': 8},
        },
      };

      final model = DocumentTemplateApiModel.fromJson(json);

      expect(model.policies!.expiration!.durationInDays, 365);
      expect(model.policies!.workload!.hours, 8);
    });

    test('fromJson maps an absent rule inside the block to null', () {
      final json = {
        'id': 'tpl-1',
        'name': 'NR01',
        'description': 'Descrição',
        'policies': {
          'expiration': {'durationInDays': 365},
          'workload': null,
        },
      };

      final model = DocumentTemplateApiModel.fromJson(json);

      expect(model.policies!.expiration!.durationInDays, 365);
      expect(model.policies!.workload, isNull);
    });

    test('fromJson leaves policies null when the API omits the block', () {
      final json = {'id': 'tpl-1', 'name': 'NR01', 'description': 'Descrição'};

      final model = DocumentTemplateApiModel.fromJson(json);

      expect(model.policies, isNull);
    });

    test('toEntity prefers the policies block over the legacy fields', () {
      const model = DocumentTemplateApiModel(
        id: 'tpl-1',
        name: 'NR01',
        description: 'Descrição',
        validityDurationInDays: 365,
        workloadInHours: 8,
        usePreviousPeriod: false,
        acceptsSignature: false,
        policies: TemplatePolicies(
          expiration: ExpirationRule(durationInDays: 30),
        ),
      );

      final entity = model.toEntity();

      expect(entity.validityInDays, 30);
      expect(entity.workload, isNull);
    });

    test('toEntity derives rules from the legacy fields when the API omits the block',
        () {
      const model = DocumentTemplateApiModel(
        id: 'tpl-1',
        name: 'NR01',
        description: 'Descrição',
        validityDurationInDays: 365,
        workloadInHours: 8,
        usePreviousPeriod: false,
        acceptsSignature: false,
      );

      final entity = model.toEntity();

      expect(entity.policies.expiration!.durationInDays, 365);
      expect(entity.policies.workload!.hours, 8);
    });

    test('toEntity honours an explicitly empty policies block', () {
      const model = DocumentTemplateApiModel(
        id: 'tpl-1',
        name: 'NR01',
        description: 'Descrição',
        validityDurationInDays: 365,
        workloadInHours: 8,
        usePreviousPeriod: false,
        acceptsSignature: false,
        policies: TemplatePolicies(),
      );

      final entity = model.toEntity();

      expect(entity.policies.isEmpty, isTrue);
      expect(entity.validityInDays, isNull);
    });

    test('toJson sends the policies block alongside the mirrored legacy fields',
        () {
      const model = DocumentTemplateApiModel(
        id: 'tpl-1',
        name: 'NR01',
        description: 'Descrição',
        validityDurationInDays: 365,
        workloadInHours: 8,
        usePreviousPeriod: false,
        acceptsSignature: false,
        policies: TemplatePolicies(
          expiration: ExpirationRule(durationInDays: 365),
          workload: WorkloadRule(hours: 8),
        ),
      );

      final json = model.toJson();
      final policies = json['policies'] as Map<String, dynamic>;

      expect(policies['expiration'], {'durationInDays': 365});
      expect(policies['workload'], {'hours': 8});
      expect(json['documentValidityDurationInDays'], 365);
    });

    test('toJson omits a rule that is not active', () {
      const model = DocumentTemplateApiModel(
        id: 'tpl-1',
        name: 'NR01',
        description: 'Descrição',
        validityDurationInDays: null,
        workloadInHours: 8,
        usePreviousPeriod: false,
        acceptsSignature: false,
        policies: TemplatePolicies(workload: WorkloadRule(hours: 8)),
      );

      final json = model.toJson();
      final policies = json['policies'] as Map<String, dynamic>;

      expect(policies['expiration'], isNull);
      expect(policies['workload'], {'hours': 8});
    });

    // Null policies tells the API "fall back to the legacy fields" — distinct
    // from an empty block, which clears every rule.
    test('toJson sends a null policies block when there is no rule set', () {
      const model = DocumentTemplateApiModel(
        id: 'tpl-1',
        name: 'NR01',
        description: 'Descrição',
        validityDurationInDays: 365,
        workloadInHours: 8,
        usePreviousPeriod: false,
        acceptsSignature: false,
      );

      final json = model.toJson();

      expect(json['policies'], isNull);
    });
  });
}
