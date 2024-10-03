import 'package:rufino/modules/employee/domain/model/prop_base.dart';

class Complement extends PropBase {
  Complement(String value) : super("Complemento", value);

  static Complement get empty => Complement("");

  @override
  String? validate(String? value) {
    if (value != null && value.isNotEmpty && value.length > 50) {
      return "o $displayName não pode ser maior que 50 caracteres.";
    }
    return null;
  }
}
