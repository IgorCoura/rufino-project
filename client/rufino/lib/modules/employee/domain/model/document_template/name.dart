import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Name extends TextPropBase {
  const Name(String value) : super("Nome", value);
  const Name.empty() : super("Nome", "");

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
  Name copyWith({String? value}) {
    return Name(value ?? this.value);
  }
}
