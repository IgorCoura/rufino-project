import 'package:flutter/foundation.dart';

import '../../domain/bill_payment_enums.dart';
import '../../domain/bill_payment_exception.dart';
import '../../domain/capture_source.dart';
import '../../domain/capture_source_repository.dart';

/// Stage of the capture source detail.
enum CaptureSourceDetailStatus {
  /// The source is on its way.
  loading,

  /// The source is on screen.
  loaded,

  /// The source could not be loaded.
  error,
}

/// Drives the capture source detail with its inline edits and sync actions.
class CaptureSourceDetailViewModel extends ChangeNotifier {
  /// Creates the view model for [sourceId].
  CaptureSourceDetailViewModel({
    required CaptureSourceRepository repository,
    required this.sourceId,
  }) : _repository = repository;

  final CaptureSourceRepository _repository;

  /// The source being shown.
  final String sourceId;

  CaptureSource? _source;
  CaptureSourceDetailStatus _status = CaptureSourceDetailStatus.loading;
  String? _errorMessage;
  String? _infoMessage;
  bool _isMutating = false;

  /// The source, once loaded.
  CaptureSource? get source => _source;

  /// The stage of the detail.
  CaptureSourceDetailStatus get status => _status;

  /// The message of the last failure.
  String? get errorMessage => _errorMessage;

  /// The outcome message of the last sync/rescan, for a snackbar.
  String? get infoMessage => _infoMessage;

  /// Whether a mutation is in flight.
  bool get isMutating => _isMutating;

  /// Loads the source.
  Future<void> load() async {
    _status = CaptureSourceDetailStatus.loading;
    _errorMessage = null;
    notifyListeners();

    final result = await _repository.getSource(sourceId);
    result.fold(
      onSuccess: (source) {
        _source = source;
        _status = CaptureSourceDetailStatus.loaded;
      },
      onError: (error, _) {
        _status = CaptureSourceDetailStatus.error;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar a fonte.',
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
    _infoMessage = null;
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

  /// Renames the source.
  Future<bool> rename(String displayName) => _mutate(
        () => _repository.renameSource(sourceId, displayName),
        fallback: 'Não foi possível renomear a fonte.',
      );

  /// Enables or disables the source.
  Future<bool> setActivation({required bool isEnabled}) => _mutate(
        () => _repository.setActivation(sourceId, isEnabled: isEnabled),
        fallback: 'Não foi possível alterar a ativação.',
      );

  /// Replaces the credential.
  Future<bool> replaceCredential({
    required String directoryId,
    required String clientId,
    required String clientSecret,
  }) =>
      _mutate(
        () => _repository.replaceCredential(
          sourceId,
          GraphCredentialInput(
            directoryId: directoryId,
            clientId: clientId,
            clientSecret: clientSecret,
          ),
        ),
        fallback: 'Não foi possível substituir a credencial.',
      );

  /// Adds a watched folder.
  Future<bool> addFolder(String? folderPath) => _mutate(
        () => _repository.addFolder(
          sourceId,
          (folderPath?.trim().isEmpty ?? true) ? null : folderPath,
        ),
        fallback: 'Não foi possível adicionar a pasta.',
      );

  /// Removes a watched folder.
  Future<bool> removeFolder(String? folderPath) => _mutate(
        () => _repository.removeFolder(sourceId, folderPath),
        fallback: 'Não foi possível remover a pasta.',
      );

  /// Triggers a sync and records its outcome for the snackbar.
  Future<bool> syncNow() async {
    _isMutating = true;
    _errorMessage = null;
    _infoMessage = null;
    notifyListeners();

    var succeeded = false;
    try {
      final result = await _repository.syncSource(sourceId);
      result.fold(
        onSuccess: (outcome) {
          succeeded = true;
          _infoMessage = switch (outcome.status) {
            SyncStatuses.ok =>
              '${outcome.ingestedItems} novos, '
                  '${outcome.skippedAsAlreadyIngested} já conhecidos.',
            _ => SyncStatuses.label(outcome.status),
          };
        },
        onError: (error, _) {
          _errorMessage = billPaymentErrorMessage(
            error,
            fallback: 'Não foi possível sincronizar.',
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

  /// Discards every cursor so the next sweep rereads the whole mailbox.
  Future<bool> rescan() async {
    _isMutating = true;
    _errorMessage = null;
    _infoMessage = null;
    notifyListeners();

    var succeeded = false;
    try {
      final result = await _repository.rescanSource(sourceId);
      result.fold(
        onSuccess: (outcome) {
          succeeded = true;
          _infoMessage =
              '${outcome.foldersReset} cursores descartados — a próxima '
              'varredura relê a caixa inteira, sem duplicar.';
        },
        onError: (error, _) {
          _errorMessage = billPaymentErrorMessage(
            error,
            fallback: 'Não foi possível agendar a releitura.',
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

  /// Disconnects the source. Does not reload — the source is gone.
  Future<bool> disconnect() async {
    _isMutating = true;
    _errorMessage = null;
    notifyListeners();

    var succeeded = false;
    try {
      final result = await _repository.disconnectSource(sourceId);
      result.fold(
        onSuccess: (_) => succeeded = true,
        onError: (error, _) {
          _errorMessage = billPaymentErrorMessage(
            error,
            fallback: 'Não foi possível desconectar a fonte.',
          );
        },
      );
    } finally {
      _isMutating = false;
      notifyListeners();
    }
    return succeeded;
  }
}
