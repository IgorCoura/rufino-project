import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:uuid/uuid.dart';

import '../domain/capture_item.dart';
import '../domain/capture_item_repository.dart';
import '../domain/captured_artifact.dart';
import '../domain/email_message.dart';
import 'artifact_response.dart';
import 'bill_api_service.dart';

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
      hasArtifact: json['hasArtifact'] as bool? ?? false,
      sourceUrl: json['sourceUrl'] as String?,
      contentHash: json['contentHash'] as String?,
      billId: json['billId'] as String?,
      claimedBy: json['claimedBy'] as String?,
      claimedAt: json['claimedAt'] == null
          ? null
          : DateTime.parse(json['claimedAt'] as String),
      processingAttempts: json['processingAttempts'] as int? ?? 0,
      lastError: json['lastError'] as String?,
      linkHost: json['linkHost'] as String?,
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

  /// Downloads the item's original document.
  ///
  /// Goes through `send` instead of `get` because the body is bytes, not
  /// JSON — the decoding path of every other call here would corrupt it.
  Future<CapturedArtifact> getArtifact(String id) async {
    final request = http.Request('GET', _uri('/capture-items/$id/artifact'))
      ..headers.addAll(await _headers());

    final response =
        await http.Response.fromStream(await client.send(request));
    checkApiStatus(response);

    return artifactFromResponse(response);
  }

  /// Fetches the e-mail that brought the item.
  Future<EmailMessage> getCaptureItemEmail(String id) async {
    final response = await client.get(
      _uri('/capture-items/$id/email'),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return BillMapper.emailFromJson(
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

  /// Dismisses a quarantined item the user does not recognise.
  ///
  /// Reversible by [reprocessItem]: dismissing removes work from sight without
  /// anyone having verified the document, so it must be undoable.
  Future<void> dismissItem(String id, {String? note}) async {
    final response = await client.post(
      _uri('/capture-items/$id/dismiss'),
      headers: await _headers(write: true),
      body: jsonEncode({'note': note}),
    );
    checkApiStatus(response);
  }

  /// Uploads the bill the user fetched by hand, and returns the item to the queue.
  ///
  /// Closes the path the link ladder cannot reach — an issuer whose page needs a
  /// login, or that has no recipe registered.
  Future<void> attachArtifact(
    String id,
    List<int> bytes, {
    required String fileName,
    required String contentType,
  }) async {
    final request = http.MultipartRequest('POST', _uri('/capture-items/$id/artifact'))
      ..headers.addAll(await _headers(write: true))
      ..files.add(http.MultipartFile.fromBytes(
        'file',
        bytes,
        filename: fileName,
        contentType: MediaType.parse(contentType),
      ));

    // O multipart monta o próprio Content-Type, com o boundary — deixar o do JSON
    // aqui faria o servidor recusar antes de olhar o arquivo.
    request.headers.remove('Content-Type');

    final streamed = await client.send(request);
    checkApiStatus(await http.Response.fromStream(streamed));
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
