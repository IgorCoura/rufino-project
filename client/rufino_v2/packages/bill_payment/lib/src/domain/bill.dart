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

/// Where a bill came from.
class BillOrigin {
  /// Creates the origin record.
  const BillOrigin({
    required this.sourceKind,
    required this.receivedAt,
    this.sourceId,
    this.senderAddress,
    this.hasArtifact = false,
  });

  /// One of [BillSourceKinds].
  final String sourceKind;

  /// The capture source that brought it, when there is one.
  final String? sourceId;

  /// The sender's e-mail address, when it came from a mailbox.
  final String? senderAddress;

  /// When the document reached the source.
  final DateTime receivedAt;

  /// Whether the original document can be fetched for this bill.
  ///
  /// `false` is a normal state, not a failure: a bill imported by hand was
  /// born with the digits only, and there is no file to show.
  final bool hasArtifact;
}

/// A bill as the list endpoint projects it.
///
/// The API never returns the digitable line nor the Pix payload — whoever
/// has them can pay — so this entity has no place for them, on purpose.
class Bill {
  /// Creates the list projection of a bill.
  const Bill({
    required this.id,
    required this.status,
    required this.kind,
    required this.rail,
    required this.origin,
    required this.createdAt,
    this.beneficiary,
    this.amount,
    this.dueDate,
    this.bankCode,
    this.riskLevel,
    this.readingStatus = ReadingStatuses.notApplicable,
  });

  /// The bill's id.
  final String id;

  /// One of [BillStatuses].
  final String status;

  /// One of [BillKinds].
  final String kind;

  /// One of [PaymentRails].
  final String rail;

  /// The beneficiary, once the official lookup resolved it.
  final BillParty? beneficiary;

  /// The payable amount from the official lookup, falling back to the
  /// amount declared on the instrument.
  final double? amount;

  /// The consolidated due date: official lookup first, then the date
  /// embedded in the digitable line.
  final DateTime? dueDate;

  /// The receiving bank's COMPE code — always null for utility documents.
  final String? bankCode;

  /// `Safe`, `Attention` or `Danger` — null before the first validation.
  final String? riskLevel;

  /// Where the bill came from.
  final BillOrigin origin;

  /// When the bill entered the system.
  final DateTime createdAt;

  /// One of [ReadingStatuses] — where the AI analysis stands.
  ///
  /// The bill NEVER waits for the AI: it goes to approval with what the
  /// deterministic funnel proved. What waits is the analysis, and saying so is
  /// what stops the screen from lying — a bill with no reading used to be
  /// indistinguishable from one whose document has nothing to read.
  final String readingStatus;

  /// Whether the AI analysis is still queued.
  bool get isReadingQueued => readingStatus == ReadingStatuses.queued;

  /// Whether no further mutation applies.
  bool get isTerminal => BillStatuses.isTerminal(status);

  /// Whether the bill sits in the approval queue.
  bool get isAwaitingApproval => status == BillStatuses.awaitingApproval;
}

/// One page of the bill list.
class BillPage {
  /// Creates a page with its [items] and the opaque [nextCursor].
  const BillPage({required this.items, this.nextCursor});

  /// The bills of this page.
  final List<Bill> items;

  /// The opaque cursor of the next page, or `null` on the last one.
  final String? nextCursor;

  /// Whether another page exists.
  bool get hasMore => nextCursor != null;
}
