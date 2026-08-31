import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:rufino_core/rufino_core.dart';
import 'package:uuid/uuid.dart';

import '../domain/capture_source.dart';
import '../domain/capture_source_repository.dart';

const _uuid = Uuid();

/// Maps the capture source read model into domain entities.
abstract final class CaptureSourceMapper {
  /// Builds a [CaptureSource] from the API's JSON.
  static CaptureSource fromJson(Map<String, dynamic> json) {
    return CaptureSource(
      id: json['id'] as String,
      kind: json['kind'] as String,
      displayName: json['displayName'] as String,
      address: json['address'] as String,
      folders: (json['folders'] as List<dynamic>? ?? const [])
          .map((e) => e as Map<String, dynamic>)
          .map(
            (e) => MonitoredFolder(
              id: e['id'] as String,
              path: e['path'] as String?,
              hasSyncCursor: e['hasSyncCursor'] as bool? ?? false,
              lastSyncAt: e['lastSyncAt'] == null
                  ? null
                  : DateTime.parse(e['lastSyncAt'] as String),
              lastSyncError: e['lastSyncError'] as String?,
            ),
          )
          .toList(),
      hasCredential: json['hasCredential'] as bool? ?? false,
      isEnabled: json['isEnabled'] as bool? ?? false,
      captureSince: parseCaptureSince(json['captureSince'] as String?),
      lastSyncAt: json['lastSyncAt'] == null
          ? null
          : DateTime.parse(json['lastSyncAt'] as String),
      lastSyncError: json['lastSyncError'] as String?,
      createdAt: DateTime.parse(json['createdAt'] as String),
    );
  }

  /// Reads the API's `date` (`yyyy-MM-dd`) into a local [DateTime].
  ///
  /// The wire format carries no time and no offset on purpose — the floor is
  /// a day the user picked on a calendar, and the conversion to an instant
  /// belongs to the server's provider adapter.
  static DateTime? parseCaptureSince(String? raw) =>
      raw == null || raw.isEmpty ? null : DateTime.parse(raw);

  /// Formats a [DateTime] as the API's `date`, dropping any time part.
  static String formatCaptureSince(DateTime date) {
    final year = date.year.toString().padLeft(4, '0');
    final month = date.month.toString().padLeft(2, '0');
    final day = date.day.toString().padLeft(2, '0');
    return '$year-$month-$day';
  }

  /// Builds a [CaptureSourcePage] from the API's JSON.
  static CaptureSourcePage pageFromJson(Map<String, dynamic> json) {
    return CaptureSourcePage(
      items: (json['items'] as List<dynamic>? ?? const [])
          .map((e) => fromJson(e as Map<String, dynamic>))
          .toList(),
      nextCursor: json['nextCursor'] as String?,
    );
  }
}

/// HTTP client for the capture source endpoints.
class CaptureSourceApiService {
  /// Creates the service against [baseUrl].
  CaptureSourceApiService({
    required this.client,
    required this.baseUrl,
    required this.getAuthHeader,
    required this.getTenantId,
  });

  /// The shared HTTP client.
  final http.Client client;

  /// Host (`host:port`) or full origin (`http://host:port`) of the service.
  final String baseUrl;

  /// Supplies the `Authorization` header value.
  final Future<String> Function() getAuthHeader;

  /// Supplies the selected tenant's id.
  final String Function() getTenantId;

  Uri _uri(String path, [Map<String, dynamic>? query]) {
    final full = '/api/v1/${getTenantId()}$path';
    final params = query?.map((k, v) => MapEntry(k, v.toString()));
    if (baseUrl.contains('://')) {
      final base = Uri.parse(baseUrl);
      return base.replace(path: full, queryParameters: params);
    }
    return Uri.https(baseUrl, full, params);
  }

  Future<Map<String, String>> _headers({bool write = false}) async {
    return {
      'Authorization': await getAuthHeader(),
      'Content-Type': 'application/json',
      if (write) 'x-requestid': _uuid.v4(),
    };
  }

  /// The opaque credential string the API stores in the vault.
  static String encodeCredential(GraphCredentialInput credential) => jsonEncode({
        'directoryId': credential.directoryId.trim(),
        'clientId': credential.clientId.trim(),
        'clientSecret': credential.clientSecret.trim(),
      });

