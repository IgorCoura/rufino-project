import 'dart:convert';
import 'dart:math';
import 'package:http/http.dart' as http;

import '../models/company_api_model.dart';
import 'http_status_helper.dart';

String _newRequestId() {
  final rnd = Random.secure();
  final bytes = List<int>.generate(16, (_) => rnd.nextInt(256));
  bytes[6] = (bytes[6] & 0x0f) | 0x40; // version 4
  bytes[8] = (bytes[8] & 0x3f) | 0x80; // variant
  String hex(int i) => bytes[i].toRadixString(16).padLeft(2, '0');
  final b = List<String>.generate(16, hex);
  return '${b[0]}${b[1]}${b[2]}${b[3]}-${b[4]}${b[5]}-${b[6]}${b[7]}-${b[8]}${b[9]}-${b[10]}${b[11]}${b[12]}${b[13]}${b[14]}${b[15]}';
}

class CompanyApiService {
  CompanyApiService({
    required this.client,
    required this.baseUrl,
    required this.getAuthHeader,
  });

  final http.Client client;
  final String baseUrl;
  final Future<String> Function() getAuthHeader;

  Future<Map<String, String>> _headers({bool withRequestId = false}) async {
    return {
      'Authorization': await getAuthHeader(),
      'Content-Type': 'application/json',
      if (withRequestId) 'x-requestid': _newRequestId(),
    };
  }

  Future<List<CompanyApiModel>> getCompanies(List<String> ids) async {
    final uri = Uri.https(baseUrl, '/api/v1/company/list', {'id': ids});
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list.map((e) => CompanyApiModel.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<CompanyApiModel> getCompanyDetail(String id) async {
    final uri = Uri.https(baseUrl, '/api/v1/company/complete', {'id': id});
    final response = await client.get(uri, headers: await _headers());
    checkHttpStatus(response);
    return CompanyApiModel.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<String> createCompany(CompanyApiModel model) async {
    final uri = Uri.https(baseUrl, '/api/v1/company');
    final response = await client.post(
      uri,
      headers: await _headers(withRequestId: true),
      body: jsonEncode(model.toCreateJson()),
    );
    checkHttpStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return json['id'] as String;
  }

  Future<String> updateCompany(CompanyApiModel model) async {
    final uri = Uri.https(baseUrl, '/api/v1/company');
    final response = await client.put(
      uri,
      headers: await _headers(withRequestId: true),
      body: jsonEncode(model.toJson()),
    );
    checkHttpStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return json['id'] as String;
  }

}
