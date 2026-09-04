import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/document_content_status_api_model.dart';
import 'http_status_helper.dart';
import 'request_id_helper.dart';

/// HTTP client for the document-snapshot endpoints.
///
/// Shared by every screen that generates documents (employee profile and
/// batch), so the check and the refresh exist in exactly one place.
///
/// All methods return raw DTOs and throw on non-2xx responses. The caller
/// (repository) is responsible for wrapping these throws in `Result`.
class DocumentContentApiService {
  /// Creates a [DocumentContentApiService].
  DocumentContentApiService({
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

  /// Checks whether the snapshot of each unit in [items] is still current.
  ///
  /// Returns one status per requested unit, in the order the server reports.
  Future<DocumentContentStatusResponse> checkOutdated(
    String companyId,
    List<DocumentUnitRefApiModel> items,
  ) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/document/content/check-outdated');
    final response = await client.post(
      uri,
      headers: await _headers(),
      body: jsonEncode({'items': items.map((e) => e.toJson()).toList()}),
    );
    checkHttpStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return DocumentContentStatusResponse.fromJson(json);
  }

  /// Rewrites the snapshot of each unit in [items] with the current data.
  ///
  /// The document date is untouched — the server reuses the date already
  /// stored on each unit.
  Future<void> refresh(
    String companyId,
    List<DocumentUnitRefApiModel> items,
  ) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/document/content/refresh');
    final response = await client.post(
      uri,
      headers: await _headers(),
      body: jsonEncode({'items': items.map((e) => e.toJson()).toList()}),
    );
    checkHttpStatus(response);
  }
}
