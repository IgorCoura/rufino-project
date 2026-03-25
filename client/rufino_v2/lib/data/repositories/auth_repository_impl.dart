import '../../core/errors/auth_exception.dart';
import '../../core/result.dart';
import '../../domain/repositories/auth_repository.dart';
import '../services/auth_api_service.dart';

class AuthRepositoryImpl implements AuthRepository {
  const AuthRepositoryImpl({required this.authApiService});

  final AuthApiService authApiService;

  @override
  Future<Result<void>> login({
    required String username,
    required String password,
  }) async {
    try {
      await authApiService.login(username: username, password: password);
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
      final ids = await authApiService.getCompanyIds();
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
      final valid = await authApiService.hasValidCredentials();
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
      await authApiService.logout();
      return const Result.success(null);
    } catch (e) {
      return Result.error(NetworkAuthException(e));
    }
  }
}
