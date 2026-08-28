import 'package:rufino_core/rufino_core.dart';

import 'capture_source.dart';

/// The Entra ID app registration credential, as the connect form collects it.
///
/// Travels to the API as one opaque JSON string — the server stores it in
/// the vault and never returns it.
class GraphCredentialInput {
  /// Creates the credential input.
  const GraphCredentialInput({
    required this.directoryId,
    required this.clientId,
    required this.clientSecret,
  });

  /// The customer's Entra ID tenant (directory) id.
  final String directoryId;

  /// The registered application's id.
  final String clientId;

  /// The application's client secret.
  final String clientSecret;
}

/// What connecting a source returned.
class ConnectOutcome {
  /// Creates the outcome record.
  const ConnectOutcome({
    required this.id,
    required this.alreadyMonitoredByAnotherAccount,
  });

  /// The new source's id.
  final String id;

  /// Whether another account already monitors this mailbox — a boolean and
  /// nothing more, by design (ADR-008).
  final bool alreadyMonitoredByAnotherAccount;
}

/// What a full rescan returned.
class RescanOutcome {
  /// Creates the outcome record.
  const RescanOutcome({required this.id, required this.foldersReset});

  /// The source's id.
  final String id;

  /// How many folder cursors were discarded.
  final int foldersReset;
}

/// Contract for reading and maintaining capture sources.
abstract class CaptureSourceRepository {
  /// Lists sources, one cursor page at a time.
  Future<Result<CaptureSourcePage>> listSources({
    String? cursor,
    int limit = 50,
  });

  /// Returns one source.
  Future<Result<CaptureSource>> getSource(String id);

  /// Connects a mailbox. The access proof runs on the server before the
  /// source exists — a refused credential creates nothing.
  Future<Result<ConnectOutcome>> connectSource({
    required String displayName,
    required String address,
    required GraphCredentialInput credential,
    String? folderPath,
    DateTime? captureSince,
  });

  /// Renames the source.
  Future<Result<void>> renameSource(String id, String displayName);

  /// Enables or disables the source.
  Future<Result<void>> setActivation(String id, {required bool isEnabled});

  /// Replaces the credential. The old one stays until the new one proves
  /// access.
  Future<Result<void>> replaceCredential(
    String id,
    GraphCredentialInput credential,
  );

  /// Triggers a sync and resolves to its outcome.
  Future<Result<SyncOutcome>> syncSource(String id);

  /// Adds a watched folder (`null` = the inbox).
  Future<Result<void>> addFolder(String id, String? folderPath);

  /// Removes a watched folder. The server refuses removing the last one.
  Future<Result<void>> removeFolder(String id, String? folderPath);

  /// Moves the capture's time floor. `null` returns the source to the whole
  /// mailbox.
  ///
  /// The server drops every folder cursor as part of this: the provider
  /// stores the query options inside the delta link, so a cursor obtained
  /// with the old date would keep filtering by it.
  Future<Result<void>> changeCaptureSince(String id, DateTime? captureSince);

  /// Discards every folder cursor so the next sweep rereads the whole
  /// mailbox. Does not duplicate — ingestion is idempotent.
  Future<Result<RescanOutcome>> rescanSource(String id);

  /// Disconnects the source.
  Future<Result<void>> disconnectSource(String id);
}
