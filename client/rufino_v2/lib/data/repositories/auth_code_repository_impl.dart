import '../../core/errors/auth_exception.dart';
import '../../core/monitoring/error_reporter.dart';
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
  AuthCodeRepositoryImpl({
    required this.authCodeApiService,
    required this.reporter,
  });

  final AuthCodeApiService authCodeApiService;
  final ErrorReporter reporter;

  @override
  Future<Result<void>> login({
    required String username,
    required String password,
  }) async {
    try {
      await authCodeApiService.login();
      return const Result.success(null);
    } on AuthException catch (e, st) {
      return reporter.failure(e, st);
    } catch (e, st) {
      return reporter.failure(NetworkAuthException(e), st);
    }
  }

  @override
  Future<Result<List<String>>> getCompanyIds() async {
    try {
      final ids = await authCodeApiService.getCompanyIds();
      return Result.success(ids);
    } on AuthException catch (e, st) {
      return reporter.failure(e, st);
    } catch (e, st) {
      return reporter.failure(NetworkAuthException(e), st);
    }
  }

  @override
  Future<Result<bool>> hasValidCredentials() async {
    try {
      final valid = await authCodeApiService.hasValidCredentials();
      return Result.success(valid);
    } on AuthException catch (e, st) {
      return reporter.failure(e, st);
    } catch (e, st) {
      return reporter.failure(NetworkAuthException(e), st);
    }
  }

  @override
  Future<Result<void>> logout() async {
    try {
      await authCodeApiService.logout();
      return const Result.success(null);
    } catch (e, st) {
      return reporter.failure(NetworkAuthException(e), st);
    }
  }
}
