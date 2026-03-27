/// Voter registration (Título de Eleitor) information for an employee.
class EmployeeVoteId {
  /// Creates an [EmployeeVoteId] with the given registration [number].
  const EmployeeVoteId({required this.number});

  /// The voter registration number.
  final String number;

  /// Whether this voter registration has a number filled in.
  bool get hasNumber => number.isNotEmpty;

  // ─── Formatters ─────────────────────────────────────────────────────────

  /// Formats the voter registration number as "0000.0000.0000".
  ///
  /// Returns the raw value unchanged if it does not have exactly 12 digits.
  String get formatted {
    final digits = number.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 12) return number;
    return '${digits.substring(0, 4)}.${digits.substring(4, 8)}.'
        '${digits.substring(8)}';
  }

  // ─── Vote ID validation algorithm ────────────────────────────────────────

  /// Whether [number] passes the Brazilian voter registration (Título de
  /// Eleitor) mathematical verification algorithm.
  static bool isVoteIdValid(String number) {
    final digits = number.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 12) return false;
    if (RegExp(r'^(\d)\1{11}$').hasMatch(digits)) return false;

    final uf = '${digits[8]}${digits[9]}';

    int sum = 0;
    const multiplierOne = [2, 3, 4, 5, 6, 7, 8, 9];
    for (int i = 0; i < 8; i++) {
      sum += int.parse(digits[i]) * multiplierOne[i];
    }
    int rest = sum % 11;
    if (rest > 9) {
      rest = 0;
    } else if (rest == 0 && (uf == '01' || uf == '02')) {
      rest = 1;
    }
    final z1 = rest.toString();

    sum = 0;
    const multiplierTwo = [7, 8, 9];
    final aux = '${digits[8]}${digits[9]}$z1';
    for (int i = 0; i < 3; i++) {
      sum += int.parse(aux[i]) * multiplierTwo[i];
    }
    rest = sum % 11;
    if (rest > 9) {
      rest = 0;
    } else if (rest == 0 && (uf == '01' || uf == '02')) {
      rest = 1;
    }
    final z2 = rest.toString();

    return digits.endsWith('$z1$z2');
  }

  // ─── Validators ──────────────────────────────────────────────────────────

  /// Validates a voter registration number.
  ///
  /// Required field with exactly 12 digits that must pass the check-digit
  /// algorithm.
  static String? validateNumber(String? value) {
    final stripped = (value ?? '').replaceAll(RegExp(r'[^\d]'), '');
    if (stripped.isEmpty) {
      return 'O Número do título não pode ser vazio.';
    }
    if (stripped.length != 12) {
      return 'Número inválido (ex: 0000.0000.0000)';
    }
    if (!isVoteIdValid(stripped)) {
      return 'O Número do título não é válido.';
    }
    return null;
  }

  /// Returns a copy of this entity with the provided overrides applied.
  EmployeeVoteId copyWith({String? number}) {
    return EmployeeVoteId(number: number ?? this.number);
  }
}
