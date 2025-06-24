import 'package:equatable/equatable.dart';

class CBO extends Equatable {
  final String value;

  const CBO(this.value);

  const CBO.empty() : value = "";

  static String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }

    if (value.length > 6) {
      return "Não pode ser maior que 6 caracteres.";
    }
    return null;
  }

  @override
  List<Object?> get props => [value];
}
