import '../../core/result.dart';

abstract class AuthRepository {
  Future<Result<void>> login({
    required String username,
    required String password,
  });

  Future<Result<List<String>>> getCompanyIds();

  Future<Result<bool>> hasValidCredentials();

  Future<Result<void>> logout();
}
