import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/department.dart';
import 'package:rufino_v2/domain/entities/position.dart';
import 'package:rufino_v2/domain/entities/remuneration.dart';
import 'package:rufino_v2/domain/entities/role.dart';
import 'package:rufino_v2/domain/entities/address.dart';
import 'package:rufino_v2/domain/entities/workplace.dart';
import 'package:rufino_v2/ui/features/employee/viewmodel/employee_form_viewmodel.dart';

import '../../../../testing/fakes/fake_company_repository.dart';
import '../../../../testing/fakes/fake_department_repository.dart';
import '../../../../testing/fakes/fake_employee_repository.dart';
import '../../../../testing/fakes/fake_workplace_repository.dart';

const _fakeCompany = Company(
  id: 'company-1',
  corporateName: 'Acme Corp',
  fantasyName: 'Acme',
  cnpj: '00000000000000',
);

const _fakeRemuneration = Remuneration(
  paymentUnit: PaymentUnit(id: '5', name: 'Por Mês'),
  baseSalary: BaseSalary(type: SalaryType(id: '1', name: 'BRL'), value: '3500.00'),
  description: 'Salário mensal',
);

const _fakeRole = Role(
  id: 'role-1',
  name: 'Analista Jr',
  description: 'Analista júnior',
  cbo: '123456',
  remuneration: _fakeRemuneration,
);

const _fakePosition = Position(
  id: 'pos-1',
  name: 'Analista',
  description: 'Cargo analista',
  cbo: '123456',
  roles: [_fakeRole],
);

const _fakeDepartment = Department(
  id: 'dept-1',
  name: 'Financeiro',
  description: 'Departamento financeiro',
  positions: [_fakePosition],
);

const _fakeAddress = Address(
  zipCode: '01310100',
  street: 'Av. Paulista',
  number: '1000',
  complement: '',
  neighborhood: 'Bela Vista',
  city: 'São Paulo',
  state: 'SP',
  country: 'Brasil',
);

const _fakeWorkplace = Workplace(
  id: 'wp-1',
  name: 'Sede Principal',
  address: _fakeAddress,
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeDepartmentRepository departmentRepository;
  late FakeWorkplaceRepository workplaceRepository;
  late FakeEmployeeRepository employeeRepository;
  late EmployeeFormViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    departmentRepository = FakeDepartmentRepository()
      ..setDepartments([_fakeDepartment]);
    workplaceRepository = FakeWorkplaceRepository()
      ..setWorkplaces([_fakeWorkplace]);
    employeeRepository = FakeEmployeeRepository();
    viewModel = EmployeeFormViewModel(
      companyRepository: companyRepository,
      departmentRepository: departmentRepository,
      workplaceRepository: workplaceRepository,
      employeeRepository: employeeRepository,
    );
  });

  tearDown(() => viewModel.dispose());

  group('EmployeeFormViewModel', () {
    test('initial status is idle', () {
      expect(viewModel.status, EmployeeFormStatus.idle);
      expect(viewModel.departments, isEmpty);
      expect(viewModel.workplaces, isEmpty);
    });

    test('loadOptions populates departments and workplaces', () async {
      await viewModel.loadOptions();

      expect(viewModel.status, EmployeeFormStatus.idle);
      expect(viewModel.departments, hasLength(1));
      expect(viewModel.departments.first.name, 'Financeiro');
      expect(viewModel.workplaces, hasLength(1));
      expect(viewModel.workplaces.first.name, 'Sede Principal');
    });

    test('loadOptions sets error when no company is selected', () async {
      companyRepository.setSelectedCompany(null);

      await viewModel.loadOptions();

      expect(viewModel.status, EmployeeFormStatus.error);
      expect(viewModel.errorMessage, contains('empresa'));
    });

    test('onDepartmentChanged exposes positions and resets position and role', () async {
      await viewModel.loadOptions();

      viewModel.onDepartmentChanged(_fakeDepartment);

      expect(viewModel.selectedDepartment, _fakeDepartment);
      expect(viewModel.positions, hasLength(1));
      expect(viewModel.selectedPosition, isNull);
      expect(viewModel.selectedRole, isNull);
    });

    test('onPositionChanged exposes roles and resets role', () async {
      await viewModel.loadOptions();
      viewModel.onDepartmentChanged(_fakeDepartment);
      viewModel.onPositionChanged(_fakePosition);

      expect(viewModel.selectedPosition, _fakePosition);
      expect(viewModel.roles, hasLength(1));
      expect(viewModel.selectedRole, isNull);
    });

    test('onRoleChanged sets selectedRole', () async {
      await viewModel.loadOptions();
      viewModel.onDepartmentChanged(_fakeDepartment);
      viewModel.onPositionChanged(_fakePosition);
      viewModel.onRoleChanged(_fakeRole);

      expect(viewModel.selectedRole, _fakeRole);
    });

    test('onWorkplaceChanged sets selectedWorkplace', () async {
      await viewModel.loadOptions();
      viewModel.onWorkplaceChanged(_fakeWorkplace);

      expect(viewModel.selectedWorkplace, _fakeWorkplace);
    });

    test('save transitions to saved when all fields are valid', () async {
      await viewModel.loadOptions();
      viewModel.nameController.text = 'João Silva';
      viewModel.onDepartmentChanged(_fakeDepartment);
      viewModel.onPositionChanged(_fakePosition);
      viewModel.onRoleChanged(_fakeRole);
      viewModel.onWorkplaceChanged(_fakeWorkplace);

      await viewModel.save();

      expect(viewModel.status, EmployeeFormStatus.saved);
      expect(employeeRepository.lastCreatedName, 'João Silva');
      expect(employeeRepository.lastCreatedRoleId, 'role-1');
      expect(employeeRepository.lastCreatedWorkplaceId, 'wp-1');
    });

    test('save sets error when role is not selected', () async {
      await viewModel.loadOptions();
      viewModel.nameController.text = 'João Silva';
      viewModel.onWorkplaceChanged(_fakeWorkplace);
      // no role selected

      await viewModel.save();

      expect(viewModel.status, EmployeeFormStatus.error);
      expect(viewModel.errorMessage, isNotNull);
    });

    test('save sets error when repository fails', () async {
      employeeRepository.setShouldFail(true);
      await viewModel.loadOptions();
      viewModel.nameController.text = 'João Silva';
      viewModel.onRoleChanged(_fakeRole);
      viewModel.onWorkplaceChanged(_fakeWorkplace);

      await viewModel.save();

      expect(viewModel.status, EmployeeFormStatus.error);
      expect(viewModel.errorMessage, isNotNull);
    });

    group('validateName', () {
      test('returns null for a valid full name', () {
        expect(viewModel.validateName('João Silva'), isNull);
      });

      test('returns error for empty name', () {
        expect(viewModel.validateName(''), isNotNull);
        expect(viewModel.validateName(null), isNotNull);
      });

      test('returns error for single word name', () {
        expect(viewModel.validateName('João'), isNotNull);
      });

      test('returns error for name exceeding 100 characters', () {
        final longName = 'A ${'B ' * 50}';
        expect(viewModel.validateName(longName), isNotNull);
      });
    });
  });
}
