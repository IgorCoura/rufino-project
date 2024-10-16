import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Birthcity extends TextPropBase {
  const Birthcity(String value) : super("Cidade de nascimento", value);
  const Birthcity.empty() : super("Cidade de nascimento", "");

  @override
  Birthcity copyWith({String? value}) {
    return Birthcity(value ?? this.value);
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
