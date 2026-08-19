import 'package:flutter/foundation.dart';

import '../../domain/bill_payment_exception.dart';
import '../../domain/bill_repository.dart';

/// Drives the manual import form.
class BillImportViewModel extends ChangeNotifier {
  /// Creates the view model.
  BillImportViewModel({required BillRepository repository})
      : _repository = repository;

  final BillRepository _repository;

  bool _isSaving = false;
  String? _errorMessage;
  ImportOutcome? _outcome;

  /// Whether the import is in flight.
  bool get isSaving => _isSaving;

  /// The message to show when the import failed.
  String? get errorMessage => _errorMessage;

  /// The outcome of a successful import.
  ImportOutcome? get outcome => _outcome;

  /// Imports the bill. Resolves to its id, or `null`.
  ///
  /// At least one of the two instruments is required — the form enforces
  /// it, and the domain enforces it again.
  Future<String?> import({String? digitableLine, String? pixPayload}) async {
    _isSaving = true;
    _errorMessage = null;
    _outcome = null;
    notifyListeners();

    String? id;
    try {
      final result = await _repository.importBill(
        digitableLine:
            (digitableLine?.trim().isEmpty ?? true) ? null : digitableLine,
        pixPayload: (pixPayload?.trim().isEmpty ?? true) ? null : pixPayload,
      );
      result.fold(
        onSuccess: (outcome) {
          _outcome = outcome;
          id = outcome.id;
        },
        onError: (error, _) {
          _errorMessage = billPaymentErrorMessage(
            error,
            fallback: 'Não foi possível importar o boleto.',
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
