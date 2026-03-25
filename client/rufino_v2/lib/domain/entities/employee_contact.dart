/// Contact information for an employee.
class EmployeeContact {
  /// Creates an [EmployeeContact] with the given [cellphone] and [email].
  const EmployeeContact({
    required this.cellphone,
    required this.email,
  });

  /// The employee's mobile phone number.
  final String cellphone;

  /// The employee's email address.
  final String email;

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
