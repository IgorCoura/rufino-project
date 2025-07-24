import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:rufino/modules/employee/domain/model/document_group/document_group.dart';
import 'package:rufino/modules/employee/domain/model/document_group/document_group_with_documents.dart';
import 'package:rufino/shared/services/base_service.dart';

class DocumentGroupService extends BaseService {
  final Uri peopleManagementUrl =
      Uri.https(const String.fromEnvironment("people_management_url"));
  DocumentGroupService(super._authService);

  Future<String> create(
      String companyId, String name, String description) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = {
      "name": name,
      "description": description,
    };
    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/documentgroup");
    var response =
        await http.post(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> update(String companyId, DocumentGroup model) async {
    var headers = await getHeadersWithRequestId();

    Map<String, dynamic> body = model.toJson();

    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/documentgroup");
    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<DocumentGroup>> getAll(String companyId) async {
    var headers = await getHeaders();
    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/documentgroup");

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return DocumentGroup.fromJsonList(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<DocumentGroupWithDocuments>> getAllWithDocuments(
      String companyId, String employeeId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/documentgroup/withdocuments/$employeeId");

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return DocumentGroupWithDocuments.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }
}
