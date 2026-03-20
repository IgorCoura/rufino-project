/// Base class for all domain exceptions related to the employee feature.
sealed class EmployeeException implements Exception {
  const EmployeeException();
}

/// Thrown when an employee is not found in the remote API.
final class EmployeeNotFoundException extends EmployeeException {
  const EmployeeNotFoundException(this.id);

  final String id;

  @override
  String toString() => 'EmployeeNotFoundException: resource with id "$id" not found.';
}

/// Thrown when a network or HTTP error occurs while accessing the employee API.
final class EmployeeNetworkException extends EmployeeException {
  const EmployeeNetworkException(this.cause);

  final Object cause;

  @override
  String toString() => 'EmployeeNetworkException: $cause';
}
