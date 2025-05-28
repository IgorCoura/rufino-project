import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;
import 'package:rufino/modules/employee/domain/model/document/document.dart';
import 'package:rufino/modules/employee/domain/model/require_document/require_document_simple_with_documents.dart';
import 'package:rufino/shared/services/base_service.dart';
import 'package:rufino/shared/util/data_convetion.dart';

class DocumentService extends BaseService {
  final Uri peopleManagementUrl =
      Uri.https(const String.fromEnvironment("people_management_url"));

  DocumentService(super._authService);

  Future<String> createDocumentUnit(
      String documentId, String employeeId, String companyId) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = {
      "documentId": documentId,
      "employeeId": employeeId,
    };
    var url = peopleManagementUrl.replace(path: "/api/v1/$companyId/document");
    var response =
        await http.post(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> editDocumentUnit(String date, String documentUnitId,
      String documentId, String employeeId, String companyId) async {
    var headers = await getHeadersWithRequestId();

    Map<String, dynamic> body = {
      "documentUnitId": documentUnitId,
      "documentId": documentId,
      "employeeId": employeeId,
      "documentUnitDate": DataConvetion.convertToDataOnly(date),
    };
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/document/documentunit");
    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<Document>> getAllDocumentsSimple(
      String companyId, String employeeId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/document/$employeeId");

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return Document.fromJsonListSimple(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<Document> getByIdDocuments(
      String companyId, String employeeId, String documentId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/document/$employeeId/$documentId");

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return Document.fromJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future generateAndSend2Sign(
      String dateLimitToSign,
      int eminderEveryNDays,
      String documentUnitId,
      String documentId,
      String employeeId,
      String companyId) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = {
      "documentUnitId": documentUnitId,
      "documentId": documentId,
      "employeeId": employeeId,
      "dateLimitToSign": DataConvetion.convertToIsoUTC(dateLimitToSign),
      "eminderEveryNDays": eminderEveryNDays,
    };
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/document/generate/send2sign");
    var response =
        await http.post(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<File> downloadDocumentGenerated(
      String documentUnitId,
      String documentId,
      String employeeId,
      String companyId,
      String path) async {
    var headers = await getHeaders(contentType: "application/octet-stream");
    var url = peopleManagementUrl.replace(
        path:
            "/api/v1/$companyId/document/generate/$employeeId/$documentId/$documentUnitId");

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      var file = File(path);
      await file.writeAsBytes(response.bodyBytes);
      return file;
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<File> downloadDocumentUnit(
    String documentUnitId,
    String documentId,
    String employeeId,
    String companyId,
    String path,
  ) async {
    var headers = await getHeaders(contentType: "application/octet-stream");
    var url = peopleManagementUrl.replace(
        path:
            "/api/v1/$companyId/document/download/$employeeId/$documentId/$documentUnitId");

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      var file = File(path);
      await file.writeAsBytes(response.bodyBytes);
      return file;
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> loadDocumentUnit(String documentUnitId, String documentId,
      String employeeId, String companyId, String path) async {
    var headers = await getHeaders();
    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/document/insert");

    var request = http.MultipartRequest("POST", url);
    request.headers.addAll(headers);
    request.files.add(await http.MultipartFile.fromPath('formFile', path));
    request.fields['DocumentUnitId'] = documentUnitId;
    request.fields['DocumentId'] = documentId;
    request.fields['EmployeeId'] = employeeId;
    var response = await request.send();
    if (response.statusCode == 200) {
      var responseBody = await response.stream.bytesToString();
      dynamic jsonResponse = jsonDecode(responseBody);
      return jsonResponse["id"];
    }
    var exception = await treatUnsuccessfulStreamedResponses(response);
    throw exception;
  }

  Future<String> loadDocumentUnitToSign(
      String dateLimitToSign,
      String eminderEveryNDays,
      String documentUnitId,
      String documentId,
      String employeeId,
      String companyId,
      String path) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/document/insert/send2sign");

    var request = http.MultipartRequest("POST", url);
    request.headers.addAll(headers);
    request.files.add(await http.MultipartFile.fromPath('formFile', path));
    request.fields['DocumentUnitId'] = documentUnitId;
    request.fields['DocumentId'] = documentId;
    request.fields['EmployeeId'] = employeeId;
    request.fields['DateLimitToSign'] =
        DataConvetion.convertToIsoUTC(dateLimitToSign);
    request.fields['EminderEveryNDays'] = eminderEveryNDays;

    var response = await request.send();

    if (response.statusCode == 200) {
      var responseBody = await response.stream.bytesToString();
      dynamic jsonResponse = jsonDecode(responseBody);
      return jsonResponse["id"];
    }
    var exception = await treatUnsuccessfulStreamedResponses(response);
    throw exception;
  }

  Future<List<RequireDocumentSimpleWithDocuments>>
      getAllRequireDocumentsSimpleWithDocuments(
          String companyId, String employeeId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/requiredocuments/withdocuments/$employeeId");

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return RequireDocumentSimpleWithDocuments.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }
}
