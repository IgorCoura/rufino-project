import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/models/employee_personal_info_api_model.dart';

void main() {
  group('EmployeePersonalInfoApiModel', () {
    group('fromJson', () {
      test('parses disability ids using the plural "disabilities" GET key', () {
        final json = {
          'gender': {'id': 1, 'name': 'Male'},
          'maritalStatus': {'id': 2, 'name': 'Married'},
          'ethinicity': {'id': 3, 'name': 'Mixed'},
          'educationLevel': {'id': 7, 'name': 'University'},
          'deficiency': {
            'disabilities': [
              {'id': 1, 'name': 'Physical'},
              {'id': 4, 'name': 'Hearing'},
            ],
            'observation': 'Observação',
          },
        };

        final model = EmployeePersonalInfoApiModel.fromJson(json);

        expect(model.genderId, '1');
        expect(model.maritalStatusId, '2');
        expect(model.ethnicityId, '3');
        expect(model.educationLevelId, '7');
        expect(model.disabilityIds, ['1', '4']);
        expect(model.disabilityObservation, 'Observação');
      });

      test('falls back to singular "disability" key when "disabilities" is absent',
          () {
        final json = {
          'gender': {'id': 1, 'name': 'Male'},
          'maritalStatus': {'id': 1, 'name': 'Single'},
          'ethinicity': {'id': 1, 'name': 'White'},
          'educationLevel': {'id': 1, 'name': 'None'},
          'deficiency': {
            'disability': [
              {'id': 2, 'name': 'Intellectual'},
            ],
            'observation': '',
          },
        };

        final model = EmployeePersonalInfoApiModel.fromJson(json);

        expect(model.disabilityIds, ['2']);
      });

      test('defaults to empty string when optional fields are missing', () {
        final model = EmployeePersonalInfoApiModel.fromJson({});

        expect(model.genderId, '');
        expect(model.maritalStatusId, '');
        expect(model.ethnicityId, '');
        expect(model.educationLevelId, '');
        expect(model.disabilityIds, isEmpty);
        expect(model.disabilityObservation, '');
      });
    });

    group('toEntity', () {
      test('maps all fields to an EmployeePersonalInfo entity', () {
        const model = EmployeePersonalInfoApiModel(
          genderId: '1',
          maritalStatusId: '2',
          ethnicityId: '3',
          educationLevelId: '4',
          disabilityIds: ['1', '2'],
          disabilityObservation: 'obs',
        );

        final entity = model.toEntity();

        expect(entity.genderId, '1');
        expect(entity.maritalStatusId, '2');
        expect(entity.ethnicityId, '3');
        expect(entity.educationLevelId, '4');
        expect(entity.disabilityIds, ['1', '2']);
        expect(entity.disabilityObservation, 'obs');
      });
    });
  });

  group('PersonalInfoOptionsApiModel', () {
    group('fromJson', () {
      // Raw JSON with English/non-accented server names that should be
      // translated to Brazilian Portuguese during parsing.
      final json = {
        'gender': [
          {'id': 1, 'name': 'Male'},
          {'id': 2, 'name': 'Female'},
        ],
        'maritalStatus': [
          {'id': 1, 'name': 'Single'},
          {'id': 2, 'name': 'Married'},
          {'id': 3, 'name': 'Divorced'},
          {'id': 4, 'name': 'Widowed'},
        ],
        'ethinicity': [
          {'id': 1, 'name': 'White'},
          {'id': 2, 'name': 'Black'},
          {'id': 3, 'name': 'Mixed'},
          {'id': 4, 'name': 'Asian'},
          {'id': 5, 'name': 'Indigenous'},
        ],
        'educationLevel': [
          {'id': 1, 'name': 'Illiterate'},
          {'id': 5, 'name': 'High School'},
          {'id': 7, 'name': 'University'},
          {'id': 10, 'name': 'PhD'},
        ],
        'disability': [
          {'id': 1, 'name': 'Physical'},
          {'id': 2, 'name': 'Intellectual'},
          {'id': 4, 'name': 'Hearing'},
          {'id': 5, 'name': 'Visual'},
          {'id': 7, 'name': 'Quota'},
        ],
      };

      test('translates gender ids to Portuguese names', () {
        final model = PersonalInfoOptionsApiModel.fromJson(json);

        expect(model.genders[0].name, 'Homem');
        expect(model.genders[1].name, 'Mulher');
      });

      test('translates marital status ids to Portuguese names', () {
        final model = PersonalInfoOptionsApiModel.fromJson(json);

        expect(model.maritalStatuses[0].name, 'Solteiro(a)');
        expect(model.maritalStatuses[1].name, 'Casado(a)');
        expect(model.maritalStatuses[2].name, 'Divorciado(a)');
        expect(model.maritalStatuses[3].name, 'Viúvo(a)');
      });

      test('translates ethnicity ids to Portuguese names', () {
        final model = PersonalInfoOptionsApiModel.fromJson(json);

        expect(model.ethnicities[0].name, 'Branco');
        expect(model.ethnicities[1].name, 'Negro');
        expect(model.ethnicities[2].name, 'Pardo');
        expect(model.ethnicities[3].name, 'Amarelo');
        expect(model.ethnicities[4].name, 'Indígena');
      });

      test('translates education level ids to Portuguese names', () {
        final model = PersonalInfoOptionsApiModel.fromJson(json);

        expect(model.educationLevels[0].name, 'Analfabeto');
        expect(model.educationLevels[1].name, 'Ensino Médio Completo');
        expect(model.educationLevels[2].name, 'Ensino Superior Completo');
        expect(model.educationLevels[3].name, 'Doutorado Completo');
      });

      test('translates disability ids to Portuguese names', () {
        final model = PersonalInfoOptionsApiModel.fromJson(json);

        expect(model.disabilities[0].name, 'Física');
        expect(model.disabilities[1].name, 'Intelectual');
        expect(model.disabilities[2].name, 'Auditiva');
        expect(model.disabilities[3].name, 'Visual');
        expect(model.disabilities[4].name, 'Cota de Incapacidade');
      });

      test('falls back to server name when id has no translation', () {
        final unknownJson = {
          'gender': [
            {'id': 99, 'name': 'Unknown Gender'},
          ],
          'maritalStatus': [],
          'ethinicity': [],
          'educationLevel': [],
          'disability': [],
        };

        final model = PersonalInfoOptionsApiModel.fromJson(unknownJson);

        expect(model.genders[0].name, 'Unknown Gender');
      });

      test('returns empty lists when API keys are missing', () {
        final model = PersonalInfoOptionsApiModel.fromJson({});

        expect(model.genders, isEmpty);
        expect(model.maritalStatuses, isEmpty);
        expect(model.ethnicities, isEmpty);
        expect(model.educationLevels, isEmpty);
        expect(model.disabilities, isEmpty);
      });

      test('preserves ids unchanged after translation', () {
        final model = PersonalInfoOptionsApiModel.fromJson(json);

        expect(model.genders[0].id, '1');
        expect(model.disabilities[4].id, '7');
      });
    });

    group('toEntity', () {
      test('converts all lists to SelectionOption entities with translated names',
          () {
        final json = {
          'gender': [
            {'id': 1, 'name': 'Male'},
          ],
          'maritalStatus': [],
          'ethinicity': [],
          'educationLevel': [],
          'disability': [],
        };

        final entity = PersonalInfoOptionsApiModel.fromJson(json).toEntity();

        expect(entity.genders, hasLength(1));
        expect(entity.genders.first.id, '1');
        expect(entity.genders.first.name, 'Homem');
      });
    });
  });
}
