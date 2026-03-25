/// Voter registration (Título de Eleitor) information for an employee.
class EmployeeVoteId {
  /// Creates an [EmployeeVoteId] with the given registration [number].
  const EmployeeVoteId({required this.number});

  /// The voter registration number.
  final String number;

  /// Returns a copy of this entity with the provided overrides applied.
  EmployeeVoteId copyWith({String? number}) {
    return EmployeeVoteId(number: number ?? this.number);
  }
}
