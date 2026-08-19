import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:rufino_core/rufino_core.dart';
import 'package:uuid/uuid.dart';

import '../domain/trusted_origin.dart';

const _uuid = Uuid();

/// Maps the trusted origin read model into domain entities.
abstract final class TrustedOriginMapper {
  /// Builds a [TrustedOrigin] from the API's JSON.
  static TrustedOrigin fromJson(Map<String, dynamic> json) {
    return TrustedOrigin(
      id: json['id'] as String,
      kind: json['kind'] as String,
      value: json['value'] as String,
      decision: json['decision'] as String,
      decidedBy: json['decidedBy'] as String,
      decidedAt: DateTime.parse(json['decidedAt'] as String),
      note: json['note'] as String?,
    );
  }

  /// Builds a [TrustedOriginPage] from the API's JSON.
  static TrustedOriginPage pageFromJson(Map<String, dynamic> json) {
    return TrustedOriginPage(
      items: (json['items'] as List<dynamic>? ?? const [])
          .map((e) => fromJson(e as Map<String, dynamic>))
          .toList(),
      nextCursor: json['nextCursor'] as String?,
    );
  }
}

/// HTTP client for the trusted origin endpoints.
class TrustedOriginApiService {
  /// Creates the service against [baseUrl].
  TrustedOriginApiService({
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

  /// Lists origins, one cursor page at a time.
  Future<TrustedOriginPage> listOrigins({String? cursor, int limit = 50}) async {
    final response = await client.get(
      _uri('/trusted-origins', {
        if (cursor != null) 'cursor': cursor,
        'limit': limit,
      }),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return TrustedOriginMapper.pageFromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Returns one origin.
  Future<TrustedOrigin> getOrigin(String id) async {
    final response = await client.get(
      _uri('/trusted-origins/$id'),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return TrustedOriginMapper.fromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Resolves a sender, or returns `null` when the origin is unknown (204).
  Future<TrustedOrigin?> resolveSender(String sender) async {
    final response = await client.get(
      _uri('/trusted-origins/resolve', {'sender': sender}),
      headers: await _headers(),
    );
    if (response.statusCode == 204) return null;
    checkApiStatus(response);
    return TrustedOriginMapper.fromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Registers an origin and returns its id.
  ///
  /// No `decidedBy` in the body: who decided is the token's `sub`, resolved
  /// on the server — authorship is not forgeable by the client.
  Future<String> registerOrigin({
    required String kind,
    required String value,
    required String decision,
    String? note,
  }) async {
    final response = await client.post(
      _uri('/trusted-origins'),
      headers: await _headers(write: true),
      body: jsonEncode({
        'kind': kind,
        'value': value.trim(),
        'decision': decision,
        'note': note?.trim(),
      }),
    );
    checkApiStatus(response);
    return (jsonDecode(response.body) as Map<String, dynamic>)['id'] as String;
  }

  /// Replaces the decision.
  Future<void> changeDecision(
    String id, {
    required String decision,
    String? note,
  }) async {
    final response = await client.put(
      _uri('/trusted-origins/$id/decision'),
      headers: await _headers(write: true),
      body: jsonEncode({'decision': decision, 'note': note?.trim()}),
    );
    checkApiStatus(response);
  }

  /// Removes the origin.
  Future<void> deleteOrigin(String id) async {
    final response = await client.delete(
      _uri('/trusted-origins/$id'),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
  }
}
