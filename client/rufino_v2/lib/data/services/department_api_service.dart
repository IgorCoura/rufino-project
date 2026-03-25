import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/department_api_model.dart';

/// HTTP client for the department, position, and role endpoints of the
/// people-management service.
///
/// All methods return raw DTOs and throw [HttpException] on non-2xx responses.
/// The caller (repository) is responsible for wrapping these throws in [Result].
class DepartmentApiService {
  DepartmentApiService({
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

  // ─── Department ───────────────────────────────────────────────────────────

  /// Fetches all departments for [companyId], including nested positions and roles.
  Future<List<DepartmentApiModel>> getDepartments(String companyId) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/department/all');
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list
        .map((e) => DepartmentApiModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Fetches a single department by [departmentId] without nested positions.
  Future<DepartmentApiModel> getDepartmentById(
      String companyId, String departmentId) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/department/$departmentId');
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    return DepartmentApiModel.fromJsonSimple(
        jsonDecode(response.body) as Map<String, dynamic>);
  }

  /// Creates a new department and returns the generated id.
  Future<String> createDepartment(
      String companyId, DepartmentApiModel model) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/department');
    final response = await client.post(
      uri,
      headers: await _headers(),
      body: jsonEncode(model.toCreateJson()),
    );
    _checkStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return json['id'] as String;
  }

  /// Updates an existing department and returns its id.
  Future<String> updateDepartment(
      String companyId, DepartmentApiModel model) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/department');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode(model.toJson()),
    );
    _checkStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return json['id'] as String;
  }

  // ─── Position ─────────────────────────────────────────────────────────────

  /// Fetches a single position by [positionId] without nested roles.
  Future<PositionApiModel> getPositionById(
      String companyId, String positionId) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/position/$positionId');
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    return PositionApiModel.fromJsonSimple(
        jsonDecode(response.body) as Map<String, dynamic>);
  }

  /// Creates a new position under [departmentId] and returns the generated id.
  Future<String> createPosition(
      String companyId, String departmentId, PositionApiModel model) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/position');
    final response = await client.post(
      uri,
      headers: await _headers(),
      body: jsonEncode(model.toCreateJson(departmentId)),
    );
    _checkStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return json['id'] as String;
  }

  /// Updates an existing position and returns its id.
  Future<String> updatePosition(
      String companyId, PositionApiModel model) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/position');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode(model.toJson()),
    );
    _checkStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return json['id'] as String;
  }

  // ─── Role ─────────────────────────────────────────────────────────────────

  /// Fetches a single role by [roleId], fully populated with its remuneration.
  Future<RoleApiModel> getRoleById(String companyId, String roleId) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/role/$roleId');
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    return RoleApiModel.fromJson(
        jsonDecode(response.body) as Map<String, dynamic>);
  }

  /// Creates a new role under [positionId] and returns the generated id.
  Future<String> createRole(
      String companyId, String positionId, RoleApiModel model) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/role');
    final response = await client.post(
      uri,
      headers: await _headers(),
      body: jsonEncode(model.toCreateJson(positionId)),
    );
    _checkStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return json['id'] as String;
  }

  /// Updates an existing role and returns its id.
  Future<String> updateRole(String companyId, RoleApiModel model) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/role');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode(model.toJson()),
    );
    _checkStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return json['id'] as String;
  }

  // ─── Lookup ───────────────────────────────────────────────────────────────

  /// Returns all available payment unit options.
  Future<List<PaymentUnitApiModel>> getPaymentUnits(String companyId) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/role/paymentUnit');
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list
        .map((e) => PaymentUnitApiModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Returns all available salary type options.
  Future<List<SalaryTypeApiModel>> getSalaryTypes(String companyId) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/role/currencyType');
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list
        .map((e) => SalaryTypeApiModel.fromJson(e as Map<String, dynamic>))
        .toList();
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

/// Thrown by [DepartmentApiService] when the server returns a non-2xx status.
class HttpException implements Exception {
  const HttpException({required this.statusCode, required this.message});

  final int statusCode;
  final String message;

  @override
  String toString() => 'HttpException($statusCode): $message';
}
