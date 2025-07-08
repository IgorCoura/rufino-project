import 'package:flutter/services.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class DateOfBirth extends TextPropBase {
  static final MaskTextInputFormatter maskFormatter = MaskTextInputFormatter(
      mask: '##/##/####',
      filter: {"#": RegExp(r'[0-9]')},
      type: MaskAutoCompletionType.lazy);

  @override
  TextInputFormatter? get formatter => maskFormatter;
  @override
  TextInputType? get inputType => TextInputType.number;

  const DateOfBirth(String value) : super("Data de nascimento", value);

  const DateOfBirth.empty() : super("Data de nascimento", "");

  factory DateOfBirth.createFormatted(String number) =>
      DateOfBirth(format(number));

  static String format(String rawNumber) {
    var itens = rawNumber.split("-");
    var number = itens[2] + itens[1] + itens[0];
    maskFormatter.formatEditUpdate(
      const TextEditingValue(text: ""),
      TextEditingValue(text: number),
    );
    return maskFormatter.getMaskedText();
  }

  static String convertToData(String value) {
    var itens = value.split("/");
    var number = "${itens[2]}-${itens[1]}-${itens[0]}";
    return number;
  }

  String toData() {
    return convertToData(value);
  }

  @override
  DateOfBirth copyWith({String? value}) {
    return DateOfBirth(value ?? this.value);
  }

  @override
  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "O $displayName não pode ser vazio.";
    }

    try {
      var dataString = convertToData(value);
      var date = DateTime.tryParse(dataString);

      var dateMin = DateTime.now();
      var dateMax = DateTime.now().add(const Duration(days: -36500));

      if (date == null || date.isAfter(dateMin) || date.isBefore(dateMax)) {
        return "O $displayName é invalida.";
      }
    } catch (_) {
      return "O $displayName é invalida.";
    }
    return null;
  }
}
