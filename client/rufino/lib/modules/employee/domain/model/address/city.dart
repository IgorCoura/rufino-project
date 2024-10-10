import 'package:rufino/modules/employee/domain/model/text_prop_base.dart';

class City extends TextPropBase {
  City(String value) : super("Cidade", value);

  static City get empty => City("");

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
  City copyWith({String? value}) {
    return City(value ?? this.value);
  }
}
