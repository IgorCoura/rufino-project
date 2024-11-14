import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Name extends TextPropBase {
  const Name(String value) : super("Nome", value);
  const Name.empty() : super("Nome", "");
  const Name.defaultNameDependent() : super("Nome", "Novo Dependente");

  @override
  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "O $displayName não pode ser vazio.";
    }
    var regex = RegExp(r"^[a-zA-ZÀ-ÿ']+(?: [a-zA-ZÀ-ÿ']+)+$");
    if (regex.hasMatch(value) == false) {
      return "Formato invalido.";
    }
    var defaultValue = const Name.defaultNameDependent().value;
    if (value == defaultValue) {
      return "O nome não pode ser o valor padrão.";
    }
    return null;
  }

  @override
  Name copyWith({String? value}) {
    return Name(value ?? this.value);
  }
}
