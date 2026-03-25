sealed class AuthException implements Exception {
  const AuthException();
}

final class InvalidCredentialsException extends AuthException {
  const InvalidCredentialsException();
}

final class SessionExpiredException extends AuthException {
  const SessionExpiredException();
}

final class NoCredentialsException extends AuthException {
  const NoCredentialsException();
}

final class NetworkAuthException extends AuthException {
  const NetworkAuthException(this.cause);

  final Object cause;
}
