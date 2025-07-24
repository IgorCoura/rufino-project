import 'package:equatable/equatable.dart';

class Description extends Equatable {
  final String value;

  const Description(this.value);

  const Description.empty() : value = "";

  static String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }

    if (value.length > 1000) {
      return "Não pode ser maior que 1000 caracteres.";
    }
    return null;
  }

  @override
  List<Object?> get props => [value];
}
