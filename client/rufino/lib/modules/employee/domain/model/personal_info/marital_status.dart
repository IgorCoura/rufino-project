import 'package:rufino/modules/employee/domain/model/enumeration.dart';

class MaritalStatus extends Enumeration {
  static const Map<int, String> conversionMapIntToString = {
    1: "Solteiro(a)",
    2: "Casado(a)",
    3: "Divorciado(a)",
    4: "Viúvo(a)",
  };

  MaritalStatus(int id, String name)
      : super(id, conversionMapIntToString[id] ?? name, "Estado Civil");

  static MaritalStatus get empty =>
      MaritalStatus(Enumeration.emptyId, Enumeration.emptyName);

  static MaritalStatus fromJson(Map<String, dynamic> json) {
    return MaritalStatus(json["id"], json["name"]);
  }

  static List<MaritalStatus> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }
}