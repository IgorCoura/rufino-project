/// A dependent (Dependente) of an employee.
class EmployeeDependent {
  /// Creates an [EmployeeDependent].
  const EmployeeDependent({
    required this.originalName,
    required this.name,
    required this.genderId,
    required this.dependencyTypeId,
    required this.cpf,
    required this.motherName,
    required this.fatherName,
    required this.dateOfBirth,
    required this.birthCity,
    required this.birthState,
    required this.nationality,
  });

  /// The name as it was loaded from the API.
  ///
  /// Used as the identifier for update/remove operations. Empty for new
  /// dependents that have not yet been persisted.
  final String originalName;

  /// The dependent's full name.
  final String name;

  /// The gender id (e.g. "1" = Homem, "2" = Mulher).
  final String genderId;

  /// The dependency type id (e.g. "1" = Filho(a), "2" = Cônjuge).
  final String dependencyTypeId;

  /// The dependent's CPF.
  final String cpf;

  /// The dependent's mother's name.
  final String motherName;

  /// The dependent's father's name.
  final String fatherName;

  /// Date of birth in `dd/MM/yyyy` display format.
  final String dateOfBirth;

  /// The dependent's city of birth.
  final String birthCity;

  /// The dependent's state of birth (2-letter abbreviation).
  final String birthState;

  /// The dependent's nationality.
  final String nationality;

  /// Whether this is a new dependent not yet saved to the API.
  bool get isNew => originalName.isEmpty;

  /// Whether this dependent is a child (dependency type "1").
  bool get isChild => dependencyTypeId == '1';

  /// Whether this dependent is a spouse (dependency type "2").
  bool get isSpouse => dependencyTypeId == '2';

  /// Display label for the dependency type in Portuguese.
  String get dependencyTypeLabel => switch (dependencyTypeId) {
        '1' => 'Filho(a)',
        '2' => 'Cônjuge',
        _ => dependencyTypeId,
      };

  // ─── Validators ──────────────────────────────────────────────────────────

  /// Validates the dependent name field.
  ///
  /// Required, max 100 characters.
  static String? validateName(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'O Nome do dependente não pode ser vazio.';
    }
    if (value.trim().length > 100) {
      return 'O Nome não pode ter mais de 100 caracteres.';
    }
    return null;
  }
}
