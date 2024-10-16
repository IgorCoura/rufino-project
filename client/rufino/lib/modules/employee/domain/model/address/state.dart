import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class State extends TextPropBase {
  const State(String value) : super("Estado", value);
  const State.empty() : super("Estado", "");

  @override
  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "O $displayName não pode ser vazio.";
    }
    if (value.length > 50) {
      return "o $displayName não pode ser maior que 50 caracteres.";
    }
    return null;
  }

  @override
  State copyWith({String? value}) {
    return State(value ?? this.value);
  }
}
