import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class TypeSignature extends Enumeration {
  static const Map<String, String> conversionMapIntToString = {
    "1": "Assinatura",
    "2": "Visto",
  };

  TypeSignature(String id, String name)
      : super(
            id, conversionMapIntToString[id] ?? name, "Local das Assinaturas");
  const TypeSignature.empty()
      : super.empty(displayName: "Local das Assinaturas");

  static TypeSignature fromJson(Map<String, dynamic> json) {
    return TypeSignature((json["id"]).toString(), json["name"]);
  }

  static List<TypeSignature> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }
}
