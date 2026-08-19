import 'bill_payment_enums.dart';

/// A captured artifact, projected by the visibility rules of the server.
///
/// The financial fields ([storageKey], [sourceUrl], [contentHash], [billId],
/// [unlockedBy]) only come when the status exposes financial detail — the
/// server decides, and this entity just renders what arrived. Never infer
/// meaning from their absence.
class CaptureItem {
  /// Creates the item record.
  const CaptureItem({
    required this.id,
    required this.sourceId,
    required this.receivedAt,
    required this.status,
    this.sender,
    this.subject,
    this.reason,
    this.routingConfidence,
    this.extractionMethod,
    this.unlockedBy,
    this.storageKey,
    this.sourceUrl,
    this.contentHash,
    this.billId,
    this.claimedBy,
    this.claimedAt,
  });

  /// The item's id.
  final String id;

  /// The capture source that ingested it.
  final String sourceId;

  /// The sender's address.
  final String? sender;

  /// The message subject.
  final String? subject;

  /// When the message reached the mailbox.
  final DateTime receivedAt;

  /// One of [CaptureItemStatuses].
  final String status;

  /// Why the item ended where it is.
  final String? reason;

  /// One of [RoutingConfidences], once routed.
  final String? routingConfidence;

  /// One of [ExtractionMethods], once extracted.
  final String? extractionMethod;

  /// The label of the field that derived the opening password.
  final String? unlockedBy;

  /// The stored artifact's key. Only exposed by the server when allowed.
  final String? storageKey;

  /// The document's source URL — a bearer credential; handle with care.
  final String? sourceUrl;

  /// The artifact's content hash.
  final String? contentHash;

  /// The bill this item became, once promoted.
  final String? billId;

  /// Who claimed the item, when someone did.
  final String? claimedBy;

  /// When the claim happened.
  final DateTime? claimedAt;

  /// Whether the claim action applies.
  bool get acceptsClaim => CaptureItemStatuses.acceptsClaim(status);

  /// Whether the reprocess action applies.
  bool get acceptsReprocess => CaptureItemStatuses.acceptsReprocess(status);

  /// Whether the item points at a bill this tenant can open.
  bool get hasBill => billId != null;
}

/// One page of the capture item list.
class CaptureItemPage {
  /// Creates a page with its [items] and the opaque [nextCursor].
  const CaptureItemPage({required this.items, this.nextCursor});

  /// The items of this page.
  final List<CaptureItem> items;

  /// The opaque cursor of the next page, or `null` on the last one.
  final String? nextCursor;

  /// Whether another page exists.
  bool get hasMore => nextCursor != null;
}
