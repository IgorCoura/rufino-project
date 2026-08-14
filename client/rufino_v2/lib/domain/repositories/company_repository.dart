import 'package:rufino_core/rufino_core.dart';
import '../entities/company.dart';
import '../entities/company_detail.dart';

/// Access to the People Management company registry.
///
/// There is **no** `createCompany`: a new customer of the platform is born as
/// a tenant, in `tenant_management`, and the company here is derived from it.
/// Keeping a second door open would keep producing companies with no tenant.
abstract class CompanyRepository {
  /// Companies matching [ids].
  Future<Result<List<Company>>> getCompanies(List<String> ids);

  /// The full record of one company.
  Future<Result<CompanyDetail>> getCompanyDetail(String id);

  /// Updates an existing company.
  Future<Result<String>> updateCompany(CompanyDetail company);

  /// Stores [company] as the one People Management operates on.
  Future<Result<void>> selectCompany(Company company);

  /// The company People Management is operating on.
  Future<Result<Company>> getSelectedCompany();

  /// Forgets the selected company.
  ///
  /// Called when the current tenant has no People Management: leaving the
  /// previous company behind would let its screens keep working under a
  /// customer that never enabled the product.
  Future<Result<void>> clearSelectedCompany();
}
