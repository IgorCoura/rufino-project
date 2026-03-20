import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;

import '../models/employee_api_model.dart';
import 'department_api_service.dart';

/// HTTP client for the employee endpoints of the people-management service.
///
/// All methods return raw DTOs and throw [HttpException] on non-2xx responses.
/// The caller (repository) is responsible for wrapping these throws in [Result].
class EmployeeApiService {
  EmployeeApiService({
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

  /// Fetches a page of employees for [companyId] matching the given filters.
  ///
  /// [name] and [role] are optional search strings. [status] and
  /// [documentStatus] are optional integer filter ids. [sortOrder] is 0 for
  /// ascending, 1 for descending. [pageSize] and [sizeSkip] control pagination.
  Future<List<EmployeeApiModel>> getEmployees(
    String companyId, {
    String? name,
    String? role,
    int? status,
    int? documentStatus,
    int sortOrder = 0,
    int pageSize = 15,
    int sizeSkip = 0,
  }) async {
    final queryParams = <String, String>{
      'name': name ?? '',
      'role': role ?? '',
      'sortOrder': sortOrder.toString(),
      'pageSize': pageSize.toString(),
      'sizeSkip': sizeSkip.toString(),
    };
    if (status != null) queryParams['status'] = status.toString();
    if (documentStatus != null) {
      queryParams['documentRepresentingStatus'] = documentStatus.toString();
    }

    final uri = Uri.https(
      baseUrl,
      '/api/v1/$companyId/employee/list',
      queryParams,
    );
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list
        .map((e) => EmployeeApiModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Returns the profile image bytes for [employeeId], or null if no image exists.
  Future<Uint8List?> getEmployeeImage(
      String companyId, String employeeId) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/employee/image/$employeeId');
    final response = await client.get(uri, headers: await _headers());
    if (response.statusCode == 200) return response.bodyBytes;
    if (response.statusCode == 404) return null;
    _checkStatus(response);
    return null; // unreachable, _checkStatus throws
  }

  /// Creates a new employee and returns the generated id.
  Future<String> createEmployee(
    String companyId, {
    required String name,
    required String roleId,
    required String workplaceId,
  }) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/employee');
    final body = jsonEncode({
      'name': name,
      'roleId': roleId,
      'workPlaceId': workplaceId,
    });
    final response =
        await client.post(uri, headers: await _headers(), body: body);
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
