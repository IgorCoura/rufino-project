import 'package:equatable/equatable.dart';

class FantasyNameProp extends Equatable {
  final String value;

  const FantasyNameProp(this.value);
  const FantasyNameProp.empty() : value = "";

  factory FantasyNameProp.fromJson(Map<String, dynamic> json) {
    return FantasyNameProp(json['value']);
  }

  Map<String, dynamic> toJson() {
    return {'value': value};
  }

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
