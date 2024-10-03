import 'package:rufino/modules/employee/domain/model/prop_base.dart';

class Number extends PropBase {
  Number(String value) : super("Número", value);

  static Number get empty => Number("");

  @override
  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "O $displayName não pode ser vazio.";
    }
    if (value.length > 10) {
      return "o $displayName não pode ser maior que 10 caracteres.";
    }
    return null;
  }
}
