/// A folder watched inside a capture source.
class MonitoredFolder {
  /// Creates the folder record.
  const MonitoredFolder({
    required this.id,
    required this.hasSyncCursor,
    this.path,
    this.lastSyncAt,
    this.lastSyncError,
  });

  /// The folder's id.
  final String id;

  /// The folder path, or `null` for the inbox.
  final String? path;

  /// Whether an incremental cursor exists. The cursor itself never leaves
  /// the server.
  final bool hasSyncCursor;

  /// When this folder last synced.
  final DateTime? lastSyncAt;

  /// The fine-grained diagnosis of this folder's last failure.
  final String? lastSyncError;

  /// The label to show for the folder.
  String get label => path ?? 'Caixa de entrada';
}

/// A connected mailbox (or, in the future, a portal).
class CaptureSource {
  /// Creates the source record.
  const CaptureSource({
    required this.id,
    required this.kind,
    required this.displayName,
    required this.address,
    required this.folders,
    required this.hasCredential,
    required this.isEnabled,
    required this.createdAt,
    this.lastSyncAt,
    this.lastSyncError,
  });

  /// The most folders a source may watch — each one costs a provider call
  /// per sync cycle.
  static const int maxFolders = 20;

  /// The source's id.
  final String id;

  /// The provider kind (e.g. `MicrosoftGraphMailbox`).
  final String kind;

  /// The name the tenant gave the source.
  final String displayName;

  /// The mailbox address.
  final String address;

  /// The watched folders. Never empty.
  final List<MonitoredFolder> folders;

  /// Whether a credential is stored. The credential itself never leaves the
  /// server (ADR-009) — replacing is the only write.
  final bool hasCredential;

  /// Whether the sync worker picks this source up.
  final bool isEnabled;

  /// When the source last synced.
  final DateTime? lastSyncAt;

  /// A summary of the last failure, for the list row.
  final String? lastSyncError;

  /// When the source was connected.
  final DateTime createdAt;

  /// Whether another folder can still be added.
  bool get canAddFolder => folders.length < maxFolders;

  /// Whether a folder can be removed — the server refuses removing the last
  /// one (`BLP.CPS18`): a source without folders would scan nothing and
  /// never warn.
  bool get canRemoveFolder => folders.length > 1;
}

/// One page of the capture source list.
class CaptureSourcePage {
  /// Creates a page with its [items] and the opaque [nextCursor].
  const CaptureSourcePage({required this.items, this.nextCursor});

  /// The sources of this page.
  final List<CaptureSource> items;

  /// The opaque cursor of the next page, or `null` on the last one.
  final String? nextCursor;

  /// Whether another page exists.
  bool get hasMore => nextCursor != null;
}

/// The outcome of a manual sync.
class SyncOutcome {
  /// Creates the outcome record.
  const SyncOutcome({
    required this.id,
    required this.status,
    required this.ingestedItems,
    required this.skippedAsAlreadyIngested,
  });

  /// The source's id.
  final String id;

  /// One of `SyncStatuses`.
  final String status;

  /// How many items entered on this pass.
  final int ingestedItems;

  /// How many were already known — the dedup working, not a problem.
  final int skippedAsAlreadyIngested;
}
