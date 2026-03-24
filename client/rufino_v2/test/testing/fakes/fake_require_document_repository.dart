import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/domain/entities/require_document.dart';
import 'package:rufino_v2/domain/entities/selection_option.dart';
import 'package:rufino_v2/domain/repositories/require_document_repository.dart';

/// In-memory fake implementation of [RequireDocumentRepository] for tests.
///
/// All responses are configurable via setters before each test.
class FakeRequireDocumentRepository implements RequireDocumentRepository {
  List<RequireDocument> _requireDocuments = [];
  RequireDocument? _requireDocument;
  bool _shouldFail = false;

  /// Configures the list returned by [getRequireDocuments].
  void setRequireDocuments(List<RequireDocument> docs) =>
      _requireDocuments = docs;

  /// Configures the single item returned by [getRequireDocumentById].
  void setRequireDocument(RequireDocument? doc) => _requireDocument = doc;

  /// When true, all methods return [Result.error].
  void setShouldFail(bool value) => _shouldFail = value;

  /// The name passed to the last [createRequireDocument] call.
  String? lastCreatedName;

  /// The id passed to the last [updateRequireDocument] call.
  String? lastUpdatedId;

  /// The last association type id passed to [getAssociations].
  String? lastAssociationTypeId;

  @override
  Future<Result<List<RequireDocument>>> getRequireDocuments(
      String companyId) async {
    if (_shouldFail) {
      return Result.error(Exception('getRequireDocuments failed'));
    }
    return Result.success(_requireDocuments);
  }

  @override
  Future<Result<RequireDocument>> getRequireDocumentById(
      String companyId, String requireDocumentId) async {
    if (_shouldFail) {
      return Result.error(Exception('getRequireDocumentById failed'));
    }
    if (_requireDocument == null) {
      return Result.error(Exception('RequireDocument not found'));
    }
    return Result.success(_requireDocument!);
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
    if (_shouldFail) {
      return Result.error(Exception('createRequireDocument failed'));
    }
    lastCreatedName = name;
    return const Result.success('new-require-doc-id');
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
    if (_shouldFail) {
      return Result.error(Exception('updateRequireDocument failed'));
    }
    lastUpdatedId = id;
    return Result.success(id);
  }

  @override
  Future<Result<List<SelectionOption>>> getAssociationTypes(
      String companyId) async {
    if (_shouldFail) {
      return Result.error(Exception('getAssociationTypes failed'));
    }
    return const Result.success([
      SelectionOption(id: '1', name: 'Função'),
      SelectionOption(id: '2', name: 'Local de Trabalho'),
    ]);
  }

  @override
  Future<Result<List<SelectionOption>>> getAssociations(
      String companyId, String associationTypeId) async {
    lastAssociationTypeId = associationTypeId;
    if (_shouldFail) {
      return Result.error(Exception('getAssociations failed'));
    }
    return const Result.success([
      SelectionOption(id: 'assoc-1', name: 'Desenvolvedor'),
      SelectionOption(id: 'assoc-2', name: 'Analista'),
    ]);
  }

  @override
  Future<Result<List<SelectionOption>>> getEvents(String companyId) async {
    if (_shouldFail) {
      return Result.error(Exception('getEvents failed'));
    }
    return const Result.success([
      SelectionOption(id: '1', name: 'Evento Criado'),
      SelectionOption(id: '2', name: 'Evento de Alteração de Nome'),
    ]);
  }

  @override
  Future<Result<List<SelectionOption>>> getStatuses(String companyId) async {
    if (_shouldFail) {
      return Result.error(Exception('getStatuses failed'));
    }
    return const Result.success([
      SelectionOption(id: '1', name: 'Pendente'),
      SelectionOption(id: '2', name: 'Ativo'),
      SelectionOption(id: '3', name: 'Férias'),
    ]);
  }

  @override
  Future<Result<List<SelectionOption>>> getDocumentTemplates(
      String companyId) async {
    if (_shouldFail) {
      return Result.error(Exception('getDocumentTemplates failed'));
    }
    return const Result.success([
      SelectionOption(id: 'tpl-1', name: 'Contrato CLT'),
      SelectionOption(id: 'tpl-2', name: 'Termo de Confidencialidade'),
    ]);
  }
}
