import 'package:rufino/modules/employee/domain/model/enumeration.dart';

class Ethinicity extends Enumeration {
  const Ethinicity(int id, String name) : super(id, name, "Genero");

  static Ethinicity get empty => const Ethinicity(-1, "");

  static Ethinicity fromJson(Map<String, dynamic> json) {
    return Ethinicity(json["id"], json["name"]);
  }
}
