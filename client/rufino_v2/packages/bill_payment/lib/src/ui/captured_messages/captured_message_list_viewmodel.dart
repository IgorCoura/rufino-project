import 'dart:collection';

import 'package:flutter/foundation.dart';

import '../../domain/bill_payment_exception.dart';
import '../../domain/captured_message.dart';
import '../../domain/captured_message_repository.dart';

/// Stage of the capture log.
enum CapturedMessageListStatus {
  /// First page on its way.
  loading,

  /// Rows on screen.
  loaded,

  /// Another page on its way, rows already on screen.
  loadingMore,

  /// Nothing under the current filters.
  empty,

  /// The listing could not be loaded.
  error,
}

/// Drives the capture log: the listing, its filters, the retention control and
/// the per-message recapture.
class CapturedMessageListViewModel extends ChangeNotifier {
  /// Creates the view model.
  CapturedMessageListViewModel({required CapturedMessageRepository repository})
      : _repository = repository;

  final CapturedMessageRepository _repository;

  final List<CapturedMessage> _items = [];
  CapturedMessageListStatus _status = CapturedMessageListStatus.loading;
  CapturedMessageFilter _filter = const CapturedMessageFilter();
  CaptureSyncStatus? _syncStatus;
  CaptureRetentionPolicy? _retention;
  String? _nextCursor;
  String? _errorMessage;
  String? _infoMessage;
  bool _isMutating = false;

  /// The rows currently loaded.
  UnmodifiableListView<CapturedMessage> get items =>
      UnmodifiableListView(_items);

  /// The stage of the listing.
  CapturedMessageListStatus get status => _status;

  /// The filters in force.
  CapturedMessageFilter get filter => _filter;

  /// When the mailbox was last read — the header.
  CaptureSyncStatus? get syncStatus => _syncStatus;

  /// The retention window in force.
  CaptureRetentionPolicy? get retention => _retention;

  /// The message of the last failure.
  String? get errorMessage => _errorMessage;

  /// The outcome message of the last action, for a snackbar.
  String? get infoMessage => _infoMessage;

  /// Whether an action is in flight.
  bool get isMutating => _isMutating;

  /// Whether there is another page to ask for.
  bool get hasMore => _nextCursor != null;

  /// Loads the header, the retention policy and the first page.
  ///
  /// The three go together because the header is part of reading the list: a
  /// log without "last synced at" cannot answer "did the sweep already run
  /// after I sent it?", which is the question that brings someone here.
  Future<void> load() async {
    _status = CapturedMessageListStatus.loading;
    _errorMessage = null;
    _nextCursor = null;
    notifyListeners();

    final results = await Future.wait([
      _repository.listMessages(filter: _filter),
      _repository.getSyncStatus(),
      _repository.getRetentionPolicy(),
    ]);

    results[0].fold(
      onSuccess: (page) {
        final loaded = page as CapturedMessagePage;
        _items
          ..clear()
          ..addAll(loaded.items);
        _nextCursor = loaded.nextCursor;
        _status = _items.isEmpty
            ? CapturedMessageListStatus.empty
            : CapturedMessageListStatus.loaded;
      },
      onError: (error, _) {
        _status = CapturedMessageListStatus.error;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar os e-mails capturados.',
        );
      },
    );

    // Cabeçalho e política não derrubam a tela: a lista é o que interessa, e
    // uma falha neles vira ausência de informação, não ausência de conteúdo.
    results[1].fold(
      onSuccess: (value) => _syncStatus = value as CaptureSyncStatus,
      onError: (_, __) {},
    );
    results[2].fold(
      onSuccess: (value) => _retention = value as CaptureRetentionPolicy,
      onError: (_, __) {},
    );

