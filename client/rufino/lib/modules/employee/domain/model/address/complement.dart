import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Complement extends TextPropBase {
  const Complement(String value) : super("Complemento", value);
  const Complement.empty() : super("Complemento", "");

  @override
  String? validate(String? value) {
    if (value != null && value.isNotEmpty && value.length > 50) {
      return "o $displayName não pode ser maior que 50 caracteres.";
    }
    return null;
  }

  @override
  Complement copyWith({String? value}) {
    return Complement(value ?? this.value);
  }
}
