import 'package:rufino_core/rufino_core.dart';

import 'bill.dart';
import 'bill_detail.dart';

/// What importing a bill returned.
class ImportOutcome {
  /// Creates the outcome record.
  const ImportOutcome({
    required this.id,
    required this.kind,
    required this.rail,
  });

  /// The new bill's id.
  final String id;

  /// One of `BillKinds`, derived by the domain from the instrument.
  final String kind;

  /// One of `PaymentRails` — Pix wins when both instruments exist.
  final String rail;
}

/// What a validation run returned.
class ValidationRunOutcome {
  /// Creates the outcome record.
  const ValidationRunOutcome({
    required this.id,
    required this.status,
    required this.blockingFailures,
    required this.attentionItems,
  });

  /// The bill's id.
  final String id;

  /// The bill's status after the run.
  final String status;

  /// How many blocking checks failed.
  final int blockingFailures;

  /// How many checks deserve attention.
  final int attentionItems;
}

/// Contract for reading and deciding on bills.
abstract class BillRepository {
  /// Lists bills, one cursor page at a time, optionally filtered by
  /// [status] (server-side, case-insensitive; an unknown value returns the
  /// whole list).
  Future<Result<BillPage>> listBills({
    String? status,
    String? cursor,
    int limit = 50,
  });

  /// Returns one bill's list projection.
  Future<Result<Bill>> getBill(String id);

  /// Returns the detail the approval screen consumes.
  Future<Result<BillDetail>> getBillDetail(String id);

  /// Imports a bill from a digitable line, a Pix payload, or both.
  Future<Result<ImportOutcome>> importBill({
    String? digitableLine,
    String? pixPayload,
  });

  /// Re-runs the official lookup and the twelve checks.
  Future<Result<ValidationRunOutcome>> revalidateBill(String id);

  /// Authorizes the payment for [scheduleFor], with an optional [note].
  Future<Result<void>> approveBill(
    String id, {
    required DateTime scheduleFor,
    String? note,
  });

  /// Refuses the bill. The [reason] is mandatory — a refusal without one is
  /// an audit hole.
  Future<Result<void>> denyBill(String id, String reason);

  /// Removes the bill from the flow.
  Future<Result<void>> cancelBill(String id, String reason);
}
