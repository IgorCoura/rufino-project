import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;

import 'http_exception.dart';
import 'http_status_helper.dart';
import 'request_id_helper.dart';

import '../models/document_template_api_model.dart';

/// HTTP client for the document template endpoints of the people-management service.
///
/// All methods return raw DTOs and throw [HttpException] on non-2xx responses.
/// The caller (repository) is responsible for wrapping these throws in [Result].
class DocumentTemplateApiService {
  DocumentTemplateApiService({
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

  /// Fetches all document templates (simplified) for [companyId].
  Future<List<DocumentTemplateApiModel>> getDocumentTemplates(
      String companyId) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/documenttemplate/simple');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list
        .map((e) => DocumentTemplateApiModel.fromJsonSimple(
            e as Map<String, dynamic>))
        .toList();
  }

  /// Fetches a single document template by [templateId] within [companyId].
  Future<DocumentTemplateApiModel> getDocumentTemplateById(
      String companyId, String templateId) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/documenttemplate/$templateId');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    return DocumentTemplateApiModel.fromJson(
        jsonDecode(response.body) as Map<String, dynamic>);
  }

  /// Creates a new document template and returns the generated id.
  Future<String> createDocumentTemplate(
      String companyId, DocumentTemplateApiModel model) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/documenttemplate');
    final response = await client.post(
      uri,
      headers: await _headers(),
      body: jsonEncode(model.toCreateJson()),
    );
    checkHttpStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return json['id'] as String;
  }

  /// Updates an existing document template and returns its id.
  Future<String> updateDocumentTemplate(
      String companyId, DocumentTemplateApiModel model) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/documenttemplate');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode(model.toJson()),
    );
    checkHttpStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return json['id'] as String;
  }

  /// Fetches all document groups for [companyId].
  Future<List<Map<String, dynamic>>> getDocumentGroups(
      String companyId) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/documentgroup');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list.cast<Map<String, dynamic>>();
  }

  /// Fetches all recover data type options for [companyId].
  Future<List<Map<String, dynamic>>> getRecoverDataTypes(
      String companyId) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/documenttemplate/recoverdatatype');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list.cast<Map<String, dynamic>>();
  }

  /// Fetches the recover data models JSON string for [companyId].
  Future<String> getRecoverDataModels(String companyId) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/documenttemplate/RecoverDataModels');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    return response.body.trim().replaceAll(RegExp(r'[\r\n]+'), '');
  }

  /// Returns whether the template identified by [templateId] has an uploaded file.
  Future<bool> hasFile(String companyId, String templateId) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/documenttemplate/hasfile/$templateId');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    return jsonDecode(response.body) as bool;
  }

  /// Uploads a file to the template identified by [templateId].
  ///
  /// [fileBytes] is the raw file content and [fileName] is the original name.
  Future<void> uploadFile(
    String companyId,
    String templateId,
    Uint8List fileBytes,
    String fileName,
  ) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/documenttemplate/upload');
    final authHeader = await getAuthHeader();
    final request = http.MultipartRequest('POST', uri)
      ..headers['Authorization'] = authHeader
      ..fields['id'] = templateId
      ..fields['company'] = companyId
      ..files.add(
        http.MultipartFile.fromBytes('formFile', fileBytes, filename: fileName),
      );
    final streamed = await client.send(request);
    if (streamed.statusCode < 200 || streamed.statusCode >= 300) {
      throw HttpException(
        statusCode: streamed.statusCode,
        message: 'HTTP ${streamed.statusCode}',
      );
    }
  }

  /// Downloads the file for the template identified by [templateId].
  ///
  /// Returns the raw file bytes.
  Future<Uint8List> downloadFile(
      String companyId, String templateId) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/documenttemplate/download/$templateId');
    final headers = await _headers();
    headers['Content-Type'] = 'application/octet-stream';
    final response = await client.get(uri, headers: headers);
    checkHttpStatus(response);
    return response.bodyBytes;
  }

  /// Fetches all type signature options for [companyId].
  Future<List<Map<String, dynamic>>> getTypeSignatures(
      String companyId) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/documenttemplate/typesignature');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list.cast<Map<String, dynamic>>();
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────

}
