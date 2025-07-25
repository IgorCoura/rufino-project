import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Observation extends TextPropBase {
  const Observation(String value)
      : super("Observações sobre a deficiência", value);

  const Observation.empty() : super("Observações sobre a deficiência", "");

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
  Observation copyWith({String? value}) {
    return Observation(value ?? this.value);
  }
}
