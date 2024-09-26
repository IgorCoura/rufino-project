import 'dart:convert';

import 'package:rufino/domain/model/company.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';
import 'package:rufino/shared/services/base_service.dart';
import 'package:http/http.dart' as http;

class CompanyService extends BaseService {
  final Uri peopleManagementUrl =
      Uri.https(const String.fromEnvironment("people_management_url"));

  Company? _selectedCompany;

  CompanyService(super.authService);

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

  void selectCompany(Company company) {
    _selectedCompany = company;
  }

  Company getSelectedCompany() {
    if (_selectedCompany == null) {
      throw AplicationErrors.company.selectedCompanyErro;
    }
    return _selectedCompany!;
  }

  Future<bool> hasCompanySeleted() async {
    if (_selectedCompany == null) {
      return false;
    }
    var companies = await getCompanies([_selectedCompany!.id]);
    _selectedCompany = companies.firstOrNull;

    if (_selectedCompany == null) {
      return false;
    }
    return true;
  }
}
