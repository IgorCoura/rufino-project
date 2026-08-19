import 'bill_payment_enums.dart';

/// Where a bill came from.
class BillOrigin {
  /// Creates the origin record.
  const BillOrigin({
    required this.sourceKind,
    required this.receivedAt,
    this.sourceId,
    this.senderAddress,
  });

  /// One of [BillSourceKinds].
  final String sourceKind;

  /// The capture source that brought it, when there is one.
  final String? sourceId;

  /// The sender's e-mail address, when it came from a mailbox.
  final String? senderAddress;

  /// When the document reached the source.
  final DateTime receivedAt;
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
    this.amount,
    this.dueDate,
    this.bankCode,
  });

  /// The bill's id.
  final String id;

  /// One of [BillStatuses].
  final String status;

  /// One of [BillKinds].
  final String kind;

  /// One of [PaymentRails].
  final String rail;

  /// The declared amount, when the instrument carries one.
  final double? amount;

  /// The due date, when the instrument carries one.
  final DateTime? dueDate;

  /// The receiving bank's COMPE code — always null for utility documents.
  final String? bankCode;

  /// Where the bill came from.
  final BillOrigin origin;

  /// When the bill entered the system.
  final DateTime createdAt;

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
