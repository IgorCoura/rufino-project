import 'package:rufino/modules/employee/domain/model/prop_base.dart';

class Country extends PropBase {
  Country(String value) : super("País", value);

  static Country get empty => Country("");

  @override
  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "O $displayName não pode ser vazio.";
    }
    if (value.length > 50) {
      return "o $displayName não pode ser maior que 50 caracteres.";
    }
    return null;
  }
}
