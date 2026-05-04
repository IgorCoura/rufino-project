import 'dart:collection';

import 'package:flutter/foundation.dart';

import '../../../../data/services/file_save_service.dart'
    show FileSaveOutcome, FileSaveService;
import '../../../../data/services/spreadsheet_service.dart';
import '../../../../domain/entities/company_detail.dart';
import '../../../../domain/entities/department.dart';
import '../../../../domain/entities/employee.dart';
import '../../../../domain/entities/employee_contract.dart';
import '../../../../domain/entities/employee_id_card.dart';
import '../../../../domain/entities/employee_personal_info.dart';
import '../../../../domain/entities/employee_profile.dart';
import '../../../../domain/entities/employee_social_integration_program.dart';
import '../../../../domain/entities/personal_info_options.dart';
import '../../../../domain/entities/position.dart';
import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/department_repository.dart';
import '../../../../domain/repositories/employee_repository.dart';
import '../export/employee_export_row.dart';
import '../export/etiquetas_xlsx_builder.dart';
import '../export/soc_xlsx_builder.dart';

/// Which field the search query is applied to.
enum SearchParam {
  /// Searches by employee name.
  name('Nome'),

  /// Searches by employee role name.
  role('Função');

  const SearchParam(this.label);

  /// The human-readable Portuguese label for this parameter.
  final String label;
}

/// Possible statuses for the employee list screen.
enum EmployeeListStatus { loading, loadingMore, idle, error }

/// Loads and exposes a paginated list of employees for the currently selected
/// company, with support for search, status/document-status filtering, and sort.
///
/// Pagination is offset-based: each call to [loadNextPage] appends the next
/// batch of [pageSize] employees. Call [refresh] to reset filters and reload.
///
/// Search is applied on submit only — [onSearchQueryChanged] stores the query
/// without triggering a reload; [onSearchSubmitted] applies it.
///
/// Also exposes [exportEtiquetas] and [exportSoc] for spreadsheet downloads
/// of the currently filtered set.
class EmployeeListViewModel extends ChangeNotifier {
  EmployeeListViewModel({
    required CompanyRepository companyRepository,
    required EmployeeRepository employeeRepository,
    required DepartmentRepository departmentRepository,
    required SpreadsheetService spreadsheetService,
    required FileSaveService fileSaveService,
    this.pageSize = 15,
    this.exportPageSize = 1000000,
    this.exportDetailConcurrency = 10,
  })  : _companyRepository = companyRepository,
        _employeeRepository = employeeRepository,
        _departmentRepository = departmentRepository,
        _spreadsheetService = spreadsheetService,
        _fileSaveService = fileSaveService;

  final CompanyRepository _companyRepository;
  final EmployeeRepository _employeeRepository;
  final DepartmentRepository _departmentRepository;
  final SpreadsheetService _spreadsheetService;
  final FileSaveService _fileSaveService;

  /// The number of items fetched per UI page.
  final int pageSize;

  /// The page size used while fetching all employees during an export.
  ///
  /// Larger than [pageSize] to reduce the number of round-trips when paging
  /// through hundreds of records for a single spreadsheet.
  final int exportPageSize;

  /// How many employees have their detail endpoints fetched in parallel
  /// during the SOC export.
  ///
  /// Each employee triggers four detail calls, so a value of 10 keeps the
  /// number of concurrent HTTP requests bounded around 40 — safe for the
  /// default `http.Client` connection pool.
  final int exportDetailConcurrency;

  // ─── State ────────────────────────────────────────────────────────────────

  List<Employee> _employees = [];
  EmployeeListStatus _status = EmployeeListStatus.idle;
  String? _errorMessage;
  String? _companyId;

  // Pagination
  int _sizeSkip = 0;
  bool _hasMore = true;

  // Filters
  String _searchQuery = '';
  SearchParam _searchParam = SearchParam.name;
  EmployeeStatus _selectedStatus = EmployeeStatus.none;
  DocumentStatus _selectedDocumentStatus = DocumentStatus.all;
  bool _ascending = true;

  // Image cache — keyed by employee id.
  final Map<String, Uint8List> _imageCache = {};

