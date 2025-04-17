import 'package:rufino/modules/employee/domain/model/base/text_base.dart';

class Salary extends TextBase {
  static const Map<String, String> conversionMapIntToPortugues = {
    "0": "Não se Aplica",
    "1": "Por Hora",
    "2": "Por Dia",
    "3": "Por Semana",
    "4": "Por Quinzena",
    "5": "Por Mês",
    "6": "Por Tarefa"
  };

  Salary(String currencyType, String value, String idUnit)
      : super("Salario", unifyValues(currencyType, value, idUnit));
  const Salary.empty() : super("Salario", "");

  static String unifyValues(String type, String currency, String idUnit) {
    var convertType = conversionMapIntToPortugues[idUnit];

    return "$type \$$currency $convertType";
  }
}
