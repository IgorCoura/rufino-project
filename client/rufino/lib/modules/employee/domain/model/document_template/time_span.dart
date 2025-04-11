import 'package:flutter/services.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class TimeSpan extends TextPropBase {
  static final MaskTextInputFormatter maskFormatter = MaskTextInputFormatter(
      mask: 'Dias: ## Tempo: ##:##:##',
      filter: {"#": RegExp(r'[0-9]')},
      type: MaskAutoCompletionType.lazy);

  @override
  TextInputFormatter? get formatter => maskFormatter;
  @override
  TextInputType? get inputType => TextInputType.number;

  const TimeSpan(String value, String displayName) : super(displayName, value);

  factory TimeSpan.createFormatted(String number, String displayName) =>
      TimeSpan(format(number), displayName);

  static String format(String rawNumber) {
    var number = rawNumber.replaceAll(".", "").replaceAll(":", "");
    maskFormatter.formatEditUpdate(
      const TextEditingValue(text: ""),
      TextEditingValue(text: number),
    );
    return maskFormatter.getMaskedText();
  }

  String toJson() {
    var itens = value.split("Tempo");
    var number = "${itens[2]}-${itens[1]}-${itens[0]}";
    return number;
  }

  @override
  TimeSpan copyWith({String? value}) {
    return TimeSpan(value ?? this.value, displayName);
  }

  @override
  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "O $displayName não pode ser vazio.";
    }

    try {
      var dataString = value;
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

class DocumentValidityDuration extends TimeSpan {
  static const String contDisplayName = "Validade do documento";
  const DocumentValidityDuration(String value) : super(value, contDisplayName);
  const DocumentValidityDuration.empty() : super("", contDisplayName);
}

class Workload extends TimeSpan {
  static const String contDisplayName = "Carga horária";
  const Workload(String value) : super(value, contDisplayName);
  const Workload.empty() : super("", contDisplayName);
}
