import 'dart:collection';

import 'package:flutter/foundation.dart';

import '../../domain/bill_payment_enums.dart';
import '../../domain/bill_payment_exception.dart';
import '../../domain/expectation.dart';
import '../../domain/expectation_repository.dart';
import '../../domain/payee.dart';
import '../../domain/payee_repository.dart';

/// Drives the expectation form — registering and editing.
///
/// The account reference is informed by the person, never deduced — it is
/// what separates four EDP installations of the same tenant.
///
/// The payee can only be picked while registering. Changing it would describe
/// a different expectation, not this one corrected, and the cycles already
/// open would start waiting for an account they never related to — so the way
/// to change it is to delete and register again.
class ExpectationFormViewModel extends ChangeNotifier {
  /// Creates the view model. Passing [expectationId] puts it in edit mode.
  ExpectationFormViewModel({
    required ExpectationRepository repository,
    required PayeeRepository payeeRepository,
    this.expectationId,
  })  : _repository = repository,
        _payees = payeeRepository;

  final ExpectationRepository _repository;
  final PayeeRepository _payees;

  /// The expectation being edited, or `null` when registering.
  final String? expectationId;

  final List<Payee> _payeeOptions = [];
  String? _selectedPayeeId;
  String _recurrence = Recurrences.monthly;
  Expectation? _existing;
  bool _isLoading = true;
  bool _isSaving = false;
  String? _errorMessage;

  /// Whether the form edits an expectation that already exists.
  bool get isEditing => expectationId != null;

  /// The payees the person can pick from.
  UnmodifiableListView<Payee> get payeeOptions =>
      UnmodifiableListView(_payeeOptions);

  /// The selected payee, when one is.
  String? get selectedPayeeId => _selectedPayeeId;

  /// The selected recurrence.
  String get recurrence => _recurrence;

  /// The expectation being edited, once loaded — it fills the initial values.
  Expectation? get existing => _existing;

  /// Whether the form is still assembling itself.
  bool get isLoading => _isLoading;

  /// Whether a save is in flight.
  bool get isSaving => _isSaving;

  /// The message to show when something failed.
  String? get errorMessage => _errorMessage;

  /// Loads what the form needs: the payee options and, when editing, the
  /// expectation itself.
  Future<void> load() async {
    _isLoading = true;
    notifyListeners();

    await _loadPayees();
    if (isEditing) await _loadExpectation();

    _isLoading = false;
    notifyListeners();
  }

  /// Loads the payee options (first page — the dropdown is a picker, not a
  /// browser).
  Future<void> loadPayees() => load();

  Future<void> _loadPayees() async {
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
  }

  Future<void> _loadExpectation() async {
    final result = await _repository.getExpectation(expectationId!);
    result.fold(
      onSuccess: (expectation) {
        _existing = expectation;
        _selectedPayeeId = expectation.payeeId;
        _recurrence = expectation.recurrence;
      },
      onError: (error, _) {
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar a expectativa.',
        );
      },
    );
  }

  /// Selects the payee. Ignored while editing — the payee is not editable.
  void selectPayee(String? payeeId) {
    if (isEditing) return;
    _selectedPayeeId = payeeId;
    notifyListeners();
  }

  /// Selects the recurrence.
  void selectRecurrence(String recurrence) {
    _recurrence = recurrence;
    notifyListeners();
  }

  /// Saves the form. Resolves to the expectation's id, or `null` on failure.
  Future<String?> save({
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

    try {
      return isEditing
          ? await _edit(
              label: label,
              expectedDueDay: expectedDueDay,
              observedLeadDays: observedLeadDays,
              accountReference: accountReference,
              alertLeadDays: alertLeadDays,
            )
          : await _register(
              payeeId: payeeId,
              label: label,
              expectedDueDay: expectedDueDay,
              observedLeadDays: observedLeadDays,
              accountReference: accountReference,
              alertLeadDays: alertLeadDays,
            );
    } finally {
      _isSaving = false;
      notifyListeners();
    }
  }

  Future<String?> _register({
    required String payeeId,
    required String label,
    required int expectedDueDay,
    required int observedLeadDays,
    String? accountReference,
    int? alertLeadDays,
  }) async {
    String? id;
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
    return id;
  }

  Future<String?> _edit({
    required String label,
    required int expectedDueDay,
    required int observedLeadDays,
    String? accountReference,
    int? alertLeadDays,
  }) async {
    String? id;
    final result = await _repository.editExpectation(
      expectationId!,
      label: label,
      recurrence: _recurrence,
      expectedDueDay: expectedDueDay,
      observedLeadDays: observedLeadDays,
      accountReference: accountReference,
      alertLeadDays: alertLeadDays,
    );
    result.fold(
      onSuccess: (_) => id = expectationId,
      onError: (error, _) {
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível salvar a expectativa.',
        );
      },
    );
    return id;
  }
}
