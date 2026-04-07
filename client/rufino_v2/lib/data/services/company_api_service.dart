import 'dart:convert';
import 'package:http/http.dart' as http;

import '../models/company_api_model.dart';
import 'http_status_helper.dart';
import 'request_id_helper.dart';

class CompanyApiService {
  CompanyApiService({
    required this.client,
    required this.baseUrl,
    required this.getAuthHeader,
  });

  final http.Client client;
  final String baseUrl;
  final Future<String> Function() getAuthHeader;

  Future<Map<String, String>> _headers() async {
    return {
      'Authorization': await getAuthHeader(),
      'Content-Type': 'application/json',
      'x-requestid': newRequestId(),
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
      headers: await _headers(),
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
      headers: await _headers(),
      body: jsonEncode(model.toJson()),
    );
    checkHttpStatus(response);
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return json['id'] as String;
  }

}
