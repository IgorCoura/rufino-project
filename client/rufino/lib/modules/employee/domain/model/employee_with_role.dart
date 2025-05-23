import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/model/document/document_status.dart';
import 'package:rufino/modules/employee/domain/model/status.dart';

class EmployeeWithRole extends Equatable {
  final String id;
  final String name;
  final String registration;
  final Status status;
  final String? roleId;
  final String roleName;
  final DocumentStatus documentStatus;

  const EmployeeWithRole(this.id, this.name, this.registration, this.status,
      this.roleId, this.roleName, this.documentStatus);

  @override
  List<Object?> get props =>
      [id, name, registration, status, roleId, roleName, documentStatus];

  factory EmployeeWithRole.fromJson(Map<String, dynamic> json) {
    return EmployeeWithRole(
      json["id"],
      json["name"],
      json["registration"],
      Status.fromJson(json["status"]),
      json["roleId"],
      json["roleName"] ?? "",
      DocumentStatus.fromJson(json["documentRepresentingStatus"]),
    );
  }

  static List<EmployeeWithRole> fromListJson(List<dynamic> jsonList) {
    return jsonList
        .map<EmployeeWithRole>((element) => EmployeeWithRole.fromJson(element))
        .toList();
  }
}
