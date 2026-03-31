import 'dart:collection';

import 'package:flutter/foundation.dart';

import '../../../../core/utils/error_messages.dart';
import '../../../../domain/entities/batch_document_unit.dart';
import '../../../../domain/entities/document_group_with_templates.dart';
import '../../../../domain/repositories/batch_document_repository.dart';
import '../../../../domain/repositories/document_group_repository.dart';

/// Possible states for the batch document screen.
enum BatchDocumentStatus {
  idle,
  loading,
  loaded,
  error,
  uploading,
  uploadComplete,
}

/// Manages state and business logic for the batch document management screen.
///
/// Coordinates between [BatchDocumentRepository] for batch operations and
/// [DocumentTemplateRepository] for populating the template dropdown.
/// Implements a staging pattern where the user selects files locally before
/// sending all of them in a single multipart request.
class BatchDocumentViewModel extends ChangeNotifier {
  BatchDocumentViewModel({
    required BatchDocumentRepository batchDocumentRepository,
    required DocumentGroupRepository documentGroupRepository,
    required String companyId,
  })  : _batchDocumentRepository = batchDocumentRepository,
        _documentGroupRepository = documentGroupRepository,
        _companyId = companyId;

  final BatchDocumentRepository _batchDocumentRepository;
  final DocumentGroupRepository _documentGroupRepository;
  final String _companyId;

  /// Sentinel value for the "Todos" option in the template dropdown.
  static const allTemplatesId = '__all__';

  // ─── State ───────────────────────────────────────────────

  BatchDocumentStatus _status = BatchDocumentStatus.idle;
  String? _errorMessage;

  List<DocumentGroupWithTemplates> _groupsWithTemplates = [];
  String? _selectedGroupId;

  List<DocumentTemplateSummary> _groupTemplates = [];
  String? _selectedTemplateId;

  List<BatchDocumentUnitItem> _pendingUnits = [];
  int _totalCount = 0;
  int _pageNumber = 1;
  int _pageSize = 50;

  List<EmployeeMissingDocument> _missingEmployees = [];

  int? _employeeStatusFilter;
  String? _employeeNameFilter;
  int? _periodTypeFilter;
  int? _periodYearFilter;
  int? _periodMonthFilter;
  int? _periodDayFilter;
  int? _periodWeekFilter;

  final Set<String> _selectedUnitIds = {};
  final Map<String, BatchUploadItem> _stagedFiles = {};

  String? _globalSignDeadline;
  int _globalReminderDays = 7;

  List<BatchUploadResult> _uploadResults = [];

  // ─── Getters ─────────────────────────────────────────────

  /// The current screen status.
  BatchDocumentStatus get status => _status;

  /// Error message when [status] is [BatchDocumentStatus.error].
  String? get errorMessage => _errorMessage;

  /// Available document groups for the group dropdown.
  UnmodifiableListView<DocumentGroupWithTemplates> get groups =>
      UnmodifiableListView(_groupsWithTemplates);

  /// The currently selected group identifier.
  String? get selectedGroupId => _selectedGroupId;

  /// Document templates for the selected group.
  UnmodifiableListView<DocumentTemplateSummary> get templates =>
      UnmodifiableListView(_groupTemplates);

  /// The currently selected template identifier (or [allTemplatesId]).
  String? get selectedTemplateId => _selectedTemplateId;

  /// Whether the "Todos" option is selected in the template dropdown.
  bool get isAllTemplatesSelected => _selectedTemplateId == allTemplatesId;

  /// The template ids to query — all in group when "Todos", single otherwise.
  List<String> get _activeTemplateIds {
    if (_selectedTemplateId == allTemplatesId) {
      return _groupTemplates.map((t) => t.id).toList();
    }
    if (_selectedTemplateId != null) return [_selectedTemplateId!];
    return [];
  }

  /// Whether the current document template supports PDF generation.
  bool get canGenerateDocument =>
      _pendingUnits.isNotEmpty && _pendingUnits.first.canGenerateDocument;

  /// Whether the current document template supports digital signature.
  bool get isSignable =>
      _pendingUnits.isNotEmpty && _pendingUnits.first.isSignable;

  /// Pending document units on the current page.
  UnmodifiableListView<BatchDocumentUnitItem> get pendingUnits =>
      UnmodifiableListView(_pendingUnits);

  /// Total count of matching pending units across all pages.
  int get totalCount => _totalCount;

