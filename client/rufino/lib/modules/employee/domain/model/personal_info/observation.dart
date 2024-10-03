import 'package:rufino/modules/employee/domain/model/prop_base.dart';

class Observation extends PropBase {
  Observation(String value) : super("Observação", value);

  static Observation get empty => Observation("");

  @override
  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "O $displayName não pode ser vazio.";
    }
    if (value.length > 100) {
      return "o $displayName não pode ser maior que 100 caracteres.";
    }
    return null;
  }
}
