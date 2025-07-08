import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class City extends TextPropBase {
  const City(String value) : super("Cidade", value);
  const City.empty() : super("Cidade", "");

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
