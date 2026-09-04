/// Identity document (Identidade) information for an employee.
class EmployeeIdCard {
  /// Creates an [EmployeeIdCard] with all identity document fields.
  const EmployeeIdCard({
    required this.cpf,
    required this.motherName,
    required this.fatherName,
    required this.dateOfBirth,
    required this.birthCity,
    required this.birthState,
    required this.nationality,
  });

  /// The employee's CPF (Brazilian tax identification number).
  final String cpf;

  /// The employee's mother's full name.
  final String motherName;

  /// The employee's father's full name.
  final String fatherName;

  /// The employee's date of birth in dd/MM/yyyy format for display.
  final String dateOfBirth;

  /// The city where the employee was born.
  final String birthCity;

  /// The state where the employee was born.
  final String birthState;

  /// The employee's nationality.
  final String nationality;

  /// Whether this ID card has a CPF filled in.
  bool get hasCpf => cpf.isNotEmpty;

  // ─── Formatters ─────────────────────────────────────────────────────────

  /// Formats the CPF for display as "123.456.789-10".
  ///
  /// Returns the raw value unchanged if it does not have exactly 11 digits.
  String get formattedCpf {
    final digits = cpf.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 11) return cpf;
    return '${digits.substring(0, 3)}.${digits.substring(3, 6)}.'
        '${digits.substring(6, 9)}-${digits.substring(9)}';
  }

  /// Returns the birth place formatted as "São Paulo — SP".
  ///
  /// Returns just the city if the state is empty, or "Não informado" if both
  /// are empty.
  String get formattedBirthPlace {
    if (birthCity.isEmpty) return 'Não informado';
    if (birthState.isNotEmpty) {
      return '$birthCity — ${birthState.toUpperCase()}';
    }
    return birthCity;
  }

  // ─── CPF validation algorithm ────────────────────────────────────────────

  /// Whether [cpf] passes the Brazilian CPF mathematical verification
  /// algorithm.
  static bool isCpfValid(String cpf) {
    final digits = cpf.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 11) return false;
    if (RegExp(r'^(\d)\1{10}$').hasMatch(digits)) return false;

    int firstSum = 0;
    for (int i = 0; i < 9; i++) {
      firstSum += int.parse(digits[i]) * (10 - i);
    }
    final mod1 = firstSum % 11;
    final d1 = mod1 < 2 ? 0 : 11 - mod1;
    if (d1 != int.parse(digits[9])) return false;

    int secondSum = 0;
    for (int i = 0; i < 10; i++) {
      secondSum += int.parse(digits[i]) * (11 - i);
    }
    final mod2 = secondSum % 11;
    final d2 = mod2 < 2 ? 0 : 11 - mod2;
    return d2 == int.parse(digits[10]);
  }

  // ─── Validators ──────────────────────────────────────────────────────────

  /// Validates a CPF field.
  ///
  /// Required field. Must have exactly 11 digits and pass the CPF check-digit
  /// algorithm.
  static String? validateCpf(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'O CPF não pode ser vazio.';
    }
    if (value.trim().length > 100) {
      return 'O CPF não pode ser maior que 100 caracteres.';
    }
    if (!isCpfValid(value)) {
      return 'O CPF não é válido.';
    }
    return null;
  }

  /// Validates a date of birth in `dd/MM/yyyy` format.
  ///
  /// Must not be empty, must be a parseable date, not in the future, and not
  /// older than 100 years.
  static String? validateDateOfBirth(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'A Data de nascimento não pode ser vazia.';
    }
    final digits = value.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 8) {
      return 'Data inválida (ex: 15/06/1990)';
    }
    try {
      final parts = value.split('/');
      final isoDate = '${parts[2]}-${parts[1]}-${parts[0]}';
      final date = DateTime.tryParse(isoDate);
      final now = DateTime.now();
      final hundredYearsAgo = now.subtract(const Duration(days: 36500));
      if (date == null ||
          date.isAfter(now) ||
          date.isBefore(hundredYearsAgo)) {
        return 'A Data de nascimento é inválida.';
      }
    } catch (_) {
      return 'A Data de nascimento é inválida.';
    }
    return null;
  }

  /// Validates the mother name field.
  ///
  /// Required, max 100 characters.
  static String? validateMotherName(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'O Nome da mãe não pode ser vazio.';
    }
    if (value.trim().length > 100) {
      return 'O Nome da mãe não pode ser maior que 100 caracteres.';
    }
    return null;
  }

  /// Validates the father name field.
  ///
  /// Optional, max 100 characters.
  static String? validateFatherName(String? value) {
    if (value != null && value.trim().length > 100) {
      return 'O Nome do pai não pode ser maior que 100 caracteres.';
    }
    return null;
  }

  /// Validates the birth city field.
  ///
  /// Required, max 100 characters.
  static String? validateBirthCity(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'A Cidade de nascimento não pode ser vazia.';
    }
    if (value.trim().length > 100) {
      return 'A Cidade de nascimento não pode ser maior que 100 caracteres.';
    }
    return null;
  }

  /// Validates a required state abbreviation (for ID card birth state).
  ///
  /// Required, must be exactly 2 characters.
  static String? validateBirthState(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'O Estado de nascimento não pode ser vazio.';
    }
    if (value.trim().length != 2) {
      return 'Use a sigla de 2 letras (ex: SP)';
    }
    return null;
  }

  /// Validates the nationality field.
  ///
  /// Required, max 100 characters.
  static String? validateNationality(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'A Nacionalidade não pode ser vazia.';
    }
    if (value.trim().length > 100) {
      return 'A Nacionalidade não pode ser maior que 100 caracteres.';
    }
    return null;
  }

  /// Returns a copy of this entity with the provided overrides applied.
  EmployeeIdCard copyWith({
    String? cpf,
    String? motherName,
    String? fatherName,
    String? dateOfBirth,
    String? birthCity,
    String? birthState,
    String? nationality,
  }) {
    return EmployeeIdCard(
      cpf: cpf ?? this.cpf,
      motherName: motherName ?? this.motherName,
      fatherName: fatherName ?? this.fatherName,
      dateOfBirth: dateOfBirth ?? this.dateOfBirth,
      birthCity: birthCity ?? this.birthCity,
      birthState: birthState ?? this.birthState,
      nationality: nationality ?? this.nationality,
    );
  }
}
