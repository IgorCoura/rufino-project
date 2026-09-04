import 'package:rufino_core/rufino_core.dart';

import 'captured_artifact.dart';
import 'payment_order.dart';

/// Contract for reading and acting on payment orders (phase 3).
abstract class PaymentRepository {
  /// Returns the (most recent) order behind a bill, or `null` while the
  /// approval has not produced one yet — a normal state, not a failure.
  Future<Result<PaymentOrder?>> getForBill(String billId);

  /// Cancels the order — the reaction window the 24h policy exists for.
  ///
  /// After submission the provider decides whether it still can; its refusal
  /// arrives as a domain message through the generic path.
  Future<Result<void>> cancel(String orderId);

  /// Confirms an immediate (overdue) payment a person still needs to allow.
  Future<Result<void>> confirmImmediate(String orderId);

  /// Downloads the stored payment receipt of the bill's order.
  ///
  /// The file is the evidence — the provider's URL never reaches the client.
  Future<Result<CapturedArtifact>> getReceiptForBill(String billId);
}
