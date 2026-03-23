import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/models/employee_military_document_api_model.dart';

void main() {
  group('EmployeeMilitaryDocumentApiModel', () {
    test('fromJson parses all fields correctly from the GET response keys', () {
      final json = <String, dynamic>{
        'number': 'RM-12345',
        'type': 'Reservista',
        'isRequired': true,
      };

      final model = EmployeeMilitaryDocumentApiModel.fromJson(json);

      expect(model.number, 'RM-12345');
      expect(model.type, 'Reservista');
      expect(model.isRequired, true);
    });

    test('fromJson defaults missing fields to empty strings and false', () {
      final json = <String, dynamic>{};

      final model = EmployeeMilitaryDocumentApiModel.fromJson(json);

      expect(model.number, '');
      expect(model.type, '');
      expect(model.isRequired, false);
    });

    test('toEntity converts to EmployeeMilitaryDocument with all fields', () {
      const model = EmployeeMilitaryDocumentApiModel(
        number: 'RM-99999',
        type: 'Certificado de Dispensa',
        isRequired: true,
      );

      final entity = model.toEntity();

      expect(entity.number, 'RM-99999');
      expect(entity.type, 'Certificado de Dispensa');
      expect(entity.isRequired, true);
    });

    test('toJsonMap produces the expected PUT body shape', () {
      final json = EmployeeMilitaryDocumentApiModel.toJsonMap(
        'emp-1',
        'RM-12345',
        'Reservista',
      );

      expect(json, {
        'employeeId': 'emp-1',
        'documentNumber': 'RM-12345',
        'documentType': 'Reservista',
      });
    });
  });
}
