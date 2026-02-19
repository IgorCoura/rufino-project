import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class DependencyType extends Enumeration {
  static const Map<String, String> conversionMapIntToString = {
    "1": "Filho(a)",
    "2": "Cônjuge",
  };

  DependencyType(String id, String name)
      : super(id, conversionMapIntToString[id] ?? name, "Tipo de dependência");
  const DependencyType.empty()
      : super.empty(displayName: "Tipo de dependência");

  static DependencyType fromJson(Map<String, dynamic> json) {
    return DependencyType((json["id"]).toString(), json["name"]);
  }

  static List<DependencyType> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }
}
