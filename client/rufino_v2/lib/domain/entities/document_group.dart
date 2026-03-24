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
}