  // Export state
  bool _isExporting = false;
  double _exportProgress = 0.0;
  String? _exportErrorMessage;
  String? _exportSuccessLabel;

  // ─── Getters ──────────────────────────────────────────────────────────────

  /// The employees loaded so far, empty while loading on first page or on error.
  UnmodifiableListView<Employee> get employees =>
      UnmodifiableListView(_employees);

  /// Whether the first page is being fetched.
  bool get isLoading => _status == EmployeeListStatus.loading;

  /// Whether a subsequent page is being appended.
  bool get isLoadingMore => _status == EmployeeListStatus.loadingMore;

  /// Whether there are more pages to load.
  bool get hasMore => _hasMore;

  /// Whether the last fetch resulted in an error.
  bool get hasError => _status == EmployeeListStatus.error;

  /// Human-readable error message set when [hasError] is true.
  String? get errorMessage => _errorMessage;

  /// The current search query string.
  String get searchQuery => _searchQuery;

  /// The field that [searchQuery] is applied to.
  SearchParam get searchParam => _searchParam;

  /// The currently selected employee status filter.
  EmployeeStatus get selectedStatus => _selectedStatus;

  /// The currently selected document status filter.
  DocumentStatus get selectedDocumentStatus => _selectedDocumentStatus;

  /// Whether the list is sorted in ascending order.
  bool get ascending => _ascending;

  /// Returns the cached profile image bytes for [employeeId], or null if not
  /// yet loaded or unavailable.
  Uint8List? imageFor(String employeeId) => _imageCache[employeeId];

  /// Whether a spreadsheet export is currently running.
  bool get isExporting => _isExporting;

  /// Progress of the running export between 0.0 and 1.0.
  double get exportProgress => _exportProgress;

  /// Localized error message set when the last export failed.
  String? get exportErrorMessage => _exportErrorMessage;

  /// Localized success message set when the last export finished cleanly.
  String? get exportSuccessLabel => _exportSuccessLabel;

  // ─── Filter / Sort actions ─────────────────────────────────────────────────

  /// Stores the search query locally without triggering a reload.
  ///
  /// Call [onSearchSubmitted] to apply the query and reload the list.
  void onSearchQueryChanged(String query) {
    _searchQuery = query;
  }

  /// Applies the current [searchQuery] and refreshes the employee list.
  Future<void> onSearchSubmitted() async {
    await refresh();
  }

  /// Updates the search parameter to [param] and refreshes the list.
  Future<void> onSearchParamChanged(SearchParam param) async {
    _searchParam = param;
    await refresh();
  }

  /// Updates the employee status filter to [status] and refreshes the list.
  Future<void> onStatusChanged(EmployeeStatus status) async {
    _selectedStatus = status;
    await refresh();
  }

  /// Updates the document status filter to [status] and refreshes the list.
  Future<void> onDocumentStatusChanged(DocumentStatus status) async {
    _selectedDocumentStatus = status;
    await refresh();
  }

  /// Toggles the sort direction between ascending and descending and refreshes.
  Future<void> toggleSort() async {
    _ascending = !_ascending;
    await refresh();
  }

  // ─── Load actions ─────────────────────────────────────────────────────────

  /// Resets pagination and reloads the first page with the current filters.
  Future<void> refresh() async {
    _employees = [];
    _sizeSkip = 0;
    _hasMore = true;
    _status = EmployeeListStatus.loading;
    _errorMessage = null;
    notifyListeners();

    await _fetchPage();
  }

  /// Appends the next page of employees to the current list.
  ///
  /// Does nothing if [isLoading], [isLoadingMore], or [hasMore] is false.
  Future<void> loadNextPage() async {
    if (_status == EmployeeListStatus.loading ||
        _status == EmployeeListStatus.loadingMore ||
        !_hasMore) {
      return;
    }

    _status = EmployeeListStatus.loadingMore;
    notifyListeners();

    await _fetchPage();
  }

  // ─── Export actions ───────────────────────────────────────────────────────