  /// The current page number (1-based).
  int get pageNumber => _pageNumber;

  /// The current page size.
  int get pageSize => _pageSize;

  /// Employees without a pending document for the selected template.
  UnmodifiableListView<EmployeeMissingDocument> get missingEmployees =>
      UnmodifiableListView(_missingEmployees);

  /// Currently selected unit identifiers for batch operations.
  UnmodifiableSetView<String> get selectedUnitIds =>
      UnmodifiableSetView(_selectedUnitIds);

  /// The number of files currently staged for upload.
  int get stagedFileCount => _stagedFiles.length;

  /// The global signature deadline in ISO 8601 format.
  String? get globalSignDeadline => _globalSignDeadline;

  /// The global reminder interval in days.
  int get globalReminderDays => _globalReminderDays;

  /// Results from the most recent batch upload.
  UnmodifiableListView<BatchUploadResult> get uploadResults =>
      UnmodifiableListView(_uploadResults);

  /// Whether the given [unitId] has a file staged for upload.
  bool hasStaged(String unitId) => _stagedFiles.containsKey(unitId);

  /// Returns the staged file name for [unitId], or null if not staged.
  String? stagedFileName(String unitId) => _stagedFiles[unitId]?.fileName;

  // ─── Filter getters ──────────────────────────────────────

  /// The current employee status filter, or null for all.
  int? get employeeStatusFilter => _employeeStatusFilter;

  /// The current employee name search filter.
  String? get employeeNameFilter => _employeeNameFilter;

  /// The current period type filter.
  int? get periodTypeFilter => _periodTypeFilter;

  /// The current period year filter.
  int? get periodYearFilter => _periodYearFilter;

  /// The current period month filter.
  int? get periodMonthFilter => _periodMonthFilter;

  /// The current period day filter (daily only).
  int? get periodDayFilter => _periodDayFilter;

  /// The current period week filter (weekly only).
  int? get periodWeekFilter => _periodWeekFilter;

  /// Extracts a user-facing error message from [error].
  ///
  /// Delegates to [extractServerMessages] to unwrap domain errors from
  /// [HttpException]. Falls back to [fallback] when no server messages
  /// are available.
  String _errorFrom(Object error, String fallback) {
    final messages = extractServerMessages(error);
    return messages.isNotEmpty ? messages.join('\n') : fallback;
  }

  // ─── Group & template loading ─────────────────────────────

  /// Loads document groups with their templates for the dropdowns.
  Future<void> loadGroupsAndTemplates() async {
    _status = BatchDocumentStatus.loading;
    notifyListeners();
    try {
      final result = await _documentGroupRepository
          .getDocumentGroupsWithTemplates(_companyId);
      result.fold(
        onSuccess: (groups) {
          _groupsWithTemplates = groups;
          _status = BatchDocumentStatus.loaded;
        },
        onError: (e) {
          _errorMessage = _errorFrom(e, 'Falha ao carregar grupos.');
          _status = BatchDocumentStatus.error;
        },
      );
    } finally {
      notifyListeners();
    }
  }

  /// Selects a document group and populates the template dropdown.
  void selectGroup(String groupId) {
    _selectedGroupId = groupId;
    _selectedTemplateId = null;
    final group = _groupsWithTemplates.where((g) => g.id == groupId).firstOrNull;
    _groupTemplates = group?.templates ?? [];
    _pendingUnits = [];
    _totalCount = 0;
    _selectedUnitIds.clear();
    _stagedFiles.clear();
    _uploadResults = [];
    notifyListeners();
  }

  /// Selects a template (or [allTemplatesId]) and loads pending units.
  Future<void> selectTemplate(String templateId) async {
    _selectedTemplateId = templateId;
    _pageNumber = 1;
    _selectedUnitIds.clear();
    _stagedFiles.clear();
    _uploadResults = [];
    await loadPendingUnits();
  }

  // ─── Pending units ───────────────────────────────────────

