/// The server's answer to "if I authorize for this date, when does the money
/// actually move?".
///
/// Mirrors `GET /bills/{id}/schedule-preview` — the same ADR-017 policy the
/// submission queue applies (24h lead, banking calendar, working-day slide),
/// computed by the server so the sheet never re-implements it. Purely
/// informative: the approval never depends on this being available.
class SchedulePreview {
  /// Creates the preview.
  const SchedulePreview({
    required this.requestedDate,
    required this.effectiveDate,
    required this.slid,
    required this.immediate,
  });

  /// The date the user asked to pay on.
  final DateTime requestedDate;

  /// The date the provider will actually execute the payment.
  final DateTime effectiveDate;

  /// Whether [effectiveDate] slid away from [requestedDate] (lead time,
  /// weekend or banking holiday).
  final bool slid;

  /// Whether the server classified this as an immediate execution — an
  /// overdue bill pays at once, with no reaction window (ADR-017).
  final bool immediate;
}
