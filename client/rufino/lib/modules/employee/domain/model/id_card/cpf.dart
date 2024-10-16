import 'package:flutter/services.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';
import 'package:rufino/shared/util/cpf_validator.dart';

class Cpf extends TextPropBase {
  static final maskFormatter = MaskTextInputFormatter(
      mask: '###.###.###-##',
      filter: {"#": RegExp(r'[0-9]')},
      type: MaskAutoCompletionType.lazy);

  static const numberInputType = TextInputType.number;

  Cpf(String value)
      : super("CPF", value,
            formatter: maskFormatter, inputType: numberInputType);
  const Cpf.empty()
      : super("CPF", "", formatter: null, inputType: numberInputType);

  factory Cpf.createFormatted(String number) => Cpf(format(number));

  static String format(String rawNumber) {
    maskFormatter.formatEditUpdate(
      const TextEditingValue(text: ""),
      TextEditingValue(text: rawNumber),
    );
    return maskFormatter.getMaskedText();
  }

  @override
  Cpf copyWith({String? value}) {
    return Cpf(value ?? this.value);
  }

  @override
  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "O $displayName não pode ser vazio.";
    }
    if (value.length > 100) {
      return "o $displayName não pode ser maior que 100 caracteres.";
    }
    if (CPFValidator.isValid(value) == false) {
      return "o $displayName não é valido.";
    }
    return null;
  }
}