  /// Loads pending document units with the current filters and pagination.
  ///
  /// When "Todos" is selected, queries each template in the group and
  /// merges the results into a single list.
  Future<void> loadPendingUnits() async {
    final templateIds = _activeTemplateIds;
    if (templateIds.isEmpty) return;
    _status = BatchDocumentStatus.loading;
    notifyListeners();
    try {
      final allItems = <BatchDocumentUnitItem>[];
      var totalCount = 0;

      for (final templateId in templateIds) {
        final result = await _batchDocumentRepository.getPendingDocumentUnits(
          _companyId,
          templateId,
          employeeStatusId: _employeeStatusFilter,
          employeeName: _employeeNameFilter,
          periodTypeId: _periodTypeFilter,
          periodYear: _periodYearFilter,
          periodMonth: _periodMonthFilter,
          periodDay: _periodDayFilter,
          periodWeek: _periodWeekFilter,
          pageSize: _pageSize,
          pageNumber: _pageNumber,
        );
        result.fold(
          onSuccess: (page) {
            allItems.addAll(page.items);
            totalCount += page.totalCount;
          },
          onError: (e) {
            _errorMessage =
                _errorFrom(e, 'Falha ao carregar documentos pendentes.');
            _status = BatchDocumentStatus.error;
          },
        );
        if (_status == BatchDocumentStatus.error) break;
      }

      if (_status != BatchDocumentStatus.error) {
        _pendingUnits = allItems;
        _totalCount = totalCount;
        _status = BatchDocumentStatus.loaded;
      }
    } finally {
      notifyListeners();
    }
  }

  // ─── Missing employees ───────────────────────────────────

  /// Loads employees who do not have a pending document for the selected template(s).
  Future<void> loadMissingEmployees() async {
    final templateIds = _activeTemplateIds;
    if (templateIds.isEmpty) return;
    try {
      final allMissing = <EmployeeMissingDocument>[];
      final seenIds = <String>{};
      for (final templateId in templateIds) {
        final result = await _batchDocumentRepository.getMissingEmployees(
          _companyId,
          templateId,
          employeeStatusId: _employeeStatusFilter,
          employeeName: _employeeNameFilter,
        );
        result.fold(
          onSuccess: (employees) {
            for (final emp in employees) {
              if (seenIds.add(emp.employeeId)) allMissing.add(emp);
            }
          },
          onError: (e) => _errorMessage =
              _errorFrom(e, 'Falha ao carregar funcionários.'),
        );
      }
      _missingEmployees = allMissing;
    } finally {
      notifyListeners();
    }
  }

  // ─── Batch create ────────────────────────────────────────

  /// Creates document units in batch for the given [employeeIds].
  Future<void> batchCreateDocumentUnits(List<String> employeeIds) async {
    final templateIds = _activeTemplateIds;
    if (templateIds.isEmpty || employeeIds.isEmpty) return;
    _status = BatchDocumentStatus.loading;
    notifyListeners();
    try {
      for (final templateId in templateIds) {
        final result = await _batchDocumentRepository
            .batchCreateDocumentUnits(_companyId, templateId, employeeIds);
        result.fold(
          onSuccess: (_) => null,
          onError: (e) => _errorMessage =
              _errorFrom(e, 'Falha ao criar documentos.'),
        );
      }
    } finally {
      notifyListeners();
    }
    await loadPendingUnits();
  }

  // ─── Batch date update ───────────────────────────────────

  /// Updates the date for all selected document units.
  ///
  /// The [date] must be in `yyyy-MM-dd` API format.
  Future<void> batchUpdateDate(String date) async {
    if (_selectedUnitIds.isEmpty) return;
    final items = _pendingUnits
        .where((u) => _selectedUnitIds.contains(u.documentUnitId))
        .toList();
    if (items.isEmpty) return;
    _status = BatchDocumentStatus.loading;
    notifyListeners();
    try {
      final result = await _batchDocumentRepository.batchUpdateDate(
          _companyId, items, date);
      result.fold(
        onSuccess: (_) {
          _selectedUnitIds.clear();
        },
        onError: (e) => _errorMessage = _errorFrom(e, 'Falha ao atualizar datas.'),
      );
    } finally {
      notifyListeners();
    }
    await loadPendingUnits();
  }

  // ─── File staging ────────────────────────────────────────

  /// Stages a file for later batch upload.
  ///
  /// The file is held in memory until [uploadAllStaged] or
  /// [uploadAllStagedToSign] is called.
  void stageFile(
    String documentUnitId,
    String documentId,
    String employeeId,
    Uint8List bytes,
    String fileName,
  ) {
    _stagedFiles[documentUnitId] = BatchUploadItem(
      documentUnitId: documentUnitId,
      documentId: documentId,
      employeeId: employeeId,
      fileBytes: bytes,
      fileName: fileName,
    );
    notifyListeners();
  }

