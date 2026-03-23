/// A generic id/name pair used for dropdown selection in forms.
class SelectionOption {
  /// Creates a [SelectionOption] with the given [id] and [name].
  const SelectionOption({required this.id, required this.name});

  /// The unique identifier for this option.
  final String id;

  /// The human-readable display name for this option.
  final String name;
}
