import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class MaritalStatus extends Enumeration {
  static const Map<String, String> conversionMapIntToString = {
    "1": "Solteiro(a)",
    "2": "Casado(a)",
    "3": "Divorciado(a)",
    "4": "Viúvo(a)",
  };

  MaritalStatus(String id, String name)
      : super(id, conversionMapIntToString[id] ?? name, "Estado Civil");

  const MaritalStatus.empty() : super.empty(displayName: "Estado Civil");

  static MaritalStatus fromJson(Map<String, dynamic> json) {
    return MaritalStatus((json["id"]).toString(), json["name"]);
  }

  static List<MaritalStatus> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }
}
