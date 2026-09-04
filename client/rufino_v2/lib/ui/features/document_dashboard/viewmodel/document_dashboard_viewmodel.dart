import 'dart:collection';

import 'package:flutter/foundation.dart';

import 'package:people_management/people_management.dart';

/// Possible states for the document dashboard screen.
enum DocumentDashboardStatus { idle, loading, loaded, error }

/// How the unit list is visually grouped.
enum DashboardGrouping { byDocument, byEmployee }

/// Manages state and business logic for the company-wide document dashboard.
///
/// Coordinates [DocumentDashboardRepository] for the bucket summary and the
/// urgency-ordered unit list, and [DocumentGroupRepository] for the group /
/// template filter dropdowns. Every filter change reloads both the summary
/// and the list so the KPI cards and the rows never disagree.
class DocumentDashboardViewModel extends ChangeNotifier {
  /// Creates a [DocumentDashboardViewModel].
  ///
  /// The optional [nowProvider] lets tests fix the reference date used by
  /// the urgency labels.
  DocumentDashboardViewModel({
    required DocumentDashboardRepository dashboardRepository,
    required DocumentGroupRepository documentGroupRepository,
    required String companyId,
    DateTime Function()? nowProvider,
  })  : _dashboardRepository = dashboardRepository,
        _documentGroupRepository = documentGroupRepository,
        _companyId = companyId,
        _nowProvider = nowProvider ?? DateTime.now;

  final DocumentDashboardRepository _dashboardRepository;
  final DocumentGroupRepository _documentGroupRepository;
  final String _companyId;
  final DateTime Function() _nowProvider;

  /// Selectable horizons (in days) for the "a vencer" bucket.
  static const horizonOptions = [30, 60, 90];

  /// Employee status id of active employees — the default filter.
  static const activeEmployeeStatusId = 2;

  // ─── State ───────────────────────────────────────────────

  DocumentDashboardStatus _status = DocumentDashboardStatus.idle;
  String? _errorMessage;

  DashboardSummary _summary = DashboardSummary.empty;
  DashboardBucket _selectedBucket = DashboardBucket.expired;
  int _expiringInDays = 30;

  List<DocumentGroupWithTemplates> _groupsWithTemplates = [];
  String? _selectedGroupId;
  String? _selectedTemplateId;
  int? _employeeStatusFilter = activeEmployeeStatusId;
  String? _employeeNameFilter;

  List<DashboardUnitItem> _units = [];
  int _totalCount = 0;
  int _pageNumber = 1;
  int _pageSize = 50;
  bool _isLoadingUnits = false;

  DashboardGrouping _grouping = DashboardGrouping.byDocument;

  // ─── Getters ─────────────────────────────────────────────

  /// The current screen status.
  DocumentDashboardStatus get status => _status;

  /// Error message when [status] is [DocumentDashboardStatus.error].
  String? get errorMessage => _errorMessage;

  /// The per-bucket counts shown on the KPI cards.
  DashboardSummary get summary => _summary;

  /// The bucket whose units are listed.
  DashboardBucket get selectedBucket => _selectedBucket;

  /// The "a vencer" horizon in days from today.
  int get expiringInDays => _expiringInDays;

  /// Available document groups for the filter dropdown.
  UnmodifiableListView<DocumentGroupWithTemplates> get groups =>
      UnmodifiableListView(_groupsWithTemplates);

  /// The currently selected group id filter, or null for all groups.
  String? get selectedGroupId => _selectedGroupId;

  /// Templates of the selected group for the template filter dropdown.
  UnmodifiableListView<DocumentTemplateSummary> get templates =>
      UnmodifiableListView(_groupsWithTemplates
              .where((g) => g.id == _selectedGroupId)
              .firstOrNull
              ?.templates ??
          const []);

  /// The currently selected template id filter, or null for all templates.
  String? get selectedTemplateId => _selectedTemplateId;

  /// The employee status filter (defaults to active), or null for all.
  int? get employeeStatusFilter => _employeeStatusFilter;

  /// The employee name search filter.
  String? get employeeNameFilter => _employeeNameFilter;

  /// The units of the selected bucket on the current page, ordered by
  /// urgency on the server.
  UnmodifiableListView<DashboardUnitItem> get units =>
      UnmodifiableListView(_units);

  /// Total matching units of the selected bucket across all pages.
  int get totalCount => _totalCount;

  /// The current page number (1-based).
  int get pageNumber => _pageNumber;

  /// The current page size.
  int get pageSize => _pageSize;

  /// Total number of pages for the current bucket.
  int get pageCount =>
      _totalCount == 0 ? 1 : ((_totalCount - 1) ~/ _pageSize) + 1;

  /// Whether the unit list is being reloaded (the summary stays visible).
  bool get isLoadingUnits => _isLoadingUnits;

  /// How the unit list is grouped.
  DashboardGrouping get grouping => _grouping;

  /// The reference date used for urgency labels.
  DateTime get referenceDate => _nowProvider();

  /// The current page's units grouped by employee, preserving the urgency
  /// order inside each employee group.
  UnmodifiableMapView<String, List<DashboardUnitItem>> get unitsByEmployee {
    final map = <String, List<DashboardUnitItem>>{};
    for (final unit in _units) {
      map.putIfAbsent(unit.employeeId, () => []).add(unit);
    }
    return UnmodifiableMapView(map);
  }

