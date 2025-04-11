import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class DirectoryName extends TextPropBase {
  const DirectoryName(String value) : super("Nome do Diretorio", value);

  const DirectoryName.empty() : super("Nome do Diretorio", "");

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
  DirectoryName copyWith({String? value}) {
    return DirectoryName(value ?? this.value);
  }
}
