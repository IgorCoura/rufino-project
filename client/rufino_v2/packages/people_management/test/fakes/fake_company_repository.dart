import 'package:rufino_core/rufino_core.dart';
import 'package:people_management/people_management.dart';

class FakeCompanyRepository implements CompanyRepository {
  List<Company> _companies = [
    const Company(
      id: 'company-1',
      corporateName: 'Acme Corp S.A.',
      fantasyName: 'Acme',
      cnpj: '12.345.678/0001-90',
    ),
  ];
  Company? _selectedCompany;
  bool _selectShouldFail = false;
  bool _detailShouldFail = false;

  /// Whether [clearSelectedCompany] was called.
  bool selectionCleared = false;

  void setCompanies(List<Company> companies) => _companies = companies;
  void setSelectedCompany(Company? company) => _selectedCompany = company;
  void setSelectShouldFail(bool value) => _selectShouldFail = value;
  void setDetailShouldFail(bool value) => _detailShouldFail = value;

  @override
  Future<Result<List<Company>>> getCompanies(List<String> ids) async {
    return Result.success(_companies);
  }

  @override
  Future<Result<CompanyDetail>> getCompanyDetail(String id) async {
    if (_detailShouldFail) {
      return Result.error(Exception('Company not found'));
    }
    return Result.success(CompanyDetail(
      id: id,
      corporateName: 'Acme Corp S.A.',
      fantasyName: 'Acme',
      cnpj: '12.345.678/0001-90',
      email: 'contato@acme.com',
      phone: '11999999999',
      zipCode: '01310100',
      street: 'Av. Paulista',
      number: '1000',
      complement: '',
      neighborhood: 'Bela Vista',
      city: 'São Paulo',
      state: 'SP',
      country: 'Brasil',
    ));
  }

  @override
  Future<Result<String>> updateCompany(CompanyDetail company) async {
    return Result.success(company.id);
  }

  @override
  Future<Result<void>> selectCompany(Company company) async {
    if (_selectShouldFail) {
      return Result.error(Exception('Select company failed'));
    }
    _selectedCompany = company;
    return const Result.success(null);
  }

  @override
  Future<Result<Company>> getSelectedCompany() async {
    if (_selectedCompany == null) {
      return Result.error(Exception('No company selected'));
    }
    return Result.success(_selectedCompany!);
  }

  @override
  Future<Result<void>> clearSelectedCompany() async {
    selectionCleared = true;
    _selectedCompany = null;
    return const Result.success(null);
  }
}
