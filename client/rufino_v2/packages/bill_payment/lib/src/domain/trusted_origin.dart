import 'bill_payment_enums.dart';

/// A sender the tenant declared trusted or blocked.
class TrustedOrigin {
  /// Creates the origin record.
  const TrustedOrigin({
    required this.id,
    required this.kind,
    required this.value,
    required this.decision,
    required this.decidedBy,
    required this.decidedAt,
    this.note,
  });

  /// The origin's id.
  final String id;

  /// One of [OriginKinds].
  final String kind;

  /// The normalized address or domain.
  final String value;

  /// One of [TrustDecisions].
  final String decision;

  /// The `sub` of whoever decided.
  final String decidedBy;

  /// When the decision happened.
  final DateTime decidedAt;

  /// A free note about the decision.
  final String? note;

  /// Whether bills from this origin are refused.
  bool get isBlocked => decision == TrustDecisions.blocked;
}

/// One page of the trusted origin list.
class TrustedOriginPage {
  /// Creates a page with its [items] and the opaque [nextCursor].
  const TrustedOriginPage({required this.items, this.nextCursor});

  /// The origins of this page.
  final List<TrustedOrigin> items;

  /// The opaque cursor of the next page, or `null` on the last one.
  final String? nextCursor;

  /// Whether another page exists.
  bool get hasMore => nextCursor != null;
}
