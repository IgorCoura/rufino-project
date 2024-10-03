import 'package:flutter/material.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';
import 'package:rufino/modules/employee/domain/model/prop_base.dart';

class Zipcode extends PropBase {
  static final maskFormatter = MaskTextInputFormatter(
      mask: '#####-###',
      filter: {"#": RegExp(r'[0-9]')},
      type: MaskAutoCompletionType.lazy);

  static const numberInputType = TextInputType.number;
  Zipcode(String value)
      : super("CEP", value,
            formatter: maskFormatter, inputType: numberInputType);

  static Zipcode get empty => Zipcode("");

  factory Zipcode.createFormatNumber(String number) =>
      Zipcode(formatNumber(number));

  static String formatNumber(String rawNumber) {
    maskFormatter.formatEditUpdate(
      const TextEditingValue(text: ""),
      TextEditingValue(text: rawNumber),
    );
    return maskFormatter.getMaskedText();
  }

  @override
  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "O $displayName não pode ser vazio.";
    }
    final cepRegex = RegExp(r'^\d{5}-?\d{3}$');
    if (cepRegex.hasMatch(value) == false) {
      return "$displayName inválido.";
    }
    return null;
  }
}
