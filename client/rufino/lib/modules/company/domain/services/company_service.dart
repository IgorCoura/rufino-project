import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:rufino/modules/company/domain/models/company_model.dart';
import 'package:rufino/shared/services/base_service.dart';

class CompanyService extends BaseService {
  final Uri peopleManagementUrl =
      Uri.https(const String.fromEnvironment("people_management_url"));

  CompanyService(super.authService);

  Future<String> createCompany(CompanyModel company) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = company.toJsonCreate();

    var url = peopleManagementUrl.replace(path: "/api/v1/company");
    var response =
        await http.post(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> editCompany(CompanyModel company) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = company.toJson();

    var url = peopleManagementUrl.replace(path: "/api/v1/company");
    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<CompanyModel> getCompany(String id) async {
    Map<String, dynamic> queryParams = {"id": id};

    var url = peopleManagementUrl.replace(
        path: "/api/v1/company/complete", queryParameters: queryParams);

    var headers = await getHeaders();

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      Map<String, dynamic> jsonResponse = jsonDecode(response.body);
      return CompanyModel.fromJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }
}
