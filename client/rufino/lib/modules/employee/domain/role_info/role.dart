import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';
import 'package:rufino/modules/employee/domain/model/base/text_base.dart';
import 'package:rufino/modules/employee/domain/role_info/role_cbo.dart';
import 'package:rufino/modules/employee/domain/role_info/role_description.dart';
import 'package:rufino/modules/employee/domain/role_info/salary.dart';
import 'package:rufino/modules/employee/domain/role_info/salary_description.dart';

class Role extends Enumeration {
  final RoleDescription roleDescription;
  final RoleCbo roleCbo;
  final Salary salary;
  final SalaryDescription salaryDescription;

  const Role(String id, String name, this.roleDescription, this.roleCbo,
      this.salary, this.salaryDescription)
      : super(id, name, "Função");
  const Role.empty(
      {this.roleDescription = const RoleDescription.empty(),
      this.roleCbo = const RoleCbo.empty(),
      this.salary = const Salary.empty(),
      this.salaryDescription = const SalaryDescription.empty()})
      : super.empty(displayName: "Função");

  static Role fromJson(Map<String, dynamic> json) {
    return Role(
      json["id"],
      json["name"],
      RoleDescription(json["description"]),
      RoleCbo(json["cbo"]),
      Salary(
        json["remuneration"]["baseSalary"]["type"]["name"],
        json["remuneration"]["baseSalary"]["value"],
        (json["remuneration"]["paymentUnit"]["id"]).toString(),
      ),
      SalaryDescription(json["remuneration"]["description"]),
    );
  }

  static List<Role> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }

  List<TextBase> get textBase => [roleDescription, roleCbo];
}
