import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/workplace_api_model.dart';
import 'http_status_helper.dart';

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
    checkHttpStatus(response);
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
    checkHttpStatus(response);
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
    checkHttpStatus(response);
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
    checkHttpStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return json['id'] as String;
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────

}
