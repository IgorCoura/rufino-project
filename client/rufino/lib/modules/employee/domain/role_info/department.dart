import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';
import 'package:rufino/modules/employee/domain/model/base/text_base.dart';
import 'package:rufino/modules/employee/domain/role_info/department_description.dart';

class Department extends Enumeration {
  final DepartmentDescription depertmentDescription;

  const Department(String id, String name, this.depertmentDescription)
      : super(id, name, "Setor");
  const Department.empty(
      {this.depertmentDescription = const DepartmentDescription.empty()})
      : super.empty(displayName: "Setor");

  static Department fromJson(Map<String, dynamic> json) {
    return Department(
        json["id"], json["name"], DepartmentDescription(json["description"]));
  }

  static List<Department> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }

  List<TextBase> get textBase => [depertmentDescription];
}
