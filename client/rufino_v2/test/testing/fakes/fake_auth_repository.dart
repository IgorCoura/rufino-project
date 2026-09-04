import 'package:rufino_core/rufino_core.dart';
import 'package:rufino_v2/domain/repositories/auth_repository.dart';

class FakeAuthRepository implements AuthRepository {
  bool _isAuthenticated = true;
  Exception? _loginError;
  bool _throwOnHasValidCredentials = false;

  void setAuthenticated(bool value) => _isAuthenticated = value;
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
  Future<Result<bool>> hasValidCredentials() async {
    if (_throwOnHasValidCredentials) {
      throw Exception('Unexpected error');
    }
    return Result.success(_isAuthenticated);
  }

  @override
  Future<Result<void>> logout() async => const Result.success(null);

  /// Number of times [clearLocalSession] was called.
  int clearLocalSessionCalls = 0;

  @override
  Future<Result<void>> clearLocalSession() async {
    clearLocalSessionCalls++;
    _isAuthenticated = false;
    return const Result.success(null);
  }
}
