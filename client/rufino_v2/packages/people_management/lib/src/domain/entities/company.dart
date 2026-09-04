/// A company registered in the system.
class Company {
  const Company({
    required this.id,
    required this.corporateName,
    required this.fantasyName,
    required this.cnpj,
  });

  final String id;

  /// The official corporate name (Razão Social).
  final String corporateName;

  /// The trade/fantasy name (Nome Fantasia).
  final String fantasyName;

  /// The Brazilian company registration number (CNPJ), digits only.
  final String cnpj;

  /// Whether this company has a fantasy name filled in.
  bool get hasFantasyName => fantasyName.isNotEmpty;

  /// Returns the best display name for this company.
  ///
  /// Prefers [fantasyName], falls back to [corporateName].
  String get displayName =>
      fantasyName.isNotEmpty ? fantasyName : corporateName;

  /// Formats the CNPJ for display as "12.345.678/0001-90".
  ///
  /// Returns the raw value unchanged if it does not have exactly 14 digits.
  String get formattedCnpj {
    final digits = cnpj.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 14) return cnpj;
    return '${digits.substring(0, 2)}.${digits.substring(2, 5)}.'
        '${digits.substring(5, 8)}/${digits.substring(8, 12)}-'
        '${digits.substring(12)}';
  }

  // ─── Validators ──────────────────────────────────────────────────────────

  /// Validates a required text field.
  static String? validateRequired(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    return null;
  }

  /// Validates a CNPJ field.
  ///
  /// Required, must have exactly 14 digits.
  static String? validateCnpj(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    final digits = value.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 14) return 'CNPJ inválido.';
    return null;
  }

  /// Validates a company email field.
  ///
  /// Required, must match a basic email pattern.
  static String? validateEmail(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    final regex =
        RegExp(r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$');
    if (!regex.hasMatch(value)) return 'Email inválido.';
    return null;
  }
}
