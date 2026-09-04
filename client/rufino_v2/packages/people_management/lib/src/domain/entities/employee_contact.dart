/// Contact information for an employee.
class EmployeeContact {
  /// Creates an [EmployeeContact] with the given [cellphone] and [email].
  const EmployeeContact({
    required this.cellphone,
    required this.email,
  });

  /// The employee's mobile phone number (digits only).
  final String cellphone;

  /// The employee's email address.
  final String email;

  /// Whether this contact has a phone number filled in.
  bool get hasPhone => cellphone.isNotEmpty;

  /// Whether this contact has an email filled in.
  bool get hasEmail => email.isNotEmpty;

  /// Whether both phone and email are empty.
  bool get isEmpty => !hasPhone && !hasEmail;

  // ─── Formatters ─────────────────────────────────────────────────────────

  /// Formats raw digit string for display as "+55 11 96860-8425".
  ///
  /// Handles both 10-digit (landline) and 11-digit (mobile) numbers.
  /// Returns the raw digits unchanged if the length is unexpected.
  String get formattedPhone {
    final digits = cellphone.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.isEmpty) return '';
    if (digits.length == 11) {
      return '+55 ${digits.substring(0, 2)} '
          '${digits.substring(2, 7)}-${digits.substring(7)}';
    }
    if (digits.length == 10) {
      return '+55 ${digits.substring(0, 2)} '
          '${digits.substring(2, 6)}-${digits.substring(6)}';
    }
    return digits;
  }

  // ─── Validators ──────────────────────────────────────────────────────────

  static final _emailRegex = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');

  /// Validates a phone number.
  ///
  /// Optional field — returns null when empty. When filled, must have 10 or
  /// 11 digits.
  static String? validatePhone(String? value) {
    if (value == null || value.trim().isEmpty) return null;
    final digits = value.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 10 && digits.length != 11) {
      return 'Número inválido (ex: 11 98765-4321)';
    }
    return null;
  }

  /// Validates an email address.
  ///
  /// Optional field — returns null when empty. When filled, must match a
  /// basic email pattern.
  static String? validateEmail(String? value) {
    if (value == null || value.trim().isEmpty) return null;
    if (!_emailRegex.hasMatch(value.trim())) return 'E-mail inválido';
    return null;
  }

  /// Returns a copy of this contact with the provided overrides applied.
  EmployeeContact copyWith({
    String? cellphone,
    String? email,
  }) {
    return EmployeeContact(
      cellphone: cellphone ?? this.cellphone,
      email: email ?? this.email,
    );
  }
}
