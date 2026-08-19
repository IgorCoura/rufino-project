import 'dart:collection';

import 'package:flutter/foundation.dart';

import '../../domain/bill_payment_enums.dart';
import '../../domain/bill_payment_exception.dart';
import '../../domain/expectation_repository.dart';
import '../../domain/payee.dart';
import '../../domain/payee_repository.dart';

/// Drives the expectation register form.
///
/// The account reference is informed by the person, never deduced — it is
/// what separates four EDP installations of the same tenant.
class ExpectationFormViewModel extends ChangeNotifier {
  /// Creates the view model.
  ExpectationFormViewModel({
    required ExpectationRepository repository,
    required PayeeRepository payeeRepository,
  })  : _repository = repository,
        _payees = payeeRepository;

  final ExpectationRepository _repository;
  final PayeeRepository _payees;

  final List<Payee> _payeeOptions = [];
  String? _selectedPayeeId;
  String _recurrence = Recurrences.monthly;
  bool _isLoadingPayees = true;
  bool _isSaving = false;
  String? _errorMessage;

  /// The payees the person can pick from.
  UnmodifiableListView<Payee> get payeeOptions =>
      UnmodifiableListView(_payeeOptions);

  /// The selected payee, when one is.
  String? get selectedPayeeId => _selectedPayeeId;

  /// The selected recurrence.
  String get recurrence => _recurrence;

  /// Whether the payee options are on their way.
  bool get isLoadingPayees => _isLoadingPayees;

  /// Whether a save is in flight.
  bool get isSaving => _isSaving;

  /// The message to show when something failed.
  String? get errorMessage => _errorMessage;

  /// Loads the payee options (first page — the dropdown is a picker, not a
  /// browser).
  Future<void> loadPayees() async {
    _isLoadingPayees = true;
    notifyListeners();

    final result = await _payees.listPayees(limit: 200);
    result.fold(
      onSuccess: (page) {
        _payeeOptions
          ..clear()
          ..addAll(page.items.where((p) => p.isActive));
      },
      onError: (error, _) {
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar os beneficiários.',
        );
      },
    );
    _isLoadingPayees = false;
    notifyListeners();
  }

  /// Selects the payee.
  void selectPayee(String? payeeId) {
    _selectedPayeeId = payeeId;
    notifyListeners();
  }

  /// Selects the recurrence.
  void selectRecurrence(String recurrence) {
    _recurrence = recurrence;
    notifyListeners();
  }

  /// Registers the expectation. Resolves to its id, or `null`.
  Future<String?> register({
    required String label,
    required int expectedDueDay,
    required int observedLeadDays,
    String? accountReference,
    int? alertLeadDays,
  }) async {
    final payeeId = _selectedPayeeId;
    if (payeeId == null) {
      _errorMessage = 'Escolha o beneficiário.';
      notifyListeners();
      return null;
    }

    _isSaving = true;
    _errorMessage = null;
    notifyListeners();

    String? id;
    try {
      final result = await _repository.registerExpectation(
        payeeId: payeeId,
        label: label,
        recurrence: _recurrence,
        expectedDueDay: expectedDueDay,
        observedLeadDays: observedLeadDays,
        accountReference: accountReference,
        alertLeadDays: alertLeadDays,
      );
      result.fold(
        onSuccess: (newId) => id = newId,
        onError: (error, _) {
          _errorMessage = billPaymentErrorMessage(
            error,
            fallback: 'Não foi possível cadastrar a expectativa.',
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
