import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:rufino_core/rufino_core.dart';
import 'package:uuid/uuid.dart';

import '../domain/capture_item.dart';
import '../domain/capture_item_repository.dart';

const _uuid = Uuid();

/// Maps the capture item read model into domain entities.
///
/// The financial fields arrive `null` unless the status exposes them — the
/// server decides, this mapper just carries what came.
abstract final class CaptureItemMapper {
  /// Builds a [CaptureItem] from the API's JSON.
  static CaptureItem fromJson(Map<String, dynamic> json) {
    return CaptureItem(
      id: json['id'] as String,
      sourceId: json['sourceId'] as String,
      sender: json['sender'] as String?,
      subject: json['subject'] as String?,
      receivedAt: DateTime.parse(json['receivedAt'] as String),
      status: json['status'] as String,
      reason: json['reason'] as String?,
      routingConfidence: json['routingConfidence'] as String?,
      extractionMethod: json['extractionMethod'] as String?,
      unlockedBy: json['unlockedBy'] as String?,
      storageKey: json['storageKey'] as String?,
      sourceUrl: json['sourceUrl'] as String?,
      contentHash: json['contentHash'] as String?,
      billId: json['billId'] as String?,
      claimedBy: json['claimedBy'] as String?,
      claimedAt: json['claimedAt'] == null
          ? null
          : DateTime.parse(json['claimedAt'] as String),
    );
  }

  /// Builds a [CaptureItemPage] from the API's JSON.
  static CaptureItemPage pageFromJson(Map<String, dynamic> json) {
    return CaptureItemPage(
      items: (json['items'] as List<dynamic>? ?? const [])
          .map((e) => fromJson(e as Map<String, dynamic>))
          .toList(),
      nextCursor: json['nextCursor'] as String?,
    );
  }
}

/// HTTP client for the capture item endpoints.
class CaptureItemApiService {
  /// Creates the service against [baseUrl].
  CaptureItemApiService({
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

  /// Lists items, optionally filtered by status.
  Future<CaptureItemPage> listItems({
    String? status,
    String? cursor,
    int limit = 50,
  }) async {
    final response = await client.get(
      _uri('/capture-items', {
        if (status != null) 'status': status,
        if (cursor != null) 'cursor': cursor,
        'limit': limit,
      }),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return CaptureItemMapper.pageFromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Returns one item.
  Future<CaptureItem> getItem(String id) async {
    final response = await client.get(
      _uri('/capture-items/$id'),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return CaptureItemMapper.fromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Sends the item back to `Received` for the worker to re-evaluate.
  Future<void> reprocessItem(String id) async {
    final response = await client.post(
      _uri('/capture-items/$id/reprocess'),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
  }

  /// Claims an unrouted item — it becomes this tenant's bill.
  Future<ClaimOutcome> claimItem(String id) async {
    final response = await client.post(
      _uri('/capture-items/$id/claim'),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
    final body = jsonDecode(response.body) as Map<String, dynamic>;
    return ClaimOutcome(
      id: body['id'] as String,
      billId: body['billId'] as String,
      status: body['status'] as String,
    );
  }
}
