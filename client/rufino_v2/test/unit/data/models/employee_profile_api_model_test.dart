import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/models/employee_profile_api_model.dart';
import 'package:rufino_v2/domain/entities/employee.dart';

void main() {
  const json = {
    'id': 'emp-1',
    'name': 'João Silva',
    'registration': 'REG001',
    'status': {'id': 2, 'name': 'active'},
    'roleId': 'role-1',
    'workplaceId': 'wp-1',
  };

  group('EmployeeProfileApiModel', () {
    test('fromJson parses all fields correctly', () {
      final model = EmployeeProfileApiModel.fromJson(json);

      expect(model.id, 'emp-1');
      expect(model.name, 'João Silva');
      expect(model.registration, 'REG001');
      expect(model.statusId, 2);
      expect(model.roleId, 'role-1');
      expect(model.workplaceId, 'wp-1');
    });

    test('fromJson defaults missing ids and registration to empty strings', () {
      final partialJson = Map<String, dynamic>.from(json)
        ..remove('registration')
        ..remove('roleId')
        ..remove('workplaceId');

      final model = EmployeeProfileApiModel.fromJson(partialJson);

      expect(model.registration, isEmpty);
      expect(model.roleId, isEmpty);
      expect(model.workplaceId, isEmpty);
    });

    test('toEntity maps the profile correctly', () {
      final entity = EmployeeProfileApiModel.fromJson(json).toEntity();

      expect(entity.id, 'emp-1');
      expect(entity.name, 'João Silva');
      expect(entity.registration, 'REG001');
      expect(entity.status, EmployeeStatus.active);
      expect(entity.roleId, 'role-1');
      expect(entity.workplaceId, 'wp-1');
    });

    test('toEntity maps unknown status ids to EmployeeStatus.none', () {
      final invalidJson = Map<String, dynamic>.from(json)
        ..['status'] = {'id': 99, 'name': 'unknown'};

      final entity = EmployeeProfileApiModel.fromJson(invalidJson).toEntity();

      expect(entity.status, EmployeeStatus.none);
    });
  });
}
