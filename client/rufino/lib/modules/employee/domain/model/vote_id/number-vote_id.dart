import 'package:flutter/services.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';
import 'package:rufino/shared/util/voteid_validator.dart';

class NumberVoteId extends TextPropBase {
  static final MaskTextInputFormatter maskFormatter = MaskTextInputFormatter(
      mask: '####.####.####',
      filter: {"#": RegExp(r'[0-9]')},
      type: MaskAutoCompletionType.lazy);

  @override
  TextInputFormatter get formatter => maskFormatter;
  @override
  TextInputType? get inputType => TextInputType.number;

  const NumberVoteId(String value) : super("Número", value);
  const NumberVoteId.empty() : super("Número", "");

  factory NumberVoteId.createFormatted(String number) =>
      NumberVoteId(format(number));

  static String format(String rawNumber) {
    maskFormatter.formatEditUpdate(
      const TextEditingValue(text: ""),
      TextEditingValue(text: rawNumber),
    );
    return maskFormatter.getMaskedText();
  }

  @override
  NumberVoteId copyWith({String? value}) {
    return NumberVoteId(value ?? this.value);
  }

  @override
  String? validate(String? value) {
    value = VoteIdValidator.strip(value);
    if (value.isEmpty) {
      return "O $displayName não pode ser vazio.";
    }
    if (value.length > 12) {
      return "o $displayName não pode ser maior que 12 caracteres.";
    }
    if (VoteIdValidator.isValid(value) == false) {
      return "o $displayName não é valido.";
    }
    return null;
  }
}
