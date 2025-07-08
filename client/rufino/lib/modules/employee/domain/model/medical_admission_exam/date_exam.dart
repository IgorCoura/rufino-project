import 'package:flutter/services.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class DateExam extends TextPropBase {
  static final MaskTextInputFormatter maskFormatter = MaskTextInputFormatter(
      mask: '##/##/####',
      filter: {"#": RegExp(r'[0-9]')},
      type: MaskAutoCompletionType.lazy);

  @override
  TextInputFormatter? get formatter => maskFormatter;
  @override
  TextInputType? get inputType => TextInputType.number;

  const DateExam(String value) : super("Data do exame", value);

  const DateExam.empty() : super("Data do exame", "");

  factory DateExam.createFormatted(String? number) => DateExam(format(number));

  static String format(String? rawNumber) {
    if (rawNumber == null || rawNumber.isEmpty) {
      return "";
    }
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
  DateExam copyWith({String? value}) {
    return DateExam(value ?? this.value);
  }

  @override
  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "O $displayName não pode ser vazio.";
    }

    try {
      var dataString = convertToData(value);
      var date = DateTime.tryParse(dataString);

      var dateMin = DateTime.now().add(const Duration(days: -365));
      var dateMax = DateTime.now().add(const Duration(days: 1));

      if (date == null || date.isAfter(dateMax) || date.isBefore(dateMin)) {
        return "O $displayName é invalida.";
      }
    } catch (_) {
      return "O $displayName é invalida.";
    }
    return null;
  }
}