  /// Clears any success or error message produced by the last export.
  ///
  /// The screen calls this after surfacing the message in a snack bar so
  /// the same notification is not shown twice.
  void clearExportFeedback() {
    if (_exportErrorMessage == null && _exportSuccessLabel == null) return;
    _exportErrorMessage = null;
    _exportSuccessLabel = null;
    notifyListeners();
  }

  /// Builds and saves the "Planilha Etiquetas" spreadsheet for the currently
  /// filtered employee list.
  ///
  /// Fetches every employee that matches the active filters along with the
  /// detailed company information, assembles the five required label
  /// columns, and asks the platform to save the resulting `.xlsx` file.
  Future<void> exportEtiquetas() async {
    if (_isExporting) return;
    await _runExport(
      label: 'Planilha Etiquetas',
      sheetName: EtiquetasXlsxBuilder.sheetName,
      baseFileName: EtiquetasXlsxBuilder.baseFileName,
      headers: EtiquetasXlsxBuilder.headers,
      build: (companyDetail, employees, onProgress) async {
        final rows = <EtiquetaRow>[];
        final total = employees.isEmpty ? 1 : employees.length;
        for (var i = 0; i < employees.length; i += exportDetailConcurrency) {
          final end = (i + exportDetailConcurrency).clamp(0, employees.length);
          final chunk = employees.sublist(i, end);
          final idCards = await Future.wait(chunk.map(_fetchIdCard));
          for (var j = 0; j < chunk.length; j++) {
            rows.add(EtiquetaRow(
              name: chunk[j].name,
              cpf: idCards[j]?.formattedCpf ?? '',
              position: chunk[j].roleName,
              companyName: companyDetail.corporateName,
              cnpj: companyDetail.formattedCnpj,
            ));
          }
          onProgress(end / total);
        }
        return EtiquetasXlsxBuilder.toRows(rows);
      },
    );
  }

  /// Builds and saves the "Planilha SOC" spreadsheet for the currently
  /// filtered employee list.
  ///
  /// In addition to the data already in the list endpoint, fetches per-
  /// employee details (id card, personal info, contracts, PIS/PASEP) in
  /// chunks of [exportDetailConcurrency] employees so the SOC columns can
  /// be populated. Detail-call failures are tolerated — the corresponding
  /// columns are emitted as empty strings.
  Future<void> exportSoc() async {
    if (_isExporting) return;
    await _runExport(
      label: 'Planilha SOC',
      sheetName: SocXlsxBuilder.sheetName,
      baseFileName: SocXlsxBuilder.baseFileName,
      headers: SocXlsxBuilder.headers,
      build: (companyDetail, employees, onProgress) async {
        final optionsResult =
            await _employeeRepository.getPersonalInfoOptions(_companyId!);
        final options = optionsResult.valueOrNull;

        final departmentsResult =
            await _departmentRepository.getDepartments(_companyId!);
        final roleLookup =
            _buildRoleLookup(departmentsResult.valueOrNull ?? const []);

        final rows = <SocRow>[];
        final total = employees.isEmpty ? 1 : employees.length;
        for (var i = 0; i < employees.length; i += exportDetailConcurrency) {
          final end = (i + exportDetailConcurrency).clamp(0, employees.length);
          final chunk = employees.sublist(i, end);
          final details = await Future.wait(
            chunk.map(_fetchSocDetails),
          );
          for (var j = 0; j < chunk.length; j++) {
            final profile = details[j].profile;
            final placement =
                profile == null ? null : roleLookup[profile.roleId];
            rows.add(_socRowFor(
              employee: chunk[j],
              details: details[j],
              companyDetail: companyDetail,
              options: options,
              position: placement?.position,
              department: placement?.department,
            ));
          }
          onProgress(end / total);
        }
        return SocXlsxBuilder.toRows(rows);
      },
    );
  }

  Map<String, _RolePlacement> _buildRoleLookup(List<Department> departments) {
    final map = <String, _RolePlacement>{};
    for (final department in departments) {
      for (final position in department.positions) {
        for (final role in position.roles) {
          map[role.id] = _RolePlacement(
            position: position,
            department: department,
          );
        }
      }
    }
    return map;
  }

