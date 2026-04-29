import '../../core/errors/auth_exception.dart';
import '../../core/result.dart';
import '../../domain/repositories/auth_repository.dart';
import '../services/auth_code_api_service.dart';

/// [AuthRepository] backed by the Authorization Code Flow + PKCE service.
///
/// The standard [AuthRepository.login] signature accepts username and
/// password, but the Auth Code Flow does not collect credentials inside
/// the app — the browser does. Both parameters are accepted and ignored
/// so the existing UI contract continues to work; the SSO login screen
/// passes empty strings.
class AuthCodeRepositoryImpl implements AuthRepository {
  const AuthCodeRepositoryImpl({required this.authCodeApiService});

  final AuthCodeApiService authCodeApiService;

  @override
  Future<Result<void>> login({
    required String username,
    required String password,
  }) async {
    try {
      await authCodeApiService.login();
      return const Result.success(null);
    } on AuthException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(NetworkAuthException(e));
    }
  }

  @override
  Future<Result<List<String>>> getCompanyIds() async {
    try {
      final ids = await authCodeApiService.getCompanyIds();
      return Result.success(ids);
    } on AuthException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(NetworkAuthException(e));
    }
  }

  @override
  Future<Result<bool>> hasValidCredentials() async {
    try {
      final valid = await authCodeApiService.hasValidCredentials();
      return Result.success(valid);
    } on AuthException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(NetworkAuthException(e));
    }
  }

  @override
  Future<Result<void>> logout() async {
    try {
      await authCodeApiService.logout();
      return const Result.success(null);
    } catch (e) {
      return Result.error(NetworkAuthException(e));
    }
  }
}
