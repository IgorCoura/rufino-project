import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Neighborhood extends TextPropBase {
  const Neighborhood(String value) : super("Bairro", value);
  const Neighborhood.empty() : super("Bairro", "");

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
  Neighborhood copyWith({String? value}) {
    return Neighborhood(value ?? this.value);
  }
}
