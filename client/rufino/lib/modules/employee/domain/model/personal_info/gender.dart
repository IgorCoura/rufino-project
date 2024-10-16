import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class Gender extends Enumeration {
  static const Map<int, String> conversionMapIntToString = {
    1: "Homem",
    2: "Mulher",
  };

  Gender(int id, String name)
      : super(id, conversionMapIntToString[id] ?? name, "Genero");
  const Gender.empty() : super.empty(displayName: "Genero");

  static Gender fromJson(Map<String, dynamic> json) {
    return Gender(json["id"], json["name"]);
  }

  static List<Gender> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }
}
