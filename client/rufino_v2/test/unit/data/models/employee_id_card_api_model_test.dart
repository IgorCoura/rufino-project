import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/models/employee_id_card_api_model.dart';

void main() {
  group('EmployeeIdCardApiModel', () {
    group('fromJson', () {
      test('parses all fields from the idCard nested object', () {
        final json = {
          'idCard': {
            'cpf': '123.456.789-00',
            'motherName': 'Maria Silva',
            'fatherName': 'José Silva',
            'dateOfBirth': '1990-06-15',
            'birthCity': 'São Paulo',
            'birthState': 'SP',
            'nacionality': 'Brasileira',
          },
        };

        final model = EmployeeIdCardApiModel.fromJson(json);

        expect(model.cpf, '123.456.789-00');
        expect(model.motherName, 'Maria Silva');
        expect(model.fatherName, 'José Silva');
        expect(model.dateOfBirth, '1990-06-15');
        expect(model.birthCity, 'São Paulo');
        expect(model.birthState, 'SP');
        expect(model.nationality, 'Brasileira');
      });

      test('falls back to root-level fields when idCard key is absent', () {
        final json = {
          'cpf': '000.000.000-00',
          'motherName': 'Ana',
          'fatherName': '',
          'dateOfBirth': '2000-01-01',
          'birthCity': 'Rio',
          'birthState': 'RJ',
          'nacionality': 'Brasileira',
        };

        final model = EmployeeIdCardApiModel.fromJson(json);

        expect(model.cpf, '000.000.000-00');
        expect(model.dateOfBirth, '2000-01-01');
      });

      test('defaults missing fields to empty strings', () {
        final model = EmployeeIdCardApiModel.fromJson({'idCard': {}});

        expect(model.cpf, '');
        expect(model.motherName, '');
        expect(model.fatherName, '');
        expect(model.dateOfBirth, '');
        expect(model.birthCity, '');
        expect(model.birthState, '');
        expect(model.nationality, '');
      });
    });

    group('toEntity', () {
      test('converts dateOfBirth from yyyy-MM-dd to dd/MM/yyyy', () {
        const model = EmployeeIdCardApiModel(
          cpf: '123.456.789-00',
          motherName: 'Maria',
          fatherName: 'José',
          dateOfBirth: '1990-06-15',
          birthCity: 'São Paulo',
          birthState: 'SP',
          nationality: 'Brasileira',
        );

        final entity = model.toEntity();

        expect(entity.dateOfBirth, '15/06/1990');
      });

      test('handles dateOfBirth with ISO time component', () {
        const model = EmployeeIdCardApiModel(
          cpf: '',
          motherName: '',
          fatherName: '',
          dateOfBirth: '1990-06-15T00:00:00',
          birthCity: '',
          birthState: '',
          nationality: '',
        );

        final entity = model.toEntity();

        expect(entity.dateOfBirth, '15/06/1990');
      });

      test('returns empty string when dateOfBirth is empty', () {
        const model = EmployeeIdCardApiModel(
          cpf: '',
          motherName: '',
          fatherName: '',
          dateOfBirth: '',
          birthCity: '',
          birthState: '',
          nationality: '',
        );

        final entity = model.toEntity();

        expect(entity.dateOfBirth, '');
      });

      test('maps all non-date fields to the entity correctly', () {
        const model = EmployeeIdCardApiModel(
          cpf: '123.456.789-00',
          motherName: 'Maria',
          fatherName: 'José',
          dateOfBirth: '1990-06-15',
          birthCity: 'São Paulo',
          birthState: 'SP',
          nationality: 'Brasileira',
        );

        final entity = model.toEntity();

        expect(entity.cpf, '123.456.789-00');
        expect(entity.motherName, 'Maria');
        expect(entity.fatherName, 'José');
        expect(entity.birthCity, 'São Paulo');
        expect(entity.birthState, 'SP');
        expect(entity.nationality, 'Brasileira');
      });
    });

    group('dateToApi', () {
      test('converts dd/MM/yyyy to yyyy-MM-dd', () {
        expect(EmployeeIdCardApiModel.dateToApi('15/06/1990'), '1990-06-15');
      });

      test('converts 01/01/2000 correctly', () {
        expect(EmployeeIdCardApiModel.dateToApi('01/01/2000'), '2000-01-01');
      });

      test('returns the input unchanged when format is unexpected', () {
        expect(EmployeeIdCardApiModel.dateToApi('invalid'), 'invalid');
        expect(EmployeeIdCardApiModel.dateToApi(''), '');
      });
    });
  });
}
