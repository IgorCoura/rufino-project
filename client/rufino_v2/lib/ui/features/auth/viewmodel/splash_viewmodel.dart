import 'package:flutter/foundation.dart';

import '../../../../domain/repositories/auth_repository.dart';
import '../../../../domain/repositories/company_repository.dart';

enum SplashStatus { loading, authenticated, unauthenticated, noCompany, error }

class SplashViewModel extends ChangeNotifier {
  SplashViewModel({
    required AuthRepository authRepository,
    required CompanyRepository companyRepository,
  })  : _authRepository = authRepository,
        _companyRepository = companyRepository;

  final AuthRepository _authRepository;
  final CompanyRepository _companyRepository;

  SplashStatus _status = SplashStatus.loading;
  String? _errorMessage;

  SplashStatus get status => _status;
  String? get errorMessage => _errorMessage;

  Future<void> initialize() async {
    if (_status != SplashStatus.loading) return;

    final credentialsResult = await _authRepository.hasValidCredentials();
    if (credentialsResult.isError || credentialsResult.valueOrNull == false) {
      _status = SplashStatus.unauthenticated;
      notifyListeners();
      return;
    }

    final idsResult = await _authRepository.getCompanyIds();
    if (idsResult.isError) {
      _status = SplashStatus.unauthenticated;
      notifyListeners();
      return;
    }

    final validIds = idsResult.valueOrNull!;
    final hasCompanyResult = await _companyRepository.verifyAndSelectCompany(validIds);

    if (hasCompanyResult.isError) {
      _status = SplashStatus.error;
      notifyListeners();
      return;
    }

    if (hasCompanyResult.valueOrNull == true) {
      _status = SplashStatus.authenticated;
    } else {
      _status = SplashStatus.noCompany;
    }
    notifyListeners();
  }
}
