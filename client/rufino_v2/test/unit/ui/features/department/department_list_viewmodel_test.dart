import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/department.dart';
import 'package:rufino_v2/domain/entities/position.dart';
import 'package:rufino_v2/domain/entities/remuneration.dart';
import 'package:rufino_v2/domain/entities/role.dart';
import 'package:rufino_v2/ui/features/department/viewmodel/department_list_viewmodel.dart';

import '../../../../testing/fakes/fake_company_repository.dart';
import '../../../../testing/fakes/fake_department_repository.dart';

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
  description: 'Analista financeiro',
  cbo: '123456',
  roles: [_fakeRole],
);

const _fakeDepartment = Department(
  id: 'dept-1',
  name: 'Financeiro',
  description: 'Departamento financeiro',
  positions: [_fakePosition],
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeDepartmentRepository departmentRepository;
  late DepartmentListViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    departmentRepository = FakeDepartmentRepository();
    viewModel = DepartmentListViewModel(
      companyRepository: companyRepository,
      departmentRepository: departmentRepository,
    );
  });

  tearDown(() => viewModel.dispose());

  group('DepartmentListViewModel', () {
    test('initial state has no loading and empty departments', () {
      expect(viewModel.isLoading, false);
      expect(viewModel.hasError, false);
      expect(viewModel.departments, isEmpty);
    });

    test('transitions through loading state then populates departments on success', () async {
      departmentRepository.setDepartments([_fakeDepartment]);

      final future = viewModel.loadDepartments();
      expect(viewModel.isLoading, true);

      await future;

      expect(viewModel.isLoading, false);
      expect(viewModel.hasError, false);
      expect(viewModel.departments, hasLength(1));
      expect(viewModel.departments.first.name, 'Financeiro');
    });

    test('sets hasError and clears departments when repository fails', () async {
      departmentRepository.setShouldFail(true);

      await viewModel.loadDepartments();

      expect(viewModel.isLoading, false);
      expect(viewModel.hasError, true);
      expect(viewModel.errorMessage, isNotNull);
      expect(viewModel.departments, isEmpty);
    });

    test('sets hasError with company message when no company is selected', () async {
      companyRepository.setSelectedCompany(null);

      await viewModel.loadDepartments();

      expect(viewModel.hasError, true);
      expect(viewModel.errorMessage, contains('empresa'));
    });

    test('succeeds with empty list when company has no departments', () async {
      departmentRepository.setDepartments([]);

      await viewModel.loadDepartments();

      expect(viewModel.hasError, false);
      expect(viewModel.departments, isEmpty);
    });

    test('notifies listeners on loading start and on completion', () async {
      departmentRepository.setDepartments([_fakeDepartment]);

      final notified = <bool>[];
      viewModel.addListener(() => notified.add(viewModel.isLoading));

      await viewModel.loadDepartments();

      expect(notified, containsAllInOrder([true, false]));
    });
  });
}
