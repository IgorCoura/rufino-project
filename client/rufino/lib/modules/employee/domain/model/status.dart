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

  @override
  String toString() {
    return name;
  }

  @override
  List<Object?> get props => [id, name];

  static const empty = Status(0, "");

  static const defaultList = [Status(0, "Todos")];

  factory Status.fromJson(Map<String, dynamic> json) {
    return Status(json["id"], _convertName(json["name"]));
  }

  factory Status.fromNumber(int id) {
    return Status(id, conversionMapIntToString[id] ?? "");
  }

  static List<Status> fromListJson(List<dynamic> jsonList) {
    return jsonList.map<Status>((element) => Status.fromJson(element)).toList();
  }

  static Status getFromList(int id, List<Status>? listStatus) {
    if (listStatus == null) {
      return Status.fromNumber(id);
    }
    return listStatus.singleWhere((status) => status.id == id,
        orElse: () => Status.empty);
  }

  static String _convertName(String name) {
    return conversionMapLanguage[name.toLowerCase()] ?? name;
  }
}
