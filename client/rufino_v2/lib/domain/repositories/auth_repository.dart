import 'package:rufino_core/rufino_core.dart';
abstract class AuthRepository {
  Future<Result<void>> login({
    required String username,
    required String password,
  });

  Future<Result<List<String>>> getCompanyIds();

  Future<Result<bool>> hasValidCredentials();

  Future<Result<void>> logout();

  /// Drops the locally stored credentials without contacting the identity
  /// provider — used when the session is already known to be dead.
  Future<Result<void>> clearLocalSession();
}
