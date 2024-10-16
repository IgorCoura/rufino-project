import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class MotherName extends TextPropBase {
  const MotherName(String value) : super("Nome da Mãe", value);
  const MotherName.empty() : super("Nome da Mãe", "");

  @override
  MotherName copyWith({String? value}) {
    return MotherName(value ?? this.value);
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