  /// Removes a staged file for the given [documentUnitId].
  void unstageFile(String documentUnitId) {
    _stagedFiles.remove(documentUnitId);
    notifyListeners();
  }

  /// Returns employee names whose staged documents have an invalid date.
  ///
  /// An empty list means all staged items have valid dates.
  List<String> validateStagedDates() {
    final invalidNames = <String>[];
    for (final staged in _stagedFiles.values) {
      final unit = _pendingUnits.where(
        (u) => u.documentUnitId == staged.documentUnitId,
      ).firstOrNull;
      if (unit != null && !unit.hasValidDate) {
        invalidNames.add(unit.employeeName);
      }
    }
    return invalidNames;
  }

  /// Uploads all staged files in a single multipart request.
  Future<void> uploadAllStaged() async {
    if (_stagedFiles.isEmpty) return;

    final invalidNames = validateStagedDates();
    if (invalidNames.isNotEmpty) {
      _errorMessage =
          'Os seguintes funcionários possuem data inválida: ${invalidNames.join(', ')}. Corrija as datas antes de enviar.';
      _status = BatchDocumentStatus.error;
      notifyListeners();
      return;
    }

    _status = BatchDocumentStatus.uploading;
    notifyListeners();
    try {
      final result = await _batchDocumentRepository.uploadDocumentRange(
          _companyId, _stagedFiles.values.toList());
      result.fold(
        onSuccess: (results) {
          _uploadResults = results;
          _stagedFiles.clear();
          _status = BatchDocumentStatus.uploadComplete;
        },
        onError: (e) {
          _errorMessage = _errorFrom(e, 'Falha ao enviar arquivos.');
          _status = BatchDocumentStatus.error;
        },
      );
    } finally {
      notifyListeners();
    }
    await loadPendingUnits();
  }

  /// Uploads all staged files and sends them for digital signature.
  ///
  /// Requires [globalSignDeadline] to be set before calling.
  Future<void> uploadAllStagedToSign() async {
    if (_stagedFiles.isEmpty || _globalSignDeadline == null) return;

    final invalidNames = validateStagedDates();
    if (invalidNames.isNotEmpty) {
      _errorMessage =
          'Os seguintes funcionários possuem data inválida: ${invalidNames.join(', ')}. Corrija as datas antes de enviar.';
      _status = BatchDocumentStatus.error;
      notifyListeners();
      return;
    }

    _status = BatchDocumentStatus.uploading;
    notifyListeners();
    try {
      final result =
          await _batchDocumentRepository.uploadDocumentRangeToSign(
        _companyId,
        _stagedFiles.values.toList(),
        _globalSignDeadline!,
        _globalReminderDays,
      );
      result.fold(
        onSuccess: (results) {
          _uploadResults = results;
          _stagedFiles.clear();
          _status = BatchDocumentStatus.uploadComplete;
        },
        onError: (e) {
          _errorMessage = _errorFrom(e, 'Falha ao enviar para assinatura.');
          _status = BatchDocumentStatus.error;
        },
      );
    } finally {
      notifyListeners();
    }
    await loadPendingUnits();
  }

  // ─── Generate PDF ────────────────────────────────────────

  /// Returns employee names from the selected units that have invalid dates.
  ///
  /// An empty list means all selected items have valid dates.
  List<String> _validateSelectedDates() {
    final invalidNames = <String>[];
    for (final unit in _pendingUnits) {
      if (_selectedUnitIds.contains(unit.documentUnitId) &&
          !unit.hasValidDate) {
        invalidNames.add(unit.employeeName);
      }
    }
    return invalidNames;
  }

  /// Generates PDFs for all selected document units.
  ///
  /// Returns the raw ZIP bytes for the caller to save.
  Future<Uint8List?> generatePdfRange() async {
    if (_selectedUnitIds.isEmpty) return null;
    final items = _pendingUnits
        .where((u) => _selectedUnitIds.contains(u.documentUnitId))
        .toList();
    if (items.isEmpty) return null;

    final invalidNames = _validateSelectedDates();
    if (invalidNames.isNotEmpty) {
      _errorMessage =
          'Os seguintes funcionários possuem data inválida: ${invalidNames.join(', ')}. Corrija as datas antes de gerar.';
      _status = BatchDocumentStatus.error;
      notifyListeners();
      return null;
    }

    _status = BatchDocumentStatus.loading;
    notifyListeners();
    Uint8List? zipBytes;
    try {
      final result =
          await _batchDocumentRepository.generatePdfRange(_companyId, items);
      result.fold(
        onSuccess: (bytes) {
          zipBytes = bytes;
          _status = BatchDocumentStatus.loaded;
        },
        onError: (e) {
          _errorMessage = _errorFrom(e, 'Falha ao gerar PDFs.');
          _status = BatchDocumentStatus.error;
        },
      );
    } finally {
      notifyListeners();
    }
    await loadPendingUnits();
    return zipBytes;
  }

