import 'dart:collection';

/// Personal demographic information for an employee.
class EmployeePersonalInfo {
  /// Creates an [EmployeePersonalInfo] with all demographic fields.
  const EmployeePersonalInfo({
    required this.genderId,
    required this.maritalStatusId,
    required this.ethnicityId,
    required this.educationLevelId,
    required this.disabilityIds,
    required this.disabilityObservation,
  });

  /// The id of the employee's gender option.
  final String genderId;

  /// The id of the employee's marital status option.
  final String maritalStatusId;

  /// The id of the employee's ethnicity option.
  final String ethnicityId;

  /// The id of the employee's education level option.
  final String educationLevelId;

  /// The list of ids of declared disabilities.
  final List<String> disabilityIds;

  /// Additional observations related to disabilities.
  final String disabilityObservation;

  /// Whether the employee has any declared disabilities.
  bool get hasDisabilities => disabilityIds.isNotEmpty;

  /// Returns the disability ids as an unmodifiable list.
  UnmodifiableListView<String> get unmodifiableDisabilityIds =>
      UnmodifiableListView(disabilityIds);

  /// Whether all demographic fields have been filled in.
  bool get isComplete =>
      genderId.isNotEmpty &&
      maritalStatusId.isNotEmpty &&
      ethnicityId.isNotEmpty &&
      educationLevelId.isNotEmpty;

  /// Returns a copy of this entity with the provided overrides applied.
  EmployeePersonalInfo copyWith({
    String? genderId,
    String? maritalStatusId,
    String? ethnicityId,
    String? educationLevelId,
    List<String>? disabilityIds,
    String? disabilityObservation,
  }) {
    return EmployeePersonalInfo(
      genderId: genderId ?? this.genderId,
      maritalStatusId: maritalStatusId ?? this.maritalStatusId,
      ethnicityId: ethnicityId ?? this.ethnicityId,
      educationLevelId: educationLevelId ?? this.educationLevelId,
      disabilityIds: disabilityIds ?? this.disabilityIds,
      disabilityObservation:
          disabilityObservation ?? this.disabilityObservation,
    );
  }
}
