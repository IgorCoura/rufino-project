import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:rufino/modules/department/domain/models/department.dart';
import 'package:rufino/shared/services/base_service.dart';

class DepartmentService extends BaseService {
  final Uri peopleManagementUrl =
      Uri.https(const String.fromEnvironment("people_management_url"));

  DepartmentService(super.authService);

  Future<String> create(String companyId, Department department) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = department.toJsonCreate();

    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/department");
    var response =
        await http.post(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> edit(String companyId, Department workplace) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = workplace.toJson();

    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/department");
    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<Department>> getAll(String companyId) async {
    var headers = await getHeaders();
    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/department/all");

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return Department.fromJsonList(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<Department> getById(String companyId, String departmentId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/department/$departmentId");

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return Department.fromJsonSimple(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }
}
