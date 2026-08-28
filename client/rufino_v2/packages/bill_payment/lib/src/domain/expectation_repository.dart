import 'package:rufino_core/rufino_core.dart';

import 'expectation.dart';

/// Contract for reading and maintaining bill expectations.
abstract class ExpectationRepository {
  /// Lists expectations, one cursor page at a time.
  ///
  /// This list's server defaults differ from the others: default 20, max
  /// 100.
  Future<Result<ExpectationPage>> listExpectations({
    String? cursor,
    int limit = 20,
  });

  /// Returns the pending panel — three semantically distinct lists.
  Future<Result<PendingExpectationsView>> getPending({
    int dueSoonWindowDays = 7,
  });

  /// Returns one expectation with its cycles.
  Future<Result<Expectation>> getExpectation(String id);

  /// Registers an expectation and returns its id.
  ///
  /// [accountReference] separates two accounts of the same payee and is
  /// informed by the person, never deduced.
  Future<Result<String>> registerExpectation({
    required String payeeId,
    required String label,
    required String recurrence,
    required int expectedDueDay,
    required int observedLeadDays,
    String? accountReference,
    int? alertLeadDays,
  });

  /// Edits an expectation — everything but the payee.
  ///
  /// The payee is deliberately absent: changing it would describe a
  /// different expectation, not this one corrected, and the cycles already
  /// open would start waiting for an account they never related to. To
  /// change it, delete and register again.
  Future<Result<void>> editExpectation(
    String id, {
    required String label,
    required String recurrence,
    required int expectedDueDay,
    required int observedLeadDays,
    String? accountReference,
    int? alertLeadDays,
  });

  /// Deletes the expectation and the cycle history that came with it.
  ///
  /// A learned expectation may be learned again from the next approved bill
  /// of that payee — to stop watching for good, deactivate via [alterWatch].
  Future<Result<void>> deleteExpectation(String id);

  /// Pauses, resumes or deactivates the watch.
  Future<Result<void>> alterWatch(
    String id, {
    required bool isActive,
    DateTime? pausedUntil,
    String? reason,
  });

  /// Dismisses one cycle — silences the safety net for that competence
  /// only.
  Future<Result<void>> waiveCycle(
    String id,
    String cycleId, {
    String? reason,
  });
}
