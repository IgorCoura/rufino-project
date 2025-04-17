import 'package:flutter/material.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Page extends TextPropBase {
  static const String contDisplayName = "Página";
  @override
  TextInputType? get inputType => TextInputType.number;
  const Page(String value) : super(contDisplayName, value);

  const Page.empty() : super(contDisplayName, "");

  @override
  String? validate(String? value) {
    if (value == null) {
      return "O $displayName é invalido.";
    }

    double? number = double.tryParse(value);

    if (number == null) {
      return "O $displayName é invalido.";
    }

    if (number < 0 || number > 100) {
      return "o $displayName não pode ser menor que 0 ou maior que 100.";
    }
    return null;
  }

  int? toInt() {
    return int.tryParse(value);
  }

  @override
  Page copyWith({String? value}) {
    return Page(value ?? this.value);
  }
}
