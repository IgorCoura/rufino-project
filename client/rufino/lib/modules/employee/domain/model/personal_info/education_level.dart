import 'package:rufino/modules/employee/domain/model/enumeration.dart';

class EducationLevel extends Enumeration {
  const EducationLevel(int id, String name)
      : super(id, name, "Nível de educação.");

  static EducationLevel get empty => const EducationLevel(-1, "");

  static EducationLevel fromJson(Map<String, dynamic> json) {
    return EducationLevel(json["id"], json["name"]);
  }
}
