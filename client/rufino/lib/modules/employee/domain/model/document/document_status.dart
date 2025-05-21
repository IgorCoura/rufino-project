import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class DocumentStatus extends Enumeration {
  static const Map<String, String> conversionMapIntToString = {
    "1": "Requer Documento",
    "2": "Requer Validade",
    "3": "OK",
    "4": "Obsoleto",
    "5": "Aguardando Assinatura",
  };

  DocumentStatus(String id, String name)
      : super(id, conversionMapIntToString[id] ?? name, "Status");

  const DocumentStatus.empty() : super.empty(displayName: "Status");

  static DocumentStatus fromJson(Map<String, dynamic> json) {
    return DocumentStatus((json["id"]).toString(), json["name"]);
  }

  static List<DocumentStatus> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }
}
