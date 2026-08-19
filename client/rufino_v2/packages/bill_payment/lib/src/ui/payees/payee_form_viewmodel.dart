import 'package:flutter/foundation.dart';

import '../../domain/bill_payment_enums.dart';
import '../../domain/bill_payment_exception.dart';
import '../../domain/payee_repository.dart';

/// Drives the payee register form.
class PayeeFormViewModel extends ChangeNotifier {
  /// Creates the view model.
  PayeeFormViewModel({required PayeeRepository repository})
      : _repository = repository;

  final PayeeRepository _repository;

  String _policyKind = AmountPolicyKinds.unbounded;
  bool _isSaving = false;
  String? _errorMessage;

  /// The selected policy kind.
  String get policyKind => _policyKind;

  /// Whether a save is in flight.
  bool get isSaving => _isSaving;

  /// The message to show when the save failed.
  String? get errorMessage => _errorMessage;

  /// Selects the policy kind, revealing its fields.
  void selectPolicyKind(String kind) {
    if (_policyKind == kind) return;
    _policyKind = kind;
    notifyListeners();
  }

  /// Registers the payee. Resolves to its id, or `null` on failure.
  Future<String?> register({
    required String legalName,
    required String taxId,
    double? expectedAmount,
    double? tolerancePercent,
    double? minAmount,
    double? maxAmount,
  }) async {
    _isSaving = true;
    _errorMessage = null;
    notifyListeners();

    String? id;
    try {
      final result = await _repository.registerPayee(
        legalName: legalName,
        taxId: taxId,
        amountPolicy: AmountPolicyInput(
          kind: _policyKind,
          expectedAmount:
              _policyKind == AmountPolicyKinds.fixed ? expectedAmount : null,
          tolerancePercent:
              _policyKind == AmountPolicyKinds.fixed ? tolerancePercent : null,
          minAmount: _policyKind == AmountPolicyKinds.range ? minAmount : null,
          maxAmount: _policyKind == AmountPolicyKinds.range ? maxAmount : null,
        ),
      );
      result.fold(
        onSuccess: (newId) => id = newId,
        onError: (error, _) {
          _errorMessage = billPaymentErrorMessage(
            error,
            fallback: 'Não foi possível cadastrar o beneficiário.',
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
