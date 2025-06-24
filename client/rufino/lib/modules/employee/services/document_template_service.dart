import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;
import 'package:rufino/modules/employee/domain/model/document_template/document_template.dart';
import 'package:rufino/modules/employee/domain/model/document_template/document_template_simple.dart';
import 'package:rufino/modules/employee/domain/model/document_template/recover_data_type.dart';
import 'package:rufino/modules/employee/domain/model/document_template/type_signature.dart';
import 'package:rufino/shared/services/base_service.dart';

class DocumentTemplateService extends BaseService {
  final Uri peopleManagementUrl =
      Uri.https(const String.fromEnvironment("people_management_url"));

  DocumentTemplateService(super._authService);

  Future<String> createDocumentTemplate(
      String companyId, DocumentTemplate documentTemplate) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = documentTemplate.toJsonCreated();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/documenttemplate");
    var response =
        await http.post(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> editDocumentTemplate(
      String companyId, DocumentTemplate documentTemplate) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = documentTemplate.toJson();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/documenttemplate");
    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<DocumentTemplate>> getAllDocumentTemplates(
      String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/documenttemplate");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return DocumentTemplate.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<DocumentTemplateSimple>> getAllDocumentTemplatesSimple(
      String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/documenttemplate/simple");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return DocumentTemplateSimple.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<DocumentTemplate> getByIdDocumentTemplates(
      String documentTemplateId, String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/documenttemplate/$documentTemplateId");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      dynamic jsonResponse = jsonDecode(response.body);
      return DocumentTemplate.fromJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<RecoverDataType>> getAllRecoverDataType(String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/documenttemplate/recoverdatatype");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return RecoverDataType.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<TypeSignature>> getAllTypeSignature(String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/documenttemplate/typesignature");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return TypeSignature.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<bool> hasFileInDocumentTemplate(
      String companyId, String documentTemplateId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path:
            "/api/v1/$companyId/documenttemplate/hasfile/$documentTemplateId");

    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      bool jsonResponse = jsonDecode(response.body);
      return jsonResponse;
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> loadFileToDocumentTemplate(
      String companyId, String documentTemplateId, String path) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/documenttemplate/upload");

    var request = http.MultipartRequest("POST", url);
    request.headers.addAll(headers);
    request.files.add(await http.MultipartFile.fromPath('formFile', path));
    request.fields['id'] = documentTemplateId;
    request.fields['company'] = companyId;
    var response = await request.send();
    if (response.statusCode == 200) {
      var responseBody = await response.stream.bytesToString();
      dynamic jsonResponse = jsonDecode(responseBody);
      return jsonResponse["id"];
    }
    var exception = await treatUnsuccessfulStreamedResponses(response);
    throw exception;
  }

  Future<File> downloadFileToDocumentTemplate(
      String companyId, String documentTemplateId, String path) async {
    var headers = await getHeaders(contentType: "application/octet-stream");
    var url = peopleManagementUrl.replace(
        path:
            "/api/v1/$companyId/documenttemplate/download/$documentTemplateId");

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      var file = File(path);
      await file.writeAsBytes(response.bodyBytes);
      return file;
    }
    throw treatUnsuccessfulResponses(response);
  }
}
