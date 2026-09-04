import '../../domain/entities/require_document.dart';

/// DTO for the require document API responses.
///
/// Provides two factory constructors: [fromJsonSimple] for list endpoints
/// and [fromJson] for detail endpoints with nested templates and events.
class RequireDocumentApiModel {
  const RequireDocumentApiModel({
    required this.id,
    required this.name,
    required this.description,
    this.companyId = '',
    this.associationIds = const [],
    this.associations = const [],
    this.associationTypeId = 0,
    this.associationTypeName = '',
    this.documentTemplates = const [],
    this.listenEvents = const [],
  });

  final String id;
  final String name;
  final String description;
  final String companyId;
  final List<String> associationIds;
  final List<AssociationItemDto> associations;
  final int associationTypeId;
  final String associationTypeName;
  final List<DocumentTemplateDto> documentTemplates;
  final List<ListenEventDto> listenEvents;

  /// Parses a simplified JSON object from the list endpoint.
  factory RequireDocumentApiModel.fromJsonSimple(Map<String, dynamic> json) {
    return RequireDocumentApiModel(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      description: json['description'] as String? ?? '',
    );
  }

  /// Parses a full JSON object from the detail endpoint.
  factory RequireDocumentApiModel.fromJson(Map<String, dynamic> json) {
    final associationType = json['associationType'] as Map<String, dynamic>?;
    final associationsRaw = json['associations'] as List<dynamic>? ?? [];

    final associations = associationsRaw
        .map((a) => AssociationItemDto.fromJson(a as Map<String, dynamic>))
        .toList();

    return RequireDocumentApiModel(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      description: json['description'] as String? ?? '',
      companyId: json['companyId'] as String? ?? '',
      associationIds: associations.map((a) => a.id).toList(),
      associations: associations,
      associationTypeId: _parseAssociationTypeId(associationType),
      associationTypeName: _associationTypeLabel(
          _parseAssociationTypeId(associationType)),
      documentTemplates: _parseTemplates(json['documentsTemplates']),
      listenEvents: _parseListenEvents(json['listenEvents']),
    );
  }

  /// Converts this DTO to a domain [RequireDocument] entity.
  RequireDocument toEntity() {
    return RequireDocument(
      id: id,
      name: name,
      description: description,
      associationIds: associationIds,
      associations: associations
          .map((a) => AssociationItem(id: a.id, name: a.name))
          .toList(),
      associationTypeId: associationTypeId,
      associationTypeName: associationTypeName,
      documentTemplates: documentTemplates
          .map((t) => RequireDocumentTemplate(
                id: t.id,
                name: t.name,
                description: t.description,
              ))
          .toList(),
      listenEvents: listenEvents
          .map((e) => ListenEvent(
                eventId: e.eventId,
                eventName: e.eventName,
                statuses: e.statusIds
                    .map((id) => EventStatus(
                          id: id,
                          name: _statusLabel(id),
                        ))
                    .toList(),
              ))
          .toList(),
    );
  }

  /// Serializes this model for a create request (no id).
  Map<String, dynamic> toCreateJson() => {
        'name': name,
        'description': description,
        'associationIds': associationIds,
        'associationType': associationTypeId,
        'documentsTemplatesIds': documentTemplates.map((t) => t.id).toList(),
        'listenEvents': listenEvents
            .map((e) => {
                  'eventId': e.eventId,
                  'status': e.statusIds,
                })
            .toList(),
      };

