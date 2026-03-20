import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/models/department_api_model.dart';

void main() {
  // ─── Fixtures ─────────────────────────────────────────────────────────────

  final remunerationJson = {
    'paymentUnit': {'id': 5, 'name': 'Por Mês'},
    'baseSalary': {
      'type': {'id': 1, 'name': 'BRL'},
      'value': '3500.00',
    },
    'description': 'Salário fixo mensal',
  };

  final roleJson = {
    'id': 'role-1',
    'name': 'Analista Jr',
    'description': 'Analista júnior',
    'cbo': '123456',
    'remuneration': remunerationJson,
  };

  final positionJson = {
    'id': 'pos-1',
    'name': 'Analista',
    'description': 'Analista financeiro',
    'cbo': '123456',
    'roles': [roleJson],
  };

  final departmentJson = {
    'id': 'dept-1',
    'name': 'Financeiro',
    'description': 'Departamento financeiro',
    'positions': [positionJson],
  };

  // ─── DepartmentApiModel ───────────────────────────────────────────────────

  group('DepartmentApiModel', () {
    test('fromJson parses all fields including nested positions', () {
      final model = DepartmentApiModel.fromJson(departmentJson);

      expect(model.id, 'dept-1');
      expect(model.name, 'Financeiro');
      expect(model.description, 'Departamento financeiro');
      expect(model.positions, hasLength(1));
      expect(model.positions.first.id, 'pos-1');
    });

    test('fromJsonSimple parses fields without positions', () {
      final model = DepartmentApiModel.fromJsonSimple(departmentJson);

      expect(model.id, 'dept-1');
      expect(model.positions, isEmpty);
    });

    test('toEntity maps to Department with nested positions and roles', () {
      final model = DepartmentApiModel.fromJson(departmentJson);
      final entity = model.toEntity();

      expect(entity.id, 'dept-1');
      expect(entity.name, 'Financeiro');
      expect(entity.positions, hasLength(1));
      expect(entity.positions.first.roles, hasLength(1));
      expect(entity.positions.first.roles.first.name, 'Analista Jr');
    });

    test('toCreateJson excludes id', () {
      final model = DepartmentApiModel.fromJson(departmentJson);
      final json = model.toCreateJson();

      expect(json.containsKey('id'), false);
      expect(json['name'], 'Financeiro');
      expect(json['description'], 'Departamento financeiro');
    });

    test('toJson includes id', () {
      final model = DepartmentApiModel.fromJson(departmentJson);
      final json = model.toJson();

      expect(json['id'], 'dept-1');
      expect(json['name'], 'Financeiro');
    });
  });

  // ─── PositionApiModel ─────────────────────────────────────────────────────

  group('PositionApiModel', () {
    test('fromJson parses all fields including nested roles', () {
      final model = PositionApiModel.fromJson(positionJson);

      expect(model.id, 'pos-1');
      expect(model.name, 'Analista');
      expect(model.cbo, '123456');
      expect(model.roles, hasLength(1));
    });

    test('fromJsonSimple parses fields without roles', () {
      final model = PositionApiModel.fromJsonSimple(positionJson);

      expect(model.id, 'pos-1');
      expect(model.roles, isEmpty);
    });

    test('toEntity maps to Position', () {
      final model = PositionApiModel.fromJson(positionJson);
      final entity = model.toEntity();

      expect(entity.id, 'pos-1');
      expect(entity.cbo, '123456');
      expect(entity.roles, hasLength(1));
    });

    test('toCreateJson includes departmentId and excludes id', () {
      final model = PositionApiModel.fromJson(positionJson);
      final json = model.toCreateJson('dept-1');

      expect(json.containsKey('id'), false);
      expect(json['departmentId'], 'dept-1');
      expect(json['name'], 'Analista');
      expect(json['cbo'], '123456');
    });
  });

  // ─── RoleApiModel ─────────────────────────────────────────────────────────

  group('RoleApiModel', () {
    test('fromJson parses all fields including remuneration', () {
      final model = RoleApiModel.fromJson(roleJson);

      expect(model.id, 'role-1');
      expect(model.name, 'Analista Jr');
      expect(model.cbo, '123456');
      expect(model.remuneration.paymentUnitId, '5');
      expect(model.remuneration.salaryTypeId, '1');
      expect(model.remuneration.baseSalaryValue, '3500.00');
      expect(model.remuneration.description, 'Salário fixo mensal');
    });

    test('toEntity maps to Role with remuneration', () {
      final model = RoleApiModel.fromJson(roleJson);
      final entity = model.toEntity();

      expect(entity.id, 'role-1');
      expect(entity.remuneration.paymentUnit.id, '5');
      expect(entity.remuneration.baseSalary.value, '3500.00');
      expect(entity.remuneration.baseSalary.type.id, '1');
    });

    test('toCreateJson includes positionId and excludes id', () {
      final model = RoleApiModel.fromJson(roleJson);
      final json = model.toCreateJson('pos-1');

      expect(json.containsKey('id'), false);
      expect(json['positionId'], 'pos-1');
      expect(json['name'], 'Analista Jr');
    });
  });

  // ─── RemunerationApiModel ─────────────────────────────────────────────────

  group('RemunerationApiModel', () {
    test('fromJson parses payment unit and salary type ids as strings', () {
      final model = RemunerationApiModel.fromJson(remunerationJson);

      expect(model.paymentUnitId, '5');
      expect(model.salaryTypeId, '1');
      expect(model.baseSalaryValue, '3500.00');
      expect(model.description, 'Salário fixo mensal');
    });

    test('toJson produces correct nested structure', () {
      final model = RemunerationApiModel.fromJson(remunerationJson);
      final json = model.toJson();

      expect(json['paymentUnit'], '5');
      expect((json['baseSalary'] as Map)['type'], '1');
      expect((json['baseSalary'] as Map)['value'], '3500.00');
    });
  });

  // ─── PaymentUnitApiModel ──────────────────────────────────────────────────

  group('PaymentUnitApiModel', () {
    test('fromJson converts numeric id to string', () {
      final model =
          PaymentUnitApiModel.fromJson({'id': 5, 'name': 'Por Mês'});

      expect(model.id, '5');
      expect(model.name, 'Por Mês');
    });

    test('toEntity maps correctly', () {
      final entity =
          PaymentUnitApiModel.fromJson({'id': 5, 'name': 'Por Mês'}).toEntity();

      expect(entity.id, '5');
      expect(entity.name, 'Por Mês');
    });
  });

  // ─── SalaryTypeApiModel ───────────────────────────────────────────────────

  group('SalaryTypeApiModel', () {
    test('fromJson converts numeric id to string', () {
      final model = SalaryTypeApiModel.fromJson({'id': 1, 'name': 'BRL'});

      expect(model.id, '1');
      expect(model.name, 'BRL');
    });

    test('toEntity maps correctly', () {
      final entity =
          SalaryTypeApiModel.fromJson({'id': 1, 'name': 'BRL'}).toEntity();

      expect(entity.id, '1');
      expect(entity.name, 'BRL');
    });
  });
}
