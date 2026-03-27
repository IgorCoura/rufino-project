import 'package:flutter/foundation.dart';

import '../../../../domain/repositories/auth_repository.dart';
import '../../../../domain/repositories/company_repository.dart';
import 'permission_notifier.dart';

enum SplashStatus { loading, authenticated, unauthenticated, noCompany, error }

class SplashViewModel extends ChangeNotifier {
  SplashViewModel({
    required AuthRepository authRepository,
    required CompanyRepository companyRepository,
    required PermissionNotifier permissionNotifier,
  })  : _authRepository = authRepository,
        _companyRepository = companyRepository,
        _permissionNotifier = permissionNotifier;

  final AuthRepository _authRepository;
  final CompanyRepository _companyRepository;
  final PermissionNotifier _permissionNotifier;

  SplashStatus _status = SplashStatus.loading;
  String? _errorMessage;

  SplashStatus get status => _status;
  String? get errorMessage => _errorMessage;

  Future<void> initialize() async {
    if (_status != SplashStatus.loading) return;

    try {
      final credentialsResult = await _authRepository.hasValidCredentials();
      if (credentialsResult.isError || credentialsResult.valueOrNull == false) {
        _status = SplashStatus.unauthenticated;
        return;
      }

      final idsResult = await _authRepository.getCompanyIds();
      if (idsResult.isError) {
        _status = SplashStatus.unauthenticated;
        return;
      }

      final validIds = idsResult.valueOrNull!;
      final hasCompanyResult =
          await _companyRepository.verifyAndSelectCompany(validIds);

      if (hasCompanyResult.isError) {
        _status = SplashStatus.error;
        return;
      }

      if (hasCompanyResult.valueOrNull == true) {
        await _permissionNotifier.loadPermissions();
        _status = SplashStatus.authenticated;
      } else {
        _status = SplashStatus.noCompany;
      }
    } catch (_) {
      _status = SplashStatus.error;
    } finally {
      notifyListeners();
    }
  }
}
