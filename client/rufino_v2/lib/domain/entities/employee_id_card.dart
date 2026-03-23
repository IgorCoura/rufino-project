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
