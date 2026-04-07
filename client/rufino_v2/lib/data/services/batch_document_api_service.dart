import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;

import '../../domain/entities/batch_document_unit.dart';
import '../models/batch_document_unit_api_model.dart';
import 'http_exception.dart';
import 'http_status_helper.dart';
import 'request_id_helper.dart';

/// HTTP client for the batch document management endpoints.
///
/// All methods return raw DTOs and throw [HttpException] on non-2xx responses.
/// The caller (repository) is responsible for wrapping these throws in [Result].
class BatchDocumentApiService {
  BatchDocumentApiService({
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

  /// Fetches pending document units across all employees for [documentTemplateId].
  ///
  /// Supports filtering by employee status, name, and exact period selection.
  /// Returns a paginated [BatchDocumentUnitsResponse].
  Future<BatchDocumentUnitsResponse> getPendingDocumentUnits(
    String companyId,
    String documentTemplateId, {
    int? employeeStatusId,
    String? employeeName,
    int? periodTypeId,
    int? periodYear,
    int? periodMonth,
    int? periodDay,
    int? periodWeek,
    int pageSize = 50,
    int pageNumber = 1,
  }) async {
    final queryParams = <String, String>{
      'PageSize': pageSize.toString(),
      'PageNumber': pageNumber.toString(),
      if (employeeStatusId != null)
        'EmployeeStatusId': employeeStatusId.toString(),
      if (employeeName != null && employeeName.isNotEmpty)
        'EmployeeName': employeeName,
      if (periodTypeId != null) 'PeriodTypeId': periodTypeId.toString(),
      if (periodYear != null) 'PeriodYear': periodYear.toString(),
      if (periodMonth != null) 'PeriodMonth': periodMonth.toString(),
      if (periodDay != null) 'PeriodDay': periodDay.toString(),
      if (periodWeek != null) 'PeriodWeek': periodWeek.toString(),
    };
    final uri = Uri.https(
      baseUrl,
      '/api/v1/$companyId/batch-document/pending-units/$documentTemplateId',
      queryParams,
    );
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return BatchDocumentUnitsResponse.fromJson(json);
  }

  /// Fetches employees who do not have a pending document for [documentTemplateId].
  Future<List<EmployeeMissingDocumentApiModel>> getMissingEmployees(
    String companyId,
    String documentTemplateId, {
    int? employeeStatusId,
    String? employeeName,
  }) async {
    final queryParams = <String, String>{
      if (employeeStatusId != null)
        'EmployeeStatusId': employeeStatusId.toString(),
      if (employeeName != null && employeeName.isNotEmpty)
        'EmployeeName': employeeName,
    };
    final uri = Uri.https(
      baseUrl,
      '/api/v1/$companyId/batch-document/missing-employees/$documentTemplateId',
      queryParams,
    );
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list
        .map((e) =>
            EmployeeMissingDocumentApiModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Creates document units in batch for the given [employeeIds] and [documentTemplateId].
  Future<BatchCreateResponse> batchCreateDocumentUnits(
    String companyId,
    String documentTemplateId,
    List<String> employeeIds,
  ) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/batch-document/batch-create');
    final response = await client.post(
      uri,
      headers: await _headers(),
      body: jsonEncode({
        'documentTemplateId': documentTemplateId,
        'employeeIds': employeeIds,
      }),
    );
    checkHttpStatus(response);
    return BatchCreateResponse.fromJson(
        jsonDecode(response.body) as Map<String, dynamic>);
  }

  /// Updates the date of multiple document units in a single request.
  ///
  /// Returns the count of updated items.
  Future<int> batchUpdateDate(
    String companyId,
    List<Map<String, String>> items,
    String date,
  ) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/batch-document/batch-update-date');
    final response = await client.put(
      uri,
      headers: await _headers(),
      body: jsonEncode({'items': items, 'date': date}),
    );
    checkHttpStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return json['updatedCount'] as int? ?? 0;
  }

  /// Uploads multiple document files in a single multipart request.
  ///
  /// Each [BatchUploadItem] in [items] is matched to its file via [fileName].
  /// Returns per-item results indicating success or failure.
  Future<InsertDocumentRangeResponse> uploadDocumentRange(
    String companyId,
    List<BatchUploadItem> items,
  ) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/batch-document/insert-range');
    final request = http.MultipartRequest('POST', uri);
    final authHeader = await getAuthHeader();
    request.headers['Authorization'] = authHeader;

    for (final item in items) {
      request.files.add(http.MultipartFile.fromBytes(
        'formFiles',
        item.fileBytes,
        filename: item.fileName,
      ));
    }

    request.fields['itemsJson'] = jsonEncode(items
        .map((i) => {
              'documentUnitId': i.documentUnitId,
              'documentId': i.documentId,
              'employeeId': i.employeeId,
              'fileName': i.fileName,
            })
        .toList());

    final streamed = await request.send();
    final responseBody = await streamed.stream.bytesToString();
    if (streamed.statusCode < 200 || streamed.statusCode >= 300) {
      throw HttpException(
        statusCode: streamed.statusCode,
        message: 'HTTP ${streamed.statusCode}',
        serverMessages: const [],
      );
    }
    return InsertDocumentRangeResponse.fromJson(
        jsonDecode(responseBody) as Map<String, dynamic>);
  }

  /// Uploads multiple document files and sends them for digital signature.
  ///
  /// Uses a global [dateLimitToSign] and [reminderEveryNDays] for all items.
  Future<InsertDocumentRangeResponse> uploadDocumentRangeToSign(
    String companyId,
    List<BatchUploadItem> items,
    String dateLimitToSign,
    int reminderEveryNDays,
  ) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/batch-document/insert-range/send2sign');
    final request = http.MultipartRequest('POST', uri);
    final authHeader = await getAuthHeader();
    request.headers['Authorization'] = authHeader;

    for (final item in items) {
      request.files.add(http.MultipartFile.fromBytes(
        'formFiles',
        item.fileBytes,
        filename: item.fileName,
      ));
    }

    request.fields['itemsJson'] = jsonEncode(items
        .map((i) => {
              'documentUnitId': i.documentUnitId,
              'documentId': i.documentId,
              'employeeId': i.employeeId,
              'fileName': i.fileName,
            })
        .toList());
    request.fields['dateLimitToSign'] = dateLimitToSign;
    request.fields['reminderEveryNDays'] = reminderEveryNDays.toString();

    final streamed = await request.send();
    final responseBody = await streamed.stream.bytesToString();
    if (streamed.statusCode < 200 || streamed.statusCode >= 300) {
      throw HttpException(
        statusCode: streamed.statusCode,
        message: 'HTTP ${streamed.statusCode}',
        serverMessages: const [],
      );
    }
    return InsertDocumentRangeResponse.fromJson(
        jsonDecode(responseBody) as Map<String, dynamic>);
  }

  /// Generates PDFs for the selected document units across employees.
  ///
  /// Returns raw ZIP bytes containing all generated PDFs.
  Future<Uint8List> generatePdfRange(
    String companyId,
    List<Map<String, String>> items,
  ) async {
    final uri =
        Uri.https(baseUrl, '/api/v1/$companyId/batch-document/generate-range');
    final response = await client.post(
      uri,
      headers: await _headers(),
      body: jsonEncode({'items': items}),
    );
    checkHttpStatus(response);
    return response.bodyBytes;
  }

  /// Generates PDFs and sends them for digital signature.
  Future<void> generateAndSignRange(
    String companyId,
    List<Map<String, String>> items,
    String dateLimitToSign,
    int reminderEveryNDays,
  ) async {
    final uri = Uri.https(
        baseUrl, '/api/v1/$companyId/batch-document/generate-range/send2sign');
    final response = await client.post(
      uri,
      headers: await _headers(),
      body: jsonEncode({
        'items': items,
        'dateLimitToSign': dateLimitToSign,
        'reminderEveryNDays': reminderEveryNDays,
      }),
    );
    checkHttpStatus(response);
  }
}
