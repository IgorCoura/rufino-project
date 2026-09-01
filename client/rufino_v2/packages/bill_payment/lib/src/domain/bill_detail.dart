import 'bill.dart';
import 'bill_check.dart';
import 'bill_payment_enums.dart';

/// The official bank-slip lookup snapshot, exposed whole (ADR-015): the
/// approver decides with the same data the system classified with.
class BankSlipLookup {
  /// Creates the snapshot record.
  const BankSlipLookup({
    required this.allowChangeValue,
    required this.isOverdue,
    required this.consultedAt,
    this.beneficiary,
    this.bankCode,
    this.amount,
    this.originalAmount,
    this.fee,
    this.dueDate,
    this.minimumScheduleDate,
  });

  /// The beneficiary the registry returned.
  final BillParty? beneficiary;

  /// The receiving bank's COMPE code.
  final String? bankCode;

  /// The amount payable today, fees included.
  final double? amount;

  /// The face amount before fees/discounts.
  final double? originalAmount;

  /// The provider's fee.
  final double? fee;

  /// Whether the amount is open (typical of utility bills).
  final bool allowChangeValue;

  /// Whether the document is past due.
  final bool isOverdue;

  /// The registered due date.
  final DateTime? dueDate;

  /// The earliest schedulable date.
  final DateTime? minimumScheduleDate;

  /// When this snapshot was taken.
  final DateTime consultedAt;
}

/// The official Pix decode snapshot, exposed whole (ADR-015).
class PixLookup {
  /// Creates the snapshot record.
  const PixLookup({
    required this.isDynamic,
    required this.canBePaid,
    required this.consultedAt,
    this.receiver,
    this.receiverIspb,
    this.receiverIspbName,
    this.amount,
    this.totalAmount,
    this.interest,
    this.fine,
    this.discount,
    this.dueDate,
    this.expiresAt,
  });

  /// Who receives the money.
  final BillParty? receiver;

  /// The receiving institution's ISPB.
  final String? receiverIspb;

  /// The receiving institution's name.
  final String? receiverIspbName;

  /// Whether the QR is dynamic (carries amount and due date).
  final bool isDynamic;

  /// Whether the provider accepts paying it right now.
  final bool canBePaid;

  /// The face amount.
  final double? amount;

  /// The amount with interest/fine/discount applied.
  final double? totalAmount;

  /// Interest accrued.
  final double? interest;

  /// Fine accrued.
  final double? fine;

  /// Discount applied.
  final double? discount;

  /// The due date, on dynamic QRs.
  final DateTime? dueDate;

  /// When the QR stops being payable.
  final DateTime? expiresAt;

  /// When this snapshot was taken.
  final DateTime consultedAt;
}

/// What the AI read off the document and the e-mail body.
///
/// Enrichment and cross-checking only — the official lookup decides what is
/// paid. Null fields mean the reader saw nothing, honestly.
class BillReading {
  /// Creates the reading record.
  const BillReading({
    required this.readAt,
    this.payerName,
    this.payerTaxId,
    this.payeeName,
    this.payeeTaxId,
    this.accountReference,
    this.amount,
    this.dueDate,
    this.billingPeriod,
    this.competenceYear,
    this.competenceMonth,
    this.description,
  });

  /// The payer's name as printed.
  final String? payerName;

  /// The payer's formatted CPF/CNPJ, DV-checked by the server.
  final String? payerTaxId;

  /// The beneficiary's name as printed.
  final String? payeeName;

  /// The beneficiary's formatted CPF/CNPJ, DV-checked by the server.
  final String? payeeTaxId;

  /// Installation, enrollment, unit or contract reference.
  final String? accountReference;

  /// The printed amount — cross-check only, never what is paid.
  final double? amount;

  /// The printed due date.
  final DateTime? dueDate;

  /// The billing period as printed ("07/2026", "julho/2026").
  final String? billingPeriod;

  /// The normalized competence year, when the period was parseable.
  final int? competenceYear;

  /// The normalized competence month, when the period was parseable.
  final int? competenceMonth;

  /// A short description of what the bill is about.
  final String? description;

  /// When the reading happened.
  final DateTime readAt;

  /// The competence as "mês/ano", falling back to the printed text.
  String? get competenceLabel {
    final year = competenceYear;
    final month = competenceMonth;
    if (year == null || month == null) return billingPeriod;
    const months = [
      'janeiro', 'fevereiro', 'março', 'abril', 'maio', 'junho',
      'julho', 'agosto', 'setembro', 'outubro', 'novembro', 'dezembro',
    ];
    return '${months[month - 1]}/$year';
  }
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
    this.reading,
    this.readingStatus = ReadingStatuses.notApplicable,
    this.riskLevel,
    this.bankSlipLookup,
    this.pixLookup,
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

  /// What the AI read off the document and the e-mail, once extracted.
  final BillReading? reading;

  /// One of [ReadingStatuses] — where the AI analysis stands.
  ///
  /// Read it before concluding anything from a null [reading]: absent and
  /// still-queued look identical otherwise, and the check `DocumentConsistency`
  /// reports "não se aplica" in both cases.
  final String readingStatus;

  /// Whether the AI analysis is still owed by the queue.
  bool get isReadingQueued => readingStatus == ReadingStatuses.queued;

  /// Whether the AI analysis gave up and can be asked for again.
  bool get isReadingUnavailable => readingStatus == ReadingStatuses.unavailable;

  /// `Safe`, `Attention` or `Danger` — null before the first validation.
  final String? riskLevel;

  /// The official bank-slip lookup, once it resolved.
  final BankSlipLookup? bankSlipLookup;

  /// The official Pix decode, once it resolved.
  final PixLookup? pixLookup;

  /// Whether approving needs the explicit risk acknowledgment (ADR-015) —
  /// true for Perigo and Extremo Perigo.
  bool get requiresRiskAcknowledgement =>
      RiskLevels.requiresAcknowledgement(riskLevel);

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
