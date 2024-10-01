import 'package:equatable/equatable.dart';

class Employee extends Equatable {
  final String id;
  final String name;
  final String registration;
  final int status;
  final String? roleId;
  final String roleName;

  const Employee(this.id, this.name, this.registration, this.status,
      this.roleId, this.roleName);

  @override
  List<Object?> get props => [id, name, registration, status, roleId, roleName];

  factory Employee.fromJson(Map<String, dynamic> json) {
    return Employee(json["id"], json["name"], json["registration"],
        json["status"], json["roleId"], json["roleName"]);
  }

  static List<Employee> fromListJson(List<dynamic> jsonList) {
    return jsonList
        .map<Employee>((element) => Employee.fromJson(element))
        .toList();
  }
}
