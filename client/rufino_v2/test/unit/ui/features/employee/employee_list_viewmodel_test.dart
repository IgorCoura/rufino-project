import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/employee.dart';
import 'package:rufino_v2/ui/features/employee/viewmodel/employee_list_viewmodel.dart';

import '../../../../testing/fakes/fake_company_repository.dart';
import '../../../../testing/fakes/fake_employee_repository.dart';

const _fakeCompany = Company(
  id: 'company-1',
  corporateName: 'Acme Corp',
  fantasyName: 'Acme',
  cnpj: '00000000000000',
);

const _emp1 = Employee(
  id: 'emp-1',
  name: 'Ana Lima',
  registration: 'R001',
  status: EmployeeStatus.active,
  roleName: 'Analista',
  documentStatus: DocumentStatus.ok,
);

const _emp2 = Employee(
  id: 'emp-2',
  name: 'Bruno Costa',
  registration: 'R002',
  status: EmployeeStatus.pending,
  roleName: 'Desenvolvedor',
  documentStatus: DocumentStatus.expiringSoon,
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeEmployeeRepository employeeRepository;
  late EmployeeListViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    employeeRepository = FakeEmployeeRepository();
    viewModel = EmployeeListViewModel(
      companyRepository: companyRepository,
      employeeRepository: employeeRepository,
      pageSize: 15,
    );
  });

  tearDown(() => viewModel.dispose());

  group('EmployeeListViewModel', () {
    test('initial state has no loading and empty employees', () {
      expect(viewModel.isLoading, false);
      expect(viewModel.hasError, false);
      expect(viewModel.employees, isEmpty);
      expect(viewModel.hasMore, true);
      expect(viewModel.searchParam, SearchParam.name);
    });

    test('transitions through loading state then populates employees on success',
        () async {
      employeeRepository.setEmployees([_emp1, _emp2]);

      final future = viewModel.refresh();
      expect(viewModel.isLoading, true);

      await future;

      expect(viewModel.isLoading, false);
      expect(viewModel.hasError, false);
      expect(viewModel.employees, hasLength(2));
      expect(viewModel.employees.first.name, 'Ana Lima');
    });

    test('sets hasError and clears employees when repository fails', () async {
      employeeRepository.setShouldFail(true);

      await viewModel.refresh();

      expect(viewModel.isLoading, false);
      expect(viewModel.hasError, true);
      expect(viewModel.errorMessage, isNotNull);
      expect(viewModel.employees, isEmpty);
    });

    test('sets hasError with company message when no company is selected',
        () async {
      companyRepository.setSelectedCompany(null);

      await viewModel.refresh();

      expect(viewModel.hasError, true);
      expect(viewModel.errorMessage, contains('empresa'));
    });

    test('sets hasMore to false when page is smaller than pageSize', () async {
      employeeRepository.setEmployees([_emp1]);

      await viewModel.refresh();

      expect(viewModel.hasMore, false);
    });

    test('sets hasMore to true when page equals pageSize', () async {
      const pageSize = 2;
      final repo = FakeEmployeeRepository()..setEmployees([_emp1, _emp2]);
      final vm = EmployeeListViewModel(
        companyRepository: companyRepository,
        employeeRepository: repo,
        pageSize: pageSize,
      );
      addTearDown(vm.dispose);

      await vm.refresh();

      expect(vm.hasMore, true);
    });

    test('loadNextPage appends employees to the existing list', () async {
      employeeRepository.setEmployees([_emp1, _emp2]);
      final vm = EmployeeListViewModel(
        companyRepository: companyRepository,
        employeeRepository: employeeRepository,
        pageSize: 2,
      );
      addTearDown(vm.dispose);

      await vm.refresh();
      expect(vm.employees, hasLength(2));
      expect(vm.hasMore, true);

      // Second page returns empty → marks end of list
      employeeRepository.setEmployees([]);
      await vm.loadNextPage();

      expect(vm.employees, hasLength(2));
      expect(vm.hasMore, false);
    });

    test('loadNextPage does nothing when hasMore is false', () async {
      employeeRepository.setEmployees([_emp1]);
      await viewModel.refresh();
      expect(viewModel.hasMore, false);

      final skipBefore = employeeRepository.lastGetEmployeesSkip;
      await viewModel.loadNextPage();

      expect(employeeRepository.lastGetEmployeesSkip, skipBefore);
    });

    test('notifies listeners on loading start and on completion', () async {
      employeeRepository.setEmployees([_emp1]);

      final notified = <bool>[];
      viewModel.addListener(() => notified.add(viewModel.isLoading));

      await viewModel.refresh();

      expect(notified, containsAllInOrder([true, false]));
    });

    test('onStatusChanged updates selectedStatus and refreshes', () async {
      employeeRepository.setEmployees([_emp1]);

      await viewModel.onStatusChanged(EmployeeStatus.active);

      expect(viewModel.selectedStatus, EmployeeStatus.active);
      expect(viewModel.employees, hasLength(1));
    });

    test('onDocumentStatusChanged updates selectedDocumentStatus and refreshes',
        () async {
      employeeRepository.setEmployees([_emp1]);

      await viewModel.onDocumentStatusChanged(DocumentStatus.ok);

      expect(viewModel.selectedDocumentStatus, DocumentStatus.ok);
    });

    test('toggleSort flips ascending flag and refreshes', () async {
      employeeRepository.setEmployees([]);
      expect(viewModel.ascending, true);

      await viewModel.toggleSort();

      expect(viewModel.ascending, false);
    });

    test('refresh resets pagination offset to zero', () async {
      employeeRepository.setEmployees([_emp1, _emp2]);
      final vm = EmployeeListViewModel(
        companyRepository: companyRepository,
        employeeRepository: employeeRepository,
        pageSize: 2,
      );
      addTearDown(vm.dispose);

      await vm.refresh();
      expect(employeeRepository.lastGetEmployeesSkip, 0);
    });

    // ─── Search-on-submit behaviour ───────────────────────────────────────────

    test('onSearchQueryChanged stores query without triggering a reload',
        () async {
      employeeRepository.setEmployees([_emp1]);
      await viewModel.refresh();

      final callCount = employeeRepository.getEmployeesCallCount;
      viewModel.onSearchQueryChanged('Ana');

      // Still same call count — no refresh fired
      expect(employeeRepository.getEmployeesCallCount, callCount);
      expect(viewModel.searchQuery, 'Ana');
    });

    test('onSearchSubmitted triggers a reload with the stored query', () async {
      employeeRepository.setEmployees([_emp1]);
      viewModel.onSearchQueryChanged('Ana');

      await viewModel.onSearchSubmitted();

      expect(employeeRepository.lastNameFilter, 'Ana');
      expect(viewModel.employees, hasLength(1));
    });

    // ─── SearchParam behaviour ────────────────────────────────────────────────

    test('onSearchParamChanged updates searchParam and refreshes', () async {
      employeeRepository.setEmployees([]);
      expect(viewModel.searchParam, SearchParam.name);

      await viewModel.onSearchParamChanged(SearchParam.role);

      expect(viewModel.searchParam, SearchParam.role);
    });

    test('routes search query to role filter when searchParam is role',
        () async {
      employeeRepository.setEmployees([_emp1]);
      viewModel.onSearchQueryChanged('Analista');
      await viewModel.onSearchParamChanged(SearchParam.role);

      expect(employeeRepository.lastRoleFilter, 'Analista');
      expect(employeeRepository.lastNameFilter, isNull);
    });

    test('routes search query to name filter when searchParam is name',
        () async {
      employeeRepository.setEmployees([_emp1]);
      viewModel.onSearchQueryChanged('Ana');
      await viewModel.onSearchSubmitted();

      expect(employeeRepository.lastNameFilter, 'Ana');
      expect(employeeRepository.lastRoleFilter, isNull);
    });

    // ─── Image cache ──────────────────────────────────────────────────────────

    test('imageFor returns null before any employees are loaded', () {
      expect(viewModel.imageFor('emp-1'), isNull);
    });

    test('imageFor returns cached bytes after image is loaded', () async {
      final fakeBytes = Uint8List.fromList([1, 2, 3]);
      employeeRepository.setEmployees([_emp1]);
      employeeRepository.setImageBytes(fakeBytes);

      await viewModel.refresh();
      // Allow fire-and-forget image loads to complete
      await Future.microtask(() {});
      await Future.delayed(Duration.zero);

      expect(viewModel.imageFor('emp-1'), fakeBytes);
    });
  });
}
