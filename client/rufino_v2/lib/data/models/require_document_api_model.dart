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
    this.associationId = '',
    this.associationName = '',
    this.associationTypeId = 0,
    this.associationTypeName = '',
    this.documentTemplates = const [],
    this.listenEvents = const [],
  });

  final String id;
  final String name;
  final String description;
  final String companyId;
  final String associationId;
  final String associationName;
  final int associationTypeId;
  final String associationTypeName;
  final List<_DocumentTemplateDto> documentTemplates;
  final List<_ListenEventDto> listenEvents;

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
    final association = json['association'] as Map<String, dynamic>?;

    return RequireDocumentApiModel(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      description: json['description'] as String? ?? '',
      companyId: json['companyId'] as String? ?? '',
      associationId: association?['id'] as String? ?? '',
      associationName: association?['name'] as String? ?? '',
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
      associationId: associationId,
      associationName: associationName,
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
        'associationId': associationId,
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
        'associationId': associationId,
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

  static List<_DocumentTemplateDto> _parseTemplates(dynamic raw) {
    if (raw == null) return [];
    final list = raw as List<dynamic>;
    return list
        .map((e) => _DocumentTemplateDto.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  static List<_ListenEventDto> _parseListenEvents(dynamic raw) {
    if (raw == null) return [];
    final list = raw as List<dynamic>;
    return list
        .map((e) => _ListenEventDto.fromJson(e as Map<String, dynamic>))
        .toList();
  }
}

class _DocumentTemplateDto {
  const _DocumentTemplateDto({
    required this.id,
    required this.name,
    this.description = '',
  });

  final String id;
  final String name;
  final String description;

  factory _DocumentTemplateDto.fromJson(Map<String, dynamic> json) {
    return _DocumentTemplateDto(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      description: json['description'] as String? ?? '',
    );
  }
}

class _ListenEventDto {
  const _ListenEventDto({
    required this.eventId,
    required this.eventName,
    this.statusIds = const [],
  });

  final int eventId;
  final String eventName;
  final List<int> statusIds;

  factory _ListenEventDto.fromJson(Map<String, dynamic> json) {
    final event = json['event'] as Map<String, dynamic>?;
    final statusRaw = json['status'] as List<dynamic>? ?? [];
    return _ListenEventDto(
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
