import 'package:intl/intl.dart';
import 'package:rufino/modules/employee/domain/model/employee_contract_type.dart';

class EmployeeContract {
  final String displayName = "Contrato";
  final String initDate;
  final String? finalDate;
  final EmployeeContractType type;

  EmployeeContract(this.initDate, this.finalDate, this.type);

  static EmployeeContract fromJson(Map<String, dynamic> json) {
    return EmployeeContract(json["initDate"], json["finalDate"],
        EmployeeContractType.fromJson(json["type"]));
  }

  static List<EmployeeContract> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }

  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "O $displayName não pode ser vazio.";
    }

    try {
      // Parse the input date string in dd/MM/yyyy format
      DateTime parsedDate = DateFormat('dd/MM/yyyy').parse(value);
      // Format the parsed date to yyyy-MM-dd
      String formattedDate = DateFormat('yyyy-MM-dd').format(parsedDate);

      var date = DateTime.tryParse(formattedDate);

      var dateMax = DateTime.now().add(const Duration(days: 365));
      var dateMin = DateTime.now().add(const Duration(days: -365));

      if (date == null || date.isAfter(dateMax) || date.isBefore(dateMin)) {
        return "O $displayName é invalida.";
      }
    } catch (_) {
      return "O $displayName é invalida.";
    }
    return null;
  }
}
