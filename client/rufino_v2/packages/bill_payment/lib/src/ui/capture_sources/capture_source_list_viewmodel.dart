import 'dart:collection';

import 'package:flutter/foundation.dart';

import '../../domain/bill_payment_exception.dart';
import '../../domain/capture_source.dart';
import '../../domain/capture_source_repository.dart';

/// Stage of the capture source listing.
enum CaptureSourceListStatus {
  /// First page on its way.
  loading,

  /// Rows on screen.
  loaded,

  /// Another page on its way, rows already on screen.
  loadingMore,

  /// Nothing connected yet.
  empty,

  /// The listing could not be loaded.
  error,
}

/// Drives the capture source listing.
class CaptureSourceListViewModel extends ChangeNotifier {
  /// Creates the view model.
  CaptureSourceListViewModel({required CaptureSourceRepository repository})
      : _repository = repository;

  final CaptureSourceRepository _repository;

  final List<CaptureSource> _items = [];
  CaptureSourceListStatus _status = CaptureSourceListStatus.loading;
  String? _nextCursor;
  String? _errorMessage;

  /// The rows currently loaded.
  UnmodifiableListView<CaptureSource> get items =>
      UnmodifiableListView(_items);

  /// The stage of the listing.
  CaptureSourceListStatus get status => _status;

  /// The message of the last failure.
  String? get errorMessage => _errorMessage;

  /// Whether there is another page to ask for.
  bool get hasMore => _nextCursor != null;

  /// Loads the first page.
  Future<void> load() async {
    _status = CaptureSourceListStatus.loading;
    _errorMessage = null;
    _nextCursor = null;
    notifyListeners();

    final result = await _repository.listSources();
    result.fold(
      onSuccess: (page) {
        _items
          ..clear()
          ..addAll(page.items);
        _nextCursor = page.nextCursor;
        _status = _items.isEmpty
            ? CaptureSourceListStatus.empty
            : CaptureSourceListStatus.loaded;
      },
      onError: (error, _) {
        _status = CaptureSourceListStatus.error;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar as fontes de captura.',
        );
      },
    );
    notifyListeners();
  }

  /// Loads the next page, keeping what is already on screen.
  Future<void> loadMore() async {
    final cursor = _nextCursor;
    if (cursor == null || _status == CaptureSourceListStatus.loadingMore) {
      return;
    }

    _status = CaptureSourceListStatus.loadingMore;
    notifyListeners();

    final result = await _repository.listSources(cursor: cursor);
    result.fold(
      onSuccess: (page) {
        _items.addAll(page.items);
        _nextCursor = page.nextCursor;
        _status = CaptureSourceListStatus.loaded;
      },
      onError: (error, _) {
        _status = CaptureSourceListStatus.loaded;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar mais fontes.',
        );
      },
    );
    notifyListeners();
  }
}
