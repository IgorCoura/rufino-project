import 'package:flutter/services.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';

class Cellphone extends TextPropBase {
  static final MaskTextInputFormatter maskFormatter = MaskTextInputFormatter(
      mask: '(##) #####-####',
      filter: {"#": RegExp(r'[0-9]')},
      type: MaskAutoCompletionType.lazy);

  @override
  TextInputFormatter? get formatter => maskFormatter;
  @override
  TextInputType? get inputType => TextInputType.number;

  const Cellphone(String value) : super("Celular", value);

  const Cellphone.empty() : super("Celular", "");

  factory Cellphone.createFormatNumber(String number) =>
      Cellphone(formatNumber(number));

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
    var regex = RegExp(
        r"^(?:\+55\s?)?(?:\(?\d{2}\)?\s?)?(?:9\d{4}-?\d{4}|[2-8]\d{3}-?\d{4})$");
    if (regex.hasMatch(value) == false) {
      return "Formato invalido";
    }
    return null;
  }

  @override
  Cellphone copyWith({String? value}) {
    return Cellphone(value ?? this.value);
  }
}
