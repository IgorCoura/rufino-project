/// Base class for all domain exceptions related to the department feature.
sealed class DepartmentException implements Exception {
  const DepartmentException();
}

/// Thrown when a department, position, or role is not found in the remote API.
final class DepartmentNotFoundException extends DepartmentException {
  const DepartmentNotFoundException(this.id);

  final String id;

  @override
  String toString() => 'DepartmentNotFoundException: resource with id "$id" not found.';
}

/// Thrown when a network or HTTP error occurs while accessing the department API.
final class DepartmentNetworkException extends DepartmentException {
  const DepartmentNetworkException(this.cause);

  final Object cause;

  @override
  String toString() => 'DepartmentNetworkException: $cause';
}
