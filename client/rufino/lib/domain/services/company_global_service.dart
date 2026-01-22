import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:rufino/domain/model/company.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';
import 'package:rufino/shared/services/base_service.dart';
import 'package:http/http.dart' as http;

class CompanyGlobalService extends BaseService {
  final Uri peopleManagementUrl =
      Uri.https(const String.fromEnvironment("people_management_url"));
  final FlutterSecureStorage _storage;

  Company? _selectedCompany;

  CompanyGlobalService(this._storage, super.authService);

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
    throw treatUnsuccessfulResponses(response);
  }

  Future<Company> getCompany(companyId) async {
    Map<String, dynamic> queryParams = {"id": companyId};

    var url = peopleManagementUrl.replace(
        path: "/api/v1/company", queryParameters: queryParams);

    var headers = await getHeaders();

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      Map<String, dynamic> jsonResponse = jsonDecode(response.body);
      return Company.fromMap(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
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

  Future<bool> hasCompanySeleted(List<String> validCompanies) async {
    if (_selectedCompany != null &&
        validCompanies.contains(_selectedCompany?.id)) {
      return true;
    }
    try {
      await refreshSelectedCompany();
      if (validCompanies.contains(_selectedCompany?.id)) {
        return true;
      }
      return false;
    } catch (ex) {
      return false;
    }
  }

  Future<bool> verifyAndSelectCompany(List<String> validCompanies) async {
    if (_selectedCompany != null &&
        validCompanies.contains(_selectedCompany?.id)) {
      var refreshedCompany = await getCompany(_selectedCompany!.id);
      await selectCompany(refreshedCompany);
      return true;
    }
    try {
      var selectedCompany = await getSelectedCompany();
      var refreshedCompany = await getCompany(selectedCompany.id);
      await selectCompany(refreshedCompany);
      if (validCompanies.contains(_selectedCompany?.id)) {
        return true;
      }
      return false;
    } catch (ex) {
      return false;
    }
  }

  Future refreshSelectedCompany() async {
    var selectedCompany = await getSelectedCompany();
    var refreshedCompany = await getCompany(selectedCompany.id);
    await selectCompany(refreshedCompany);
  }
}
