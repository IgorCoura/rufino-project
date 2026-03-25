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
