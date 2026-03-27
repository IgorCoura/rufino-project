/// A physical address associated with a [Workplace] or an employee.
class Address {
  const Address({
    required this.zipCode,
    required this.street,
    required this.number,
    required this.complement,
    required this.neighborhood,
    required this.city,
    required this.state,
    required this.country,
  });

  final String zipCode;
  final String street;
  final String number;
  final String complement;
  final String neighborhood;
  final String city;
  final String state;
  final String country;

  /// A short human-readable summary in the format "neighborhood, city - state".
  String get minimal => '$neighborhood, $city - $state';

  /// Whether this address has the main fields filled in.
  bool get isComplete =>
      zipCode.isNotEmpty &&
      street.isNotEmpty &&
      neighborhood.isNotEmpty &&
      city.isNotEmpty &&
      state.isNotEmpty;

  // ─── Formatters ─────────────────────────────────────────────────────────

  /// Formats the raw zip code digits as "01310-100".
  ///
  /// Returns the raw value unchanged if it does not have exactly 8 digits.
  String get formattedZipCode {
    final digits = zipCode.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 8) return zipCode;
    return '${digits.substring(0, 5)}-${digits.substring(5)}';
  }

  /// Returns a multi-line formatted address suitable for display.
  ///
  /// Line 1: Street, number — complement (if present).
  /// Line 2: Neighborhood (if present).
  /// Line 3: City — STATE, ZIP (if present).
  /// Line 4: Country (if present).
  String get formattedDisplay {
    final lines = <String>[];

    // Line 1: street, number — complement
    final streetParts = [street, if (number.isNotEmpty) number].join(', ');
    final line1 = complement.isNotEmpty
        ? '$streetParts — $complement'
        : streetParts;
    if (line1.trim().isNotEmpty) lines.add(line1);

    // Line 2: neighborhood
    if (neighborhood.isNotEmpty) lines.add(neighborhood);

    // Line 3: city — STATE, ZIP
    final cityState = [
      if (city.isNotEmpty) city,
      if (state.isNotEmpty) state.toUpperCase(),
    ].join(' — ');
    final zip = formattedZipCode;
    final line3 = [
      if (cityState.isNotEmpty) cityState,
      if (zip.isNotEmpty) zip,
    ].join(', ');
    if (line3.isNotEmpty) lines.add(line3);

    // Line 4: country
    if (country.isNotEmpty) lines.add(country);

    return lines.join('\n');
  }

  /// Returns a single-line summary with all parts separated by commas and
  /// the CEP appended, e.g. "Rua X, 123, Sala 1, Bela Vista, São Paulo, SP,
  /// Brasil - CEP: 01310-100".
  ///
  /// Empty parts are skipped.
  String get inlineSummary {
    final parts = <String>[
      if (street.isNotEmpty) street,
      if (number.isNotEmpty) number,
      if (complement.isNotEmpty) complement,
      if (neighborhood.isNotEmpty) neighborhood,
      if (city.isNotEmpty) city,
      if (state.isNotEmpty) state,
      if (country.isNotEmpty) country,
    ];
    final base = parts.join(', ');
    if (zipCode.isNotEmpty) return '$base - CEP: $zipCode';
    return base;
  }

  // ─── Validators ──────────────────────────────────────────────────────────

  /// Validates a Brazilian CEP (postal code).
  ///
  /// Required field with exactly 8 digits.
  static String? validateCep(String? value) {
    if (value == null || value.trim().isEmpty) return 'CEP é obrigatório';
    final digits = value.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 8) return 'CEP inválido (ex: 01310-100)';
    return null;
  }

  /// Validates a required text field with a custom [label].
  static String? validateRequired(String? value, String label) {
    if (value == null || value.trim().isEmpty) return '$label é obrigatório';
    return null;
  }

  /// Validates an optional state abbreviation field.
  ///
  /// Returns null when empty. When filled, must be exactly 2 characters.
  static String? validateState(String? value) {
    if (value == null || value.trim().isEmpty) return null;
    if (value.trim().length != 2) {
      return 'Use a sigla de 2 letras (ex: SP)';
    }
    return null;
  }

  /// Validates a street field.
  ///
  /// Required, max 100 characters.
  static String? validateStreet(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    if (value.length > 100) return 'Máx. 100 caracteres.';
    return null;
  }

  /// Validates a number field.
  ///
  /// Required.
  static String? validateNumber(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    return null;
  }

  /// Validates a complement field.
  ///
  /// Optional, max 50 characters.
  static String? validateComplement(String? value) {
    if (value != null && value.length > 50) return 'Máx. 50 caracteres.';
    return null;
  }

  /// Validates a neighborhood field.
  ///
  /// Required, max 50 characters.
  static String? validateNeighborhood(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    if (value.length > 50) return 'Máx. 50 caracteres.';
    return null;
  }

  /// Validates a city field.
  ///
  /// Required, max 50 characters.
  static String? validateCity(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    if (value.length > 50) return 'Máx. 50 caracteres.';
    return null;
  }

  /// Validates a state field (general, not abbreviation-specific).
  ///
  /// Required, max 50 characters.
  static String? validateStateFull(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    if (value.length > 50) return 'Máx. 50 caracteres.';
    return null;
  }

  /// Validates a country field.
  ///
  /// Required, max 50 characters.
  static String? validateCountry(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    if (value.length > 50) return 'Máx. 50 caracteres.';
    return null;
  }
}
