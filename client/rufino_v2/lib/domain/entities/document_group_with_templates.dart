/// A document group with its associated document templates.
///
/// Represents the combined view of a document group and its nested templates,
/// used for the unified list screen where groups can be expanded to reveal
/// their templates.
class DocumentGroupWithTemplates {
  const DocumentGroupWithTemplates({
    required this.id,
    required this.name,
    required this.description,
    this.templates = const [],
  });

  /// Unique identifier for this document group.
  final String id;

  /// Human-readable name of the group.
  final String name;

  /// Detailed description of the group purpose.
  final String description;

  /// The document templates belonging to this group.
  final List<DocumentTemplateSummary> templates;
}

/// A simplified document template used inside [DocumentGroupWithTemplates].
///
/// Contains only the fields returned by the `withtemplates` endpoint.
class DocumentTemplateSummary {
  const DocumentTemplateSummary({
    required this.id,
    required this.name,
    required this.description,
  });

  /// Unique identifier for this template.
  final String id;

  /// Human-readable name of the template.
  final String name;

  /// Detailed description of the template purpose.
  final String description;
}
