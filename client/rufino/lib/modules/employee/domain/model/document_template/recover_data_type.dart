import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class RecoverDataType extends Enumeration {
  static const Map<String, String> conversionMapIntToString = {
    "0": "",
    "1": "Dados da Empresa",
    "2": "Dados do Departamento",
    "3": "Dados do Funcionário",
    "4": "Dados do PGR",
    "5": "Dados da Cargo",
    "6": "Dados da Função",
    "7": "Dados da Local de Trabalho",
    "8": "Dados Complementares",
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
    return listJson.map((el) => fromJson(el)).toList();
  }
}
