import '../../domain/entities/employee.dart';

/// Data Transfer Object for an employee as returned by the list endpoint.
class EmployeeApiModel {
  const EmployeeApiModel({
    required this.id,
    required this.name,
    required this.registration,
    required this.statusId,
    required this.roleName,
    required this.documentStatusId,
  });

  final String id;
  final String name;
  final String registration;

  /// The numeric id of the employee status (matches [EmployeeStatus.id]).
  final int statusId;

  /// The display name of the assigned role.
  final String roleName;

  /// The numeric id of the document status (matches [DocumentStatus.id]).
  final int documentStatusId;

  /// Deserialises an [EmployeeApiModel] from the API JSON map.
  factory EmployeeApiModel.fromJson(Map<String, dynamic> json) {
    return EmployeeApiModel(
      id: json['id'] as String,
      name: json['name'] as String,
      registration: json['registration'] as String? ?? '',
      statusId: (json['status'] as Map<String, dynamic>)['id'] as int,
      roleName: json['roleName'] as String? ?? '',
      documentStatusId:
          (json['documentRepresentingStatus'] as Map<String, dynamic>)['id']
              as int,
    );
  }

  /// Converts this model to a domain [Employee] entity.
  Employee toEntity() {
    return Employee(
      id: id,
      name: name,
      registration: registration,
      status: EmployeeStatus.fromId(statusId),
      roleName: roleName,
      documentStatus: DocumentStatus.fromId(documentStatusId),
    );
  }
}
