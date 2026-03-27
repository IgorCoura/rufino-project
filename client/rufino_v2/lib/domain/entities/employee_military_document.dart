/// The employee's military document (Documento Militar).
class EmployeeMilitaryDocument {
  /// Creates an [EmployeeMilitaryDocument].
  const EmployeeMilitaryDocument({
    required this.number,
    required this.type,
    required this.isRequired,
  });

  /// The document number.
  final String number;

  /// The document type (e.g. "Reservista").
  final String type;

  /// Whether the military document is required for this employee.
  ///
  /// When false the section should indicate that the document does not apply.
  final bool isRequired;

  /// Whether this employee is exempt from providing a military document.
  bool get isExempt => !isRequired;

  /// Whether this document has data filled in.
  bool get hasData => number.isNotEmpty || type.isNotEmpty;

  // ─── Validators ──────────────────────────────────────────────────────────

  /// Validates the military document number field.
  ///
  /// Required, max 20 characters.
  static String? validateNumber(String? value) {
    final trimmed = (value ?? '').trim();
    if (trimmed.isEmpty) {
      return 'O Número do documento não pode ser vazio.';
    }
    if (trimmed.length > 20) {
      return 'O Número do documento não pode ter mais de 20 caracteres.';
    }
    return null;
  }

  /// Validates the military document type field.
  ///
  /// Required, max 50 characters.
  static String? validateType(String? value) {
    final trimmed = (value ?? '').trim();
    if (trimmed.isEmpty) {
      return 'O Tipo de documento não pode ser vazio.';
    }
    if (trimmed.length > 50) {
      return 'O Tipo de documento não pode ter mais de 50 caracteres.';
    }
    return null;
  }

  /// Returns a copy of this document with the provided overrides applied.
  EmployeeMilitaryDocument copyWith({
    String? number,
    String? type,
    bool? isRequired,
  }) {
    return EmployeeMilitaryDocument(
      number: number ?? this.number,
      type: type ?? this.type,
      isRequired: isRequired ?? this.isRequired,
    );
  }
}
