import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;

import '../../core/errors/employee_exception.dart';
import '../models/employee_api_model.dart';
import '../models/employee_address_api_model.dart';
import '../models/employee_contact_api_model.dart';
import '../models/employee_document_api_model.dart';
import '../models/employee_contract_api_model.dart';
import '../models/employee_dependent_api_model.dart';
import '../models/employee_id_card_api_model.dart';
import '../models/employee_personal_info_api_model.dart';
import '../models/employee_profile_api_model.dart';
import '../models/employee_medical_exam_api_model.dart';
import '../models/employee_military_document_api_model.dart';
import '../models/employee_vote_id_api_model.dart';
import 'http_exception.dart';
import 'http_status_helper.dart';
import 'multipart_upload_helper.dart';
import 'request_id_helper.dart';

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
      'x-requestid': newRequestId(),
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
    checkHttpStatus(response);
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
    checkHttpStatus(response);
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
    checkHttpStatus(response);
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
    checkHttpStatus(response);
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
    checkHttpStatus(response);
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
    checkHttpStatus(response);
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
    checkHttpStatus(response);
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
    checkHttpStatus(response);
  }

  /// Fetches the address for [employeeId].
  Future<EmployeeAddressApiModel> getEmployeeAddress(
    String companyId,
    String employeeId,
  ) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/employee/address/$employeeId');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
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
    checkHttpStatus(response);
  }

  /// Fetches the personal info for [employeeId].
  Future<EmployeePersonalInfoApiModel> getEmployeePersonalInfo(
    String companyId,
    String employeeId,
  ) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/employee/personalinfo/$employeeId');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
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
    checkHttpStatus(response);
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
    checkHttpStatus(response);
  }

  /// Fetches the ID card (Identidade) information for [employeeId].
  Future<EmployeeIdCardApiModel> getEmployeeIdCard(
    String companyId,
    String employeeId,
  ) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/employee/idcard/$employeeId');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
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
    checkHttpStatus(response);
  }

  /// Fetches the voter registration (Título de Eleitor) for [employeeId].
  Future<EmployeeVoteIdApiModel> getEmployeeVoteId(
    String companyId,
    String employeeId,
  ) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/employee/voteid/$employeeId');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
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
    checkHttpStatus(response);
  }

  /// Fetches the military document (Documento Militar) for [employeeId].
  Future<EmployeeMilitaryDocumentApiModel> getMilitaryDocument(
    String companyId,
    String employeeId,
  ) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/employee/militarydocument/$employeeId');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
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
    checkHttpStatus(response);
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
    checkHttpStatus(response);
  }

  /// Fetches the medical admission exam for [employeeId].
  Future<EmployeeMedicalExamApiModel> getMedicalExam(
    String companyId,
    String employeeId,
  ) async {
    final uri = Uri.https(baseUrl,
        '/api/v1/$companyId/employee/medicaladmissionexam/$employeeId');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
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
    checkHttpStatus(response);
  }

  /// Fetches the list of dependents for [employeeId].
  Future<List<EmployeeDependentApiModel>> getDependents(
    String companyId,
    String employeeId,
  ) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/employee/dependents/$employeeId');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return EmployeeDependentApiModel.fromListResponse(json);
  }

  /// Creates a new dependent for [employeeId].
  Future<void> createDependent(
    String companyId,
    Map<String, dynamic> body,
  ) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/employee/dependent/create');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode(body),
    );
    checkHttpStatus(response);
  }

  /// Updates an existing dependent for [employeeId].
  Future<void> editDependent(
    String companyId,
    Map<String, dynamic> body,
  ) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/employee/dependent/edit');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode(body),
    );
    checkHttpStatus(response);
  }

  /// Removes a dependent from [employeeId].
  Future<void> removeDependent(
    String companyId,
    Map<String, dynamic> body,
  ) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/employee/dependent/remove');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode(body),
    );
    checkHttpStatus(response);
  }

  /// Updates the workplace assignment for [employeeId].
  Future<void> editEmployeeWorkplace(
    String companyId,
    String employeeId,
    String workplaceId,
  ) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/employee/workplace');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode({
        'employeeId': employeeId,
        'workPlaceId': workplaceId,
      }),
    );
    checkHttpStatus(response);
  }

  /// Fetches the list of contracts for [employeeId].
  Future<List<EmployeeContractApiModel>> getContracts(
    String companyId,
    String employeeId,
  ) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/employee/contracts/$employeeId');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return EmployeeContractApiModel.fromListResponse(json);
  }

  /// Fetches the available contract type options.
  Future<List<ContractTypeApiModel>> getContractTypes(
      String companyId) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/employee/contracts/types');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list
        .map((e) => ContractTypeApiModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Creates a new contract (admission) for [employeeId].
  Future<void> createContract(
    String companyId,
    Map<String, dynamic> body,
  ) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/employee/admission/complete');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode(body),
    );
    checkHttpStatus(response);
  }

  /// Finishes (ends) the active contract for [employeeId].
  Future<void> finishContract(
    String companyId,
    Map<String, dynamic> body,
  ) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/employee/contract/finished');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode(body),
    );
    checkHttpStatus(response);
  }

  /// Fetches the available document signing options.
  Future<List<Map<String, dynamic>>> getDocumentSigningOptions(
      String companyId) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/employee/DocumentSigningOptions');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list.cast<Map<String, dynamic>>();
  }

  /// Updates the document signing option for [employeeId].
  Future<void> editDocumentSigningOptions(
    String companyId,
    String employeeId,
    String optionId,
  ) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/employee/DocumentSigningOptions');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode({
        'employeeId': employeeId,
        'documentSigningOptions': optionId,
      }),
    );
    checkHttpStatus(response);
  }

  /// Fetches the list of required documents (without units) for [employeeId].
  Future<List<EmployeeDocumentApiModel>> getDocuments(
    String companyId,
    String employeeId,
  ) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/document/$employeeId');
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list
        .map((e) => EmployeeDocumentApiModel.fromJsonSimple(
            e as Map<String, dynamic>))
        .toList();
  }

  /// Fetches a single document with paginated units.
  Future<EmployeeDocumentApiModel> getDocumentById(
    String companyId,
    String employeeId,
    String documentId, {
    int pageNumber = 1,
    int pageSize = 10,
    int? statusId,
  }) async {
    final queryParams = <String, String>{
      'PageNumber': pageNumber.toString(),
      'PageSize': pageSize.toString(),
    };
    if (statusId != null) queryParams['StatusId'] = statusId.toString();

    final uri = Uri.https(
      baseUrl,
      '/api/v1/$companyId/document/$employeeId/$documentId',
      queryParams,
    );
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return EmployeeDocumentApiModel.fromJson(json);
  }

  /// Creates a new document unit for [documentId].
  Future<void> createDocumentUnit(
    String companyId,
    String employeeId,
    String documentId,
  ) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/document');
    final response = await client.post(
      uri,
      headers: await _headers(),
      body: jsonEncode({
        'documentId': documentId,
        'employeeId': employeeId,
      }),
    );
    checkHttpStatus(response);
  }

  /// Updates the date of a document unit.
  Future<void> editDocumentUnit(
    String companyId,
    Map<String, dynamic> body,
  ) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/document/documentunit');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode(body),
    );
    checkHttpStatus(response);
  }

  /// Marks a document unit as not applicable.
  Future<void> setDocumentUnitNotApplicable(
    String companyId,
    Map<String, dynamic> body,
  ) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/Document/DocumentUnit/not-applicable');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode(body),
    );
    checkHttpStatus(response);
  }

  /// Generates a PDF for a document unit and returns the raw bytes.
  Future<Uint8List> generateDocument(
    String companyId,
    String employeeId,
    String documentId,
    String documentUnitId,
  ) async {
    final uri = Uri.https(
      baseUrl,
      '/api/v1/$companyId/document/generate/$employeeId/$documentId/$documentUnitId',
    );
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    return response.bodyBytes;
  }

  /// Generates a document and sends it for digital signature.
  Future<void> generateAndSendToSign(
    String companyId,
    Map<String, dynamic> body,
  ) async {
    final uri = Uri.https(
      baseUrl,
      '/api/v1/$companyId/document/generate/send2sign',
    );
    final response = await client.post(
      uri,
      headers: await _headers(),
      body: jsonEncode(body),
    );
    checkHttpStatus(response);
  }

  /// Downloads the file attached to a document unit.
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

  /// Uploads a file to a document unit via multipart form.
  ///
  /// Reports upload progress through [onProgress] with values from 0.0 to 1.0.
  Future<void> uploadDocumentUnit(
    String companyId,
    String employeeId,
    String documentId,
    String documentUnitId,
    Uint8List fileBytes,
    String fileName, {
    UploadProgressCallback? onProgress,
  }) async {
    final uri = Uri.https(baseUrl, '/api/v1/$companyId/document/insert');
    final request = http.MultipartRequest('POST', uri);
    final headers = await _headers();
    headers.remove('Content-Type'); // Let multipart set its own
    request.headers.addAll(headers);
    request.files.add(http.MultipartFile.fromBytes(
      'formFile',
      fileBytes,
      filename: fileName,
    ));
    request.fields['DocumentUnitId'] = documentUnitId;
    request.fields['DocumentId'] = documentId;
    request.fields['EmployeeId'] = employeeId;
    await sendMultipartWithProgress(
      request,
      client: client,
      onProgress: onProgress,
    );
  }

  /// Uploads a file to a document unit and sends it for digital signature.
  ///
  /// Reports upload progress through [onProgress] with values from 0.0 to 1.0.
  Future<void> uploadDocumentUnitToSign(
    String companyId,
    String employeeId,
    String documentId,
    String documentUnitId,
    Uint8List fileBytes,
    String fileName,
    String dateLimitToSign,
    int reminderEveryNDays, {
    UploadProgressCallback? onProgress,
  }) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/document/insert/send2sign');
    final request = http.MultipartRequest('POST', uri);
    final headers = await _headers();
    headers.remove('Content-Type');
    request.headers.addAll(headers);
    request.files.add(http.MultipartFile.fromBytes(
      'formFile',
      fileBytes,
      filename: fileName,
    ));
    request.fields['DocumentUnitId'] = documentUnitId;
    request.fields['DocumentId'] = documentId;
    request.fields['EmployeeId'] = employeeId;
    request.fields['DateLimitToSign'] = dateLimitToSign;
    request.fields['EminderEveryNDays'] = reminderEveryNDays.toString();
    await sendMultipartWithProgress(
      request,
      client: client,
      onProgress: onProgress,
    );
  }

  // ─── Range operations ────────────────────────────────────────────────────

  /// Generates PDFs for multiple document units and returns the ZIP bytes.
  Future<Uint8List> generateDocumentRange(
    String companyId,
    String employeeId,
    List<Map<String, dynamic>> items,
  ) async {
    final uri = Uri.https(
      baseUrl,
      '/api/v1/$companyId/document/generate/range/$employeeId',
    );
    final response = await client.post(
      uri,
      headers: await _headers(),
      body: jsonEncode(items),
    );
    checkHttpStatus(response);
    return response.bodyBytes;
  }

  /// Downloads files for multiple document units and returns the ZIP bytes.
  Future<Uint8List> downloadDocumentRange(
    String companyId,
    String employeeId,
    List<Map<String, dynamic>> items,
  ) async {
    final uri = Uri.https(
      baseUrl,
      '/api/v1/$companyId/document/download/range/$employeeId',
    );
    final response = await client.post(
      uri,
      headers: await _headers(),
      body: jsonEncode(items),
    );
    checkHttpStatus(response);
    return response.bodyBytes;
  }

}
