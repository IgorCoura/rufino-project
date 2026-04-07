import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/require_document_api_model.dart';
import 'http_exception.dart';
import 'http_status_helper.dart';
import 'request_id_helper.dart';

/// HTTP client for the require document endpoints of the people-management service.
///
/// All methods return raw DTOs and throw [HttpException] on non-2xx responses.
/// The caller (repository) is responsible for wrapping these throws in [Result].
class RequireDocumentApiService {
  RequireDocumentApiService({
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
      'x-requestid': newRequestId(),
    };
  }

  /// Fetches all require documents (simplified) for [companyId].
  Future<List<RequireDocumentApiModel>> getRequireDocuments(
      String companyId) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/requiredocuments');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list
        .map((e) => RequireDocumentApiModel.fromJsonSimple(
            e as Map<String, dynamic>))
        .toList();
  }

  /// Fetches a single require document by [requireDocumentId] within [companyId].
  Future<RequireDocumentApiModel> getRequireDocumentById(
      String companyId, String requireDocumentId) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/requiredocuments/$requireDocumentId');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    return RequireDocumentApiModel.fromJson(
        jsonDecode(response.body) as Map<String, dynamic>);
  }

  /// Creates a new require document and returns the generated id.
  ///
  /// Accepts the raw [body] map so the repository can include template ids
  /// and listen events directly.
  Future<String> createRequireDocument(
      String companyId, Map<String, dynamic> body) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/requiredocuments');
    final response = await client.post(
      uri,
      headers: await _headers(),
      body: jsonEncode(body),
    );
    checkHttpStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return json['id'] as String;
  }

  /// Updates an existing require document and returns its id.
  ///
  /// Accepts the raw [body] map so the repository can include template ids
  /// and listen events directly.
  Future<String> updateRequireDocument(
      String companyId, Map<String, dynamic> body) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/requiredocuments');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode(body),
    );
    checkHttpStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return json['id'] as String;
  }

  /// Fetches all association types for [companyId].
  Future<List<Map<String, dynamic>>> getAssociationTypes(
      String companyId) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/requiredocuments/associationtype');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list.cast<Map<String, dynamic>>();
  }

  /// Fetches all associations for the given [associationTypeId] within [companyId].
  Future<List<Map<String, dynamic>>> getAssociations(
      String companyId, String associationTypeId) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/requiredocuments/association/$associationTypeId');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list.cast<Map<String, dynamic>>();
  }

  /// Fetches employee lifecycle events from [companyId].
  ///
  /// Returns events from the `/employee/events` endpoint.
  Future<List<Map<String, dynamic>>> getEmployeeEvents(
      String companyId) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/employee/events');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list.cast<Map<String, dynamic>>();
  }

  /// Fetches require document specific events from [companyId].
  ///
  /// Returns events from the `/requiredocuments/events` endpoint.
  Future<List<Map<String, dynamic>>> getRequireDocumentEvents(
      String companyId) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/requiredocuments/events');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list.cast<Map<String, dynamic>>();
  }

  /// Fetches all employee statuses for [companyId].
  Future<List<Map<String, dynamic>>> getStatuses(String companyId) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/employee/status');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list.cast<Map<String, dynamic>>();
  }

  /// Fetches all document templates (simplified) for [companyId].
  Future<List<Map<String, dynamic>>> getDocumentTemplates(
      String companyId) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/documenttemplate/simple');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list.cast<Map<String, dynamic>>();
  }

}