    notifyListeners();
  }

  /// Loads the next page, keeping what is already on screen.
  Future<void> loadMore() async {
    final cursor = _nextCursor;
    if (cursor == null || _status == CapturedMessageListStatus.loadingMore) {
      return;
    }

    _status = CapturedMessageListStatus.loadingMore;
    notifyListeners();

    final result =
        await _repository.listMessages(filter: _filter, cursor: cursor);
    result.fold(
      onSuccess: (page) {
        _items.addAll(page.items);
        _nextCursor = page.nextCursor;
        _status = CapturedMessageListStatus.loaded;
      },
      onError: (error, _) {
        // As linhas ficam: perder a página já carregada por causa da seguinte
        // faria o usuário recomeçar a rolagem do zero.
        _status = CapturedMessageListStatus.loaded;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar mais e-mails.',
        );
      },
    );
    notifyListeners();
  }

  /// Replaces the outcome filter (`null` = everything) and reloads.
  Future<void> selectOutcome(String? outcome) {
    _filter = CapturedMessageFilter(
      outcome: outcome,
      sourceId: _filter.sourceId,
      from: _filter.from,
      to: _filter.to,
      search: _filter.search,
    );
    return load();
  }

  /// Runs the text search over sender and subject.
  Future<void> search(String? term) {
    _filter = CapturedMessageFilter(
      outcome: _filter.outcome,
      sourceId: _filter.sourceId,
      from: _filter.from,
      to: _filter.to,
      search: term,
    );
    return load();
  }

  /// Narrows the list to a received-at range.
  Future<void> selectPeriod(DateTime? from, DateTime? to) {
    _filter = CapturedMessageFilter(
      outcome: _filter.outcome,
      sourceId: _filter.sourceId,
      from: from,
      to: to,
      search: _filter.search,
    );
    return load();
  }

  /// Clears every filter at once.
  Future<void> clearFilters() {
    _filter = const CapturedMessageFilter();
    return load();
  }

  /// Turns the purge on or off and picks the window.
  Future<bool> configureRetention({
    required bool isEnabled,
    required int windowDays,
  }) async {
    _isMutating = true;
    _errorMessage = null;
    _infoMessage = null;
    notifyListeners();

    var succeeded = false;
    try {
      final result = await _repository.configureRetention(
        isEnabled: isEnabled,
        windowDays: windowDays,
      );
      result.fold(
        onSuccess: (policy) {
          succeeded = true;
          // A faixa oferecida não volta no PUT — mantém a que o GET trouxe, em
          // vez de a tela guardar uma segunda lista que envelhece sozinha.
          _retention = CaptureRetentionPolicy(
            isEnabled: policy.isEnabled,
            windowDays: policy.windowDays,
            availableWindowDays: _retention?.availableWindowDays ?? const [],
          );
          _infoMessage = policy.isEnabled
              ? 'Histórico de descartados será guardado por '
                  '${policy.windowDays} dias.'
              : 'O histórico deixará de ser purgado.';
        },
        onError: (error, _) {
          _errorMessage = billPaymentErrorMessage(
            error,
            fallback: 'Não foi possível alterar o prazo de retenção.',
          );
        },
      );
    } finally {
      _isMutating = false;
      notifyListeners();
    }
    return succeeded;
  }

  /// Wipes what the capture produced for one e-mail and pulls it in again.
  Future<bool> recapture(String id) async {
    _isMutating = true;
    _errorMessage = null;
    _infoMessage = null;
    notifyListeners();

    var succeeded = false;
    try {
      final result = await _repository.recapture(id);
      result.fold(
        onSuccess: (outcome) {
          succeeded = true;
          _infoMessage = 'E-mail devolvido à fila — ${outcome.artifactsIngested}'
              ' anexo(s) serão lidos de novo em segundo plano.';
        },
        onError: (error, _) {
          _errorMessage = billPaymentErrorMessage(
            error,
            fallback: 'Não foi possível reprocessar o e-mail.',
          );
        },
      );
    } finally {
      _isMutating = false;
      notifyListeners();
    }
    if (succeeded) await load();
    return succeeded;
  }
}
