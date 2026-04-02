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
    this.associationIds = const [],
    this.associations = const [],
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

  /// The ids of the associated entities (roles or workplaces).
  final List<String> associationIds;

  /// The associated entities with their ids and names.
  final List<AssociationItem> associations;

  /// The type of association: 1 = Role, 2 = Workplace.
  final int associationTypeId;

  /// The display name of the association type (e.g. "Função", "Local de Trabalho").
  final String associationTypeName;

  /// The document templates attached to this requirement.
  final List<RequireDocumentTemplate> documentTemplates;

  /// The lifecycle events that trigger document generation, each with a list of statuses.
  final List<ListenEvent> listenEvents;

  /// Whether this requirement has any associations configured.
  bool get hasAssociation => associationIds.isNotEmpty;

  /// Whether this requirement is associated with a role (type 1).
  bool get isRoleAssociation => associationTypeId == 1;

  /// Whether this requirement is associated with a workplace (type 2).
  bool get isWorkplaceAssociation => associationTypeId == 2;

  /// Whether this requirement has any document templates attached.
  bool get hasTemplates => documentTemplates.isNotEmpty;

  /// Whether this requirement has any listen events configured.
  bool get hasEvents => listenEvents.isNotEmpty;

  /// Returns the number of document templates.
  int get templateCount => documentTemplates.length;

  /// Returns the number of listen events.
  int get eventCount => listenEvents.length;

  // ─── Validators ──────────────────────────────────────────────────────────

  /// Validates the require document name.
  ///
  /// Required, max 100 characters.
  static String? validateName(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'O Nome não pode ser vazio.';
    }
    if (value.length > 100) {
      return 'O Nome não pode ter mais de 100 caracteres.';
    }
    return null;
  }

  /// Validates the require document description.
  ///
  /// Required, max 500 characters.
  static String? validateDescription(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'A Descrição não pode ser vazia.';
    }
    if (value.length > 500) {
      return 'A Descrição não pode ter mais de 500 caracteres.';
    }
    return null;
  }
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

  /// Whether this template has a description filled in.
  bool get hasDescription => description.isNotEmpty;
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

  /// Whether this event has any statuses configured.
  bool get hasStatuses => statuses.isNotEmpty;

  /// Returns the number of statuses.
  int get statusCount => statuses.length;

  /// Whether the given [statusId] is selected for this event.
  bool hasStatus(int statusId) => statuses.any((s) => s.id == statusId);
}

/// An employee status used within a [ListenEvent].
class EventStatus {
  const EventStatus({required this.id, required this.name});

  /// The status identifier.
  final int id;

  /// The translated display name (e.g. "Pendente", "Ativo").
  final String name;
}

/// An association item representing a role or workplace linked to a [RequireDocument].
class AssociationItem {
  const AssociationItem({required this.id, required this.name});

  /// The unique identifier of the association.
  final String id;

  /// The display name of the association.
  final String name;
}
