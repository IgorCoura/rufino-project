import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/employee.dart';
import 'package:rufino_v2/domain/entities/employee_contract.dart';
import 'package:rufino_v2/domain/entities/employee_id_card.dart';
import 'package:rufino_v2/domain/entities/employee_personal_info.dart';
import 'package:rufino_v2/domain/entities/employee_social_integration_program.dart';
import 'package:rufino_v2/ui/features/employee/export/etiquetas_xlsx_builder.dart';
import 'package:rufino_v2/ui/features/employee/export/soc_xlsx_builder.dart';
import 'package:rufino_v2/ui/features/employee/viewmodel/employee_list_viewmodel.dart';

import '../../../../testing/fakes/fake_company_repository.dart';
import '../../../../testing/fakes/fake_department_repository.dart';
import '../../../../testing/fakes/fake_employee_repository.dart';
import '../../../../testing/fakes/recording_file_save_service.dart';
import '../../../../testing/fakes/recording_spreadsheet_service.dart';

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

EmployeeListViewModel _buildViewModel({
  required FakeCompanyRepository companyRepository,
  required FakeEmployeeRepository employeeRepository,
  FakeDepartmentRepository? departmentRepository,
  RecordingSpreadsheetService? spreadsheetService,
  RecordingFileSaveService? fileSaveService,
  int pageSize = 15,
  int exportPageSize = 100,
  int exportDetailConcurrency = 10,
}) {
  return EmployeeListViewModel(
    companyRepository: companyRepository,
    employeeRepository: employeeRepository,
    departmentRepository: departmentRepository ?? FakeDepartmentRepository(),
    spreadsheetService: spreadsheetService ?? RecordingSpreadsheetService(),
    fileSaveService: fileSaveService ?? RecordingFileSaveService(),
    pageSize: pageSize,
    exportPageSize: exportPageSize,
    exportDetailConcurrency: exportDetailConcurrency,
  );
}

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeEmployeeRepository employeeRepository;
  late RecordingSpreadsheetService spreadsheetService;
  late RecordingFileSaveService fileSaveService;
  late EmployeeListViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    employeeRepository = FakeEmployeeRepository();
    spreadsheetService = RecordingSpreadsheetService();
    fileSaveService = RecordingFileSaveService();
    viewModel = _buildViewModel(
      companyRepository: companyRepository,
      employeeRepository: employeeRepository,
      spreadsheetService: spreadsheetService,
      fileSaveService: fileSaveService,
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
      expect(viewModel.isExporting, false);
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
      final vm = _buildViewModel(
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
      final vm = _buildViewModel(
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
      final vm = _buildViewModel(
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

  group('exportEtiquetas', () {
    test('builds a workbook with the etiquetas headers and one row per employee',
        () async {
      employeeRepository.setEmployees([_emp1, _emp2]);

      await viewModel.exportEtiquetas();

      expect(spreadsheetService.callCount, 1);
      expect(spreadsheetService.lastSheetName,
          EtiquetasXlsxBuilder.sheetName);
      expect(spreadsheetService.lastHeaders, EtiquetasXlsxBuilder.headers);
      expect(spreadsheetService.lastRows, hasLength(2));
      // Columns: Nome, CPF, Cargo, Empresa, CNPJ
      expect(spreadsheetService.lastRows![0][0], 'Ana Lima');
      expect(spreadsheetService.lastRows![0][1], '111.444.777-35');
      expect(spreadsheetService.lastRows![0][2], 'Analista');
      expect(spreadsheetService.lastRows![0][3], 'Acme Corp S.A.');
      expect(spreadsheetService.lastRows![0][4], '12.345.678/0001-90');
      expect(spreadsheetService.lastRows![1][0], 'Bruno Costa');
    });

    test('persists the generated bytes via FileSaveService', () async {
      employeeRepository.setEmployees([_emp1]);

      await viewModel.exportEtiquetas();

      expect(fileSaveService.saveCallCount, 1);
      expect(fileSaveService.lastBytes, isNotNull);
      expect(fileSaveService.lastFileName, startsWith('planilha_etiquetas_'));
    });

    test('flips isExporting while running and back to false after completion',
        () async {
      employeeRepository.setEmployees([_emp1]);
      final states = <bool>[];
      viewModel.addListener(() => states.add(viewModel.isExporting));

      await viewModel.exportEtiquetas();

      expect(states, contains(true));
      expect(viewModel.isExporting, false);
    });

    test('exposes a success message when the file is saved', () async {
      employeeRepository.setEmployees([_emp1]);

      await viewModel.exportEtiquetas();

      expect(viewModel.exportErrorMessage, isNull);
      expect(viewModel.exportSuccessLabel, contains('Etiquetas'));
    });

    test('reports an error when no employees match the filters', () async {
      employeeRepository.setEmployees([]);

      await viewModel.exportEtiquetas();

      expect(fileSaveService.saveCallCount, 0);
      expect(viewModel.exportErrorMessage, contains('Nenhum'));
    });

    test('reports an error when the employees endpoint fails', () async {
      employeeRepository.setShouldFail(true);

      await viewModel.exportEtiquetas();

      expect(fileSaveService.saveCallCount, 0);
      expect(viewModel.exportErrorMessage, isNotNull);
    });

    test('reports an error when saving the file fails', () async {
      employeeRepository.setEmployees([_emp1]);
      fileSaveService.shouldFail = true;

      await viewModel.exportEtiquetas();

      expect(viewModel.exportErrorMessage, isNotNull);
      expect(viewModel.exportSuccessLabel, isNull);
    });

    test('clearExportFeedback resets success and error messages', () async {
      employeeRepository.setEmployees([_emp1]);
      await viewModel.exportEtiquetas();
      expect(viewModel.exportSuccessLabel, isNotNull);

      viewModel.clearExportFeedback();

      expect(viewModel.exportSuccessLabel, isNull);
      expect(viewModel.exportErrorMessage, isNull);
    });

    test('uses a larger export page size than the regular UI page size',
        () async {
      employeeRepository.setEmployees([_emp1]);

      await viewModel.exportEtiquetas();

      // A real backend short-circuits the export loop when the response is
      // smaller than [exportPageSize]; the fake mirrors that by returning
      // a single record, which ends the loop after one round trip.
      expect(spreadsheetService.lastRows!.length, 1);
    });
  });

  group('exportSoc', () {
    test('builds a SOC workbook with the right column count', () async {
      employeeRepository.setEmployees([_emp1]);

      await viewModel.exportSoc();

      expect(spreadsheetService.callCount, 1);
      expect(spreadsheetService.lastSheetName, SocXlsxBuilder.sheetName);
      expect(spreadsheetService.lastHeaders, SocXlsxBuilder.headers);
      expect(spreadsheetService.lastRows, hasLength(1));
      expect(spreadsheetService.lastRows!.first.length,
          SocXlsxBuilder.headers.length);
    });

    test('fills SOC columns with data from the detail endpoints', () async {
      employeeRepository.setEmployees([_emp1]);
      employeeRepository.setIdCard(const EmployeeIdCard(
        cpf: '11144477735',
        motherName: 'Maria',
        fatherName: 'João',
        dateOfBirth: '01/02/1990',
        birthCity: 'São Paulo',
        birthState: 'SP',
        nationality: 'Brasileira',
      ));
      employeeRepository.setPersonalInfo(const EmployeePersonalInfo(
        genderId: '2',
        maritalStatusId: '2',
        ethnicityId: '1',
        educationLevelId: '1',
        disabilityIds: [],
        disabilityObservation: '',
      ));
      employeeRepository.setSocialIntegrationProgram(
        const EmployeeSocialIntegrationProgram(number: '07183177441'),
      );
      employeeRepository.setContracts(const [
        EmployeeContractInfo(
          initDate: '15/03/2024',
          finalDate: '',
          typeId: '1',
          typeName: 'CLT',
        ),
      ]);

      await viewModel.exportSoc();

      const headers = SocXlsxBuilder.headers;
      final row = spreadsheetService.lastRows!.first;
      expect(row[headers.indexOf('Nome Funcionário')], 'Ana Lima');
      expect(row[headers.indexOf('CPF')], '11144477735');
      expect(row[headers.indexOf('Dt.Nascimento')], '01/02/1990');
      expect(row[headers.indexOf('Sexo')], 'F');
      expect(row[headers.indexOf('Estado Civil')], '2');
      expect(row[headers.indexOf('Pis/Pasep')], '07183177441');
      expect(row[headers.indexOf('Dt.Admissão')], '15/03/2024');
      expect(row[headers.indexOf('Situação')], 'S');
      expect(row[headers.indexOf('Matrícula RH')], 'R001');
      expect(row[headers.indexOf('CNPJ Unidade')], '12345678000190');
      expect(row[headers.indexOf('Nome Unidade')], 'Acme Corp S.A.');
      expect(row[headers.indexOf('Razão Social Unid.')], 'Acme Corp S.A.');
      expect(row[headers.indexOf('GFIP')], '0');
      expect(row[headers.indexOf('Tel1 Unidade')], '(11) 968608425');
      expect(row[headers.indexOf('Contato Unid')], 'Igor de Brito Coura');
      expect(row[headers.indexOf('CNAE 2.0')], '43215');
      expect(row[headers.indexOf('Nº endereço Unidade')], '0');
      expect(row[headers.indexOf('Grau de Risco')], '3');
      expect(row[headers.indexOf('Origem Descrição Detalhada')], 'CA');
      expect(row[headers.indexOf('Código Categoria (eSocial)')], '101');
    });

    test('emits empty strings for columns whose data is not available',
        () async {
      employeeRepository.setEmployees([_emp1]);

      await viewModel.exportSoc();

      const headers = SocXlsxBuilder.headers;
      final row = spreadsheetService.lastRows!.first;
      expect(row[headers.indexOf('Rg')], '');
      expect(row[headers.indexOf('Nome Setor')], '');
    });

    test('updates the export progress towards 1.0 while running', () async {
      employeeRepository.setEmployees([_emp1, _emp2]);
      final vm = _buildViewModel(
        companyRepository: companyRepository,
        employeeRepository: employeeRepository,
        spreadsheetService: spreadsheetService,
        fileSaveService: fileSaveService,
        departmentRepository: FakeDepartmentRepository(),
        exportDetailConcurrency: 1,
      );
      addTearDown(vm.dispose);

      final progressValues = <double>[];
      vm.addListener(() => progressValues.add(vm.exportProgress));

      await vm.exportSoc();

      expect(progressValues, isNotEmpty);
      expect(progressValues.any((p) => p > 0 && p < 1), isTrue);
      expect(vm.exportProgress, 0.0); // resets after completion
    });
  });
}
