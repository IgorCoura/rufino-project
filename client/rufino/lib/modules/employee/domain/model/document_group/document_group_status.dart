import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class DocumentGroupStatus extends Enumeration {
  static const Map<String, String> conversionMapIntToString = {
    "1": "OK",
    "2": "Há Vencer",
    "3": "Requer Atenção",
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
