/// Base class for permission-related exceptions.
sealed class PermissionException implements Exception {
  const PermissionException();
}

/// Thrown when the permission fetch request to Keycloak fails.
final class PermissionFetchException extends PermissionException {
  const PermissionFetchException(this.cause);

  /// The underlying error that caused the failure.
  final Object cause;
}
