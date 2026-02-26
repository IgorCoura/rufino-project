import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class EmployeeDocumentStatus extends Enumeration {
  static const Map<String, String> conversionMapIntToString = {
    "0": "OK",
    "1": "Há Vencer",
    "2": "Requer Atenção",
  };

  EmployeeDocumentStatus(String id, String name)
      : super(id, conversionMapIntToString[id] ?? name, "Documentos");

  const EmployeeDocumentStatus.empty() : super.empty(displayName: "Documentos");

  static const defaultList = [
    EmployeeDocumentStatus._const("-1", "Todos"),
  ];

  const EmployeeDocumentStatus._const(String id, String name)
      : super(id, name, "Documentos");

  static EmployeeDocumentStatus fromJson(Map<String, dynamic> json) {
    return EmployeeDocumentStatus((json["id"]).toString(), json["name"]);
  }

  static List<EmployeeDocumentStatus> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }
}
