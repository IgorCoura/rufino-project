import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/document_group_api_model.dart';
import '../models/document_group_with_documents_api_model.dart';
import '../models/document_group_with_templates_api_model.dart';

/// HTTP client for the document group endpoints of the people-management service.
///
/// All methods return raw DTOs and throw [HttpException] on non-2xx responses.
/// The caller (repository) is responsible for wrapping these throws in [Result].
class DocumentGroupApiService {
  DocumentGroupApiService({
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

  /// Fetches all document groups for [companyId].
  Future<List<DocumentGroupApiModel>> getDocumentGroups(
      String companyId) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/documentgroup');
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list
        .map((e) =>
            DocumentGroupApiModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Fetches all document groups with their nested templates for [companyId].
  Future<List<DocumentGroupWithTemplatesApiModel>>
      getDocumentGroupsWithTemplates(String companyId) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/documentgroup/withtemplates');
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list
        .map((e) => DocumentGroupWithTemplatesApiModel.fromJson(
            e as Map<String, dynamic>))
        .toList();
  }

  /// Fetches all document groups with their nested employee documents
  /// for [employeeId].
  Future<List<DocumentGroupWithDocumentsApiModel>>
      getDocumentGroupsWithDocuments(
          String companyId, String employeeId) async {
    final uri = Uri.https(baseUrl,
        '/api/v1/$companyId/documentgroup/withdocuments/$employeeId');
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list
        .map((e) => DocumentGroupWithDocumentsApiModel.fromJson(
            e as Map<String, dynamic>))
        .toList();
  }

  /// Creates a new document group and returns the generated id.
  Future<String> createDocumentGroup(
      String companyId, DocumentGroupApiModel model) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/documentgroup');
    final response = await client.post(
      uri,
      headers: await _headers(),
      body: jsonEncode(model.toCreateJson()),
    );
    _checkStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return json['id'] as String;
  }

  /// Updates an existing document group and returns its id.
  Future<String> updateDocumentGroup(
      String companyId, DocumentGroupApiModel model) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/documentgroup');
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

/// Thrown by [DocumentGroupApiService] when the server returns a non-2xx status.
class HttpException implements Exception {
  const HttpException({required this.statusCode, required this.message});

  final int statusCode;
  final String message;

  @override
  String toString() => 'HttpException($statusCode): $message';
}
