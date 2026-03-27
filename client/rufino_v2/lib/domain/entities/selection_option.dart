/// A generic id/name pair used for dropdown selection in forms.
class SelectionOption {
  /// Creates a [SelectionOption] with the given [id] and [name].
  const SelectionOption({required this.id, required this.name});

  /// The unique identifier for this option.
  final String id;

  /// The human-readable display name for this option.
  final String name;

  /// Returns the display name for [id] in [options], or `"Não informado"`.
  static String labelForId(List<SelectionOption> options, String id) {
    if (id.isEmpty) return 'Não informado';
    return options
            .where((o) => o.id == id)
            .firstOrNull
            ?.name ??
        'Não informado';
  }

  /// Returns [id] when it exists in [options]; otherwise null.
  ///
  /// Useful to guard [DropdownButtonFormField] values against mismatches
  /// between stored IDs and available items.
  static String? safeId(String? id, List<SelectionOption> options) {
    if (id == null || id.isEmpty) return null;
    return options.any((o) => o.id == id) ? id : null;
  }
}
