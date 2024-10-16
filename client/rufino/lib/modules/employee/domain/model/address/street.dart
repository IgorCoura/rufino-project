import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Street extends TextPropBase {
  const Street(String value) : super("Endereço", value);
  const Street.empty() : super("Endereço", "");

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
