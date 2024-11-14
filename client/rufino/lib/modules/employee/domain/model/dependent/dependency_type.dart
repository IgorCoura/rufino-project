import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class DependencyType extends Enumeration {
  static const Map<int, String> conversionMapIntToString = {
    1: "Cônjuge",
    2: "Filho(a)",
  };

  DependencyType(int id, String name)
      : super(id, conversionMapIntToString[id] ?? name, "Tipo de dependência");
  const DependencyType.empty()
      : super.empty(displayName: "Tipo de dependência");

  static DependencyType fromJson(Map<String, dynamic> json) {
    return DependencyType(json["id"], json["name"]);
  }

  static List<DependencyType> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }
}
