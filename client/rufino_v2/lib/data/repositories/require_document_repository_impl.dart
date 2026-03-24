import '../../core/errors/require_document_exception.dart';
import '../../core/result.dart';
import '../../domain/entities/require_document.dart';
import '../../domain/entities/selection_option.dart';
import '../../domain/repositories/require_document_repository.dart';
import '../services/require_document_api_service.dart';

/// Concrete implementation of [RequireDocumentRepository] backed by
/// [RequireDocumentApiService].
///
/// All service calls are wrapped in try/catch. [RequireDocumentException]
/// subtypes are propagated as-is; all other errors are wrapped in
/// [RequireDocumentNetworkException].
class RequireDocumentRepositoryImpl implements RequireDocumentRepository {
  const RequireDocumentRepositoryImpl({required this.apiService});

  final RequireDocumentApiService apiService;

  @override
  Future<Result<List<RequireDocument>>> getRequireDocuments(
      String companyId) async {
    try {
      final models = await apiService.getRequireDocuments(companyId);
      return Result.success(models.map((m) => m.toEntity()).toList());
    } on RequireDocumentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(RequireDocumentNetworkException(e));
    }
  }

  @override
  Future<Result<RequireDocument>> getRequireDocumentById(
      String companyId, String requireDocumentId) async {
    try {
      final model = await apiService.getRequireDocumentById(
          companyId, requireDocumentId);
      return Result.success(model.toEntity());
    } on RequireDocumentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(RequireDocumentNetworkException(e));
    }
  }

  @override
  Future<Result<String>> createRequireDocument(
    String companyId, {
    required String name,
    required String description,
    required String associationId,
    required int associationTypeId,
    required List<String> documentTemplateIds,
    required List<ListenEventPayload> listenEvents,
  }) async {
    try {
      final body = {
        'name': name,
        'description': description,
        'associationId': associationId,
        'associationType': associationTypeId,
        'documentsTemplatesIds': documentTemplateIds,
        'listenEvents': listenEvents.map((e) => e.toJson()).toList(),
      };
      final id = await apiService.createRequireDocument(companyId, body);
      return Result.success(id);
    } on RequireDocumentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(RequireDocumentNetworkException(e));
    }
  }

  @override
  Future<Result<String>> updateRequireDocument(
    String companyId, {
    required String id,
    required String name,
    required String description,
    required String associationId,
    required int associationTypeId,
    required List<String> documentTemplateIds,
    required List<ListenEventPayload> listenEvents,
  }) async {
    try {
      final body = {
        'id': id,
        'name': name,
        'description': description,
        'associationId': associationId,
        'associationType': associationTypeId,
        'documentsTemplatesIds': documentTemplateIds,
        'listenEvents': listenEvents.map((e) => e.toJson()).toList(),
      };
      final returnedId =
          await apiService.updateRequireDocument(companyId, body);
      return Result.success(returnedId);
    } on RequireDocumentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(RequireDocumentNetworkException(e));
    }
  }

  @override
  Future<Result<List<SelectionOption>>> getAssociationTypes(
      String companyId) async {
    try {
      final list = await apiService.getAssociationTypes(companyId);
      return Result.success(list
          .map((j) => SelectionOption(
                id: (j['id']).toString(),
                name: _associationTypeLabel((j['id']).toString(), j['name'] as String? ?? ''),
              ))
          .toList());
    } on RequireDocumentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(RequireDocumentNetworkException(e));
    }
  }

  @override
  Future<Result<List<SelectionOption>>> getAssociations(
      String companyId, String associationTypeId) async {
    try {
      final list =
          await apiService.getAssociations(companyId, associationTypeId);
      return Result.success(list
          .map((j) => SelectionOption(
                id: j['id'] as String? ?? '',
                name: j['name'] as String? ?? '',
              ))
          .toList());
    } on RequireDocumentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(RequireDocumentNetworkException(e));
    }
  }

  /// Combines employee events and require document events into a single list.
  ///
  /// Event names are translated from English API keys to Brazilian Portuguese.
  @override
  Future<Result<List<SelectionOption>>> getEvents(String companyId) async {
    try {
      final employeeEvents =
          await apiService.getEmployeeEvents(companyId);
      final requireDocEvents =
          await apiService.getRequireDocumentEvents(companyId);

      final combined = <SelectionOption>[
        ...employeeEvents.map((j) => SelectionOption(
              id: (j['id']).toString(),
              name: _translateEventName(j['name'] as String? ?? ''),
            )),
        ...requireDocEvents.map((j) => SelectionOption(
              id: (j['id']).toString(),
              name: _translateEventName(j['name'] as String? ?? ''),
            )),
      ];

      return Result.success(combined);
    } on RequireDocumentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(RequireDocumentNetworkException(e));
    }
  }

  @override
  Future<Result<List<SelectionOption>>> getStatuses(String companyId) async {
    try {
      final list = await apiService.getStatuses(companyId);
      return Result.success(list
          .map((j) => SelectionOption(
                id: (j['id']).toString(),
                name: _translateStatusName(j['name'] as String? ?? ''),
              ))
          .toList());
    } on RequireDocumentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(RequireDocumentNetworkException(e));
    }
  }

  @override
  Future<Result<List<SelectionOption>>> getDocumentTemplates(
      String companyId) async {
    try {
      final list = await apiService.getDocumentTemplates(companyId);
      return Result.success(list
          .map((j) => SelectionOption(
                id: j['id'] as String? ?? '',
                name: j['name'] as String? ?? '',
              ))
          .toList());
    } on RequireDocumentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(RequireDocumentNetworkException(e));
    }
  }

  /// Translates status names from English API keys to Brazilian Portuguese.
  static String _translateStatusName(String name) {
    const map = {
      'pending': 'Pendente',
      'active': 'Ativo',
      'vacation': 'Férias',
      'away': 'Afastado',
      'inactive': 'Inativo',
    };
    return map[name.toLowerCase()] ?? name;
  }

  /// Translates association type ids to Portuguese labels.
  static String _associationTypeLabel(String id, String fallback) =>
      switch (id) {
        '1' => 'Função',
        '2' => 'Local de Trabalho',
        _ => fallback,
      };

  /// Translates event names from English API keys to Brazilian Portuguese.
  static String _translateEventName(String name) {
    const map = {
      'createdevent': 'Evento Criado',
      'namechangeevent': 'Evento de Alteração de Nome',
      'rolechangeevent': 'Evento de Alteração de Função',
      'workplacechangeevent': 'Evento de Alteração de Local de Trabalho',
      'addresschangeevent': 'Evento de Alteração de Endereço',
      'contactchangeevent': 'Evento de Alteração de Contato',
      'medicaladmissionexamchangeevent':
          'Evento de Alteração de Exame Médico Admissional',
      'personalinfochangeevent':
          'Evento de Alteração de Informações Pessoais',
      'idcardchangeevent':
          'Evento de Alteração de Documento de Identidade',
      'voteidchangeevent': 'Evento de Alteração de Título de Eleitor',
      'militardocumentchangeevent':
          'Evento de Alteração de Documento Militar',
      'completeadmissionevent': 'Evento de Admissão Completa',
      'dependentchildchangeevent':
          'Evento de Alteração de Dependente Filho',
      'dependentspousechangeevent':
          'Evento de Alteração de Dependente Cônjuge',
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
    return map[name.toLowerCase()] ?? name;
  }
}
