import 'package:flutter/foundation.dart';

import '../../domain/bill_payment_exception.dart';
import '../../domain/expectation.dart';
import '../../domain/expectation_repository.dart';

/// Stage of the expectation detail.
enum ExpectationDetailStatus {
  /// The expectation is on its way.
  loading,

  /// The expectation is on screen.
  loaded,

  /// The expectation could not be loaded.
  error,
}

/// Drives the expectation detail: the watch controls and the cycle
/// timeline.
class ExpectationDetailViewModel extends ChangeNotifier {
  /// Creates the view model for [expectationId].
  ExpectationDetailViewModel({
    required ExpectationRepository repository,
    required this.expectationId,
  }) : _repository = repository;

  final ExpectationRepository _repository;

  /// The expectation being shown.
  final String expectationId;

  Expectation? _expectation;
  ExpectationDetailStatus _status = ExpectationDetailStatus.loading;
  String? _errorMessage;
  bool _isMutating = false;

  /// The expectation, once loaded.
  Expectation? get expectation => _expectation;

  /// The stage of the detail.
  ExpectationDetailStatus get status => _status;

  /// The message of the last failure.
  String? get errorMessage => _errorMessage;

  /// Whether a mutation is in flight.
  bool get isMutating => _isMutating;

  /// Loads the expectation.
  Future<void> load() async {
    _status = ExpectationDetailStatus.loading;
    _errorMessage = null;
    notifyListeners();

    final result = await _repository.getExpectation(expectationId);
    result.fold(
      onSuccess: (expectation) {
        _expectation = expectation;
        _status = ExpectationDetailStatus.loaded;
      },
      onError: (error, _) {
        _status = ExpectationDetailStatus.error;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar a expectativa.',
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

  /// Pauses the watch until [until].
  Future<bool> pause(DateTime until) => _mutate(
        () => _repository.alterWatch(
          expectationId,
          isActive: true,
          pausedUntil: until,
        ),
        fallback: 'Não foi possível pausar.',
      );

  /// Resumes the watch.
  Future<bool> resume() => _mutate(
        () => _repository.alterWatch(expectationId, isActive: true),
        fallback: 'Não foi possível retomar.',
      );

  /// Deactivates the watch.
  Future<bool> deactivate(String? reason) => _mutate(
        () => _repository.alterWatch(
          expectationId,
          isActive: false,
          reason: reason,
        ),
        fallback: 'Não foi possível desativar.',
      );

  /// Dismisses one cycle — that competence only.
  Future<bool> waiveCycle(String cycleId, String? reason) => _mutate(
        () => _repository.waiveCycle(expectationId, cycleId, reason: reason),
        fallback: 'Não foi possível dispensar o ciclo.',
      );
}
