import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/models/employee_api_model.dart';
import 'package:rufino_v2/domain/entities/employee.dart';

void main() {
  const jsonFixture = {
    'id': 'emp-1',
    'name': 'João Silva',
    'registration': 'REG001',
    'status': {'id': 2, 'name': 'active'},
    'roleName': 'Analista',
    'documentRepresentingStatus': {'id': 0, 'name': 'OK'},
  };

  group('EmployeeApiModel', () {
    test('fromJson parses all fields correctly', () {
      final model = EmployeeApiModel.fromJson(jsonFixture);

      expect(model.id, 'emp-1');
      expect(model.name, 'João Silva');
      expect(model.registration, 'REG001');
      expect(model.statusId, 2);
      expect(model.roleName, 'Analista');
      expect(model.documentStatusId, 0);
    });

    test('fromJson defaults registration to empty string when absent', () {
      final json = Map<String, dynamic>.from(jsonFixture)..remove('registration');
      final model = EmployeeApiModel.fromJson(json);

      expect(model.registration, '');
    });

    test('fromJson defaults roleName to empty string when absent', () {
      final json = Map<String, dynamic>.from(jsonFixture)..remove('roleName');
      final model = EmployeeApiModel.fromJson(json);

      expect(model.roleName, '');
    });

    test('toEntity maps to Employee domain entity correctly', () {
      final model = EmployeeApiModel.fromJson(jsonFixture);
      final entity = model.toEntity();

      expect(entity.id, 'emp-1');
      expect(entity.name, 'João Silva');
      expect(entity.registration, 'REG001');
      expect(entity.status, EmployeeStatus.active);
      expect(entity.roleName, 'Analista');
      expect(entity.documentStatus, DocumentStatus.ok);
    });

    test('toEntity maps unknown status id to EmployeeStatus.none', () {
      final json = Map<String, dynamic>.from(jsonFixture)
        ..['status'] = {'id': 99, 'name': 'unknown'};
      final entity = EmployeeApiModel.fromJson(json).toEntity();

      expect(entity.status, EmployeeStatus.none);
    });

    test('toEntity maps unknown document status id to DocumentStatus.all', () {
      final json = Map<String, dynamic>.from(jsonFixture)
        ..['documentRepresentingStatus'] = {'id': 99, 'name': 'unknown'};
      final entity = EmployeeApiModel.fromJson(json).toEntity();

      expect(entity.documentStatus, DocumentStatus.all);
    });
  });

  group('EmployeeStatus', () {
    test('fromId returns the matching status', () {
      expect(EmployeeStatus.fromId(1), EmployeeStatus.pending);
      expect(EmployeeStatus.fromId(2), EmployeeStatus.active);
      expect(EmployeeStatus.fromId(3), EmployeeStatus.vacation);
      expect(EmployeeStatus.fromId(4), EmployeeStatus.away);
      expect(EmployeeStatus.fromId(5), EmployeeStatus.inactive);
    });

    test('fromId returns none for unknown id', () {
      expect(EmployeeStatus.fromId(99), EmployeeStatus.none);
    });
  });

  group('DocumentStatus', () {
    test('fromId returns the matching status', () {
      expect(DocumentStatus.fromId(0), DocumentStatus.ok);
      expect(DocumentStatus.fromId(1), DocumentStatus.expiringSoon);
      expect(DocumentStatus.fromId(2), DocumentStatus.requiresAttention);
    });

    test('fromId returns all for unknown id', () {
      expect(DocumentStatus.fromId(99), DocumentStatus.all);
    });
  });
}
