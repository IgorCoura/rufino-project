import 'package:flutter/foundation.dart';

import '../../../../domain/entities/company.dart';
import '../../../../domain/repositories/auth_repository.dart';
import '../../../../domain/repositories/company_repository.dart';

enum HomeStatus { loading, loaded, error }

class HomeViewModel extends ChangeNotifier {
  HomeViewModel({
    required AuthRepository authRepository,
    required CompanyRepository companyRepository,
  })  : _authRepository = authRepository,
        _companyRepository = companyRepository;

  final AuthRepository _authRepository;
  final CompanyRepository _companyRepository;

  Company? _company;
  HomeStatus _status = HomeStatus.loading;
  String? _errorMessage;

  Company? get company => _company;
  HomeStatus get status => _status;
  String? get errorMessage => _errorMessage;
  bool get isLoading => _status == HomeStatus.loading;

  /// Returns the best display name for the loaded company, or "Rufino" as
  /// fallback when no company is loaded.
  String get companyDisplayName => _company?.displayName ?? 'Rufino';

  Future<void> loadCompany() async {
    _status = HomeStatus.loading;
    notifyListeners();

    final result = await _companyRepository.getSelectedCompany();
    result.fold(
      onSuccess: (company) {
        _company = company;
        _status = HomeStatus.loaded;
      },
      onError: (_) {
        _status = HomeStatus.error;
        _errorMessage = 'Falha ao carregar empresa.';
      },
    );
    notifyListeners();
  }

  Future<void> logout() async {
    await _authRepository.logout();
  }
}
