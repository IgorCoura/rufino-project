import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/company_detail.dart';
import 'package:rufino_v2/domain/repositories/company_repository.dart';

class FakeCompanyRepository implements CompanyRepository {
  List<Company> _companies = [
    const Company(id: 'company-1', corporateName: 'Acme Corp S.A.', fantasyName: 'Acme', cnpj: '12.345.678/0001-90'),
  ];
  Company? _selectedCompany;
  bool _verifyResult = false;
  bool _verifyError = false;

  void setCompanies(List<Company> companies) => _companies = companies;
  void setSelectedCompany(Company? company) => _selectedCompany = company;
  void setVerifyResult(bool result) => _verifyResult = result;
  void setVerifyError(bool error) => _verifyError = error;

  @override
  Future<Result<List<Company>>> getCompanies(List<String> ids) async {
    return Result.success(_companies);
  }

  @override
  Future<Result<CompanyDetail>> getCompanyDetail(String id) async {
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
  Future<Result<String>> createCompany(CompanyDetail company) async {
    return const Result.success('new-company-id');
  }

  @override
  Future<Result<String>> updateCompany(CompanyDetail company) async {
    return Result.success(company.id);
  }

  @override
  Future<Result<void>> selectCompany(Company company) async {
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
  Future<Result<bool>> verifyAndSelectCompany(List<String> validIds) async {
    if (_verifyError) return Result.error(Exception('Verify failed'));
    return Result.success(_verifyResult);
  }
}
