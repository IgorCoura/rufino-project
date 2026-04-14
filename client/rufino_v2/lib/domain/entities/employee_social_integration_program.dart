/// Social Integration Program (PIS/PASEP) registration for an employee.
class EmployeeSocialIntegrationProgram {
  /// Creates an [EmployeeSocialIntegrationProgram] with the given [number].
  const EmployeeSocialIntegrationProgram({required this.number});

  /// Expected length of the PIS/PASEP number (11 digits).
  static const int expectedLength = 11;

  /// The PIS/PASEP registration number (raw digits).
  final String number;

  /// Whether this PIS has a number filled in.
  bool get hasNumber => number.isNotEmpty;

  // ─── Formatters ─────────────────────────────────────────────────────────

  /// Formats the PIS/PASEP number as `000.00000.00-0`.
  ///
  /// Returns the raw value unchanged if it does not have exactly 11 digits.
  String get formatted {
    final digits = number.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != expectedLength) return number;
    return '${digits.substring(0, 3)}.${digits.substring(3, 8)}.'
        '${digits.substring(8, 10)}-${digits.substring(10)}';
  }

  // ─── PIS check-digit algorithm ──────────────────────────────────────────

  /// Whether [number] passes the PIS/PASEP mod-11 check-digit algorithm.
  ///
  /// Mirrors the server-side validation: rejects lengths other than 11,
  /// sequences of identical digits, and numbers whose last digit does not
  /// match the computed verifier.
  static bool isPisValid(String number) {
    final digits = number.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != expectedLength) return false;
    if (RegExp(r'^(\d)\1{10}$').hasMatch(digits)) return false;

    const multiplier = [3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    var sum = 0;
    for (var i = 0; i < 10; i++) {
      sum += int.parse(digits[i]) * multiplier[i];
    }
    var rest = 11 - (sum % 11);
    if (rest > 9) rest = 0;

    return digits.endsWith(rest.toString());
  }

  // ─── Validators ─────────────────────────────────────────────────────────

  /// Validates a PIS/PASEP number for form input.
  ///
  /// Returns `null` when [value] is a valid PIS or a localized error message
  /// when it is empty, has the wrong length, or fails the check-digit rule.
  static String? validateNumber(String? value) {
    final stripped = (value ?? '').replaceAll(RegExp(r'[^\d]'), '');
    if (stripped.isEmpty) {
      return 'O número do PIS não pode ser vazio.';
    }
    if (stripped.length != expectedLength) {
      return 'Número inválido (ex: 000.00000.00-0)';
    }
    if (!isPisValid(stripped)) {
      return 'O número do PIS não é válido.';
    }
    return null;
  }

  /// Returns a copy of this entity with the provided overrides applied.
  EmployeeSocialIntegrationProgram copyWith({String? number}) {
    return EmployeeSocialIntegrationProgram(number: number ?? this.number);
  }
}
