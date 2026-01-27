import 'package:equatable/equatable.dart';

import 'package:rufino/modules/employee/domain/model/document_group/document_group_status.dart';
import 'package:rufino/modules/employee/domain/model/status.dart';

class EmployeeWithRole extends Equatable {
  final String id;
  final String name;
  final String registration;
  final Status status;
  final String? roleId;
  final String roleName;
  final DocumentGroupStatus documentStatus;
  final List<int>? image;

  const EmployeeWithRole(this.id, this.name, this.registration, this.status,
      this.roleId, this.roleName, this.documentStatus, {this.image});

  EmployeeWithRole copyWith({
    String? id,
    String? name,
    String? registration,
    Status? status,
    String? roleId,
    String? roleName,
    DocumentGroupStatus? documentStatus,
    List<int>? image,
  }) {
    return EmployeeWithRole(
      id ?? this.id,
      name ?? this.name,
      registration ?? this.registration,
      status ?? this.status,
      roleId ?? this.roleId,
      roleName ?? this.roleName,
      documentStatus ?? this.documentStatus,
      image: image ?? this.image,
    );
  }

  @override
  List<Object?> get props =>
      [id, name, registration, status, roleId, roleName, documentStatus, image];

  factory EmployeeWithRole.fromJson(Map<String, dynamic> json) {
    return EmployeeWithRole(
      json["id"],
      json["name"],
      json["registration"],
      Status.fromJson(json["status"]),
      json["roleId"],
      json["roleName"] ?? "",
      DocumentGroupStatus.fromJson(json["documentRepresentingStatus"]),
    );
  }

  static List<EmployeeWithRole> fromListJson(List<dynamic> jsonList) {
    return jsonList
        .map<EmployeeWithRole>((element) => EmployeeWithRole.fromJson(element))
        .toList();
  }
}
