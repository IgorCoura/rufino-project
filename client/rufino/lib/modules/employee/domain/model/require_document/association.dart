import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class Association extends Enumeration {
  const Association(String id, String name) : super(id, name, "Associação");
  const Association.empty() : super.empty(displayName: "Associação");

  @override
  String toString() {
    return name;
  }

  @override
  List<Object?> get props => [id, name];

  factory Association.fromJson(Map<String, dynamic> json) {
    return Association(json["id"], json["name"]);
  }

  static List<Association> fromListJson(List<dynamic> jsonList) {
    return jsonList
        .map<Association>((element) => Association.fromJson(element))
        .toList();
  }
}
