import 'package:rufino_core/rufino_core.dart';

import 'captured_message.dart';

/// What recapturing an e-mail returned.
class RecaptureOutcome {
  /// Creates the outcome record.
  const RecaptureOutcome({
    required this.id,
    required this.artifactsReingested,
    required this.billsCancelled,
    required this.previouslyDeniedBillIds,
  });

  /// The record's id.
  final String id;

  /// How many attachments came back in for a fresh pass.
  final int artifactsReingested;

  /// How many pending bills were cancelled so the new pass can recreate them.
  final int billsCancelled;

  /// Bills born from this e-mail that had already been denied once — the new
  /// pass brings them back for decision, and the user must be warned.
  final List<String> previouslyDeniedBillIds;
}

/// The filters the capture log screen sends to the server.
class CapturedMessageFilter {
  /// Creates the filter set.
  const CapturedMessageFilter({
    this.outcome,
    this.sourceId,
    this.from,
    this.to,
    this.search,
  });

  /// One of `ArtifactOutcomes`, or `null` for everything.
  final String? outcome;

  /// One mailbox, or `null` for all of them.
  final String? sourceId;

  /// Start of the received-at range.
  final DateTime? from;

  /// End of the received-at range.
  final DateTime? to;

  /// A slice of the sender or the subject.
  final String? search;

  /// Whether anything narrows the list right now.
  bool get isEmpty =>
      outcome == null &&
      sourceId == null &&
      from == null &&
      to == null &&
      (search == null || search!.trim().isEmpty);
}

/// Contract for reading the capture log and acting on it.
abstract class CapturedMessageRepository {
  /// Lists the e-mails read, newest first, one cursor page at a time.
  Future<Result<CapturedMessagePage>> listMessages({
    CapturedMessageFilter filter = const CapturedMessageFilter(),
    String? cursor,
    int limit = 50,
  });

  /// When the mailbox was last read.
  Future<Result<CaptureSyncStatus>> getSyncStatus();

  /// Wipes what the capture produced for one e-mail and pulls it in again.
  ///
  /// Not the same as reprocessing an item: reprocessing hands the cascade an
  /// item that still exists, with the same ids — which recovers nothing when
  /// the storage address is the thing that died.
  Future<Result<RecaptureOutcome>> recapture(String id);

  /// The retention window in force.
  Future<Result<CaptureRetentionPolicy>> getRetentionPolicy();

  /// Turns the purge on or off and picks the window.
  Future<Result<CaptureRetentionPolicy>> configureRetention({
    required bool isEnabled,
    required int windowDays,
  });
}
