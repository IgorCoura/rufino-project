import 'dart:collection';
import 'dart:typed_data';

import 'package:flutter/foundation.dart';

import '../../../../domain/entities/batch_download.dart';
import '../../../../domain/entities/document_group_with_templates.dart';
import '../../../../domain/entities/workplace.dart';
import '../../../../domain/repositories/batch_download_repository.dart';
import '../../../../domain/repositories/document_group_repository.dart';
import '../../../../domain/repositories/workplace_repository.dart';

/// The current step in the batch download wizard.
enum BatchDownloadStep {
  /// Step 1: Select employees.
  selectEmployees,

  /// Step 2: Select document units.
  selectUnits,

  /// Step 3: Review selection and download.
  review,
}

/// Possible states for the batch download screen.
enum BatchDownloadStatus {
  /// No operation in progress.
  idle,

  /// Loading data from the API.
  loading,

  /// Data loaded successfully.
  loaded,

  /// An error occurred.
  error,

  /// Downloading the ZIP file.
  downloading,

  /// Download completed successfully.
  downloadComplete,
}

/// Manages state and business logic for the batch download screen.
///
/// Implements a 3-step wizard: select employees, select document units,
/// review and download. Each step has its own filters and pagination.
class BatchDownloadViewModel extends ChangeNotifier {
  /// Creates a [BatchDownloadViewModel].
  BatchDownloadViewModel({
    required BatchDownloadRepository batchDownloadRepository,
    required DocumentGroupRepository documentGroupRepository,
    required WorkplaceRepository workplaceRepository,
    required String companyId,
  })  : _batchDownloadRepository = batchDownloadRepository,
        _documentGroupRepository = documentGroupRepository,
        _workplaceRepository = workplaceRepository,
        _companyId = companyId;

  final BatchDownloadRepository _batchDownloadRepository;
  final DocumentGroupRepository _documentGroupRepository;
  final WorkplaceRepository _workplaceRepository;
  final String _companyId;

  // ---------------------------------------------------------------------------
  // Wizard state
  // ---------------------------------------------------------------------------

  BatchDownloadStep _currentStep = BatchDownloadStep.selectEmployees;

  /// The current step in the wizard.
  BatchDownloadStep get currentStep => _currentStep;

  BatchDownloadStatus _status = BatchDownloadStatus.idle;

  /// The current operation status.
  BatchDownloadStatus get status => _status;

  String? _errorMessage;

  /// Error message to display, if any.
  String? get errorMessage => _errorMessage;

  // ---------------------------------------------------------------------------
  // Step 1: Employee selection
  // ---------------------------------------------------------------------------

  List<BatchDownloadEmployee> _employees = [];

  /// The employees on the current page.
  UnmodifiableListView<BatchDownloadEmployee> get employees =>
      UnmodifiableListView(_employees);

  int _employeesTotalCount = 0;

  /// Total matching employees across all pages.
  int get employeesTotalCount => _employeesTotalCount;

  final Set<String> _selectedEmployeeIds = {};

  /// The IDs of the currently selected employees.
  UnmodifiableSetView<String> get selectedEmployeeIds =>
      UnmodifiableSetView(_selectedEmployeeIds);

  /// Whether any employees are selected.
  bool get hasSelectedEmployees => _selectedEmployeeIds.isNotEmpty;

  /// The number of selected employees.
  int get selectedEmployeeCount => _selectedEmployeeIds.length;

  int _employeePageNumber = 1;

  /// The current employee page number.
  int get employeePageNumber => _employeePageNumber;

  static const int _pageSize = 50;

  /// The total number of employee pages.
  int get employeeTotalPages =>
      (_employeesTotalCount / _pageSize).ceil().clamp(1, 999);

  // Employee filters
  String? _employeeNameFilter;
  int? _employeeStatusFilter;
  String? _employeeWorkplaceFilter;
  String? _employeeRoleFilter;

  // Dropdown data
  List<Workplace> _workplaces = [];

  /// Available workplaces for the filter dropdown.
  UnmodifiableListView<Workplace> get workplaces =>
      UnmodifiableListView(_workplaces);

  // ---------------------------------------------------------------------------
  // Step 2: Document unit selection
  // ---------------------------------------------------------------------------

  List<BatchDownloadUnit> _units = [];

  /// The document units on the current page.
  UnmodifiableListView<BatchDownloadUnit> get units =>
      UnmodifiableListView(_units);

  int _unitsTotalCount = 0;

  /// Total matching units across all pages.
  int get unitsTotalCount => _unitsTotalCount;

  final Set<String> _selectedUnitKeys = {};

