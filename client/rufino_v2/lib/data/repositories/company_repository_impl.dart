import 'dart:convert';

import '../../core/result.dart';
import '../../core/storage/secure_storage.dart';
import '../../domain/entities/company.dart';
import '../../domain/entities/company_detail.dart';
import '../../domain/repositories/company_repository.dart';
import '../models/company_api_model.dart';
import '../services/company_api_service.dart';

class CompanyRepositoryImpl implements CompanyRepository {
  const CompanyRepositoryImpl({
    required this.companyApiService,
    required this.storage,
  });

  final CompanyApiService companyApiService;
  final SecureStorage storage;

  static const _selectedCompanyKey = 'selected_company';

  @override
  Future<Result<List<Company>>> getCompanies(List<String> ids) async {
    try {
      final models = await companyApiService.getCompanies(ids);
      return Result.success(models.map((m) => m.toEntity()).toList());
    } catch (e) {
      return Result.error(e);
    }
  }

  @override
  Future<Result<CompanyDetail>> getCompanyDetail(String id) async {
    try {
      final model = await companyApiService.getCompanyDetail(id);
      return Result.success(model.toDetailEntity());
    } catch (e) {
      return Result.error(e);
    }
  }

  @override
  Future<Result<String>> createCompany(CompanyDetail company) async {
    try {
      final model = CompanyApiModel(
        id: company.id,
        corporateName: company.corporateName,
        fantasyName: company.fantasyName,
        cnpj: company.cnpj,
        email: company.email,
        phone: company.phone,
        zipCode: company.zipCode,
        street: company.street,
        number: company.number,
        complement: company.complement,
        neighborhood: company.neighborhood,
        city: company.city,
        state: company.state,
        country: company.country,
      );
      final id = await companyApiService.createCompany(model);
      return Result.success(id);
    } catch (e) {
      return Result.error(e);
    }
  }

  @override
  Future<Result<String>> updateCompany(CompanyDetail company) async {
    try {
      final model = CompanyApiModel(
        id: company.id,
        corporateName: company.corporateName,
        fantasyName: company.fantasyName,
        cnpj: company.cnpj,
        email: company.email,
        phone: company.phone,
        zipCode: company.zipCode,
        street: company.street,
        number: company.number,
        complement: company.complement,
        neighborhood: company.neighborhood,
        city: company.city,
        state: company.state,
        country: company.country,
      );
      final id = await companyApiService.updateCompany(model);
      return Result.success(id);
    } catch (e) {
      return Result.error(e);
    }
  }

  @override
  Future<Result<void>> selectCompany(Company company) async {
    try {
      final json = jsonEncode({
        'id': company.id,
        'corporateName': company.corporateName,
        'fantasyName': company.fantasyName,
        'cnpj': company.cnpj,
      });
      await storage.write(key: _selectedCompanyKey, value: json);
      return const Result.success(null);
    } catch (e) {
      return Result.error(e);
    }
  }

  @override
  Future<Result<Company>> getSelectedCompany() async {
    try {
      final json = await storage.read(key: _selectedCompanyKey);
      if (json == null) {
        return Result.error(Exception('No company selected'));
      }
      final map = jsonDecode(json) as Map<String, dynamic>;
      return Result.success(Company(
        id: map['id'] as String,
        corporateName: map['corporateName'] as String,
        fantasyName: map['fantasyName'] as String,
        cnpj: map['cnpj'] as String,
      ));
    } catch (e) {
      return Result.error(e);
    }
  }

  @override
  Future<Result<bool>> verifyAndSelectCompany(List<String> validIds) async {
    // Try to load saved company and verify it is still valid
    final savedResult = await getSelectedCompany();
    if (savedResult.isSuccess) {
      final saved = savedResult.valueOrNull!;
      if (validIds.contains(saved.id)) {
        // Refresh company data from API
        final detailResult = await getCompanyDetail(saved.id);
        if (detailResult.isSuccess) {
          final refreshed = detailResult.valueOrNull!;
          await selectCompany(refreshed);
          return const Result.success(true);
        }
      }
    }
    return const Result.success(false);
  }
}
