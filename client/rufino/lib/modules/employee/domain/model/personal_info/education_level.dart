import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class EducationLevel extends Enumeration {
  static const Map<int, String> conversionMapIntToString = {
    1: "Analfabeto",
    2: "Ensino Fundamental Incompleto",
    3: "Ensino Fundamental Completo",
    4: "Ensino Medio Incompleto",
    5: "Ensino Medio Completo",
    6: "Ensino Superior Incompleto",
    7: "Ensino Superior Completo",
    8: "Pós Graduação Completo",
    9: "Mestrado Completo",
    10: "Doudorado Completo",
  };

  EducationLevel(int id, String name)
      : super(id, conversionMapIntToString[id] ?? name, "Nível de educação.");

  const EducationLevel.empty() : super.empty(displayName: "Nível de educação.");

  static EducationLevel fromJson(Map<String, dynamic> json) {
    return EducationLevel(json["id"], json["name"]);
  }

  static List<EducationLevel> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }
}
