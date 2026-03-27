import 'selection_option.dart';

/// The set of available options for personal info dropdown fields.
class PersonalInfoOptions {
  /// Creates a [PersonalInfoOptions] with all selection lists.
  const PersonalInfoOptions({
    required this.genders,
    required this.maritalStatuses,
    required this.ethnicities,
    required this.educationLevels,
    required this.disabilities,
  });

  /// Available gender options.
  final List<SelectionOption> genders;

  /// Available marital status options.
  final List<SelectionOption> maritalStatuses;

  /// Available ethnicity options.
  final List<SelectionOption> ethnicities;

  /// Available education level options.
  final List<SelectionOption> educationLevels;

  /// Available disability options.
  final List<SelectionOption> disabilities;

  /// Whether all option lists are populated.
  bool get isLoaded =>
      genders.isNotEmpty &&
      maritalStatuses.isNotEmpty &&
      ethnicities.isNotEmpty &&
      educationLevels.isNotEmpty &&
      disabilities.isNotEmpty;

  /// Resolves a gender [id] to its display name.
  String genderLabel(String id) =>
      SelectionOption.labelForId(genders, id);

  /// Resolves a marital status [id] to its display name.
  String maritalStatusLabel(String id) =>
      SelectionOption.labelForId(maritalStatuses, id);

  /// Resolves an ethnicity [id] to its display name.
  String ethnicityLabel(String id) =>
      SelectionOption.labelForId(ethnicities, id);

  /// Resolves an education level [id] to its display name.
  String educationLevelLabel(String id) =>
      SelectionOption.labelForId(educationLevels, id);

  /// Resolves a disability [id] to its display name.
  String disabilityLabel(String id) =>
      SelectionOption.labelForId(disabilities, id);
}
