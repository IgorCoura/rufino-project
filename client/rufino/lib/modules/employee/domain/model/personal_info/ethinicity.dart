import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class Ethinicity extends Enumeration {
  static const Map<int, String> conversionMapIntToString = {
    1: "Branco",
    2: "Negro",
    3: "Pardo",
    4: "Amarelo",
    5: "Indígena",
  };

  Ethinicity(int id, String name)
      : super(id, conversionMapIntToString[id] ?? name, "Etnia");
  const Ethinicity.empty() : super.empty(displayName: "Etnia");

  static Ethinicity fromJson(Map<String, dynamic> json) {
    return Ethinicity(json["id"], json["name"]);
  }

  static List<Ethinicity> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }
}