  Future<void> _runExport({
    required String label,
    required String sheetName,
    required String baseFileName,
    required List<String> headers,
    required Future<List<List<String>>> Function(
      CompanyDetail companyDetail,
      List<Employee> employees,
      void Function(double progress) onProgress,
    ) build,
  }) async {
    _isExporting = true;
    _exportProgress = 0.0;
    _exportErrorMessage = null;
    _exportSuccessLabel = null;
    notifyListeners();

    try {
      if (_companyId == null) {
        final companyResult = await _companyRepository.getSelectedCompany();
        _companyId = companyResult.valueOrNull?.id;
      }
      if (_companyId == null) {
        _exportErrorMessage = 'Nenhuma empresa selecionada.';
        return;
      }

      final companyDetailResult =
          await _companyRepository.getCompanyDetail(_companyId!);
      final companyDetail = companyDetailResult.valueOrNull;
      if (companyDetail == null) {
        _exportErrorMessage = 'Não foi possível carregar a empresa.';
        return;
      }

      final employeesResult = await _fetchAllFilteredEmployees();
      final employees = employeesResult.valueOrNull;
      if (employees == null) {
        _exportErrorMessage = 'Falha ao carregar funcionários para exportação.';
        return;
      }
      if (employees.isEmpty) {
        _exportErrorMessage =
            'Nenhum funcionário encontrado para os filtros atuais.';
        return;
      }

      final rows = await build(companyDetail, employees, (progress) {
        _exportProgress = progress.clamp(0.0, 1.0);
        notifyListeners();
      });

      final bytes = _spreadsheetService.buildXlsx(
        sheetName: sheetName,
        headers: headers,
        rows: rows,
      );

      final fileName =
          '${baseFileName}_${_sanitizeFileName(companyDetail.displayName)}_${_todayStamp()}';

      final saveResult =
          await _fileSaveService.saveXlsx(fileName: fileName, bytes: bytes);
      saveResult.fold(
        onSuccess: (outcome) {
          switch (outcome) {
            case FileSaveOutcome.saved:
              _exportSuccessLabel = '$label baixada.';
            case FileSaveOutcome.cancelled:
              // User dismissed the save dialog — silent no-op.
              break;
          }
        },
        onError: (_, __) => _exportErrorMessage = 'Falha ao salvar o arquivo.',
      );
    } catch (_) {
      _exportErrorMessage = 'Erro inesperado durante a exportação.';
    } finally {
      _isExporting = false;
      _exportProgress = 0.0;
      notifyListeners();
    }
  }

  Future<EmployeeIdCard?> _fetchIdCard(Employee employee) async {
    final result =
        await _employeeRepository.getEmployeeIdCard(_companyId!, employee.id);
    return result.valueOrNull;
  }

  Future<_SocEmployeeDetails> _fetchSocDetails(Employee employee) async {
    final results = await Future.wait([
      _employeeRepository.getEmployeeIdCard(_companyId!, employee.id),
      _employeeRepository.getEmployeePersonalInfo(_companyId!, employee.id),
      _employeeRepository.getContracts(_companyId!, employee.id),
      _employeeRepository.getEmployeeSocialIntegrationProgram(
        _companyId!,
        employee.id,
      ),
      _employeeRepository.getEmployeeProfile(_companyId!, employee.id),
    ]);
    return _SocEmployeeDetails(
      idCard: results[0].valueOrNull as EmployeeIdCard?,
      personalInfo: results[1].valueOrNull as EmployeePersonalInfo?,
      contracts: results[2].valueOrNull as List<EmployeeContractInfo>?,
      pis: results[3].valueOrNull as EmployeeSocialIntegrationProgram?,
      profile: results[4].valueOrNull as EmployeeProfile?,
    );
  }

