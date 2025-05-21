import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class DocumentUnitStatus extends Enumeration {
  static const Map<String, String> conversionMapIntToString = {
    "1": "Pendente",
    "2": "OK",
    "3": "Obsoleto",
    "4": "Inválido",
    "5": "Requer Validade",
    "6": "Não Aplicável",
    "7": "Aguardando Assinatura",
  };

  DocumentUnitStatus(String id, String name)
      : super(id, conversionMapIntToString[id] ?? name, "Status");

  const DocumentUnitStatus.empty() : super.empty(displayName: "Status");

  static DocumentUnitStatus fromJson(Map<String, dynamic> json) {
    return DocumentUnitStatus((json["id"]).toString(), json["name"]);
  }

  static List<DocumentUnitStatus> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }
}
