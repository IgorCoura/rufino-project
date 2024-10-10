import 'package:rufino/modules/employee/domain/model/text_prop_base.dart';

class Street extends TextPropBase {
  Street(String value) : super("Endereço", value);

  static Street get empty => Street("");

  @override
  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "O $displayName não pode ser vazio.";
    }
    if (value.length > 100) {
      return "o $displayName não pode ser maior que 100 caracteres.";
    }
    return null;
  }

  @override
  Street copyWith({String? value}) {
    return Street(value ?? this.value);
  }
}
