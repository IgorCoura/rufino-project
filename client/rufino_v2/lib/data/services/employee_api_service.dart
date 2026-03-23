import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;

import '../../core/errors/employee_exception.dart';
import '../models/employee_api_model.dart';
import '../models/employee_address_api_model.dart';
import '../models/employee_contact_api_model.dart';
import '../models/employee_id_card_api_model.dart';
import '../models/employee_personal_info_api_model.dart';
import '../models/employee_profile_api_model.dart';
import '../models/employee_medical_exam_api_model.dart';
import '../models/employee_military_document_api_model.dart';
import '../models/employee_vote_id_api_model.dart';
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

  /// Fetches the detailed profile for [employeeId].
  Future<EmployeeProfileApiModel> getEmployeeProfile(
    String companyId,
    String employeeId,
  ) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/employee/$employeeId');
    final response = await client.get(uri, headers: await _headers());
    if (response.statusCode == 404) {
      throw EmployeeNotFoundException(employeeId);
    }
    _checkStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return EmployeeProfileApiModel.fromJson(json);
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
    return null;
  }

  /// Uploads a new profile photo for [employeeId].
  ///
  /// Sends [imageBytes] as a multipart/form-data PUT using the `formFile` field.
  /// [fileName] should include the extension (e.g. `"photo.jpg"`).
  Future<void> uploadEmployeeImage(
    String companyId,
    String employeeId,
    Uint8List imageBytes,
    String fileName,
  ) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/employee/image/$employeeId');
    final authHeader = await getAuthHeader();
    final request = http.MultipartRequest('PUT', uri)
      ..headers['Authorization'] = authHeader
      ..files.add(
        http.MultipartFile.fromBytes('formFile', imageBytes, filename: fileName),
      );
    final streamed = await client.send(request);
    if (streamed.statusCode < 200 || streamed.statusCode >= 300) {
      throw HttpException(
        statusCode: streamed.statusCode,
        message: 'HTTP ${streamed.statusCode}',
      );
    }
  }

  /// Updates the full name of the employee identified by [employeeId].
  ///
  /// Sends both [employeeId] and [name] in the request body, targeting the
  /// shared `/employee/name` endpoint (the employee id is not in the path).
  Future<void> editEmployeeName(
    String companyId,
    String employeeId,
    String name,
  ) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/employee/name');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode({'employeeId': employeeId, 'name': name}),
    );
    _checkStatus(response);
  }

  /// Marks the employee identified by [employeeId] as inactive.
  Future<void> markEmployeeAsInactive(
    String companyId,
    String employeeId,
  ) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/Employee/mark-as-inactive');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode({'employeeId': employeeId}),
    );
    _checkStatus(response);
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

  /// Fetches the contact information for [employeeId].
  Future<EmployeeContactApiModel> getEmployeeContact(
    String companyId,
    String employeeId,
  ) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/employee/contact/$employeeId');
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return EmployeeContactApiModel.fromJson(json);
  }

  /// Updates the contact information for [employeeId].
  Future<void> editEmployeeContact(
    String companyId,
    String employeeId,
    String cellphone,
    String email,
  ) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/employee/contact');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode({
        'employeeId': employeeId,
        'cellphone': cellphone,
        'email': email,
      }),
    );
    _checkStatus(response);
  }

  /// Fetches the address for [employeeId].
  Future<EmployeeAddressApiModel> getEmployeeAddress(
    String companyId,
    String employeeId,
  ) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/employee/address/$employeeId');
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return EmployeeAddressApiModel.fromJson(json);
  }

  /// Updates the address for [employeeId].
  ///
  /// [addressJson] must be the PUT body map (use
  /// [EmployeeAddressApiModel.toJsonMap] to build it).
  Future<void> editEmployeeAddress(
    String companyId,
    String employeeId,
    Map<String, dynamic> addressJson,
  ) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/employee/address');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode(addressJson),
    );
    _checkStatus(response);
  }

  /// Fetches the personal info for [employeeId].
  Future<EmployeePersonalInfoApiModel> getEmployeePersonalInfo(
    String companyId,
    String employeeId,
  ) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/employee/personalinfo/$employeeId');
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return EmployeePersonalInfoApiModel.fromJson(json);
  }

  /// Fetches the available selection options for the personal info form.
  Future<PersonalInfoOptionsApiModel> getPersonalInfoOptions(
    String companyId,
  ) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/employee/personalinfo/selectionoptions');
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return PersonalInfoOptionsApiModel.fromJson(json);
  }

  /// Updates the personal info for [employeeId].
  ///
  /// [personalInfoJson] must be the PUT body map.
  Future<void> editEmployeePersonalInfo(
    String companyId,
    String employeeId,
    Map<String, dynamic> personalInfoJson,
  ) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/employee/personalinfo');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode(personalInfoJson),
    );
    _checkStatus(response);
  }

  /// Fetches the ID card (Identidade) information for [employeeId].
  Future<EmployeeIdCardApiModel> getEmployeeIdCard(
    String companyId,
    String employeeId,
  ) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/employee/idcard/$employeeId');
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return EmployeeIdCardApiModel.fromJson(json);
  }

  /// Updates the ID card (Identidade) information for [employeeId].
  ///
  /// [idCardJson] must be the PUT body map.
  Future<void> editEmployeeIdCard(
    String companyId,
    String employeeId,
    Map<String, dynamic> idCardJson,
  ) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/employee/idcard');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode(idCardJson),
    );
    _checkStatus(response);
  }

  /// Fetches the voter registration (Título de Eleitor) for [employeeId].
  Future<EmployeeVoteIdApiModel> getEmployeeVoteId(
    String companyId,
    String employeeId,
  ) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/employee/voteid/$employeeId');
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return EmployeeVoteIdApiModel.fromJson(json);
  }

  /// Updates the voter registration (Título de Eleitor) for [employeeId].
  Future<void> editEmployeeVoteId(
    String companyId,
    String employeeId,
    String voteIdNumber,
  ) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/employee/voteid');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode({'employeeId': employeeId, 'voteIdNumber': voteIdNumber}),
    );
    _checkStatus(response);
  }

  /// Fetches the military document (Documento Militar) for [employeeId].
  Future<EmployeeMilitaryDocumentApiModel> getMilitaryDocument(
    String companyId,
    String employeeId,
  ) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/employee/militarydocument/$employeeId');
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return EmployeeMilitaryDocumentApiModel.fromJson(json);
  }

  /// Updates the military document (Documento Militar) for [employeeId].
  Future<void> editMilitaryDocument(
    String companyId,
    String employeeId,
    Map<String, dynamic> militaryDocumentJson,
  ) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/employee/MilitaryDocument');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode(militaryDocumentJson),
    );
    _checkStatus(response);
  }

  /// Updates the role assignment for [employeeId].
  Future<void> editEmployeeRole(
    String companyId,
    String employeeId,
    String roleId,
  ) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/employee/role');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode({'employeeId': employeeId, 'roleId': roleId}),
    );
    _checkStatus(response);
  }

  /// Fetches the medical admission exam for [employeeId].
  Future<EmployeeMedicalExamApiModel> getMedicalExam(
    String companyId,
    String employeeId,
  ) async {
    final uri = Uri.https(baseUrl,
        '/api/v1/$companyId/employee/medicaladmissionexam/$employeeId');
    final response = await client.get(uri, headers: await _headers());
    _checkStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return EmployeeMedicalExamApiModel.fromJson(json);
  }

  /// Updates the medical admission exam for [employeeId].
  Future<void> editMedicalExam(
    String companyId,
    String employeeId,
    Map<String, dynamic> medicalExamJson,
  ) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/employee/medicaladmissionexam');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode(medicalExamJson),
    );
    _checkStatus(response);
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