  /// Lists sources, one cursor page at a time.
  Future<CaptureSourcePage> listSources({String? cursor, int limit = 50}) async {
    final response = await client.get(
      _uri('/capture-sources', {
        if (cursor != null) 'cursor': cursor,
        'limit': limit,
      }),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return CaptureSourceMapper.pageFromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Returns one source.
  Future<CaptureSource> getSource(String id) async {
    final response = await client.get(
      _uri('/capture-sources/$id'),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return CaptureSourceMapper.fromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Connects a mailbox.
  Future<ConnectOutcome> connectSource({
    required String displayName,
    required String address,
    required GraphCredentialInput credential,
    String? folderPath,
    DateTime? captureSince,
  }) async {
    final response = await client.post(
      _uri('/capture-sources'),
      headers: await _headers(write: true),
      body: jsonEncode({
        'kind': 'MicrosoftGraphMailbox',
        'displayName': displayName.trim(),
        'address': address.trim(),
        'credential': encodeCredential(credential),
        'folderPath': folderPath?.trim(),
        'captureSince': captureSince == null
            ? null
            : CaptureSourceMapper.formatCaptureSince(captureSince),
      }),
    );
    checkApiStatus(response);
    final body = jsonDecode(response.body) as Map<String, dynamic>;
    return ConnectOutcome(id: body['id'] as String);
  }

  /// Renames the source.
  Future<void> renameSource(String id, String displayName) async {
    final response = await client.put(
      _uri('/capture-sources/$id/name'),
      headers: await _headers(write: true),
      body: jsonEncode({'displayName': displayName.trim()}),
    );
    checkApiStatus(response);
  }

  /// Enables or disables the source.
  Future<void> setActivation(String id, {required bool isEnabled}) async {
    final response = await client.put(
      _uri('/capture-sources/$id/activation'),
      headers: await _headers(write: true),
      body: jsonEncode({'isEnabled': isEnabled}),
    );
    checkApiStatus(response);
  }

  /// Replaces the credential.
  Future<void> replaceCredential(
    String id,
    GraphCredentialInput credential,
  ) async {
    final response = await client.put(
      _uri('/capture-sources/$id/credential'),
      headers: await _headers(write: true),
      body: jsonEncode({'credential': encodeCredential(credential)}),
    );
    checkApiStatus(response);
  }

  /// Triggers a sync and returns its outcome.
  Future<SyncOutcome> syncSource(String id) async {
    final response = await client.post(
      _uri('/capture-sources/$id/sync'),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
    final body = jsonDecode(response.body) as Map<String, dynamic>;
    return SyncOutcome(
      id: body['id'] as String,
      status: body['status'] as String,
      ingestedItems: body['ingestedItems'] as int? ?? 0,
      skippedAsAlreadyIngested:
          body['skippedAsAlreadyIngested'] as int? ?? 0,
    );
  }

  /// Moves the capture's time floor. `null` returns it to the whole mailbox.
  ///
  /// The server drops every folder cursor as part of this — the provider
  /// stores the filter inside the delta link, so the new date would mean
  /// nothing over an old cursor.
  Future<void> changeCaptureSince(String id, DateTime? captureSince) async {
    final response = await client.put(
      _uri('/capture-sources/$id/capture-since'),
      headers: await _headers(write: true),
      body: jsonEncode({
        'captureSince': captureSince == null
            ? null
            : CaptureSourceMapper.formatCaptureSince(captureSince),
      }),
    );
    checkApiStatus(response);
  }

  /// Adds a watched folder. The path goes in the body — a folder name may
  /// contain `/`.
  Future<void> addFolder(String id, String? folderPath) async {
    final response = await client.post(
      _uri('/capture-sources/$id/folders'),
      headers: await _headers(write: true),
      body: jsonEncode({'folderPath': folderPath?.trim()}),
    );
    checkApiStatus(response);
  }

  /// Removes a watched folder. Here the path goes in the query string.
  Future<void> removeFolder(String id, String? folderPath) async {
    final response = await client.delete(
      _uri(
        '/capture-sources/$id/folders',
        folderPath == null ? null : {'folderPath': folderPath},
      ),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
  }

  /// Discards every folder cursor for a full reread.
  Future<RescanOutcome> rescanSource(String id) async {
    final response = await client.post(
      _uri('/capture-sources/$id/rescan'),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
    final body = jsonDecode(response.body) as Map<String, dynamic>;
    return RescanOutcome(
      id: body['id'] as String,
      foldersReset: body['foldersReset'] as int? ?? 0,
    );
  }

  /// Disconnects the source.
  Future<void> disconnectSource(String id) async {
    final response = await client.delete(
      _uri('/capture-sources/$id'),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
  }
}
