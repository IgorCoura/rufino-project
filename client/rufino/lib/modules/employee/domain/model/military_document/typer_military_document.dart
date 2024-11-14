import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class TyperMilitaryDocument extends TextPropBase {
  const TyperMilitaryDocument(String value)
      : super("Tipo de documento.", value);

  const TyperMilitaryDocument.empty() : super("Tipo de documento.", "");

  @override
  TyperMilitaryDocument copyWith({String? value}) {
    return TyperMilitaryDocument(value ?? this.value);
  }

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
