import 'package:equatable/equatable.dart';

class CorporateNameProp extends Equatable {
  final String value;

  const CorporateNameProp(this.value);
  const CorporateNameProp.empty() : value = "";

  factory CorporateNameProp.fromJson(Map<String, dynamic> json) {
    return CorporateNameProp(json['value']);
  }

  Map<String, dynamic> toJson() {
    return {'value': value};
  }

  static String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }
    var regex = RegExp(r"^[a-zA-ZÀ-ÿ'_\\/-]+(?: [a-zA-ZÀ-ÿ'_\\/-]+)+$");
    if (regex.hasMatch(value) == false) {
      return "Formato invalido.";
    }
    if (value.length > 100) {
      return "Não pode ser maior que 100 caracteres.";
    }
    return null;
  }

  @override
  List<Object?> get props => [value];
}
