import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/document_dashboard_api_model.dart';
import 'http_status_helper.dart';
import 'request_id_helper.dart';

/// HTTP client for the document dashboard endpoints.
///
/// All methods return raw DTOs and throw on non-2xx responses.
/// The caller (repository) is responsible for wrapping these throws in
/// `Result`.
class DocumentDashboardApiService {
  DocumentDashboardApiService({
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

  static Map<String, String> _filterParams({
    required int expiringInDays,
    int? employeeStatusId,
    String? employeeName,
    String? documentGroupId,
    String? documentTemplateId,
  }) {
    return {
      'ExpiringInDays': expiringInDays.toString(),
      if (employeeStatusId != null)
        'EmployeeStatusId': employeeStatusId.toString(),
      if (employeeName != null && employeeName.isNotEmpty)
        'EmployeeName': employeeName,
      if (documentGroupId != null && documentGroupId.isNotEmpty)
        'DocumentGroupId': documentGroupId,
      if (documentTemplateId != null && documentTemplateId.isNotEmpty)
        'DocumentTemplateId': documentTemplateId,
    };
  }

  /// Fetches the per-bucket unit counts for the company.
  ///
  /// Returns a [DashboardSummaryApiModel] with the counts computed by the
  /// backend using the same predicates as [getUnits].
  Future<DashboardSummaryApiModel> getSummary(
    String companyId, {
    required int expiringInDays,
    int? employeeStatusId,
    String? employeeName,
    String? documentGroupId,
    String? documentTemplateId,
  }) async {
    final uri = Uri.https(
      baseUrl,
      '/api/v1/$companyId/document-dashboard/summary',
      _filterParams(
        expiringInDays: expiringInDays,
        employeeStatusId: employeeStatusId,
        employeeName: employeeName,
        documentGroupId: documentGroupId,
        documentTemplateId: documentTemplateId,
      ),
    );
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return DashboardSummaryApiModel.fromJson(json);
  }

  /// Fetches the paginated unit list of a dashboard [bucket].
  ///
  /// [bucket] is the API enum name (e.g. `Expired`). Returns a paginated
  /// [DashboardUnitsResponse] ordered by urgency on the server.
  Future<DashboardUnitsResponse> getUnits(
    String companyId, {
    required String bucket,
    required int expiringInDays,
    int? employeeStatusId,
    String? employeeName,
    String? documentGroupId,
    String? documentTemplateId,
    int pageSize = 50,
    int pageNumber = 1,
  }) async {
    final uri = Uri.https(
      baseUrl,
      '/api/v1/$companyId/document-dashboard/units',
      {
        'Bucket': bucket,
        'PageSize': pageSize.toString(),
        'PageNumber': pageNumber.toString(),
        ..._filterParams(
          expiringInDays: expiringInDays,
          employeeStatusId: employeeStatusId,
          employeeName: employeeName,
          documentGroupId: documentGroupId,
          documentTemplateId: documentTemplateId,
        ),
      },
    );
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return DashboardUnitsResponse.fromJson(json);
  }
}
