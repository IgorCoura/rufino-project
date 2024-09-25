import 'dart:convert';
import 'package:http/http.dart';
import 'package:rufino/domain/services/auth_service.dart';
import 'package:rufino/modules/people/presentation/domain/model/employee.dart';
import 'package:http/http.dart' as http;
import 'package:rufino/modules/people/presentation/domain/model/status.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';
import 'package:rufino/shared/errors/table_convert_errors.dart';

class PeopleManagementService {
  final Uri peopleManagementUrl =
      Uri.https(const String.fromEnvironment("people_management_url"));
  final AuthService _authService;

  PeopleManagementService(this._authService);

  Future<List<Employee>> getEmployees(String? name, String? role, int? status,
      int sortOrder, int pageSize, int sizeSkip) async {
    var company = await getCurrentCompany();

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
        path: "/api/v1/$company/employee/list", queryParameters: queryParams);

    var headers = await getHeaders();

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return Employee.fromListJson(jsonResponse);
    }
    return treatUnsuccessfulResponses(response);
  }

  Future<List<Status>> getStatus() async {
    var company = await getCurrentCompany();
    var headers = await getHeaders();
    var url =
        peopleManagementUrl.replace(path: "/api/v1/$company/employee/status");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return Status.fromListJson(jsonResponse);
    }
    return treatUnsuccessfulResponses(response);
  }

  Future<String> getCurrentCompany() {
    var company =
        "725f687c-7219-42c5-92ba-07064849f524"; //TODO: Implementar comapny Feature
    return Future.value(company);
  }

  Future<Map<String, String>> getHeaders() async {
    var headers = <String, String>{};
    headers["Connection"] = "keep-alive";
    headers["Content-Type"] = "application/json";
    headers["Accept"] = "*/*";
    headers["Authorization"] = await _authService.getAuthorizationHeader();
    return headers;
  }

  T treatUnsuccessfulResponses<T>(Response response) {
    try {
      if (response.statusCode == 401 || response.statusCode == 403) {
        throw AplicationErrors.auth.unauthorizedAccess;
      }
      Map<String, dynamic> jsonResponse = jsonDecode(response.body);
      throw ConvertErrors.fromResponseBodyServer(jsonResponse);
    } catch (ex) {
      throw AplicationErrors.serverError;
    }
  }
}
