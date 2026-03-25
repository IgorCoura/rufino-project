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
}
