import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/models/employee_contact_api_model.dart';

void main() {
  group('EmployeeContactApiModel', () {
    group('fromJson', () {
      test('parses cellphone and email correctly', () {
        final json = {'cellphone': '11999990000', 'email': 'user@example.com'};

        final model = EmployeeContactApiModel.fromJson(json);

        expect(model.cellphone, '11999990000');
        expect(model.email, 'user@example.com');
      });

      test('defaults to empty string when fields are missing', () {
        final model = EmployeeContactApiModel.fromJson({});

        expect(model.cellphone, '');
        expect(model.email, '');
      });

      test('defaults to empty string when fields are null', () {
        final json = {'cellphone': null, 'email': null};

        final model = EmployeeContactApiModel.fromJson(json);

        expect(model.cellphone, '');
        expect(model.email, '');
      });
    });

    group('toEntity', () {
      test('maps all fields to an EmployeeContact entity', () {
        const model = EmployeeContactApiModel(
          cellphone: '11999990000',
          email: 'user@example.com',
        );

        final entity = model.toEntity();

        expect(entity.cellphone, '11999990000');
        expect(entity.email, 'user@example.com');
      });
    });
  });
}
