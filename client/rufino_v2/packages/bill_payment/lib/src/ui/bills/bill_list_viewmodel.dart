import 'dart:collection';

import 'package:flutter/foundation.dart';

import '../../domain/bill.dart';
import '../../domain/bill_payment_exception.dart';
import '../../domain/bill_repository.dart';

/// Stage of the bill listing.
enum BillListStatus {
  /// First page on its way.
  loading,

  /// Rows on screen.
  loaded,

  /// Another page on its way, rows already on screen.
  loadingMore,

  /// Nothing under the current filter.
  empty,

  /// The listing could not be loaded.
  error,
}

/// Drives the bill listing, filtered by status on the server.
class BillListViewModel extends ChangeNotifier {
  /// Creates the view model, optionally opening on [initialStatus].
  BillListViewModel({
    required BillRepository repository,
    String? initialStatus,
  })  : _repository = repository,
        _statusFilter = initialStatus;

  final BillRepository _repository;

  final List<Bill> _items = [];
  BillListStatus _status = BillListStatus.loading;
  String? _statusFilter;
  String? _nextCursor;
  String? _errorMessage;

  /// The rows currently loaded.
  UnmodifiableListView<Bill> get items => UnmodifiableListView(_items);

  /// The stage of the listing.
  BillListStatus get status => _status;

  /// The status filter in force, or `null` for everything.
  String? get statusFilter => _statusFilter;

  /// The message of the last failure.
  String? get errorMessage => _errorMessage;

  /// Whether there is another page to ask for.
  bool get hasMore => _nextCursor != null;

  /// Loads the first page under the current filter.
  Future<void> load() async {
    _status = BillListStatus.loading;
    _errorMessage = null;
    _nextCursor = null;
    notifyListeners();

    final result = await _repository.listBills(status: _statusFilter);
    result.fold(
      onSuccess: (page) {
        _items
          ..clear()
          ..addAll(page.items);
        _nextCursor = page.nextCursor;
        _status =
            _items.isEmpty ? BillListStatus.empty : BillListStatus.loaded;
      },
      onError: (error, _) {
        _status = BillListStatus.error;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar os boletos.',
        );
      },
    );
    notifyListeners();
  }

  /// Loads the next page, keeping what is already on screen.
  Future<void> loadMore() async {
    final cursor = _nextCursor;
    if (cursor == null || _status == BillListStatus.loadingMore) return;

    _status = BillListStatus.loadingMore;
    notifyListeners();

    final result = await _repository.listBills(
      status: _statusFilter,
      cursor: cursor,
    );
    result.fold(
      onSuccess: (page) {
        _items.addAll(page.items);
        _nextCursor = page.nextCursor;
        _status = BillListStatus.loaded;
      },
      onError: (error, _) {
        _status = BillListStatus.loaded;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar mais boletos.',
        );
      },
    );
    notifyListeners();
  }

  /// Selects the status filter (`null` = everything) and reloads.
  Future<void> selectStatus(String? status) {
    _statusFilter = status;
    return load();
  }
}
