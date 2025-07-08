import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class AssociationType extends Enumeration {
  static const Map<String, String> conversionMapIntToString = {
    "1": "Cargo",
    "2": "Local de Trabalho",
  };

  AssociationType(String id, String name)
      : super(id, conversionMapIntToString[id] ?? name, "Tipo de Associação");
  const AssociationType.empty()
      : super.empty(displayName: "Tipo de Associação");

  static AssociationType fromJson(Map<String, dynamic> json) {
    return AssociationType((json["id"]).toString(), json["name"]);
  }

  int toInt() {
    return int.parse(id);
  }

  static List<AssociationType> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }
}