  /// Generates PDFs and sends them for digital signature.
  ///
  /// Requires [globalSignDeadline] to be set before calling.
  Future<void> generateAndSignRange() async {
    if (_selectedUnitIds.isEmpty || _globalSignDeadline == null) return;
    final items = _pendingUnits
        .where((u) => _selectedUnitIds.contains(u.documentUnitId))
        .toList();
    if (items.isEmpty) return;

    final invalidNames = _validateSelectedDates();
    if (invalidNames.isNotEmpty) {
      _errorMessage =
          'Os seguintes funcionários possuem data inválida: ${invalidNames.join(', ')}. Corrija as datas antes de gerar.';
      _status = BatchDocumentStatus.error;
      notifyListeners();
      return;
    }

    _status = BatchDocumentStatus.loading;
    notifyListeners();
    try {
      final result = await _batchDocumentRepository.generateAndSignRange(
        _companyId,
        items,
        _globalSignDeadline!,
        _globalReminderDays,
      );
      result.fold(
        onSuccess: (_) {
          _selectedUnitIds.clear();
          _status = BatchDocumentStatus.loaded;
        },
        onError: (e) {
          _errorMessage =
              _errorFrom(e, 'Falha ao gerar e enviar para assinatura.');
          _status = BatchDocumentStatus.error;
        },
      );
    } finally {
      notifyListeners();
    }
    await loadPendingUnits();
  }

  // ─── Selection ───────────────────────────────────────────

  /// Toggles selection of a document unit by [unitId].
  void toggleSelection(String unitId) {
    if (_selectedUnitIds.contains(unitId)) {
      _selectedUnitIds.remove(unitId);
    } else {
      _selectedUnitIds.add(unitId);
    }
    notifyListeners();
  }

  /// Selects all pending units on the current page.
  void selectAll() {
    _selectedUnitIds.addAll(_pendingUnits.map((u) => u.documentUnitId));
    notifyListeners();
  }

  /// Clears the selection.
  void clearSelection() {
    _selectedUnitIds.clear();
    notifyListeners();
  }

  // ─── Filters ─────────────────────────────────────────────

  /// Sets the employee status filter without reloading.
  void setEmployeeStatusFilter(int? status) {
    _employeeStatusFilter = status;
    notifyListeners();
  }

  /// Sets the employee name search filter without reloading.
  void setEmployeeNameFilter(String? name) {
    _employeeNameFilter = name;
  }

  /// Sets the period filter fields without reloading.
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

  /// Applies the current filters and reloads the pending units from page 1.
  Future<void> applyFilters() async {
    _pageNumber = 1;
    await loadPendingUnits();
  }

  /// Clears all filters and reloads.
  Future<void> clearFilters() async {
    _employeeStatusFilter = null;
    _employeeNameFilter = null;
    _periodTypeFilter = null;
    _periodYearFilter = null;
    _periodMonthFilter = null;
    _periodDayFilter = null;
    _periodWeekFilter = null;
    _pageNumber = 1;
    await loadPendingUnits();
  }

  // ─── Pagination ──────────────────────────────────────────

  /// Navigates to [page] and reloads.
  Future<void> setPage(int page) async {
    _pageNumber = page;
    await loadPendingUnits();
  }

  /// Changes page size and reloads from page 1.
  Future<void> setPageSize(int size) async {
    _pageSize = size;
    _pageNumber = 1;
    await loadPendingUnits();
  }

  // ─── Signature settings ──────────────────────────────────

  /// Sets the global signature deadline in ISO 8601 format.
  void setGlobalSignDeadline(String date) {
    _globalSignDeadline = date;
    notifyListeners();
  }

  /// Sets the global reminder interval in days.
  void setGlobalReminderDays(int days) {
    _globalReminderDays = days;
    notifyListeners();
  }
}
