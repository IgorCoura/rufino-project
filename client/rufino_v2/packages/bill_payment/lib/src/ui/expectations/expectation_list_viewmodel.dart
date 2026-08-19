import 'dart:collection';

import 'package:flutter/foundation.dart';

import '../../domain/bill_payment_exception.dart';
import '../../domain/expectation.dart';
import '../../domain/expectation_repository.dart';

/// Stage of the expectation listing.
enum ExpectationListStatus {
  /// First page on its way.
  loading,

  /// Rows on screen.
  loaded,

  /// Another page on its way, rows already on screen.
  loadingMore,

  /// Nothing registered.
  empty,

  /// The listing could not be loaded.
  error,
}

/// Drives the expectation listing.
class ExpectationListViewModel extends ChangeNotifier {
  /// Creates the view model.
  ExpectationListViewModel({required ExpectationRepository repository})
      : _repository = repository;

  final ExpectationRepository _repository;

  final List<Expectation> _items = [];
  ExpectationListStatus _status = ExpectationListStatus.loading;
  String? _nextCursor;
  String? _errorMessage;

  /// The rows currently loaded.
  UnmodifiableListView<Expectation> get items => UnmodifiableListView(_items);

  /// The stage of the listing.
  ExpectationListStatus get status => _status;

  /// The message of the last failure.
  String? get errorMessage => _errorMessage;

  /// Whether there is another page to ask for.
  bool get hasMore => _nextCursor != null;

  /// Loads the first page.
  Future<void> load() async {
    _status = ExpectationListStatus.loading;
    _errorMessage = null;
    _nextCursor = null;
    notifyListeners();

    final result = await _repository.listExpectations();
    result.fold(
      onSuccess: (page) {
        _items
          ..clear()
          ..addAll(page.items);
        _nextCursor = page.nextCursor;
        _status = _items.isEmpty
            ? ExpectationListStatus.empty
            : ExpectationListStatus.loaded;
      },
      onError: (error, _) {
        _status = ExpectationListStatus.error;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar as expectativas.',
        );
      },
    );
    notifyListeners();
  }

  /// Loads the next page, keeping what is already on screen.
  Future<void> loadMore() async {
    final cursor = _nextCursor;
    if (cursor == null || _status == ExpectationListStatus.loadingMore) {
      return;
    }

    _status = ExpectationListStatus.loadingMore;
    notifyListeners();

    final result = await _repository.listExpectations(cursor: cursor);
    result.fold(
      onSuccess: (page) {
        _items.addAll(page.items);
        _nextCursor = page.nextCursor;
        _status = ExpectationListStatus.loaded;
      },
      onError: (error, _) {
        _status = ExpectationListStatus.loaded;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar mais expectativas.',
        );
      },
    );
    notifyListeners();
  }
}
