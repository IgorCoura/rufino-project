import 'package:rufino/modules/employee/domain/model/text_prop_base.dart';

class Complement extends TextPropBase {
  Complement(String value) : super("Complemento", value);

  static Complement get empty => Complement("");

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
