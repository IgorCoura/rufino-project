import 'package:rufino_core/rufino_core.dart';
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

/// The backend answered 403: the session is valid but the user lacks
/// permission for the requested resource.
final class AccessDeniedException extends AuthException with ExpectedFailure {
  const AccessDeniedException();
}

final class NetworkAuthException extends AuthException {
  const NetworkAuthException(this.cause);

  final Object cause;
}
