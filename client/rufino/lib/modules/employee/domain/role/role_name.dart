import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class RoleName extends Enumeration {
  const RoleName(int id, String name) : super(id, name, "Função");
  const RoleName.empty() : super.empty(displayName: "Função");

  static RoleName fromJson(Map<String, dynamic> json) {
    return RoleName(1, json["name"]);
  }

  static List<RoleName> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }
}
