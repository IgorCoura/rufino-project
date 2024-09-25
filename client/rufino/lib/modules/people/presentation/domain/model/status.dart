import 'package:equatable/equatable.dart';

class Status extends Equatable {
  static const Map<String, String> conversionMap = {
    "pending": "Pendente",
    "active": "Ativo",
    "vacation": "Ferias",
    "away": "Afastado",
    "inactive": "Inativo",
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

  static List<Status> fromListJson(List<dynamic> jsonList) {
    return jsonList.map<Status>((element) => Status.fromJson(element)).toList();
  }

  static String _convertName(String name) {
    return conversionMap[name.toLowerCase()] ?? name;
  }
}
