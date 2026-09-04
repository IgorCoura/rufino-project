import 'dart:collection';

import 'package:flutter/foundation.dart';

import '../../domain/bill_payment_exception.dart';
import '../../domain/trusted_origin.dart';
import '../../domain/trusted_origin_repository.dart';

/// Stage of the trusted origin listing.
enum TrustedOriginListStatus {
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

/// The answer of the sender resolver widget.
enum ResolveOutcome {
  /// Nothing asked yet.
  idle,

  /// The resolver is asking the server.
  resolving,

  /// A registered origin matched.
  matched,

  /// The sender is unknown — a valid and common state.
  unknown,
}

/// Drives the trusted origin listing, its register and its row actions.
class TrustedOriginListViewModel extends ChangeNotifier {
  /// Creates the view model.
  TrustedOriginListViewModel({required TrustedOriginRepository repository})
      : _repository = repository;

  final TrustedOriginRepository _repository;

  final List<TrustedOrigin> _items = [];
  TrustedOriginListStatus _status = TrustedOriginListStatus.loading;
  String? _nextCursor;
  String? _errorMessage;
  bool _isMutating = false;

  ResolveOutcome _resolveOutcome = ResolveOutcome.idle;
  TrustedOrigin? _resolved;

  /// The rows currently loaded.
  UnmodifiableListView<TrustedOrigin> get items =>
      UnmodifiableListView(_items);

  /// The stage of the listing.
  TrustedOriginListStatus get status => _status;

  /// The message of the last failure.
  String? get errorMessage => _errorMessage;

  /// Whether there is another page to ask for.
  bool get hasMore => _nextCursor != null;

  /// Whether a mutation is in flight.
  bool get isMutating => _isMutating;

  /// The state of the sender resolver.
  ResolveOutcome get resolveOutcome => _resolveOutcome;

  /// The origin the resolver matched, when it did.
  TrustedOrigin? get resolved => _resolved;

  /// Loads the first page.
  Future<void> load() async {
    _status = TrustedOriginListStatus.loading;
    _errorMessage = null;
    _nextCursor = null;
    notifyListeners();

    final result = await _repository.listOrigins();
    result.fold(
      onSuccess: (page) {
        _items
          ..clear()
          ..addAll(page.items);
        _nextCursor = page.nextCursor;
        _status = _items.isEmpty
            ? TrustedOriginListStatus.empty
            : TrustedOriginListStatus.loaded;
      },
      onError: (error, _) {
        _status = TrustedOriginListStatus.error;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar as origens.',
        );
      },
    );
    notifyListeners();
  }

  /// Loads the next page, keeping what is already on screen.
  Future<void> loadMore() async {
    final cursor = _nextCursor;
    if (cursor == null || _status == TrustedOriginListStatus.loadingMore) {
      return;
    }

    _status = TrustedOriginListStatus.loadingMore;
    notifyListeners();

    final result = await _repository.listOrigins(cursor: cursor);
    result.fold(
      onSuccess: (page) {
        _items.addAll(page.items);
        _nextCursor = page.nextCursor;
        _status = TrustedOriginListStatus.loaded;
      },
      onError: (error, _) {
        _status = TrustedOriginListStatus.loaded;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar mais origens.',
        );
      },
    );
    notifyListeners();
  }

  /// Asks who answers for [sender], respecting the match precedence.
  Future<void> resolveSender(String sender) async {
    final trimmed = sender.trim();
    if (trimmed.isEmpty) {
      _resolveOutcome = ResolveOutcome.idle;
      _resolved = null;
      notifyListeners();
      return;
    }

    _resolveOutcome = ResolveOutcome.resolving;
    notifyListeners();

    final result = await _repository.resolveSender(trimmed);
    result.fold(
      onSuccess: (origin) {
        _resolved = origin;
        _resolveOutcome =
            origin == null ? ResolveOutcome.unknown : ResolveOutcome.matched;
      },
      onError: (error, _) {
        _resolveOutcome = ResolveOutcome.idle;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível resolver o remetente.',
        );
      },
    );
    notifyListeners();
  }

  Future<bool> _mutate(
    Future<dynamic> Function() action, {
    required String fallback,
  }) async {
    _isMutating = true;
    _errorMessage = null;
    notifyListeners();

    var succeeded = false;
    try {
      final result = await action();
      (result as dynamic).fold(
        onSuccess: (_) => succeeded = true,
        onError: (error, _) {
          _errorMessage = billPaymentErrorMessage(error, fallback: fallback);
        },
      );
    } finally {
      _isMutating = false;
      notifyListeners();
    }
    if (succeeded) await load();
    return succeeded;
  }

  /// Registers an origin.
  Future<bool> register({
    required String kind,
    required String value,
    required String decision,
    String? note,
  }) =>
      _mutate(
        () => _repository.registerOrigin(
          kind: kind,
          value: value,
          decision: decision,
          note: note,
        ),
        fallback: 'Não foi possível cadastrar a origem.',
      );

  /// Replaces the decision of one origin.
  Future<bool> changeDecision(String id, String decision) => _mutate(
        () => _repository.changeDecision(id, decision: decision),
        fallback: 'Não foi possível alterar a decisão.',
      );

  /// Removes one origin.
  Future<bool> deleteOrigin(String id) => _mutate(
        () => _repository.deleteOrigin(id),
        fallback: 'Não foi possível excluir a origem.',
      );
}
