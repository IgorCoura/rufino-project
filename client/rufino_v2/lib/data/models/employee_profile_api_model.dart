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
    required this.documentSigningOptionsId,
  });

  final String id;
  final String name;
  final String registration;
  final int statusId;
  final String roleId;
  final String workplaceId;
  final String documentSigningOptionsId;

  /// Deserialises an [EmployeeProfileApiModel] from the API JSON map.
  factory EmployeeProfileApiModel.fromJson(Map<String, dynamic> json) {
    final signingOpt = json['documentSigningOptions'];
    String signingId = '';
    if (signingOpt is Map<String, dynamic>) {
      signingId = (signingOpt['id'] ?? '').toString();
    } else if (signingOpt != null) {
      signingId = signingOpt.toString();
    }

    return EmployeeProfileApiModel(
      id: json['id'] as String,
      name: json['name'] as String,
      registration: json['registration'] as String? ?? '',
      statusId: (json['status'] as Map<String, dynamic>)['id'] as int,
      roleId: json['roleId'] as String? ?? '',
      workplaceId: json['workplaceId'] as String? ?? '',
      documentSigningOptionsId: signingId,
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
      documentSigningOptionsId: documentSigningOptionsId,
    );
  }
}
