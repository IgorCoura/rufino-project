import 'dart:convert';
import 'package:rufino/modules/employee/domain/model/Address/Address.dart';
import 'package:rufino/modules/employee/domain/model/contact/contact.dart';
import 'package:rufino/modules/employee/domain/model/employee.dart';
import 'package:rufino/modules/employee/domain/model/employee_with_role.dart';
import 'package:http/http.dart' as http;
import 'package:rufino/modules/employee/domain/model/status.dart';
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

  Future<String> editEmployeeName(
      String employeeId, String companyId, String name) async {
    var headers = await getHeadersWithRequestId();
    Map<String, String> body = {
      "employeeId": employeeId,
      "name": name,
    };
    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/employee/name");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    return treatUnsuccessfulResponses(response);
  }

  Future<String> editEmployeeContact(String employeeId, String companyId,
      String cellphone, String email) async {
    var headers = await getHeadersWithRequestId();
    Map<String, String> body = {
      "employeeId": employeeId,
      "cellphone": cellphone,
      "email": email,
    };
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/contact");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    return treatUnsuccessfulResponses(response);
  }

  Future<String> editEmployeeAddress(
      String employeeId, String companyId, Address address) async {
    var headers = await getHeadersWithRequestId();
    Map<String, String> body = address.toJson();
    body.addAll({
      "employeeId": employeeId,
    });
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/address");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    return treatUnsuccessfulResponses(response);
  }

  Future<Employee> getEmployee(String id, String companyId) async {
    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/employee/$id");

    var headers = await getHeaders();

    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return Employee.fromJson(jsonResponse);
    }
    return treatUnsuccessfulResponses(response);
  }

  Future<Contact> getEmployeeContact(String id, String companyId) async {
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/contact/$id");

    var headers = await getHeaders();

    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return Contact.fromJson(jsonResponse);
    }
    return treatUnsuccessfulResponses(response);
  }

  Future<Address> getEmployeeAddress(String id, String companyId) async {
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/address/$id");

    var headers = await getHeaders();

    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return Address.fromJson(jsonResponse);
    }
    return treatUnsuccessfulResponses(response);
  }

  Future<List<EmployeeWithRole>> getEmployeesWithRoles(
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
        path: "/api/v1/$companyId/employee/list/roles",
        queryParameters: queryParams);

    var headers = await getHeaders();

    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return EmployeeWithRole.fromListJson(jsonResponse);
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