import 'expected_failure.dart';

sealed class AuthException implements Exception {
  const AuthException();
}

final class InvalidCredentialsException extends AuthException
    with ExpectedFailure {
  const InvalidCredentialsException();
}

final class SessionExpiredException extends AuthException with ExpectedFailure {
  const SessionExpiredException();
}

final class NoCredentialsException extends AuthException with ExpectedFailure {
  const NoCredentialsException();
}

final class NetworkAuthException extends AuthException {
  const NetworkAuthException(this.cause);

  final Object cause;
}
