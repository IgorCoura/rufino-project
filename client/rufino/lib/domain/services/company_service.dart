import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:rufino/domain/model/company.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';
import 'package:rufino/shared/services/base_service.dart';
import 'package:http/http.dart' as http;

class CompanyService extends BaseService {
  final Uri peopleManagementUrl =
      Uri.https(const String.fromEnvironment("people_management_url"));
  final FlutterSecureStorage _storage;

  Company? _selectedCompany;

  CompanyService(this._storage, super.authService);

  Future<List<Company>> getCompanies(List<String> companiesIds) async {
    Map<String, dynamic> queryParams = {"id": companiesIds};

    var url = peopleManagementUrl.replace(
        path: "/api/v1/company/list", queryParameters: queryParams);

    var headers = await getHeaders();

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return Company.fromListJson(jsonResponse);
    }
    return treatUnsuccessfulResponses(response);
  }

  Future selectCompany(Company company) async {
    _selectedCompany = company;
    await _storage.write(key: "company", value: company.toJson());
  }

  Future<Company> getSelectedCompany() async {
    if (_selectedCompany == null) {
      var selectedCompany = await _storage.read(key: "company");
      if (selectedCompany == null) {
        throw AplicationErrors.company.selectedCompanyErro;
      }
      _selectedCompany = Company.fromJson(selectedCompany);
    }
    return _selectedCompany!;
  }

  Future<bool> hasCompanySeleted() async {
    if (_selectedCompany == null) {
      try {
        await getSelectedCompany();
        return true;
      } catch (ex) {
        return false;
      }
    }
    return true;
  }
}