  /// Serializes this model for an update request (includes id).
  Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'description': description,
        'associationIds': associationIds,
        'associationType': associationTypeId,
        'documentsTemplatesIds': documentTemplates.map((t) => t.id).toList(),
        'listenEvents': listenEvents
            .map((e) => {
                  'eventId': e.eventId,
                  'status': e.statusIds,
                })
            .toList(),
      };

  // ─── Private helpers ──────────────────────────────────────────────────────

  static int _parseAssociationTypeId(Map<String, dynamic>? json) {
    if (json == null) return 0;
    final raw = json['id'];
    if (raw is int) return raw;
    if (raw is String) return int.tryParse(raw) ?? 0;
    return 0;
  }

  static String _associationTypeLabel(int id) => switch (id) {
        1 => 'Função',
        2 => 'Local de Trabalho',
        _ => '',
      };

  static String _statusLabel(int id) => switch (id) {
        1 => 'Pendente',
        2 => 'Ativo',
        3 => 'Férias',
        4 => 'Afastado',
        5 => 'Inativo',
        _ => id.toString(),
      };

  static List<DocumentTemplateDto> _parseTemplates(dynamic raw) {
    if (raw == null) return [];
    final list = raw as List<dynamic>;
    return list
        .map((e) => DocumentTemplateDto.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  static List<ListenEventDto> _parseListenEvents(dynamic raw) {
    if (raw == null) return [];
    final list = raw as List<dynamic>;
    return list
        .map((e) => ListenEventDto.fromJson(e as Map<String, dynamic>))
        .toList();
  }
}

/// A nested DTO representing a document template within a require document response.
class DocumentTemplateDto {
  const DocumentTemplateDto({
    required this.id,
    required this.name,
    this.description = '',
  });

  final String id;
  final String name;
  final String description;

  factory DocumentTemplateDto.fromJson(Map<String, dynamic> json) {
    return DocumentTemplateDto(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      description: json['description'] as String? ?? '',
    );
  }
}

/// A nested DTO representing a listen event within a require document response.
class ListenEventDto {
  const ListenEventDto({
    required this.eventId,
    required this.eventName,
    this.statusIds = const [],
  });

  final int eventId;
  final String eventName;
  final List<int> statusIds;

  factory ListenEventDto.fromJson(Map<String, dynamic> json) {
    final event = json['event'] as Map<String, dynamic>?;
    final statusRaw = json['status'] as List<dynamic>? ?? [];
    return ListenEventDto(
      eventId: event?['id'] as int? ?? 0,
      eventName: _convertEventName(
        event?['id'] as int? ?? 0,
        event?['name'] as String? ?? '',
      ),
      statusIds: statusRaw.map((s) => s as int).toList(),
    );
  }

  /// Translates event names from English API keys to Brazilian Portuguese.
  static String _convertEventName(int id, String name) {
    const map = {
      'createdevent': 'Evento Criado',
      'namechangeevent': 'Evento de Alteração de Nome',
      'rolechangeevent': 'Evento de Alteração de Função',
      'workplacechangeevent': 'Evento de Alteração de Local de Trabalho',
      'addresschangeevent': 'Evento de Alteração de Endereço',
      'contactchangeevent': 'Evento de Alteração de Contato',
      'medicaladmissionexamchangeevent':
          'Evento de Alteração de Exame Médico Admissional',
      'personalinfochangeevent': 'Evento de Alteração de Informações Pessoais',
      'idcardchangeevent': 'Evento de Alteração de Documento de Identidade',
      'voteidchangeevent': 'Evento de Alteração de Título de Eleitor',
      'militardocumentchangeevent': 'Evento de Alteração de Documento Militar',
      'completeadmissionevent': 'Evento de Admissão Completa',
      'dependentchildchangeevent': 'Evento de Alteração de Dependente Filho',
      'dependentspousechangeevent': 'Evento de Alteração de Dependente Cônjuge',
      'dependentremovedevent': 'Evento de Remoção de Dependente',
      'finishedcontractevent': 'Evento de Contrato Finalizado',
      'demissionalexamrequestevent':
          'Evento de Solicitação de Exame Demissional',
      'documentsigningoptionschangeevent':
          'Evento de Alteração de Opções de Assinatura de Documentos',
      'januaryevent': 'Evento Recorrente de Janeiro',
      'februaryevent': 'Evento Recorrente de Fevereiro',
      'marchevent': 'Evento Recorrente de Março',
      'aprilevent': 'Evento Recorrente de Abril',
      'mayevent': 'Evento Recorrente de Maio',
      'juneevent': 'Evento Recorrente de Junho',
      'julyevent': 'Evento Recorrente de Julho',
      'augustevent': 'Evento Recorrente de Agosto',
      'septemberevent': 'Evento Recorrente de Setembro',
      'octoberevent': 'Evento Recorrente de Outubro',
      'novemberevent': 'Evento Recorrente de Novembro',
      'decemberevent': 'Evento Recorrente de Dezembro',
      'dailyevent': 'Evento Recorrente Diário',
      'weeklyevent': 'Evento Recorrente Semanal',
      'monthlyevent': 'Evento Recorrente Mensal',
      'yearlyevent': 'Evento Recorrente Anual',
    };
    final translated = map[name.toLowerCase()];
    if (translated != null) return translated;
    return name.isEmpty ? id.toString() : name;
  }
}

/// A nested DTO representing an association item (role or workplace) within a require document response.
class AssociationItemDto {
  const AssociationItemDto({required this.id, required this.name});

  final String id;
  final String name;

  factory AssociationItemDto.fromJson(Map<String, dynamic> json) {
    return AssociationItemDto(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
    );
  }
}
