/// The employee's medical admission exam (Exame Médico Admissional / ASO).
class EmployeeMedicalExam {
  /// Creates an [EmployeeMedicalExam].
  const EmployeeMedicalExam({
    required this.dateExam,
    required this.validityExam,
  });

  /// The exam date in `dd/MM/yyyy` display format.
  final String dateExam;

  /// The exam validity/expiry date in `dd/MM/yyyy` display format.
  final String validityExam;

  /// Whether this exam has a date filled in.
  bool get hasExamDate => dateExam.isNotEmpty;

  /// Whether this exam has expired based on [validityExam].
  ///
  /// Returns false if the validity date cannot be parsed.
  bool get isExpired {
    if (validityExam.isEmpty) return false;
    try {
      final parts = validityExam.split('/');
      final date = DateTime.tryParse('${parts[2]}-${parts[1]}-${parts[0]}');
      if (date == null) return false;
      return date.isBefore(DateTime.now());
    } catch (_) {
      return false;
    }
  }

  // ─── Validators ──────────────────────────────────────────────────────────

  /// Parses a `dd/MM/yyyy` string into a [DateTime], or null if invalid.
  static DateTime? _parseDate(String value) {
    try {
      final parts = value.split('/');
      return DateTime.tryParse('${parts[2]}-${parts[1]}-${parts[0]}');
    } catch (_) {
      return null;
    }
  }

  /// Validates the exam date field.
  ///
  /// Required, must be a valid date within the last 365 days.
  static String? validateDateExam(String? value) {
    final stripped = (value ?? '').replaceAll(RegExp(r'[^\d]'), '');
    if (stripped.isEmpty) {
      return 'A Data do exame não pode ser vazia.';
    }
    if (stripped.length != 8) {
      return 'A Data do exame é inválida.';
    }
    final date = _parseDate(value!);
    if (date == null) return 'A Data do exame é inválida.';

    final now = DateTime.now();
    final minDate = now.subtract(const Duration(days: 365));
    final maxDate = now.add(const Duration(days: 1));
    if (date.isBefore(minDate) || date.isAfter(maxDate)) {
      return 'A Data do exame é inválida.';
    }
    return null;
  }

  /// Validates the exam validity/expiry date field.
  ///
  /// Required, must be a future date (up to 10 years from now).
  static String? validateExamValidity(String? value) {
    final stripped = (value ?? '').replaceAll(RegExp(r'[^\d]'), '');
    if (stripped.isEmpty) {
      return 'A Validade do exame não pode ser vazia.';
    }
    if (stripped.length != 8) {
      return 'A Validade do exame é inválida.';
    }
    final date = _parseDate(value!);
    if (date == null) return 'A Validade do exame é inválida.';

    final now = DateTime.now();
    final minDate = now.add(const Duration(days: 1));
    final maxDate = now.add(const Duration(days: 3650));
    if (date.isBefore(minDate) || date.isAfter(maxDate)) {
      return 'A Validade do exame é inválida.';
    }
    return null;
  }
}
