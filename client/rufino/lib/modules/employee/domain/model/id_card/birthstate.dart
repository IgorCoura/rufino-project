import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Birthstate extends TextPropBase {
  const Birthstate(String value) : super("Estado de nascimento", value);
  const Birthstate.empty() : super("Estado de nascimento", "");

  @override
  Birthstate copyWith({String? value}) {
    return Birthstate(value ?? this.value);
  }

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
