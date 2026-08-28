import 'bill_payment_enums.dart';

/// One e-mail the capture read, and what it decided about each attachment.
///
/// The registry exists because the quarantine cannot answer for the e-mail that
/// **did not** become an item: what the triage discards leaves no row behind, on
/// purpose. Only metadata lives here — the discarded file is still never kept.
class CapturedMessage {
  /// Creates the record.
  const CapturedMessage({
    required this.id,
    required this.sourceId,
    required this.sender,
    required this.receivedAt,
    required this.firstSeenAt,
    required this.outcome,
    required this.artifactCount,
    required this.canRecapture,
    required this.artifacts,
    this.subject,
    this.processedAt,
  });

  /// The record's id.
  final String id;

  /// The mailbox that brought it.
  final String sourceId;

  /// Who sent it.
  final String sender;

  /// The subject line, when the message had one.
  final String? subject;

  /// When the e-mail reached the mailbox.
  final DateTime receivedAt;

  /// When the sweep found it.
  final DateTime firstSeenAt;

  /// When the last attachment had its outcome decided.
  final DateTime? processedAt;

  /// The dominant outcome — what the row shows without expanding.
  final String outcome;

  /// How many attachments the e-mail carried.
  final int artifactCount;

  /// Whether it can be pulled in again from scratch.
  final bool canRecapture;

  /// One entry per attachment.
  final List<CapturedArtifactOutcome> artifacts;

  /// Whether the processing already ran.
  bool get isProcessed => processedAt != null;

  /// The bill this e-mail produced, when it produced one.
  String? get billId {
    for (final artifact in artifacts) {
      if (artifact.billId != null) return artifact.billId;
    }
    return null;
  }

  /// The quarantine item to open, when one is still there.
  String? get captureItemId {
    for (final artifact in artifacts) {
      if (artifact.captureItemId != null) return artifact.captureItemId;
    }
    return null;
  }
}

/// What the capture decided about one attachment.
class CapturedArtifactOutcome {
  /// Creates the entry.
  const CapturedArtifactOutcome({
    required this.outcome,
    this.fileName,
    this.contentType,
    this.reason,
    this.captureItemId,
    this.billId,
    this.decidedAt,
  });

  /// The attachment's name, when the provider informed one.
  final String? fileName;

  /// The declared media type.
  final String? contentType;

  /// One of [ArtifactOutcomes].
  final String outcome;

  /// The stable reason code, when the outcome has one.
  final String? reason;

  /// The quarantine item, when it still exists. Null once discarded.
  final String? captureItemId;

  /// The bill this attachment became.
  final String? billId;

  /// When the outcome was decided.
  final DateTime? decidedAt;
}

/// A cursor page of the capture log.
class CapturedMessagePage {
  /// Creates the page.
  const CapturedMessagePage({required this.items, this.nextCursor});

  /// The rows of this page.
  final List<CapturedMessage> items;

  /// The cursor of the next page, or `null` when this was the last.
  final String? nextCursor;
}

/// When the mailbox was last read — the screen's header.
class CaptureSyncStatus {
  /// Creates the status.
  const CaptureSyncStatus({required this.sourceCount, this.lastSyncAt});

  /// The most recent sweep across every mailbox. Null when none ran yet.
  final DateTime? lastSyncAt;

  /// How many mailboxes feed the capture.
  final int sourceCount;
}

/// How long the log of discarded e-mails is kept.
class CaptureRetentionPolicy {
  /// Creates the policy.
  const CaptureRetentionPolicy({
    required this.isEnabled,
    required this.windowDays,
    required this.availableWindowDays,
  });

  /// Whether the purge runs. `false` is the initial state, on purpose.
  final bool isEnabled;

  /// The window in force. It exists even while the purge is off.
  final int windowDays;

  /// The windows the screen offers — served by the server so the app does not
  /// keep its own copy of the list.
  final List<int> availableWindowDays;
}
