import 'dart:collection';
import 'dart:typed_data';

import 'package:flutter/foundation.dart';

import '../../../../domain/entities/employee.dart';
import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/employee_repository.dart';

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
class EmployeeListViewModel extends ChangeNotifier {
  EmployeeListViewModel({
    required CompanyRepository companyRepository,
    required EmployeeRepository employeeRepository,
    this.pageSize = 15,
  })  : _companyRepository = companyRepository,
        _employeeRepository = employeeRepository;

  final CompanyRepository _companyRepository;
  final EmployeeRepository _employeeRepository;

  /// The number of items fetched per page.
  final int pageSize;

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

      final int? statusFilter =
          _selectedStatus == EmployeeStatus.none ? null : _selectedStatus.id;
      final int? docStatusFilter = _selectedDocumentStatus == DocumentStatus.all
          ? null
          : _selectedDocumentStatus.id;

      final String? nameFilter =
          _searchQuery.isEmpty || _searchParam != SearchParam.name
              ? null
              : _searchQuery;
      final String? roleFilter =
          _searchQuery.isEmpty || _searchParam != SearchParam.role
              ? null
              : _searchQuery;

      final result = await _employeeRepository.getEmployees(
        _companyId!,
        name: nameFilter,
        role: roleFilter,
        status: statusFilter,
        documentStatus: docStatusFilter,
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
        onError: (_) {
          _status = EmployeeListStatus.error;
          _errorMessage = 'Falha ao carregar funcionários.';
        },
      );
    } finally {
      notifyListeners();
    }
  }

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
      onError: (_) {}, // silently ignore image errors
    );
  }
}
