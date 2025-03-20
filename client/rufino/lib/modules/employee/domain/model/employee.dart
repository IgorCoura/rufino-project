import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/model/name.dart';
import 'package:rufino/modules/employee/domain/model/status.dart';

class Employee extends Equatable {
  final String id;
  final Name name;
  final String registration;
  final Status status;
  final String roleId;
  final String companyId;

  const Employee(this.id, this.name, this.registration, this.status,
      this.companyId, this.roleId);
  const Employee.empty(
      {this.id = "",
      this.name = const Name.empty(),
      this.registration = "",
      this.status = const Status.empty(),
      this.companyId = "",
      this.roleId = ""});

  Employee copyWith({String? id, Name? name}) => Employee(id ?? this.id,
      name ?? this.name, registration, status, companyId, roleId);

  @override
  List<Object?> get props => [id, name, registration, status];

  factory Employee.fromJson(Map<String, dynamic> json) {
    return Employee(json["id"], Name(json["name"]), json["registration"],
        Status.fromJson(json["status"]), json["companyId"], json["roleId"]);
  }

  static List<Employee> fromListJson(List<dynamic> jsonList) {
    return jsonList
        .map<Employee>((element) => Employee.fromJson(element))
        .toList();
  }
}
