import 'package:equatable/equatable.dart';

class Name extends Equatable {
  final String value;

  const Name(this.value);

  const Name.empty() : value = "";

  static String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }

    if (value.length > 100) {
      return "Não pode ser maior que 100 caracteres.";
    }
    return null;
  }

  @override
  List<Object?> get props => [value];
}
