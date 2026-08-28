import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:rufino_core/rufino_core.dart';
import 'package:uuid/uuid.dart';

import '../domain/captured_message.dart';
import '../domain/captured_message_repository.dart';

const _uuid = Uuid();

/// Maps the capture log read model into domain entities.
abstract final class CapturedMessageMapper {
  /// Builds a [CapturedMessage] from the API's JSON.
  static CapturedMessage fromJson(Map<String, dynamic> json) {
    return CapturedMessage(
      id: json['id'] as String,
      sourceId: json['sourceId'] as String,
      sender: json['sender'] as String,
      subject: json['subject'] as String?,
      receivedAt: DateTime.parse(json['receivedAt'] as String),
      firstSeenAt: DateTime.parse(json['firstSeenAt'] as String),
      processedAt: json['processedAt'] == null
          ? null
          : DateTime.parse(json['processedAt'] as String),
      outcome: json['outcome'] as String,
      artifactCount: json['artifactCount'] as int? ?? 0,
      canRecapture: json['canRecapture'] as bool? ?? false,
      artifacts: (json['artifacts'] as List<dynamic>? ?? const [])
          .map((e) => artifactFromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  /// Builds one attachment outcome from the API's JSON.
  static CapturedArtifactOutcome artifactFromJson(Map<String, dynamic> json) {
    return CapturedArtifactOutcome(
      fileName: json['fileName'] as String?,
      contentType: json['contentType'] as String?,
      outcome: json['outcome'] as String,
      reason: json['reason'] as String?,
      captureItemId: json['captureItemId'] as String?,
      billId: json['billId'] as String?,
      decidedAt: json['decidedAt'] == null
          ? null
          : DateTime.parse(json['decidedAt'] as String),
    );
  }

  /// Builds a [CapturedMessagePage] from the API's JSON.
  static CapturedMessagePage pageFromJson(Map<String, dynamic> json) {
    return CapturedMessagePage(
      items: (json['items'] as List<dynamic>? ?? const [])
          .map((e) => fromJson(e as Map<String, dynamic>))
          .toList(),
      nextCursor: json['nextCursor'] as String?,
    );
  }

  /// Builds the retention policy from the API's JSON.
  static CaptureRetentionPolicy retentionFromJson(Map<String, dynamic> json) {
    return CaptureRetentionPolicy(
      isEnabled: json['isEnabled'] as bool? ?? false,
      windowDays: json['windowDays'] as int? ?? 90,
      availableWindowDays:
          (json['availableWindowDays'] as List<dynamic>? ?? const [])
              .map((e) => e as int)
              .toList(),
    );
  }
}

/// HTTP client for the capture log endpoints.
class CapturedMessageApiService {
  /// Creates the service against [baseUrl].
  CapturedMessageApiService({
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

  /// Lists the e-mails read, newest first.
  Future<CapturedMessagePage> listMessages({
    CapturedMessageFilter filter = const CapturedMessageFilter(),
    String? cursor,
    int limit = 50,
  }) async {
    final search = filter.search?.trim();

    final response = await client.get(
      _uri('/captured-messages', {
        if (filter.outcome != null) 'outcome': filter.outcome,
        if (filter.sourceId != null) 'sourceId': filter.sourceId,
        if (filter.from != null) 'from': filter.from!.toIso8601String(),
        if (filter.to != null) 'to': filter.to!.toIso8601String(),
        if (search != null && search.isNotEmpty) 'search': search,
        if (cursor != null) 'cursor': cursor,
        'limit': limit,
      }),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return CapturedMessageMapper.pageFromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Returns when the mailbox was last read.
  Future<CaptureSyncStatus> getSyncStatus() async {
    final response = await client.get(
      _uri('/captured-messages/sync-status'),
      headers: await _headers(),
    );
    checkApiStatus(response);
    final body = jsonDecode(response.body) as Map<String, dynamic>;
    return CaptureSyncStatus(
      lastSyncAt: body['lastSyncAt'] == null
          ? null
          : DateTime.parse(body['lastSyncAt'] as String),
      sourceCount: body['sourceCount'] as int? ?? 0,
    );
  }

  /// Wipes and re-ingests one e-mail.
  Future<RecaptureOutcome> recapture(String id) async {
    final response = await client.post(
      _uri('/captured-messages/$id/recapture'),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
    final body = jsonDecode(response.body) as Map<String, dynamic>;
    return RecaptureOutcome(
      id: body['id'] as String,
      itemsRemoved: body['itemsRemoved'] as int? ?? 0,
      artifactsIngested: body['artifactsIngested'] as int? ?? 0,
    );
  }

  /// Returns the retention window in force.
  Future<CaptureRetentionPolicy> getRetentionPolicy() async {
    final response = await client.get(
      _uri('/capture-retention'),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return CapturedMessageMapper.retentionFromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Turns the purge on or off and picks the window.
  Future<CaptureRetentionPolicy> configureRetention({
    required bool isEnabled,
    required int windowDays,
  }) async {
    final response = await client.put(
      _uri('/capture-retention'),
      headers: await _headers(write: true),
      body: jsonEncode({'isEnabled': isEnabled, 'windowDays': windowDays}),
    );
    checkApiStatus(response);

    // A resposta do PUT confirma o que foi aceito, mas não traz a faixa
    // oferecida — quem a conhece é o GET, e inventá-la aqui seria a tela
    // manter uma segunda lista que envelhece sozinha.
    final body = jsonDecode(response.body) as Map<String, dynamic>;
    return CaptureRetentionPolicy(
      isEnabled: body['isEnabled'] as bool? ?? isEnabled,
      windowDays: body['windowDays'] as int? ?? windowDays,
      availableWindowDays: const [],
    );
  }
}
