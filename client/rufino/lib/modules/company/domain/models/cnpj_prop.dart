import 'package:equatable/equatable.dart';

class CNPJProp extends Equatable {
  final String value;

  const CNPJProp(this.value);

  const CNPJProp.empty() : value = "";

  factory CNPJProp.fromJson(Map<String, dynamic> json) {
    return CNPJProp(json['value']);
  }

  Map<String, dynamic> toJson() {
    return {'value': value};
  }

  static String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }

    // Remove non-numeric characters
    value = value.replaceAll(RegExp(r'\D'), '');

    // Check if the CNPJ is valid
    final cnpjPattern = RegExp(r'^\d{14}$');
    if (!cnpjPattern.hasMatch(value)) {
      return "CNPJ inválido.";
    }

    // Validate CNPJ digits
    final numbers = value.split('').map(int.parse).toList();
    final firstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    final secondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    final firstDigit = calculateDigit(numbers.sublist(0, 12), firstWeights);
    final secondDigit = calculateDigit(numbers.sublist(0, 13), secondWeights);

    if (numbers[12] != firstDigit || numbers[13] != secondDigit) {
      return "CNPJ inválido.";
    }
    return null;
  }

  static int calculateDigit(List<int> numbers, List<int> weights) {
    int sum = 0;
    for (int i = 0; i < weights.length; i++) {
      sum += numbers[i] * weights[i];
    }
    int mod = sum % 11;
    return mod < 2 ? 0 : 11 - mod;
  }

  @override
  List<Object?> get props => [value];
}
