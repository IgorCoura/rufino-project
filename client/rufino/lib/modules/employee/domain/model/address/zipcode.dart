import 'package:flutter/services.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Zipcode extends TextPropBase {
  static final MaskTextInputFormatter maskFormatter = MaskTextInputFormatter(
      mask: '#####-###',
      filter: {"#": RegExp(r'[0-9]')},
      type: MaskAutoCompletionType.lazy);

  @override
  TextInputFormatter? get formatter => maskFormatter;
  @override
  TextInputType? get inputType => TextInputType.number;

  const Zipcode(String value) : super("CEP", value);

  const Zipcode.empty() : super("CEP", "");

  factory Zipcode.createFormatNumber(String number) => Zipcode(format(number));

  static String format(String rawNumber) {
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

  @override
  Zipcode copyWith({String? value}) {
    return Zipcode(value ?? this.value);
  }
}