  /// The composite keys (`documentId:documentUnitId`) of selected units.
  UnmodifiableSetView<String> get selectedUnitKeys =>
      UnmodifiableSetView(_selectedUnitKeys);

  /// Whether any units are selected.
  bool get hasSelectedUnits => _selectedUnitKeys.isNotEmpty;

  /// The number of selected units.
  int get selectedUnitCount => _selectedUnitKeys.length;

  int _unitPageNumber = 1;

  /// The current unit page number.
  int get unitPageNumber => _unitPageNumber;

  /// The total number of unit pages.
  int get unitTotalPages =>
      (_unitsTotalCount / _pageSize).ceil().clamp(1, 999);

  // Unit filters
  String? _unitGroupFilter;
  String? _unitTemplateFilter;
  int? _unitStatusFilter;
  String? _dateFromFilter;
  String? _dateToFilter;
  int? _periodTypeFilter;
  int? _periodYearFilter;
  int? _periodMonthFilter;
  int? _periodDayFilter;
  int? _periodWeekFilter;

  // Dropdown data
  List<DocumentGroupWithTemplates> _groups = [];

  /// Available document groups with templates for the filter dropdowns.
  UnmodifiableListView<DocumentGroupWithTemplates> get groups =>
      UnmodifiableListView(_groups);

  /// The current period type filter (used by UI to conditionally show fields).
  int? get periodTypeFilter => _periodTypeFilter;

  /// Returns the templates for the currently selected group, or all templates.
  List<DocumentTemplateSummary> get availableTemplates {
    if (_unitGroupFilter != null) {
      final group = _groups.where((g) => g.id == _unitGroupFilter).firstOrNull;
      return group?.templates ?? [];
    }
    return _groups.expand((g) => g.templates).toList();
  }

  // ---------------------------------------------------------------------------
  // Step 3: Review
  // ---------------------------------------------------------------------------

  /// Returns the selected units grouped by employee name for the review step.
  Map<String, List<BatchDownloadUnit>> get selectedUnitsByEmployee {
    final allUnits = _allLoadedUnits.where(
        (u) => _selectedUnitKeys.contains(u.selectionKey));
    final grouped = <String, List<BatchDownloadUnit>>{};
    for (final unit in allUnits) {
      grouped.putIfAbsent(unit.employeeName, () => []).add(unit);
    }
    return grouped;
  }

  /// Number of distinct employees in the selection.
  int get selectedEmployeeNamesCount => selectedUnitsByEmployee.keys.length;

  // We cache all loaded units so the review step can display them
  final List<BatchDownloadUnit> _allLoadedUnits = [];

  // ---------------------------------------------------------------------------
  // Data loading
  // ---------------------------------------------------------------------------

  /// Loads initial data for the wizard (employees + workplaces).
  Future<void> initialize() async {
    _status = BatchDownloadStatus.loading;
    notifyListeners();
    try {
      await Future.wait([
        loadEmployees(),
        _loadWorkplaces(),
      ]);
      _status = BatchDownloadStatus.loaded;
    } catch (e) {
      _status = BatchDownloadStatus.error;
      _errorMessage = 'Erro ao carregar dados iniciais';
    } finally {
      notifyListeners();
    }
  }

  /// Loads the employee list applying current filters and pagination.
  Future<void> loadEmployees() async {
    _status = BatchDownloadStatus.loading;
    notifyListeners();
    try {
      final result = await _batchDownloadRepository.getEmployeesForDownload(
        _companyId,
        name: _employeeNameFilter,
        statusId: _employeeStatusFilter,
        workplaceId: _employeeWorkplaceFilter,
        roleId: _employeeRoleFilter,
        pageSize: _pageSize,
        pageNumber: _employeePageNumber,
      );
      result.fold(
        onSuccess: (page) {
          _employees = page.items;
          _employeesTotalCount = page.totalCount;
          _status = BatchDownloadStatus.loaded;
          _errorMessage = null;
        },
        onError: (e) {
          _status = BatchDownloadStatus.error;
          _errorMessage = e.toString();
        },
      );
    } finally {
      notifyListeners();
    }
  }

  Future<void> _loadWorkplaces() async {
    final result = await _workplaceRepository.getWorkplaces(_companyId);
    result.fold(
      onSuccess: (data) => _workplaces = data,
      onError: (_) => _workplaces = [],
    );
  }

  /// Loads document groups with templates for the filter dropdowns.
  Future<void> _loadGroupsAndTemplates() async {
    final result =
        await _documentGroupRepository.getDocumentGroupsWithTemplates(
            _companyId);
    result.fold(
      onSuccess: (data) => _groups = data,
      onError: (_) => _groups = [],
    );
  }

