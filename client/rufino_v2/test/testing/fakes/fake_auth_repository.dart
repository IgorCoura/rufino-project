import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/domain/repositories/auth_repository.dart';

class FakeAuthRepository implements AuthRepository {
  bool _isAuthenticated = true;
  List<String> _companyIds = ['company-1'];
  Exception? _loginError;
  bool _throwOnHasValidCredentials = false;

  void setAuthenticated(bool value) => _isAuthenticated = value;
  void setCompanyIds(List<String> ids) => _companyIds = ids;
  void setLoginError(Exception error) => _loginError = error;

  /// Causes [hasValidCredentials] to throw instead of returning a [Result].
  void setThrowOnHasValidCredentials(bool value) =>
      _throwOnHasValidCredentials = value;

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
    if (_throwOnHasValidCredentials) {
      throw Exception('Unexpected error');
    }
    return Result.success(_isAuthenticated);
  }

  @override
  Future<Result<void>> logout() async => const Result.success(null);
}
