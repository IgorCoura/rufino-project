import 'package:flutter/foundation.dart';

import '../../domain/bill_payment_exception.dart';
import '../../domain/payee.dart';
import '../../domain/payee_repository.dart';

/// Stage of the payee detail.
enum PayeeDetailStatus {
  /// The cadastro is on its way.
  loading,

  /// The cadastro is on screen.
  loaded,

  /// The cadastro could not be loaded.
  error,
}

/// Drives the payee detail with its inline edits.
///
/// One block = one endpoint = one `x-requestid`. Every successful write
/// reloads the cadastro from the server — what the screen shows is what the
/// server accepted, never an optimistic guess.
class PayeeDetailViewModel extends ChangeNotifier {
  /// Creates the view model for [payeeId].
  PayeeDetailViewModel({
    required PayeeRepository repository,
    required this.payeeId,
  }) : _repository = repository;

  final PayeeRepository _repository;

  /// The payee being shown.
  final String payeeId;

  Payee? _payee;
  PayeeDetailStatus _status = PayeeDetailStatus.loading;
  String? _errorMessage;
  bool _isMutating = false;

  /// The cadastro, once loaded.
  Payee? get payee => _payee;

  /// The stage of the detail.
  PayeeDetailStatus get status => _status;

  /// The message of the last failure — load or mutation.
  String? get errorMessage => _errorMessage;

  /// Whether a mutation is in flight.
  bool get isMutating => _isMutating;

  /// Loads the cadastro.
  Future<void> load() async {
    _status = PayeeDetailStatus.loading;
    _errorMessage = null;
    notifyListeners();

    final result = await _repository.getPayee(payeeId);
    result.fold(
      onSuccess: (payee) {
        _payee = payee;
        _status = PayeeDetailStatus.loaded;
      },
      onError: (error, _) {
        _status = PayeeDetailStatus.error;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar o beneficiário.',
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
      // Every repository write resolves to a Result — fold decides.
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

  /// Renames the payee.
  Future<bool> saveLegalName(String legalName) => _mutate(
        () => _repository.changeLegalName(payeeId, legalName),
        fallback: 'Não foi possível renomear.',
      );

  /// Replaces the amount policy.
  Future<bool> savePolicy(AmountPolicyInput policy) => _mutate(
        () => _repository.changeAmountPolicy(payeeId, policy),
        fallback: 'Não foi possível alterar a política de valor.',
      );

  /// Adds an alias.
  Future<bool> addAlias(String alias) => _mutate(
        () => _repository.addAlias(payeeId, alias),
        fallback: 'Não foi possível adicionar o apelido.',
      );

  /// Removes an alias.
  Future<bool> removeAlias(String alias) => _mutate(
        () => _repository.removeAlias(payeeId, alias),
        fallback: 'Não foi possível remover o apelido.',
      );

  /// Adds an accepted bank.
  Future<bool> addBank(String bankCode) => _mutate(
        () => _repository.addAcceptedBank(payeeId, bankCode),
        fallback: 'Não foi possível adicionar o banco.',
      );

  /// Removes an accepted bank.
  Future<bool> removeBank(String bankCode) => _mutate(
        () => _repository.removeAcceptedBank(payeeId, bankCode),
        fallback: 'Não foi possível remover o banco.',
      );

  /// Activates or deactivates the payee.
  Future<bool> setActivation({required bool isActive}) => _mutate(
        () => _repository.setActivation(payeeId, isActive: isActive),
        fallback: 'Não foi possível alterar a ativação.',
      );

  /// Removes the payee. Does not reload — the cadastro is gone.
  Future<bool> deletePayee() async {
    _isMutating = true;
    _errorMessage = null;
    notifyListeners();

    var succeeded = false;
    try {
      final result = await _repository.deletePayee(payeeId);
      result.fold(
        onSuccess: (_) => succeeded = true,
        onError: (error, _) {
          _errorMessage = billPaymentErrorMessage(
            error,
            fallback: 'Não foi possível excluir o beneficiário.',
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
