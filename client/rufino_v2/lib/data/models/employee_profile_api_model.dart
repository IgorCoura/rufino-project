import '../../domain/entities/employee.dart';
import '../../domain/entities/employee_profile.dart';

/// Data Transfer Object for the employee detail endpoint.
class EmployeeProfileApiModel {
  const EmployeeProfileApiModel({
    required this.id,
    required this.name,
    required this.registration,
    required this.statusId,
    required this.roleId,
    required this.workplaceId,
  });

  final String id;
  final String name;
  final String registration;
  final int statusId;
  final String roleId;
  final String workplaceId;

  /// Deserialises an [EmployeeProfileApiModel] from the API JSON map.
  factory EmployeeProfileApiModel.fromJson(Map<String, dynamic> json) {
    return EmployeeProfileApiModel(
      id: json['id'] as String,
      name: json['name'] as String,
      registration: json['registration'] as String? ?? '',
      statusId: (json['status'] as Map<String, dynamic>)['id'] as int,
      roleId: json['roleId'] as String? ?? '',
      workplaceId: json['workplaceId'] as String? ?? '',
    );
  }

  /// Converts this model to a domain [EmployeeProfile] entity.
  EmployeeProfile toEntity() {
    return EmployeeProfile(
      id: id,
      name: name,
      registration: registration,
      status: EmployeeStatus.fromId(statusId),
      roleId: roleId,
      workplaceId: workplaceId,
    );
  }
}
