import 'dart:collection';

import 'package:flutter/foundation.dart';

import '../../domain/bill_payment_exception.dart';
import '../../domain/payee.dart';
import '../../domain/payee_repository.dart';

/// Stage of the payee listing.
enum PayeeListStatus {
  /// First page on its way.
  loading,

  /// Rows on screen.
  loaded,

  /// Another page on its way, rows already on screen.
  loadingMore,

  /// Nothing registered (or the document search matched nothing).
  empty,

  /// The listing could not be loaded.
  error,
}

/// Drives the payee listing, with cursor pagination and an exact document
/// search.
class PayeeListViewModel extends ChangeNotifier {
  /// Creates the view model.
  PayeeListViewModel({required PayeeRepository repository})
      : _repository = repository;

  final PayeeRepository _repository;

  final List<Payee> _items = [];
  PayeeListStatus _status = PayeeListStatus.loading;
  String? _nextCursor;
  String? _errorMessage;
  String _taxIdQuery = '';

  /// The rows currently loaded.
  UnmodifiableListView<Payee> get items => UnmodifiableListView(_items);

  /// The stage of the listing.
  PayeeListStatus get status => _status;

  /// The message to show when something failed.
  String? get errorMessage => _errorMessage;

  /// Whether there is another page to ask for.
  bool get hasMore => _nextCursor != null;

  /// Whether an exact document search is in force.
  bool get isSearching => _taxIdQuery.isNotEmpty;

  /// Loads the first page (or re-runs the current search).
  Future<void> load() async {
    if (isSearching) return searchByTaxId(_taxIdQuery);

    _status = PayeeListStatus.loading;
    _errorMessage = null;
    _nextCursor = null;
    notifyListeners();

    final result = await _repository.listPayees();
    result.fold(
      onSuccess: (page) {
        _items
          ..clear()
          ..addAll(page.items);
        _nextCursor = page.nextCursor;
        _status =
            _items.isEmpty ? PayeeListStatus.empty : PayeeListStatus.loaded;
      },
      onError: (error, _) {
        _status = PayeeListStatus.error;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar os beneficiários.',
        );
      },
    );
    notifyListeners();
  }

  /// Loads the next page, keeping what is already on screen.
  Future<void> loadMore() async {
    final cursor = _nextCursor;
    if (cursor == null || _status == PayeeListStatus.loadingMore) return;
    if (isSearching) return;

    _status = PayeeListStatus.loadingMore;
    notifyListeners();

    final result = await _repository.listPayees(cursor: cursor);
    result.fold(
      onSuccess: (page) {
        _items.addAll(page.items);
        _nextCursor = page.nextCursor;
        _status = PayeeListStatus.loaded;
      },
      onError: (error, _) {
        // A página seguinte falhou, mas o que já está na tela continua
        // válido: perder as linhas carregadas seria punir quem rolou.
        _status = PayeeListStatus.loaded;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar mais beneficiários.',
        );
      },
    );
    notifyListeners();
  }

  /// Runs the exact search by CPF/CNPJ, or clears it for a blank [taxId].
  ///
  /// "Not registered" is an empty result, not an error — that absence is
  /// what makes the payee check inconclusive on a bill.
  Future<void> searchByTaxId(String taxId) async {
    final trimmed = taxId.trim();
    if (trimmed.isEmpty) {
      _taxIdQuery = '';
      return load();
    }

    _taxIdQuery = trimmed;
    _status = PayeeListStatus.loading;
    _errorMessage = null;
    _nextCursor = null;
    notifyListeners();

    final result = await _repository.findByTaxId(trimmed);
    result.fold(
      onSuccess: (payee) {
        _items.clear();
        if (payee != null) _items.add(payee);
        _status =
            _items.isEmpty ? PayeeListStatus.empty : PayeeListStatus.loaded;
      },
      onError: (error, _) {
        _status = PayeeListStatus.error;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível buscar o beneficiário.',
        );
      },
    );
    notifyListeners();
  }
}
