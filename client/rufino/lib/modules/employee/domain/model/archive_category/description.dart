import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Description extends TextPropBase {
  const Description(String value) : super("Description", value);

  const Description.empty() : super("Description", "");

  @override
  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "O $displayName não pode ser vazio.";
    }
    if (value.length > 500) {
      return "o $displayName não pode ser maior que 100 caracteres.";
    }
    return null;
  }

  @override
  Description copyWith({String? value}) {
    return Description(value ?? this.value);
  }
}
