import 'bill.dart';
import 'bill_check.dart';
import 'bill_payment_enums.dart';

/// The beneficiary the official lookup returned.
class BillParty {
  /// Creates the party record.
  const BillParty({this.name, this.tradingName, this.taxId});

  /// The legal name.
  final String? name;

  /// The trading name, when there is one.
  final String? tradingName;

  /// The formatted CPF/CNPJ, when the lookup returned one.
  final String? taxId;

  /// The best display name available.
  String? get displayName => tradingName ?? name;
}

/// The human decision recorded on a bill.
class BillApproval {
  /// Creates the decision record.
  const BillApproval({
    required this.decidedBy,
    required this.decision,
    required this.decidedAt,
    this.note,
  });

  /// The `sub` of whoever decided.
  final String decidedBy;

  /// `Approved`, `Denied` or `Cancelled`.
  final String decision;

  /// When the decision happened.
  final DateTime decidedAt;

  /// The approver's note or the refusal reason.
  final String? note;
}

/// A bill as the approval screen consumes it: the document, the beneficiary
/// the lookup returned, and the twelve checks with evidence.
class BillDetail {
  /// Creates the detail projection of a bill.
  const BillDetail({
    required this.id,
    required this.status,
    required this.kind,
    required this.rail,
    required this.checks,
    required this.origin,
    required this.createdAt,
    this.beneficiary,
    this.amount,
    this.originalAmount,
    this.dueDate,
    this.bankCode,
    this.minimumScheduleDate,
    this.lastConsultedAt,
    this.approval,
    this.scheduledFor,
  });

  /// How old the lookup snapshot may be before approval requires a
  /// revalidation. Mirrors the server's `Approval:MaxSnapshotAgeHours`.
  static const Duration maxSnapshotAge = Duration(hours: 12);

  /// The bill's id.
  final String id;

  /// One of [BillStatuses].
  final String status;

  /// One of [BillKinds].
  final String kind;

  /// One of [PaymentRails].
  final String rail;

  /// The beneficiary the lookup returned, when it resolved.
  final BillParty? beneficiary;

  /// The amount that will be paid.
  final double? amount;

  /// The original amount before discounts/fees, when different.
  final double? originalAmount;

  /// The due date.
  final DateTime? dueDate;

  /// The receiving bank's COMPE code.
  final String? bankCode;

  /// The earliest date the provider accepts for scheduling.
  final DateTime? minimumScheduleDate;

  /// When the official lookup last answered.
  final DateTime? lastConsultedAt;

  /// The twelve checks, in the catalog's reading order.
  final List<BillCheck> checks;

  /// The human decision, once one exists.
  final BillApproval? approval;

  /// The scheduled payment date, once approved.
  final DateTime? scheduledFor;

  /// Where the bill came from.
  final BillOrigin origin;

  /// When the bill entered the system.
  final DateTime createdAt;

  /// Whether no further mutation applies.
  bool get isTerminal => BillStatuses.isTerminal(status);

  /// Whether the revalidate button applies.
  bool get acceptsValidation => BillStatuses.acceptsValidation(status);

  /// Whether approve/deny apply to the current status.
  bool get acceptsDecision => BillStatuses.acceptsDecision(status);

  /// Whether cancel applies to the current status.
  bool get acceptsCancellation => BillStatuses.acceptsCancellation(status);

  /// Whether the lookup snapshot is too old to sustain an approval at [now].
  ///
  /// A bill never consulted counts as stale: there is no snapshot to trust.
  bool isSnapshotStaleAt(DateTime now) {
    final consultedAt = lastConsultedAt;
    if (consultedAt == null) return true;
    return now.difference(consultedAt) > maxSnapshotAge;
  }

  /// Whether the approve button can be enabled at [now].
  ///
  /// Approval needs the right status AND a fresh snapshot — a stale one gets
  /// "revalide antes de aprovar" instead of a click that bounces on a 409.
  bool canApproveAt(DateTime now) =>
      acceptsDecision && !isSnapshotStaleAt(now);

  /// The earliest schedule date selectable at [today].
  ///
  /// The provider's minimum wins when it is later than today.
  DateTime earliestScheduleDate(DateTime today) {
    final min = minimumScheduleDate;
    if (min == null || min.isBefore(today)) return today;
    return min;
  }

  /// The checks that deserve the approver's eyes, in catalog order.
  List<BillCheck> get attentionChecks =>
      checks.where((c) => c.requiresAttention).toList();

  /// How many checks block the approval right now.
  int get blockingFailures => checks.where((c) => c.isBlockingFailure).length;
}
