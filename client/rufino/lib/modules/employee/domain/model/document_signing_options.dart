import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class DocumentSigningOptions extends Enumeration {
  static const Map<String, String> conversionMapIntToString = {
    "0": "",
    "1": "Assinatura Fisica",
    "2": "Assinatura Digital e Whatsapp",
    "3": "Assinatura Digital e Selfie",
    "4": "Assinatura Digital e SMS",
  };

  DocumentSigningOptions(String id, String name)
      : super(id, conversionMapIntToString[id] ?? name,
            "Opções de Assinatura de Documentos");

  const DocumentSigningOptions.empty()
      : super.empty(displayName: "Opções de Assinatura de Documentos");

  @override
  String toString() {
    return name;
  }

  @override
  List<Object?> get props => [id, name];

  factory DocumentSigningOptions.fromJson(Map<String, dynamic> json) {
    return DocumentSigningOptions((json["id"]).toString(), json["name"]);
  }

  static List<DocumentSigningOptions> fromListJson(List<dynamic> jsonList) {
    return jsonList
        .map<DocumentSigningOptions>(
            (element) => DocumentSigningOptions.fromJson(element))
        .toList();
  }
}
