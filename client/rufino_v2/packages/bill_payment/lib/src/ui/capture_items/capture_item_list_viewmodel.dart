import 'dart:collection';

import 'package:flutter/foundation.dart';

import '../../domain/bill_payment_enums.dart';
import '../../domain/bill_payment_exception.dart';
import '../../domain/capture_item.dart';
import '../../domain/capture_item_repository.dart';

/// Stage of the quarantine listing.
enum CaptureItemListStatus {
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

/// Drives the quarantine listing, filtered by status on the server.
///
/// Opens on the claim queue ([CaptureItemStatuses.unrouted]) — the list
/// whose items a person can actually resolve.
class CaptureItemListViewModel extends ChangeNotifier {
  /// Creates the view model.
  CaptureItemListViewModel({required CaptureItemRepository repository})
      : _repository = repository;

  final CaptureItemRepository _repository;

  final List<CaptureItem> _items = [];
  CaptureItemListStatus _status = CaptureItemListStatus.loading;
  String? _statusFilter = CaptureItemStatuses.unrouted;
  String? _nextCursor;
  String? _errorMessage;

  /// The rows currently loaded.
  UnmodifiableListView<CaptureItem> get items => UnmodifiableListView(_items);

  /// The stage of the listing.
  CaptureItemListStatus get status => _status;

  /// The status filter in force, or `null` for everything.
  String? get statusFilter => _statusFilter;

  /// The message of the last failure.
  String? get errorMessage => _errorMessage;

  /// Whether there is another page to ask for.
  bool get hasMore => _nextCursor != null;

  /// Loads the first page under the current filter.
  Future<void> load() async {
    _status = CaptureItemListStatus.loading;
    _errorMessage = null;
    _nextCursor = null;
    notifyListeners();

    final result = await _repository.listItems(status: _statusFilter);
    result.fold(
      onSuccess: (page) {
        _items
          ..clear()
          ..addAll(page.items);
        _nextCursor = page.nextCursor;
        _status = _items.isEmpty
            ? CaptureItemListStatus.empty
            : CaptureItemListStatus.loaded;
      },
      onError: (error, _) {
        _status = CaptureItemListStatus.error;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar a quarentena.',
        );
      },
    );
    notifyListeners();
  }

  /// Loads the next page, keeping what is already on screen.
  Future<void> loadMore() async {
    final cursor = _nextCursor;
    if (cursor == null || _status == CaptureItemListStatus.loadingMore) {
      return;
    }

    _status = CaptureItemListStatus.loadingMore;
    notifyListeners();

    final result = await _repository.listItems(
      status: _statusFilter,
      cursor: cursor,
    );
    result.fold(
      onSuccess: (page) {
        _items.addAll(page.items);
        _nextCursor = page.nextCursor;
        _status = CaptureItemListStatus.loaded;
      },
      onError: (error, _) {
        _status = CaptureItemListStatus.loaded;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar mais itens.',
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
