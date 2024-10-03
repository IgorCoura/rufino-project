import 'package:rufino/modules/employee/domain/model/prop_base.dart';

class Name extends PropBase {
  Name(String value) : super("Nome", value);

  static Name get empty => Name("");

  @override
  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "O $displayName não pode ser vazio.";
    }
    var regex = RegExp(r"^[a-zA-ZÀ-ÿ']+(?: [a-zA-ZÀ-ÿ']+)+$");
    if (regex.hasMatch(value) == false) {
      return "Formato invalido";
    }
    return null;
  }
}
