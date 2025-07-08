import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Number extends TextPropBase {
  const Number(String value) : super("Número", value);
  const Number.empty() : super("Número", "");

  @override
  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "O $displayName não pode ser vazio.";
    }
    if (value.length > 10) {
      return "o $displayName não pode ser maior que 10 caracteres.";
    }
    return null;
  }

  @override
  Number copyWith({String? value}) {
    return Number(value ?? this.value);
  }
}
