import 'package:flutter/services.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Workload extends TextPropBase {
  static const String contDisplayName = "Carga Horária";
  static final MaskTextInputFormatter maskFormatter = MaskTextInputFormatter(
      mask: 'Horas: ### ',
      filter: {"#": RegExp(r'[0-9]')},
      type: MaskAutoCompletionType.lazy);

  @override
  TextInputFormatter? get formatter => maskFormatter;
  @override
  TextInputType? get inputType => TextInputType.number;

  const Workload(String value) : super(contDisplayName, value);
  const Workload.empty() : super(contDisplayName, "");

  factory Workload.createFormatted(String number) => Workload(format(number));

  static String format(String rawNumber) {
    var number = rawNumber.replaceAll(".", "").replaceAll(":", "");
    maskFormatter.formatEditUpdate(
      const TextEditingValue(text: ""),
      TextEditingValue(text: number),
    );
    return maskFormatter.getMaskedText();
  }

  double? toDouble() {
    var number = value.replaceAll(" ", "").replaceAll("Horas:", "");
    return double.tryParse(number);
  }

  @override
  Workload copyWith({String? value}) {
    return Workload(value ?? this.value);
  }

  @override
  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return null;
    }

    try {
      var number = value.replaceAll(" ", "").replaceAll("Horas:", "");
      var days = int.tryParse(number);

      if (days == null || days < 0 || days > 999) {
        return "O $displayName é invalida.";
      }
    } catch (_) {
      return "O $displayName é invalida.";
    }
    return null;
  }
}
