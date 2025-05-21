import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class RecoverDataType extends Enumeration {
  static const Map<String, String> conversionMapIntToString = {
    "0": "",
    "1": "NR01",
  };

  RecoverDataType(String id, String name)
      : super(id, conversionMapIntToString[id] ?? name,
            "Tipo de Recuperação de Dados");
  const RecoverDataType.empty()
      : super("0", "", "Tipo de Recuperação de Dados");

  static RecoverDataType fromJson(Map<String, dynamic> json) {
    return RecoverDataType((json["id"]).toString(), json["name"]);
  }

  int toInt() {
    return int.parse(id);
  }

  bool get isEmpty {
    return name.isEmpty;
  }

  bool get isNotEmpty {
    return name.isNotEmpty;
  }

  static List<RecoverDataType> fromListJson(List<dynamic> listJson) {
    var list = [RecoverDataType("0", "")];
    list.addAll(listJson.map((el) => fromJson(el)).toList());
    return list;
  }
}
