import '../expected_failure.dart';

/// Base class for authentication and authorization failures.
sealed class AuthException implements Exception {
  /// Const constructor for subclasses.
  const AuthException();
}

/// The identity provider rejected the credentials supplied at login.
final class InvalidCredentialsException extends AuthException
    with ExpectedFailure {
  /// Creates the exception.
  const InvalidCredentialsException();
}

/// The API answered 401: the session's token is no longer accepted.
final class SessionExpiredException extends AuthException with ExpectedFailure {
  /// Creates the exception.
  const SessionExpiredException();
}

/// No credentials are stored on the device.
final class NoCredentialsException extends AuthException with ExpectedFailure {
  /// Creates the exception.
  const NoCredentialsException();
}

/// The backend answered 403: the session is valid but the user lacks
/// permission for the requested resource.
final class AccessDeniedException extends AuthException with ExpectedFailure {
  /// Creates the exception.
  const AccessDeniedException();
}

/// A transport failure while talking to the identity provider.
final class NetworkAuthException extends AuthException {
  /// Creates the exception wrapping the underlying [cause].
  const NetworkAuthException(this.cause);

  /// The underlying error that caused the failure.
  final Object cause;
}
