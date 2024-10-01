import 'dart:convert';
import 'package:rufino/modules/people/presentation/domain/model/employee.dart';
import 'package:http/http.dart' as http;
import 'package:rufino/modules/people/presentation/domain/model/status.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';
import 'package:rufino/shared/services/base_service.dart';

class PeopleManagementService extends BaseService {
  final Uri peopleManagementUrl =
      Uri.https(const String.fromEnvironment("people_management_url"));

  PeopleManagementService(super._authService);

  Future<String> createEmployee(String companyId, String name) async {
    var headers = await getHeadersWithRequestId();
    Map<String, String> body = {
      "name": name,
    };
    var url = peopleManagementUrl.replace(path: "/api/v1/$companyId/employee");

    var response =
        await http.post(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    return treatUnsuccessfulResponses(response);
  }

  Future<List<Employee>> getEmployees(
      String companyId,
      String? name,
      String? role,
      int? status,
      int sortOrder,
      int pageSize,
      int sizeSkip) async {
    Map<String, dynamic> queryParams = {
      "name": name,
      "role": role,
      "status": status,
      "sortOrder": sortOrder,
      "pageSize": pageSize,
      "sizeSkip": sizeSkip
    }..removeWhere((key, value) => value == null);

    queryParams =
        queryParams.map((key, value) => MapEntry(key, value.toString()));

    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/list", queryParameters: queryParams);

    var headers = await getHeaders();

    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return Employee.fromListJson(jsonResponse);
    }
    return treatUnsuccessfulResponses(response);
  }

  Future<List<Status>> getStatus(String companyId) async {
    var headers = await getHeaders();
    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/employee/status");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return Status.fromListJson(jsonResponse);
    }
    return treatUnsuccessfulResponses(response);
  }
}
