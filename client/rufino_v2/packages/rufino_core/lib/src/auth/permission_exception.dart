/// Base class for permission-related exceptions.
sealed class PermissionException implements Exception {
  /// Const constructor for subclasses.
  const PermissionException();
}

/// Thrown when the permission fetch request to Keycloak fails.
final class PermissionFetchException extends PermissionException {
  /// Creates the exception wrapping the underlying [cause].
  const PermissionFetchException(this.cause);

  /// The underlying error that caused the failure.
  final Object cause;
}
