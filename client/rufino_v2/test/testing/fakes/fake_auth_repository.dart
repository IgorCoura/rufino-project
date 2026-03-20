import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/domain/repositories/auth_repository.dart';

class FakeAuthRepository implements AuthRepository {
  bool _isAuthenticated = true;
  List<String> _companyIds = ['company-1'];
  Exception? _loginError;

  void setAuthenticated(bool value) => _isAuthenticated = value;
  void setCompanyIds(List<String> ids) => _companyIds = ids;
  void setLoginError(Exception error) => _loginError = error;

  @override
  Future<Result<void>> login({required String username, required String password}) async {
    if (_loginError != null) return Result.error(_loginError!);
    return const Result.success(null);
  }

  @override
  Future<Result<List<String>>> getCompanyIds() async {
    if (!_isAuthenticated) return Result.error(Exception('Not authenticated'));
    return Result.success(_companyIds);
  }

  @override
  Future<Result<bool>> hasValidCredentials() async {
    return Result.success(_isAuthenticated);
  }

  @override
  Future<Result<void>> logout() async => const Result.success(null);
}