  /// Loads the document unit list applying current filters and pagination.
  Future<void> loadDocumentUnits() async {
    _status = BatchDownloadStatus.loading;
    notifyListeners();
    try {
      final result =
          await _batchDownloadRepository.getDocumentUnitsForDownload(
        _companyId,
        employeeIds: _selectedEmployeeIds.toList(),
        documentGroupId: _unitGroupFilter,
        documentTemplateId: _unitTemplateFilter,
        unitStatusId: _unitStatusFilter,
        dateFrom: _dateFromFilter,
        dateTo: _dateToFilter,
        periodTypeId: _periodTypeFilter,
        periodYear: _periodYearFilter,
        periodMonth: _periodMonthFilter,
        periodDay: _periodDayFilter,
        periodWeek: _periodWeekFilter,
        pageSize: _pageSize,
        pageNumber: _unitPageNumber,
      );
      result.fold(
        onSuccess: (page) {
          _units = page.items;
          _unitsTotalCount = page.totalCount;
          _status = BatchDownloadStatus.loaded;
          _errorMessage = null;
          // Cache loaded units for review
          for (final unit in page.items) {
            if (!_allLoadedUnits.any(
                (u) => u.selectionKey == unit.selectionKey)) {
              _allLoadedUnits.add(unit);
            }
          }
        },
        onError: (e) {
          _status = BatchDownloadStatus.error;
          _errorMessage = e.toString();
        },
      );
    } finally {
      notifyListeners();
    }
  }

  // ---------------------------------------------------------------------------
  // Employee selection
  // ---------------------------------------------------------------------------

  /// Toggles selection of an employee by [id].
  void toggleEmployeeSelection(String id) {
    if (_selectedEmployeeIds.contains(id)) {
      _selectedEmployeeIds.remove(id);
    } else {
      _selectedEmployeeIds.add(id);
    }
    notifyListeners();
  }

  /// Selects all employees on the current page.
  void selectAllEmployeesOnPage() {
    _selectedEmployeeIds.addAll(_employees.map((e) => e.id));
    notifyListeners();
  }

  /// Clears all employee selections.
  void clearEmployeeSelection() {
    _selectedEmployeeIds.clear();
    notifyListeners();
  }

  // ---------------------------------------------------------------------------
  // Unit selection
  // ---------------------------------------------------------------------------

  /// Toggles selection of a document unit.
  void toggleUnitSelection(String documentId, String documentUnitId) {
    final key = '$documentId:$documentUnitId';
    if (_selectedUnitKeys.contains(key)) {
      _selectedUnitKeys.remove(key);
    } else {
      _selectedUnitKeys.add(key);
    }
    notifyListeners();
  }

  /// Selects all downloadable units on the current page.
  void selectAllUnitsOnPage() {
    for (final unit in _units) {
      if (unit.hasFile) {
        _selectedUnitKeys.add(unit.selectionKey);
      }
    }
    notifyListeners();
  }

  /// Clears all unit selections.
  void clearUnitSelection() {
    _selectedUnitKeys.clear();
    notifyListeners();
  }

  // ---------------------------------------------------------------------------
  // Employee filters
  // ---------------------------------------------------------------------------

  /// Sets the employee name filter.
  void setEmployeeNameFilter(String? name) {
    _employeeNameFilter = (name != null && name.isEmpty) ? null : name;
  }

  /// Sets the employee status filter.
  void setEmployeeStatusFilter(int? statusId) {
    _employeeStatusFilter = statusId;
  }

  /// Sets the employee workplace filter.
  void setEmployeeWorkplaceFilter(String? workplaceId) {
    _employeeWorkplaceFilter = workplaceId;
  }

  /// Sets the employee role filter.
  void setEmployeeRoleFilter(String? roleId) {
    _employeeRoleFilter = roleId;
  }

  /// Applies the employee filters and reloads from page 1.
  Future<void> applyEmployeeFilters() async {
    _employeePageNumber = 1;
    await loadEmployees();
  }

  /// Clears all employee filters and reloads.
  Future<void> clearEmployeeFilters() async {
    _employeeNameFilter = null;
    _employeeStatusFilter = null;
    _employeeWorkplaceFilter = null;
    _employeeRoleFilter = null;
    _employeePageNumber = 1;
    await loadEmployees();
  }

  /// Navigates to the specified employee page.
  Future<void> setEmployeePage(int page) async {
    _employeePageNumber = page;
    await loadEmployees();
  }

  // ---------------------------------------------------------------------------
  // Unit filters
  // ---------------------------------------------------------------------------

