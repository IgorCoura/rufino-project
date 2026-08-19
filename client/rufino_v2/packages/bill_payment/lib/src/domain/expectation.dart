import 'bill_payment_enums.dart';

/// One competence cycle of an expectation.
class ExpectationCycle {
  /// Creates the cycle record.
  const ExpectationCycle({
    required this.id,
    required this.competence,
    required this.expectedDueDate,
    required this.alertAt,
    required this.status,
    this.missReason,
    this.arrived,
    this.fulfilledByBillId,
    this.blockedByCaptureItemId,
    this.lastAlertLevel,
  });

  /// The cycle's id.
  final String id;

  /// The competence this cycle covers (e.g. `2026-08`).
  final String competence;

  /// When the bill is expected to be due.
  final DateTime expectedDueDate;

  /// When the first alert fires.
  final DateTime alertAt;

  /// One of [CycleStatuses].
  final String status;

  /// One of [MissReasons], when the cycle went missing.
  final String? missReason;

  /// Whether something DID arrive — separates "go fetch it" from "fix the
  /// item". `null` while nothing is known.
  final bool? arrived;

  /// The bill that fulfilled this cycle.
  final String? fulfilledByBillId;

  /// The capture item blocking this cycle, when something arrived broken.
  final String? blockedByCaptureItemId;

  /// One of [AlertLevels] — the highest already fired.
  final String? lastAlertLevel;

  /// Whether the cycle still needs someone's attention.
  bool get isOpen =>
      status == CycleStatuses.waiting ||
      status == CycleStatuses.partiallyCaptured ||
      status == CycleStatuses.missing;
}

/// An expected recurring bill — the safety net against silent failure.
class Expectation {
  /// Creates the expectation record.
  const Expectation({
    required this.id,
    required this.payeeId,
    required this.label,
    required this.recurrence,
    required this.expectedDueDay,
    required this.observedLeadDays,
    required this.alertLeadDays,
    required this.origin,
    required this.observationCount,
    required this.isActive,
    required this.cycles,
    this.accountReference,
    this.pausedUntil,
  });

  /// The expectation's id.
  final String id;

  /// The payee this expectation watches.
  final String payeeId;

  /// The account reference that separates two accounts of the same payee.
  /// Informed at registration, never deduced.
  final String? accountReference;

  /// The name the tenant gave this expectation.
  final String label;

  /// One of [Recurrences].
  final String recurrence;

  /// The day of the month the bill usually falls due.
  final int expectedDueDay;

  /// How many days before the due date the bill usually arrives.
  final int observedLeadDays;

  /// How many days before the due date the alert fires.
  final int alertLeadDays;

  /// One of [ExpectationOrigins].
  final String origin;

  /// How many arrivals sustained the learning.
  final int observationCount;

  /// Whether the sweep still watches this expectation.
  final bool isActive;

  /// Paused until this date, when paused.
  final DateTime? pausedUntil;

  /// The cycles, newest first as the server sends them.
  final List<ExpectationCycle> cycles;

  /// Whether the expectation is paused at [now].
  bool isPausedAt(DateTime now) =>
      pausedUntil != null && now.isBefore(pausedUntil!);
}

/// One page of the expectation list.
class ExpectationPage {
  /// Creates a page with its [items] and the opaque [nextCursor].
  const ExpectationPage({required this.items, this.nextCursor});

  /// The expectations of this page.
  final List<Expectation> items;

  /// The opaque cursor of the next page, or `null` on the last one.
  final String? nextCursor;

  /// Whether another page exists.
  bool get hasMore => nextCursor != null;
}

/// One pending line of the panel.
class PendingExpectation {
  /// Creates the pending record.
  const PendingExpectation({
    required this.expectationId,
    required this.cycleId,
    required this.label,
    required this.competence,
    required this.expectedDueDate,
    required this.status,
    this.missReason,
    this.arrived,
    this.blockedByCaptureItemId,
    this.lastAlertLevel,
  });

  /// The expectation's id.
  final String expectationId;

  /// The cycle's id.
  final String cycleId;

  /// The expectation's label.
  final String label;

  /// The competence of the pending cycle.
  final String competence;

  /// When the bill was expected to be due.
  final DateTime expectedDueDate;

  /// One of [CycleStatuses].
  final String status;

  /// One of [MissReasons].
  final String? missReason;

  /// Whether something arrived.
  final bool? arrived;

  /// The capture item to fix, when something arrived broken.
  final String? blockedByCaptureItemId;

  /// One of [AlertLevels].
  final String? lastAlertLevel;
}

/// The pending panel: three lists with three different calls to action.
///
/// They are semantically distinct — "go fetch it", "fix the item", and
/// plain anticipation — and must never be collapsed into one.
class PendingExpectationsView {
  /// Creates the panel with its three lists.
  const PendingExpectationsView({
    required this.missing,
    required this.captureFailed,
    required this.dueSoon,
  });

  /// An empty panel.
  const PendingExpectationsView.empty()
      : missing = const [],
        captureFailed = const [],
        dueSoon = const [];

  /// Bills that never arrived — go fetch them.
  final List<PendingExpectation> missing;

  /// Bills that arrived and could not be read — fix the item.
  final List<PendingExpectation> captureFailed;

  /// Bills due soon — anticipation, no action needed yet.
  final List<PendingExpectation> dueSoon;

  /// Whether there is nothing pending at all.
  bool get isEmpty =>
      missing.isEmpty && captureFailed.isEmpty && dueSoon.isEmpty;

  /// How many lines demand action (due-soon is anticipation, not action).
  int get actionableCount => missing.length + captureFailed.length;
}