  /// Extracts a user-facing error message from [error].
  String _errorFrom(Object error, String fallback) {
    final messages = extractServerMessages(error);
    return messages.isNotEmpty ? messages.join('\n') : fallback;
  }

  // ─── Loading ─────────────────────────────────────────────

  /// Loads the filter dropdowns, the summary and the unit list.
  Future<void> loadDashboard() async {
    _status = DocumentDashboardStatus.loading;
    _errorMessage = null;
    notifyListeners();
    try {
      final groupsResult = await _documentGroupRepository
          .getDocumentGroupsWithTemplates(_companyId);
      groupsResult.fold(
        onSuccess: (groups) => _groupsWithTemplates = groups,
        onError: (e, _) {
          _errorMessage = _errorFrom(e, 'Falha ao carregar grupos.');
          _status = DocumentDashboardStatus.error;
        },
      );
      if (_status == DocumentDashboardStatus.error) return;

      await _loadSummary();
      if (_status == DocumentDashboardStatus.error) return;

      await _loadUnits();
    } finally {
      notifyListeners();
    }
  }

  /// Reloads the summary and the unit list with the current filters.
  Future<void> refresh() async {
    _status = DocumentDashboardStatus.loading;
    _errorMessage = null;
    notifyListeners();
    try {
      await _loadSummary();
      if (_status == DocumentDashboardStatus.error) return;
      await _loadUnits();
    } finally {
      notifyListeners();
    }
  }

  Future<void> _loadSummary() async {
    final result = await _dashboardRepository.getSummary(
      _companyId,
      expiringInDays: _expiringInDays,
      employeeStatusId: _employeeStatusFilter,
      employeeName: _employeeNameFilter,
      documentGroupId: _selectedGroupId,
      documentTemplateId: _selectedTemplateId,
    );
    result.fold(
      onSuccess: (summary) => _summary = summary,
      onError: (e, _) {
        _errorMessage = _errorFrom(e, 'Falha ao carregar o resumo.');
        _status = DocumentDashboardStatus.error;
      },
    );
  }

  Future<void> _loadUnits() async {
    final result = await _dashboardRepository.getUnits(
      _companyId,
      bucket: _selectedBucket,
      expiringInDays: _expiringInDays,
      employeeStatusId: _employeeStatusFilter,
      employeeName: _employeeNameFilter,
      documentGroupId: _selectedGroupId,
      documentTemplateId: _selectedTemplateId,
      pageSize: _pageSize,
      pageNumber: _pageNumber,
    );
    result.fold(
      onSuccess: (page) {
        _units = page.items;
        _totalCount = page.totalCount;
        _status = DocumentDashboardStatus.loaded;
      },
      onError: (e, _) {
        _errorMessage = _errorFrom(e, 'Falha ao carregar os documentos.');
        _status = DocumentDashboardStatus.error;
      },
    );
  }

  /// Reloads only the unit list, keeping the summary cards stable.
  Future<void> _reloadUnitsOnly() async {
    _isLoadingUnits = true;
    notifyListeners();
    try {
      await _loadUnits();
    } finally {
      _isLoadingUnits = false;
      notifyListeners();
    }
  }

  // ─── User interactions ───────────────────────────────────

  /// Selects the bucket whose units are listed and reloads the list.
  Future<void> selectBucket(DashboardBucket bucket) async {
    if (_selectedBucket == bucket) return;
    _selectedBucket = bucket;
    _pageNumber = 1;
    await _reloadUnitsOnly();
  }

  /// Changes the "a vencer" horizon and reloads summary and list.
  Future<void> setHorizon(int days) async {
    if (_expiringInDays == days) return;
    _expiringInDays = days;
    _pageNumber = 1;
    await refresh();
  }

  /// Applies the employee status filter and reloads summary and list.
  Future<void> setEmployeeStatusFilter(int? statusId) async {
    _employeeStatusFilter = statusId;
    _pageNumber = 1;
    await refresh();
  }

  /// Applies the employee name search filter and reloads summary and list.
  Future<void> setEmployeeNameFilter(String? name) async {
    final normalized = (name ?? '').trim();
    _employeeNameFilter = normalized.isEmpty ? null : normalized;
    _pageNumber = 1;
    await refresh();
  }

  /// Applies the group filter (clearing the template filter) and reloads.
  Future<void> selectGroup(String? groupId) async {
    _selectedGroupId = groupId;
    _selectedTemplateId = null;
    _pageNumber = 1;
    await refresh();
  }

  /// Applies the template filter and reloads summary and list.
  Future<void> selectTemplate(String? templateId) async {
    _selectedTemplateId = templateId;
    _pageNumber = 1;
    await refresh();
  }

  /// Navigates to [page] (1-based) within the current bucket.
  Future<void> setPage(int page) async {
    if (page < 1 || page > pageCount || page == _pageNumber) return;
    _pageNumber = page;
    await _reloadUnitsOnly();
  }

  /// Changes the page size and reloads the list from the first page.
  Future<void> setPageSize(int size) async {
    if (_pageSize == size) return;
    _pageSize = size;
    _pageNumber = 1;
    await _reloadUnitsOnly();
  }

  /// Switches how the unit list is grouped.
  void setGrouping(DashboardGrouping grouping) {
    if (_grouping == grouping) return;
    _grouping = grouping;
    notifyListeners();
  }
}
