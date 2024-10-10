import 'package:rufino/modules/employee/domain/model/text_prop_base.dart';

class State extends TextPropBase {
  State(String value) : super("Estado", value);

  static State get empty => State("");

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
