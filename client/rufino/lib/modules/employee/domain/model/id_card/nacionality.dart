import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Nacionality extends TextPropBase {
  const Nacionality(String value) : super("Nacionalidade", value);
  const Nacionality.empty() : super("Nacionalidade", "");

  @override
  Nacionality copyWith({String? value}) {
    return Nacionality(value ?? this.value);
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
