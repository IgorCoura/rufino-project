import 'bill_payment_enums.dart';
import 'check_translations.dart';

/// One of the twelve verifications recorded on a bill.
class BillCheck {
  /// Creates the check record.
  const BillCheck({
    required this.type,
    required this.outcome,
    required this.severity,
    required this.isBlockingFailure,
    required this.evaluatedAt,
    this.reasonCode,
    this.evidence,
  });

  /// One of [CheckTypes].
  final String type;

  /// One of [CheckOutcomes].
  final String outcome;

  /// One of [CheckSeverities] — travels per check because three advisory
  /// checks turn blocking in specific situations.
  final String severity;

  /// The stable reason code (`CheckReasons` on the server), when there is
  /// something to explain.
  final String? reasonCode;

  /// The human-readable evidence the server recorded.
  final String? evidence;

  /// Whether this check alone reproves the bill.
  final bool isBlockingFailure;

  /// When the check ran.
  final DateTime evaluatedAt;

  /// The label of the check's type.
  String get typeLabel => CheckTypes.label(type);

  /// Whether this check deserves the approver's eyes.
  bool get requiresAttention => CheckOutcomes.requiresAttention(outcome);

  /// The message to show for this check's reason.
  ///
  /// Translates the [reasonCode] — the contract — and falls back to the
  /// server's [evidence] when the code is unknown or absent. Returns `null`
  /// for a clean pass with nothing to explain.
  String? get reasonMessage => checkReasonMessage(reasonCode) ?? evidence;
}
