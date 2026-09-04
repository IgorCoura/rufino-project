/// A document group belonging to a company.
///
/// Document groups categorize document templates into logical collections
/// such as "Admissão", "Periódicos", etc. Each group has a human-readable
/// [name] and an optional [description] explaining its purpose.
class DocumentGroup {
  const DocumentGroup({
    required this.id,
    required this.name,
    required this.description,
  });

  /// Unique identifier for this document group.
  final String id;

  /// Human-readable name of the group (max 100 characters).
  final String name;

  /// Detailed description of the group purpose (max 1000 characters).
  final String description;

  /// Whether this group has a description filled in.
  bool get hasDescription => description.isNotEmpty;

  // ─── Validators ──────────────────────────────────────────────────────────

  /// Validates the group name.
  ///
  /// Required, max 100 characters.
  static String? validateName(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'O Nome não pode ser vazio.';
    }
    if (value.length > 100) {
      return 'O Nome não pode ser maior que 100 caracteres.';
    }
    return null;
  }

  /// Validates the group description.
  ///
  /// Required, max 1000 characters.
  static String? validateDescription(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'A Descrição não pode ser vazia.';
    }
    if (value.length > 1000) {
      return 'A Descrição não pode ser maior que 1000 caracteres.';
    }
    return null;
  }
}
