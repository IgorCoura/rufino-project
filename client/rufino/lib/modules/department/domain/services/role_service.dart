import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:rufino/modules/department/domain/models/remuneration.dart';
import 'package:rufino/modules/department/domain/models/role.dart';
import 'package:rufino/shared/services/base_service.dart';

class RoleService extends BaseService {
  final Uri peopleManagementUrl =
      Uri.https(const String.fromEnvironment("people_management_url"));

  RoleService(super.authService);

  Future<String> create(String companyId, String positionId, Role role) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = role.toJsonCreate();
    body["positionId"] = positionId;

    var url = peopleManagementUrl.replace(path: "/api/v1/$companyId/role");
    var response =
        await http.post(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> edit(String companyId, Role role) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = role.toJson();

    var url = peopleManagementUrl.replace(path: "/api/v1/$companyId/role");
    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<Role> getById(String companyId, String positionId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/role/$positionId");

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return Role.fromJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<PaymentUnit>> getPaymentUnit(String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/role/paymentUnit");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return PaymentUnit.fromJsonList(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<SalaryType>> getSalaryType(String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/role/currencyType");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return SalaryType.fromJsonList(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }
}
