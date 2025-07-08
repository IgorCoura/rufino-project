import 'package:equatable/equatable.dart';

class Status extends Equatable {
  static const Map<String, String> conversionMapLanguage = {
    "pending": "Pendente",
    "active": "Ativo",
    "vacation": "Ferias",
    "away": "Afastado",
    "inactive": "Inativo",
  };

  static const Map<int, String> conversionMapIntToString = {
    0: "",
    1: "Pendente",
    2: "Ativo",
    3: "Ferias",
    4: "Afastado",
    5: "Inativo",
  };

  final int id;
  final String name;

  const Status(this.id, this.name);
  const Status.empty({this.id = 0, this.name = ""});

  @override
  String toString() {
    return name;
  }

  @override
  List<Object?> get props => [id, name];

  static const defaultList = [Status(0, "Todos")];

  factory Status.fromJson(Map<String, dynamic> json) {
    return Status(json["id"], _convertName(json["id"], json["name"]));
  }

  factory Status.fromNumber(int id) {
    return Status(id, conversionMapIntToString[id] ?? id.toString());
  }

  static List<Status> fromListJson(List<dynamic> jsonList) {
    return jsonList.map<Status>((element) => Status.fromJson(element)).toList();
  }

  static Status getFromList(int id, List<Status>? listStatus) {
    if (listStatus == null) {
      return Status.fromNumber(id);
    }
    return listStatus.singleWhere((status) => status.id == id,
        orElse: () => const Status.empty());
  }

  static String _convertName(int id, String name) {
    var convertedName = conversionMapLanguage[name.toLowerCase()] ?? name;
    return convertedName.isEmpty ? id.toString() : convertedName;
  }
}
