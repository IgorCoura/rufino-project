/// The employment status of an employee.
enum EmployeeStatus {
  /// No status assigned (used as a sentinel / "all" filter value).
  none(0, 'Todos'),

  /// Employee is pending onboarding.
  pending(1, 'Pendente'),

  /// Employee is actively employed.
  active(2, 'Ativo'),

  /// Employee is on vacation.
  vacation(3, 'Férias'),

  /// Employee is on leave.
  away(4, 'Afastado'),

  /// Employee is no longer employed.
  inactive(5, 'Inativo');

  const EmployeeStatus(this.id, this.label);

  /// The numeric identifier used by the API.
  final int id;

  /// The human-readable Portuguese label for this status.
  final String label;

  /// Returns the [EmployeeStatus] matching [id], or [none] if not found.
  static EmployeeStatus fromId(int id) {
    return EmployeeStatus.values.firstWhere(
      (s) => s.id == id,
      orElse: () => EmployeeStatus.none,
    );
  }
}

/// The document compliance status of an employee.
enum DocumentStatus {
  /// All document statuses (used as a sentinel / "all" filter value).
  all(-1, 'Todos'),

  /// All documents are current.
  ok(0, 'OK'),

  /// Some documents are about to expire.
  expiringSoon(1, 'Há Vencer'),

  /// Some documents require attention.
  requiresAttention(2, 'Requer Atenção');

  const DocumentStatus(this.id, this.label);

  /// The numeric identifier used by the API.
  final int id;

  /// The human-readable Portuguese label for this status.
  final String label;

  /// Returns the [DocumentStatus] matching [id], or [all] if not found.
  static DocumentStatus fromId(int id) {
    return DocumentStatus.values.firstWhere(
      (s) => s.id == id,
      orElse: () => DocumentStatus.all,
    );
  }
}

/// A summary of an employee as returned by the employee list endpoint.
class Employee {
  const Employee({
    required this.id,
    required this.name,
    required this.registration,
    required this.status,
    required this.roleName,
    required this.documentStatus,
  });

  final String id;
  final String name;
  final String registration;

  /// The current employment status of this employee.
  final EmployeeStatus status;

  /// The name of the role assigned to this employee.
  final String roleName;

  /// The document compliance status of this employee.
  final DocumentStatus documentStatus;
}