  SocRow _socRowFor({
    required Employee employee,
    required _SocEmployeeDetails details,
    required CompanyDetail companyDetail,
    required PersonalInfoOptions? options,
    Position? position,
    Department? department,
  }) {
    final contracts = details.contracts ?? const [];
    final lastContract = contracts.isEmpty ? null : contracts.last;
    final dismissalDate = (lastContract != null && lastContract.isTerminated)
        ? lastContract.finalDate
        : '';

    final genderLabel = options != null && details.personalInfo != null
        ? options.genderLabel(details.personalInfo!.genderId)
        : '';
    final maritalLabel = options != null && details.personalInfo != null
        ? options.maritalStatusLabel(details.personalInfo!.maritalStatusId)
        : '';

    final positionName = position?.name ?? employee.roleName;
    final positionDescription = position?.description ?? '';
    final positionCbo = position?.cbo ?? '';

    return SocRow(
      unitName: companyDetail.corporateName,
      sectorName: department?.name ?? '',
      positionName: positionName,
      employeeName: employee.name,
      dateOfBirth: details.idCard?.dateOfBirth ?? '',
      gender: _genderCode(genderLabel),
      status: _statusCode(employee.status),
      admissionDate: lastContract?.initDate ?? '',
      dismissalDate: dismissalDate,
      maritalStatus: _maritalCode(maritalLabel),
      pisPasep: _digitsOnly(details.pis?.number ?? ''),
      cpf: _digitsOnly(details.idCard?.cpf ?? ''),
      cbo: positionCbo,
      gfip: '0',
      unitAddress: _unitAddress(companyDetail),
      unitNeighborhood: companyDetail.neighborhood,
      unitCity: companyDetail.city,
      unitState: companyDetail.state,
      unitZipCode: _digitsOnly(companyDetail.zipCode),
      unitCnpj: _digitsOnly(companyDetail.cnpj),
      unitPhone: '(11) 968608425',
      unitContact: 'Igor de Brito Coura',
      unitCorporateName: companyDetail.corporateName,
      cnae: '43215',
      role: positionName,
      positionDescription: positionDescription,
      unitAddressNumber: '0',
      unitAddressComplement: _unitAddressComplement(companyDetail),
      riskLevel: '3',
      descriptionOrigin: 'CA',
      eSocialCategoryCode: '101',
      hrRegistration: employee.registration,
    );
  }

  String _digitsOnly(String value) => value.replaceAll(RegExp(r'\D'), '');

  String _genderCode(String label) {
    final normalized = label.trim().toLowerCase();
    if (normalized.startsWith('hom')) return 'M';
    if (normalized.startsWith('mul')) return 'F';
    return '';
  }

  String _statusCode(EmployeeStatus status) {
    switch (status) {
      case EmployeeStatus.active:
        return 'S';
      case EmployeeStatus.inactive:
        return 'N';
      case EmployeeStatus.pending:
        return 'P';
      case EmployeeStatus.away:
        return 'A';
      case EmployeeStatus.vacation:
        return 'F';
      case EmployeeStatus.none:
        return '';
    }
  }

  String _maritalCode(String label) {
    final normalized = label.trim().toLowerCase();
    if (normalized.contains('solteir')) return '1';
    if (normalized.contains('casad')) return '2';
    if (normalized.contains('separad')) return '3';
    if (normalized.contains('divorciad')) return '4';
    if (normalized.contains('viúv') || normalized.contains('viuv')) return '5';
    if (normalized.isEmpty) return '';
    return '6';
  }

  String _unitAddress(CompanyDetail company) => [
        if (company.street.isNotEmpty) company.street,
        if (company.number.isNotEmpty) company.number,
        if (company.complement.isNotEmpty) company.complement,
      ].join(', ');

  String _unitAddressComplement(CompanyDetail company) => [
        if (company.street.isNotEmpty) company.street,
        if (company.number.isNotEmpty) company.number,
        if (company.neighborhood.isNotEmpty) company.neighborhood,
        if (company.city.isNotEmpty) company.city,
        if (company.state.isNotEmpty) company.state,
      ].join(', ');

  String _sanitizeFileName(String value) {
    final cleaned = value
        .replaceAll(RegExp(r'[^\w\d\-]+', unicode: true), '_')
        .replaceAll(RegExp(r'_+'), '_')
        .replaceAll(RegExp(r'^_|_$'), '');
    return cleaned.isEmpty ? 'empresa' : cleaned;
  }

  String _todayStamp() {
    final now = DateTime.now();
    String two(int n) => n.toString().padLeft(2, '0');
    return '${now.year}${two(now.month)}${two(now.day)}';
  }

  // ─── Internals ────────────────────────────────────────────────────────────

