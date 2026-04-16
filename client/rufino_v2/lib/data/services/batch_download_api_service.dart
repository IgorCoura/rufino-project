import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;

import '../models/batch_download_api_model.dart';
import 'http_status_helper.dart';
import 'request_id_helper.dart';

/// HTTP client for the batch download endpoints.
///
/// All methods return raw DTOs and throw on non-2xx responses.
/// The caller (repository) is responsible for wrapping these throws in [Result].
class BatchDownloadApiService {
  BatchDownloadApiService({
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

  /// Fetches employees available for batch download with optional filters.
  ///
  /// Returns a paginated [BatchDownloadEmployeesResponse].
  Future<BatchDownloadEmployeesResponse> getEmployeesForDownload(
    String companyId, {
    String? name,
    int? statusId,
    String? workplaceId,
    String? roleId,
    int pageSize = 50,
    int pageNumber = 1,
  }) async {
    final queryParams = <String, String>{
      'PageSize': pageSize.toString(),
      'PageNumber': pageNumber.toString(),
      if (name != null && name.isNotEmpty) 'Name': name,
      if (statusId != null) 'StatusId': statusId.toString(),
      if (workplaceId != null && workplaceId.isNotEmpty)
        'WorkplaceId': workplaceId,
      if (roleId != null && roleId.isNotEmpty) 'RoleId': roleId,
    };
    final uri = Uri.https(
      baseUrl,
      '/api/v1/$companyId/batch-download/employees',
      queryParams,
    );
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return BatchDownloadEmployeesResponse.fromJson(json);
  }

  /// Fetches document units for the selected employees with optional filters.
  ///
  /// Uses POST because the employee ID list can be large.
  /// Returns a paginated [BatchDownloadUnitsResponse].
  Future<BatchDownloadUnitsResponse> getDocumentUnitsForDownload(
    String companyId, {
    required List<String> employeeIds,
    String? documentGroupId,
    String? documentTemplateId,
    int? unitStatusId,
    String? dateFrom,
    String? dateTo,
    int? periodTypeId,
    int? periodYear,
    int? periodMonth,
    int? periodDay,
    int? periodWeek,
    int pageSize = 50,
    int pageNumber = 1,
  }) async {
    final uri = Uri.https(
      baseUrl,
      '/api/v1/$companyId/batch-download/units',
    );
    final body = <String, dynamic>{
      'employeeIds': employeeIds,
      'pageSize': pageSize,
      'pageNumber': pageNumber,
      if (documentGroupId != null && documentGroupId.isNotEmpty)
        'documentGroupId': documentGroupId,
      if (documentTemplateId != null && documentTemplateId.isNotEmpty)
        'documentTemplateId': documentTemplateId,
      if (unitStatusId != null) 'unitStatusId': unitStatusId,
      if (dateFrom != null && dateFrom.isNotEmpty) 'dateFrom': dateFrom,
      if (dateTo != null && dateTo.isNotEmpty) 'dateTo': dateTo,
      if (periodTypeId != null) 'periodTypeId': periodTypeId,
      if (periodYear != null) 'periodYear': periodYear,
      if (periodMonth != null) 'periodMonth': periodMonth,
      if (periodDay != null) 'periodDay': periodDay,
      if (periodWeek != null) 'periodWeek': periodWeek,
    };
    final response = await client.post(
      uri,
      headers: await _headers(),
      body: jsonEncode(body),
    );
    checkHttpStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return BatchDownloadUnitsResponse.fromJson(json);
  }

  /// Downloads a single document unit file.
  ///
  /// Returns the raw file bytes.
  Future<Uint8List> downloadDocumentUnit(
    String companyId,
    String employeeId,
    String documentId,
    String documentUnitId,
  ) async {
    final uri = Uri.https(
      baseUrl,
      '/api/v1/$companyId/document/download/$employeeId/$documentId/$documentUnitId',
    );
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    return response.bodyBytes;
  }

  /// Downloads the selected document units as a ZIP file.
  ///
  /// Returns the raw ZIP bytes.
  Future<Uint8List> downloadBatch(
    String companyId,
    List<Map<String, dynamic>> items,
  ) async {
    final uri = Uri.https(
      baseUrl,
      '/api/v1/$companyId/batch-download/download',
    );
    final body = jsonEncode({'items': items});
    final response = await client.post(
      uri,
      headers: await _headers(),
      body: body,
    );
    checkHttpStatus(response);
    return response.bodyBytes;
  }
}
