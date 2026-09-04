import 'dart:collection';

import 'package:flutter/foundation.dart';

import '../../domain/tenant_exception.dart';
import '../../domain/tenant_repository.dart';
import '../../domain/tenant_summary.dart';

/// Stage of the back-office listing.
enum TenantListStatus {
  /// First page on its way.
  loading,

  /// Rows on screen.
  loaded,

  /// Another page on its way, rows already on screen.
  loadingMore,

  /// The filters matched nothing.
  empty,

  /// The listing could not be loaded.
  error,
}

/// Drives the back-office listing of every tenant on the platform.
///
/// Paginates by cursor, filters on the server. There is no "new customer"
/// action here on purpose: a tenant is born in one place only, the selection
/// screen, and a second door would be a second set of rules to keep in sync.
class TenantListViewModel extends ChangeNotifier {
  /// Creates the view model.
  TenantListViewModel({required TenantRepository repository})
      : _repository = repository;

  final TenantRepository _repository;

  final List<TenantSummary> _items = [];
  TenantListFilter _filter = const TenantListFilter();
  TenantListStatus _status = TenantListStatus.loading;
  String? _nextCursor;
  String? _errorMessage;

  /// The rows currently loaded.
  UnmodifiableListView<TenantSummary> get items => UnmodifiableListView(_items);

  /// The filters in force.
  TenantListFilter get filter => _filter;

  /// The stage of the listing.
  TenantListStatus get status => _status;

  /// The message to show when something failed.
  String? get errorMessage => _errorMessage;

  /// Whether there is another page to ask for.
  bool get hasMore => _nextCursor != null;

  /// Whether any filter is in force.
  bool get hasFilters => !_filter.isEmpty;

  /// Loads the first page under the current filters.
  Future<void> load() async {
    _status = TenantListStatus.loading;
    _errorMessage = null;
    _nextCursor = null;
    notifyListeners();

    final result = await _repository.listTenants(filter: _filter);
    result.fold(
      onSuccess: (page) {
        _items
          ..clear()
          ..addAll(page.items);
        _nextCursor = page.nextCursor;
        _status = _items.isEmpty
            ? TenantListStatus.empty
            : TenantListStatus.loaded;
      },
      onError: (error, _) {
        _status = TenantListStatus.error;
        _errorMessage = tenantErrorMessage(
          error,
          fallback: 'Não foi possível carregar os clientes.',
        );
      },
    );
    notifyListeners();
  }

  /// Loads the next page, keeping what is already on screen.
  Future<void> loadMore() async {
    final cursor = _nextCursor;
    if (cursor == null || _status == TenantListStatus.loadingMore) return;

    _status = TenantListStatus.loadingMore;
    notifyListeners();

    final result = await _repository.listTenants(
      filter: _filter,
      cursor: cursor,
    );
    result.fold(
      onSuccess: (page) {
        _items.addAll(page.items);
        _nextCursor = page.nextCursor;
        _status = TenantListStatus.loaded;
      },
      onError: (error, _) {
        // A página seguinte falhou, mas o que já está na tela continua
        // válido: perder as linhas carregadas seria punir quem rolou.
        _status = TenantListStatus.loaded;
        _errorMessage = tenantErrorMessage(
          error,
          fallback: 'Não foi possível carregar mais clientes.',
        );
      },
    );
    notifyListeners();
  }

  /// Applies a free-text search and reloads.
  Future<void> search(String term) {
    final trimmed = term.trim();
    _filter = trimmed.isEmpty
        ? _filter.copyWith(clearSearch: true)
        : _filter.copyWith(search: trimmed);
    return load();
  }

  /// Toggles the kind filter and reloads.
  Future<void> toggleKind(String kind) {
    _filter = _filter.kind == kind
        ? _filter.copyWith(clearKind: true)
        : _filter.copyWith(kind: kind);
    return load();
  }

  /// Toggles the status filter and reloads.
  Future<void> toggleStatus(String status) {
    _filter = _filter.status == status
        ? _filter.copyWith(clearStatus: true)
        : _filter.copyWith(status: status);
    return load();
  }

  /// Toggles the product filter and reloads.
  Future<void> toggleProduct(String product) {
    _filter = _filter.product == product
        ? _filter.copyWith(clearProduct: true)
        : _filter.copyWith(product: product);
    return load();
  }

  /// Drops every filter and reloads.
  Future<void> clearFilters() {
    _filter = const TenantListFilter();
    return load();
  }
}
