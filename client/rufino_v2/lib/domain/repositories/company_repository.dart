import '../../core/result.dart';
import '../entities/company.dart';
import '../entities/company_detail.dart';

abstract class CompanyRepository {
  Future<Result<List<Company>>> getCompanies(List<String> ids);
  Future<Result<CompanyDetail>> getCompanyDetail(String id);
  Future<Result<String>> createCompany(CompanyDetail company);
  Future<Result<String>> updateCompany(CompanyDetail company);
  Future<Result<void>> selectCompany(Company company);
  Future<Result<Company>> getSelectedCompany();
  Future<Result<bool>> verifyAndSelectCompany(List<String> validIds);
}
