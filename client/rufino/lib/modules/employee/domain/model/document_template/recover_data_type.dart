import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class RecoverDataType extends Enumeration {
  static const Map<String, String> conversionMapIntToString = {
    "1": "NR01",
  };

  RecoverDataType(String id, String name)
      : super(id, conversionMapIntToString[id] ?? name,
            "Tipo de Recuperação de Dados");
  const RecoverDataType.empty()
      : super.empty(displayName: "Tipo de Recuperação de Dados");

  static RecoverDataType fromJson(Map<String, dynamic> json) {
    return RecoverDataType((json["id"]).toString(), json["name"]);
  }

  static List<RecoverDataType> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }
}