  /// Sets the document group filter.
  void setUnitGroupFilter(String? groupId) {
    _unitGroupFilter = groupId;
    _unitTemplateFilter = null;
  }

  /// Sets the document template filter.
  void setUnitTemplateFilter(String? templateId) {
    _unitTemplateFilter = templateId;
  }

  /// Sets the unit status filter.
  void setUnitStatusFilter(int? statusId) {
    _unitStatusFilter = statusId;
  }

  /// Sets the date range filter (from).
  void setDateFromFilter(String? date) {
    _dateFromFilter = (date != null && date.isEmpty) ? null : date;
  }

  /// Sets the date range filter (to).
  void setDateToFilter(String? date) {
    _dateToFilter = (date != null && date.isEmpty) ? null : date;
  }

  /// Sets the period filter fields without reloading.
  ///
  /// Follows the same pattern as [BatchDocumentViewModel.setPeriodFilter]:
  /// first select period type, then fill year/month/day/week.
  void setPeriodFilter({
    int? typeId,
    int? year,
    int? month,
    int? day,
    int? week,
  }) {
    _periodTypeFilter = typeId;
    _periodYearFilter = year;
    _periodMonthFilter = month;
    _periodDayFilter = day;
    _periodWeekFilter = week;
    notifyListeners();
  }

  /// Applies the unit filters and reloads from page 1.
  Future<void> applyUnitFilters() async {
    _unitPageNumber = 1;
    await loadDocumentUnits();
  }

  /// Clears all unit filters and reloads.
  Future<void> clearUnitFilters() async {
    _unitGroupFilter = null;
    _unitTemplateFilter = null;
    _unitStatusFilter = null;
    _dateFromFilter = null;
    _dateToFilter = null;
    _periodTypeFilter = null;
    _periodYearFilter = null;
    _periodMonthFilter = null;
    _periodDayFilter = null;
    _periodWeekFilter = null;
    _unitPageNumber = 1;
    await loadDocumentUnits();
  }

  /// Navigates to the specified unit page.
  Future<void> setUnitPage(int page) async {
    _unitPageNumber = page;
    await loadDocumentUnits();
  }

  // ---------------------------------------------------------------------------
  // Step navigation
  // ---------------------------------------------------------------------------

  /// Advances from step 1 to step 2, loading document units and dropdowns.
  Future<void> proceedToUnitSelection() async {
    if (!hasSelectedEmployees) return;
    _currentStep = BatchDownloadStep.selectUnits;
    _unitPageNumber = 1;
    _allLoadedUnits.clear();
    _selectedUnitKeys.clear();
    notifyListeners();
    await Future.wait([
      loadDocumentUnits(),
      _loadGroupsAndTemplates(),
    ]);
  }

  /// Advances from step 2 to step 3 (review).
  void proceedToReview() {
    if (!hasSelectedUnits) return;
    _currentStep = BatchDownloadStep.review;
    _status = BatchDownloadStatus.loaded;
    notifyListeners();
  }

  /// Goes back one step in the wizard.
  void goBack() {
    switch (_currentStep) {
      case BatchDownloadStep.selectUnits:
        _currentStep = BatchDownloadStep.selectEmployees;
      case BatchDownloadStep.review:
        _currentStep = BatchDownloadStep.selectUnits;
      case BatchDownloadStep.selectEmployees:
        break;
    }
    _status = BatchDownloadStatus.loaded;
    notifyListeners();
  }

  // ---------------------------------------------------------------------------
  // Download
  // ---------------------------------------------------------------------------

  /// Downloads the selected document units as a ZIP file.
  ///
  /// Returns the raw ZIP bytes on success, or null on error.
  Future<Uint8List?> downloadSelected() async {
    if (!hasSelectedUnits) return null;

    _status = BatchDownloadStatus.downloading;
    _errorMessage = null;
    notifyListeners();

    try {
      final items = _selectedUnitKeys.map((key) {
        final parts = key.split(':');
        return BatchDownloadItem(
          documentId: parts[0],
          documentUnitId: parts[1],
        );
      }).toList();

      final result = await _batchDownloadRepository.downloadBatch(
        _companyId,
        items,
      );

      return result.fold(
        onSuccess: (bytes) {
          _status = BatchDownloadStatus.downloadComplete;
          notifyListeners();
          return bytes;
        },
        onError: (e) {
          _status = BatchDownloadStatus.error;
          _errorMessage = e.toString();
          notifyListeners();
          return null;
        },
      );
    } catch (e) {
      _status = BatchDownloadStatus.error;
      _errorMessage = 'Erro ao baixar documentos';
      notifyListeners();
      return null;
    }
  }
}
