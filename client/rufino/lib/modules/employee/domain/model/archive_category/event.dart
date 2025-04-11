import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class Event extends Enumeration {
  static const Map<String, String> conversionMapIntToString = {
    "1": "Documentos para Admissão",
    "2": "Documentos pós Admissão",
    "3": "Documentos dos Filhos",
    "4": "Documentos Militar",
    "5": "Documentos da Esposa",
    "6": "Documentos do Exame Demissional",
  };

  Event(String id, String name)
      : super(id, conversionMapIntToString[id] ?? name, "Evento");
  const Event.empty() : super.empty(displayName: "Evento");

  static Event fromJson(Map<String, dynamic> json) {
    return Event((json["id"]).toString(), json["name"]);
  }

  static List<Event> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }
}
