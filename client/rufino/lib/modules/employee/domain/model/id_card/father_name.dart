import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class FatherName extends TextPropBase {
  const FatherName(String value) : super("Nome do Pai", value);
  const FatherName.empty() : super("Nome do Pai", "");

  @override
  FatherName copyWith({String? value}) {
    return FatherName(value ?? this.value);
  }

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
}
