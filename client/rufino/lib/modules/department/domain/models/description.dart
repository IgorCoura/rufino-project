import 'package:equatable/equatable.dart';

class Description extends Equatable {
  final String value;

  const Description(this.value);

  const Description.empty() : value = "";

  static String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }

    if (value.length > 500) {
      return "Não pode ser maior que 500 caracteres.";
    }
    return null;
  }

  @override
  List<Object?> get props => [value];
}

class SecundaryDescription extends Equatable {
  final String value;

  const SecundaryDescription(this.value);

  const SecundaryDescription.empty() : value = "";

  static String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }

    if (value.length > 500) {
      return "Não pode ser maior que 500 caracteres.";
    }
    return null;
  }

  @override
  List<Object?> get props => [value];
}
