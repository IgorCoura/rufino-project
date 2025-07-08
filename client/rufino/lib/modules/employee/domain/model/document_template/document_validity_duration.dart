import 'package:flutter/services.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class DocumentValidityDuration extends TextPropBase {
  static const String contDisplayName = "Validade do Documento";
  static final MaskTextInputFormatter maskFormatter = MaskTextInputFormatter(
      mask: 'Dias: ### ',
      filter: {"#": RegExp(r'[0-9]')},
      type: MaskAutoCompletionType.lazy);

  @override
  TextInputFormatter? get formatter => maskFormatter;
  @override
  TextInputType? get inputType => TextInputType.number;

  const DocumentValidityDuration(String value) : super(contDisplayName, value);
  const DocumentValidityDuration.empty() : super(contDisplayName, "");

  factory DocumentValidityDuration.createFormatted(String number) =>
      DocumentValidityDuration(format(number));

  static String format(String rawNumber) {
    maskFormatter.formatEditUpdate(
      const TextEditingValue(text: ""),
      TextEditingValue(text: rawNumber),
    );
    return maskFormatter.getMaskedText();
  }

  double? toDouble() {
    var number = value.replaceAll(" ", "").replaceAll("Dias:", "");
    return double.tryParse(number);
  }

  @override
  DocumentValidityDuration copyWith({String? value}) {
    return DocumentValidityDuration(value ?? this.value);
  }

  @override
  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return null;
    }

    try {
      var number = value.replaceAll(" ", "").replaceAll("Dias:", "");
      var days = double.tryParse(number);

      if (days == null || days < 0 || days > 999) {
        return "O $displayName é invalida.";
      }
    } catch (_) {
      return "O $displayName é invalida.";
    }
    return null;
  }
}
