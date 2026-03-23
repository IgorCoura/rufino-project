import 'employee.dart';

/// A detailed employee profile used by the employee detail screen.
class EmployeeProfile {
  const EmployeeProfile({
    required this.id,
    required this.name,
    required this.registration,
    required this.status,
    required this.roleId,
    required this.workplaceId,
    this.documentSigningOptionsId = '',
  });

  final String id;
  final String name;
  final String registration;

  /// The current employment status of this employee.
  final EmployeeStatus status;

  /// The assigned role id, or an empty string when no role is assigned.
  final String roleId;

  /// The assigned workplace id, or an empty string when no workplace is assigned.
  final String workplaceId;

  /// The current document signing option id, or empty when not set.
  final String documentSigningOptionsId;

  /// Whether the employee can be marked as inactive from the profile screen.
  bool get canMarkAsInactive => status == EmployeeStatus.active;

  /// Returns a copy of this profile with the provided overrides applied.
  EmployeeProfile copyWith({
    String? id,
    String? name,
    String? registration,
    EmployeeStatus? status,
    String? roleId,
    String? workplaceId,
    String? documentSigningOptionsId,
  }) {
    return EmployeeProfile(
      id: id ?? this.id,
      name: name ?? this.name,
      registration: registration ?? this.registration,
      status: status ?? this.status,
      roleId: roleId ?? this.roleId,
      workplaceId: workplaceId ?? this.workplaceId,
      documentSigningOptionsId:
          documentSigningOptionsId ?? this.documentSigningOptionsId,
    );
  }
}
