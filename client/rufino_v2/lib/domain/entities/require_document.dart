/// A document requirement rule that binds document templates to associations.
///
/// Each [RequireDocument] specifies which document templates must be generated
/// for a given association (role or workplace), and which employee lifecycle
/// events trigger that generation.
class RequireDocument {
  const RequireDocument({
    required this.id,
    required this.name,
    required this.description,
    this.associationId = '',
    this.associationName = '',
    this.associationTypeId = 0,
    this.associationTypeName = '',
    this.documentTemplates = const [],
    this.listenEvents = const [],
  });

  /// The unique identifier of this require document.
  final String id;

  /// The display name of this require document.
  final String name;

  /// A longer description of the requirement's purpose.
  final String description;

  /// The id of the associated entity (role or workplace).
  final String associationId;

  /// The display name of the associated entity.
  final String associationName;

  /// The type of association: 1 = Role, 2 = Workplace.
  final int associationTypeId;

  /// The display name of the association type (e.g. "Função", "Local de Trabalho").
  final String associationTypeName;

  /// The document templates attached to this requirement.
  final List<RequireDocumentTemplate> documentTemplates;

  /// The lifecycle events that trigger document generation, each with a list of statuses.
  final List<ListenEvent> listenEvents;
}

/// A simplified reference to a document template within a [RequireDocument].
class RequireDocumentTemplate {
  const RequireDocumentTemplate({
    required this.id,
    required this.name,
    this.description = '',
  });

  /// The unique identifier of the template.
  final String id;

  /// The display name of the template.
  final String name;

  /// The description of the template.
  final String description;
}

/// An employee lifecycle event paired with the statuses that trigger it.
class ListenEvent {
  const ListenEvent({
    required this.eventId,
    required this.eventName,
    this.statuses = const [],
  });

  /// The event identifier.
  final int eventId;

  /// The translated display name of the event.
  final String eventName;

  /// The employee statuses that, combined with this event, trigger document generation.
  final List<EventStatus> statuses;
}

/// An employee status used within a [ListenEvent].
class EventStatus {
  const EventStatus({required this.id, required this.name});

  /// The status identifier.
  final int id;

  /// The translated display name (e.g. "Pendente", "Ativo").
  final String name;
}
