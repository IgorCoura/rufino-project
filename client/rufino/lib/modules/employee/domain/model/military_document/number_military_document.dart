import 'package:flutter/services.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class NumberMilitaryDocument extends TextPropBase {
  @override
  TextInputType? get inputType => TextInputType.number;

  const NumberMilitaryDocument(String value)
      : super("Número do documento", value);

  const NumberMilitaryDocument.empty() : super("Número do documento", "");

  @override
  TextPropBase copyWith({String? value}) {
    return NumberMilitaryDocument(value ?? this.value);
  }

  @override
  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "O $displayName não pode ser vazio.";
    }
    if (value.length > 20) {
      return "o $displayName não pode ser maior que 20 caracteres.";
    }
    return null;
  }
}
