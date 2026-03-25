import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/workplace_api_model.dart';

/// HTTP client for the workplace endpoints of the people-management service.
///
/// All methods return raw DTOs and throw [HttpException] on non-2xx responses.
/// The caller (repository) is responsible for wrapping these throws in [Result].
class WorkplaceApiService {
  WorkplaceApiService({
    required this.client,
    required this.baseUrl,
    required this.getAuthHeader,
  });

  final http.Client client;
  final String baseUrl;

  /// Callback that resolves the current `Authorization` header value.
  final Future<String> Function() getAuthHeader;

  Future<Map<String, String>> _headers() async {
    return {
      'Authorization': await getAuthHeader(),
      'Content-Type': 'application/json',
    };
  }

  /// Fetches all workplaces for [companyId].
  Future<List<WorkplaceApiModel>> getWorkplaces(String companyId) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/workplace');
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list
        .map((e) => WorkplaceApiModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Fetches a single workplace by [workplaceId] within [companyId].
  Future<WorkplaceApiModel> getWorkplaceById(
      String companyId, String workplaceId) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/workplace/$workplaceId');
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    return WorkplaceApiModel.fromJson(
        jsonDecode(response.body) as Map<String, dynamic>);
  }

  /// Creates a new workplace and returns the generated id.
  Future<String> createWorkplace(
      String companyId, WorkplaceApiModel model) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/workplace');
    final response = await client.post(
      uri,
      headers: await _headers(),
      body: jsonEncode(model.toCreateJson()),
    );
    _checkStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return json['id'] as String;
  }

  /// Updates an existing workplace and returns its id.
  Future<String> updateWorkplace(
      String companyId, WorkplaceApiModel model) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/workplace');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode(model.toJson()),
    );
    _checkStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return json['id'] as String;
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────

  void _checkStatus(http.Response response) {
    if (response.statusCode >= 200 && response.statusCode < 300) return;
    throw HttpException(
      statusCode: response.statusCode,
      message: 'HTTP ${response.statusCode}: ${response.reasonPhrase}',
    );
  }
}

/// Thrown by [WorkplaceApiService] when the server returns a non-2xx status.
class HttpException implements Exception {
  const HttpException({required this.statusCode, required this.message});

  final int statusCode;
  final String message;

  @override
  String toString() => 'HttpException($statusCode): $message';
}
