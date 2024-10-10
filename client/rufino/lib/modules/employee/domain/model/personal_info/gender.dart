import 'package:rufino/modules/employee/domain/model/enumeration.dart';

class Gender extends Enumeration {
  const Gender(int id, String name) : super(id, name, "Genero");

  static Gender get empty => const Gender(-1, "");

  static Gender fromJson(Map<String, dynamic> json) {
    return Gender(json["id"], json["name"]);
  }
}
