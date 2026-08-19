import 'package:flutter/foundation.dart';

import '../../domain/bill_payment_exception.dart';
import '../../domain/capture_source_repository.dart';

/// Drives the connect-mailbox form.
class CaptureSourceConnectViewModel extends ChangeNotifier {
  /// Creates the view model.
  CaptureSourceConnectViewModel({
    required CaptureSourceRepository repository,
  }) : _repository = repository;

  final CaptureSourceRepository _repository;

  bool _isSaving = false;
  String? _errorMessage;
  bool _sharedMailboxWarning = false;

  /// Whether the connect is in flight.
  bool get isSaving => _isSaving;

  /// The message to show when the connect failed.
  String? get errorMessage => _errorMessage;

  /// Whether the server warned that another account already monitors this
  /// mailbox — a boolean and nothing more, by design (ADR-008).
  bool get sharedMailboxWarning => _sharedMailboxWarning;

  /// Connects the mailbox. Resolves to the new source id, or `null`.
  Future<String?> connect({
    required String displayName,
    required String address,
    required String directoryId,
    required String clientId,
    required String clientSecret,
    String? folderPath,
  }) async {
    _isSaving = true;
    _errorMessage = null;
    _sharedMailboxWarning = false;
    notifyListeners();

    String? id;
    try {
      final result = await _repository.connectSource(
        displayName: displayName,
        address: address,
        credential: GraphCredentialInput(
          directoryId: directoryId,
          clientId: clientId,
          clientSecret: clientSecret,
        ),
        folderPath: (folderPath?.trim().isEmpty ?? true) ? null : folderPath,
      );
      result.fold(
        onSuccess: (outcome) {
          id = outcome.id;
          _sharedMailboxWarning = outcome.alreadyMonitoredByAnotherAccount;
        },
        onError: (error, _) {
          _errorMessage = billPaymentErrorMessage(
            error,
            fallback: 'Não foi possível conectar a caixa.',
          );
        },
      );
    } finally {
      _isSaving = false;
      notifyListeners();
    }
    return id;
  }
}
