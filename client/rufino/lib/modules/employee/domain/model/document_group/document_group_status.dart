import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class DocumentGroupStatus extends Enumeration {
  // Ids do EmployeeDocumentStatus (0=Okay, 1=Warning, 2=RequiresAttention): o
  // servidor devolve o mesmo rollup de conformidade do funcionário em
  // `documentsStatus`, não uma escala própria do grupo.
  static const Map<String, String> conversionMapIntToString = {
    "0": "OK",
    "1": "A Vencer",
    "2": "Requer Atenção",
  };

  DocumentGroupStatus(String id, String name)
      : super(id, conversionMapIntToString[id] ?? name, "Status");

  const DocumentGroupStatus.empty() : super.empty(displayName: "Status");

  static DocumentGroupStatus fromJson(Map<String, dynamic> json) {
    return DocumentGroupStatus((json["id"]).toString(), json["name"]);
  }

  static List<DocumentGroupStatus> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }
}