  Future<void> _fetchPage() async {
    try {
      if (_companyId == null) {
        final companyResult = await _companyRepository.getSelectedCompany();
        _companyId = companyResult.valueOrNull?.id;
      }

      if (_companyId == null) {
        _status = EmployeeListStatus.error;
        _errorMessage = 'Nenhuma empresa selecionada.';
        return;
      }

      final result = await _employeeRepository.getEmployees(
        _companyId!,
        name: _nameFilter,
        role: _roleFilter,
        status: _statusFilter,
        documentStatus: _documentStatusFilter,
        ascending: _ascending,
        pageSize: pageSize,
        sizeSkip: _sizeSkip,
      );

      result.fold(
        onSuccess: (data) {
          _employees.addAll(data);
          _sizeSkip += data.length;
          _hasMore = data.length >= pageSize;
          _status = EmployeeListStatus.idle;
          for (final employee in data) {
            _loadImageFor(employee.id);
          }
        },
        onError: (_, __) {
          _status = EmployeeListStatus.error;
          _errorMessage = 'Falha ao carregar funcionários.';
        },
      );
    } finally {
      notifyListeners();
    }
  }

  /// Pages through every employee that matches the current filters.
  ///
  /// Aborts on the first failed page and propagates the underlying error.
  Future<_FetchAllResult> _fetchAllFilteredEmployees() async {
    final all = <Employee>[];
    var skip = 0;
    while (true) {
      final result = await _employeeRepository.getEmployees(
        _companyId!,
        name: _nameFilter,
        role: _roleFilter,
        status: _statusFilter,
        documentStatus: _documentStatusFilter,
        ascending: _ascending,
        pageSize: exportPageSize,
        sizeSkip: skip,
      );
      final page = result.valueOrNull;
      if (page == null) {
        return _FetchAllResult.error(result.errorOrNull ?? 'unknown');
      }
      all.addAll(page);
      if (page.length < exportPageSize) {
        return _FetchAllResult.success(all);
      }
      skip += page.length;
    }
  }

  int? get _statusFilter =>
      _selectedStatus == EmployeeStatus.none ? null : _selectedStatus.id;

  int? get _documentStatusFilter =>
      _selectedDocumentStatus == DocumentStatus.all
          ? null
          : _selectedDocumentStatus.id;

  String? get _nameFilter =>
      _searchQuery.isEmpty || _searchParam != SearchParam.name
          ? null
          : _searchQuery;

  String? get _roleFilter =>
      _searchQuery.isEmpty || _searchParam != SearchParam.role
          ? null
          : _searchQuery;

  /// Loads the profile image for [employeeId] in the background.
  ///
  /// Caches the result in [_imageCache] and notifies listeners when the image
  /// arrives. Silently ignores errors and skips already-cached ids.
  Future<void> _loadImageFor(String employeeId) async {
    if (_companyId == null || _imageCache.containsKey(employeeId)) return;
    final result =
        await _employeeRepository.getEmployeeImage(_companyId!, employeeId);
    result.fold(
      onSuccess: (bytes) {
        if (bytes != null) {
          _imageCache[employeeId] = bytes;
          notifyListeners();
        }
      },
      onError: (_, __) {}, // silently ignore image errors
    );
  }
}

class _SocEmployeeDetails {
  const _SocEmployeeDetails({
    required this.idCard,
    required this.personalInfo,
    required this.contracts,
    required this.pis,
    required this.profile,
  });

  final EmployeeIdCard? idCard;
  final EmployeePersonalInfo? personalInfo;
  final List<EmployeeContractInfo>? contracts;
  final EmployeeSocialIntegrationProgram? pis;
  final EmployeeProfile? profile;
}

class _RolePlacement {
  const _RolePlacement({required this.position, required this.department});

  final Position position;
  final Department department;
}

class _FetchAllResult {
  const _FetchAllResult.success(List<Employee> value)
      : valueOrNull = value,
        errorOrNull = null;
  const _FetchAllResult.error(Object error)
      : valueOrNull = null,
        errorOrNull = error;

  final List<Employee>? valueOrNull;
  final Object? errorOrNull;
}
