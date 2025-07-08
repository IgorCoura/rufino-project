import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Country extends TextPropBase {
  const Country(String value) : super("País", value);
  const Country.empty() : super("País", "");

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
  Country copyWith({String? value}) {
    return Country(value ?? this.value);
  }
}
