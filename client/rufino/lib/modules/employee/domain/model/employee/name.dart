import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Name extends TextPropBase {
  const Name(String value) : super("Nome", value);
  const Name.empty() : super("Nome", "");

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

  @override
  Name copyWith({String? value}) {
    return Name(value ?? this.value);
  }
}
