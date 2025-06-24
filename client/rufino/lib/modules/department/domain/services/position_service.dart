import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:rufino/modules/department/domain/models/position.dart';
import 'package:rufino/shared/services/base_service.dart';

class PositionService extends BaseService {
  final Uri peopleManagementUrl =
      Uri.https(const String.fromEnvironment("people_management_url"));

  PositionService(super.authService);

  Future<String> create(
      String companyId, String departmentId, Position position) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = position.toJsonCreate();
    body["departmentId"] = departmentId;

    var url = peopleManagementUrl.replace(path: "/api/v1/$companyId/position");
    var response =
        await http.post(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> edit(String companyId, Position position) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = position.toJson();

    var url = peopleManagementUrl.replace(path: "/api/v1/$companyId/position");
    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<Position> getById(String companyId, String positionId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/position/$positionId");

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return Position.fromJsonSimple(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }
}
