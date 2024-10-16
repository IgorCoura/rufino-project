import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class Disability extends Enumeration {
  static const Map<int, String> conversionMapIntToString = {
    1: "Fisíca",
    2: "Intelectual",
    3: "Mental",
    4: "Auditiva",
    5: "Visual",
    6: "Reabilitado",
    7: "Cota de Incapacidade",
  };

  Disability(int id, String name)
      : super(id, conversionMapIntToString[id] ?? name, "Deficiência");
  const Disability.empty() : super.empty(displayName: "Deficiência");

  static Disability fromJson(Map<String, dynamic> json) {
    return Disability(json["id"], json["name"]);
  }

  static List<Disability> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }
}
