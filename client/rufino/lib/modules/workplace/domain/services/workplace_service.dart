import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:rufino/modules/workplace/domain/model/workplace.dart';
import 'package:rufino/shared/services/base_service.dart';

class WorkplaceService extends BaseService {
  final Uri peopleManagementUrl =
      Uri.https(const String.fromEnvironment("people_management_url"));
  WorkplaceService(super.authService);

  Future<String> create(String companyId, Workplace workplace) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = workplace.toJsonCreate();

    var url = peopleManagementUrl.replace(path: "/api/v1/$companyId/workplace");
    var response =
        await http.post(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> edit(String companyId, Workplace workplace) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = workplace.toJson();

    var url = peopleManagementUrl.replace(path: "/api/v1/$companyId/workplace");
    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<Workplace>> getAll(String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(path: "/api/v1/$companyId/workplace");

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return Workplace.fromJsonList(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<Workplace> getById(String companyId, String workplaceId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/workplace/$workplaceId");

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return Workplace.fromJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }
}
