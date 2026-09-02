import 'bill_payment_enums.dart';

/// The payment order behind a bill — the execution's source of truth.
///
/// The bill mirrors it (`Scheduled`/`Paid`/`Failed`); what this entity adds is
/// the execution's own story: the dates asked and granted, the provider's fee,
/// why it failed, and whether a person still needs to confirm an immediate
/// payment (an overdue bill is processed at once, with no reaction window).
class PaymentOrder {
  /// Creates the order projection.
  const PaymentOrder({
    required this.id,
    required this.billId,
    required this.rail,
    required this.status,
    required this.hold,
    required this.requestedScheduleDate,
    required this.createdAt,
    this.effectiveScheduleDate,
    this.amount,
    this.fee,
    this.paidAt,
    this.failReasons = const [],
    this.lastError,
    this.submissionAttempts = 0,
    this.requiresConfirmation = false,
    this.hasReceipt = false,
  });

  /// The order's id.
  final String id;

  /// The bill this order pays.
  final String billId;

  /// One of `PaymentRails`.
  final String rail;

  /// One of [PaymentOrderStatuses].
  final String status;

  /// One of [PaymentOrderHolds].
  final String hold;

  /// The date the approver asked for.
  final DateTime requestedScheduleDate;

  /// The date the scheduling granted — may differ from the asked one (the
  /// 24h lead, the submission window, working days).
  final DateTime? effectiveScheduleDate;

  /// The amount submitted (or to be submitted).
  final double? amount;

  /// The provider's fee, when known.
  final double? fee;

  /// When the money actually left.
  final DateTime? paidAt;

  /// Why the execution failed — the operational queue's content.
  final List<String> failReasons;

  /// The last error the submission queue saw.
  final String? lastError;

  /// How many times the submission queue picked this order.
  final int submissionAttempts;

  /// Whether a person still needs to confirm an immediate (overdue) payment.
  final bool requiresConfirmation;

  /// Whether the payment receipt is stored and can be fetched.
  final bool hasReceipt;

  /// When the order was created.
  final DateTime createdAt;

  /// Whether the cancel action still applies — the reaction window.
  bool get canCancel => PaymentOrderStatuses.canCancel(status);

  /// Whether the order failed and the bill can be reopened for a new try.
  bool get hasFailed => status == PaymentOrderStatuses.failed;
}
