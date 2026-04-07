import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/document_group_api_model.dart';
import 'http_status_helper.dart';
import 'request_id_helper.dart';
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
      'x-requestid': newRequestId(),
    };
  }

  /// Fetches all document groups for [companyId].
  Future<List<DocumentGroupApiModel>> getDocumentGroups(
      String companyId) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/documentgroup');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
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
    checkHttpStatus(response);
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
    checkHttpStatus(response);
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
    checkHttpStatus(response);
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
    checkHttpStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return json['id'] as String;
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────

}
