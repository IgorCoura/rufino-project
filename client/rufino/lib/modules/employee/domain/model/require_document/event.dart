import 'package:equatable/equatable.dart';

class Event extends Equatable {
  static const Map<String, String> conversionMapLanguage = {
    "completeadmissionevent": "Evento de Admissão Completo",
  };

  static const Map<int, String> conversionMapIntToString = {
    0: "",
  };

  final int id;
  final String name;

  const Event(this.id, this.name);
  const Event.empty({this.id = 0, this.name = ""});

  @override
  String toString() {
    return name;
  }

  @override
  List<Object?> get props => [id, name];

  static const defaultList = [Event(0, "Todos")];

  factory Event.fromJson(Map<String, dynamic> json) {
    return Event(json["id"], _convertName(json["id"], json["name"]));
  }

  factory Event.fromNumber(int id) {
    return Event(id, conversionMapIntToString[id] ?? id.toString());
  }

  static List<Event> fromListJson(List<dynamic> jsonList) {
    return jsonList.map<Event>((element) => Event.fromJson(element)).toList();
  }

  static Event getFromList(int id, List<Event>? listStatus) {
    if (listStatus == null) {
      return Event.fromNumber(id);
    }
    return listStatus.singleWhere((status) => status.id == id,
        orElse: () => const Event.empty());
  }

  static String _convertName(int id, String name) {
    var convertedName = conversionMapLanguage[name.toLowerCase()] ?? name;
    return convertedName.isEmpty ? id.toString() : convertedName;
  }
}
