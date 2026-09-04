import 'package:rufino_core/rufino_core.dart';

import 'bill.dart';
import 'bill_detail.dart';
import 'captured_artifact.dart';
import 'email_message.dart';
import 'schedule_preview.dart';

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

  /// Imports a bill from a digitable line, a Pix payload, a file, or any
  /// combination of the three.
  ///
  /// [documentBytes] carry the bill's own file — a PDF or a picture of it. The
  /// server reads the instruments out of it and keeps it as the paper the
  /// approver checks against; when it carries nothing readable and no digits
  /// came along, the import is refused. The three document parameters travel
  /// as primitives, like [CaptureItemRepository.attachArtifact] — the picker's
  /// record is a UI type, and the domain does not depend on the UI.
  Future<Result<ImportOutcome>> importBill({
    String? digitableLine,
    String? pixPayload,
    List<int>? documentBytes,
    String? documentFileName,
    String? documentContentType,
  });

  /// Re-runs the official lookup and the twelve checks.
  Future<Result<ValidationRunOutcome>> revalidateBill(String id);

  /// Authorizes the payment for [scheduleFor], with an optional [note].
  ///
  /// [acknowledgeRisk] must be `true` for a bill classified as Danger — the
  /// explicit acceptance the audit trail records (ADR-015).
  /// [acknowledgeImmediateExecution] must be `true` for an OVERDUE bill: the
  /// provider processes it at once, with no reaction window, and the server
  /// refuses the approval without the explicit consent (ADR-017).
  Future<Result<void>> approveBill(
    String id, {
    required DateTime scheduleFor,
    String? note,
    bool acknowledgeRisk = false,
    bool acknowledgeImmediateExecution = false,
  });

  /// Asks the server when a payment authorized for [date] would actually
  /// execute — the ADR-017 policy (lead time, banking calendar) computed
  /// where it lives.
  ///
  /// Purely informative: callers must keep working when this fails — the
  /// approval never waits on it.
  Future<Result<SchedulePreview>> previewSchedule(String id, DateTime date);

  /// Returns a FAILED bill to the decision queue — the new try is a new
  /// approval and a new payment order.
  Future<Result<void>> reopenBill(String id);

  /// Refuses the bill. The [reason] is mandatory — a refusal without one is
  /// an audit hole.
  Future<Result<void>> denyBill(String id, String reason);

  /// Removes the bill from the flow.
  Future<Result<void>> cancelBill(String id, String reason);

  /// Downloads the original document the bill came from.
  ///
  /// The screen offers this beside the checks so the approver can read the
  /// paper the verifications talk about. It is not the digitable line: those
  /// digits never leave the server, and whoever has them, pays.
  Future<Result<CapturedArtifact>> getArtifact(String id);

  /// Fetches the e-mail that brought this bill — title, sender and body.
  ///
  /// Only applies to bills born from a mailbox; a manual import has no
  /// e-mail behind it and the server answers 404.
  Future<Result<EmailMessage>> getEmail(String id);
}
