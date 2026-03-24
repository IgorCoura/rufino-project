import '../../core/errors/document_group_exception.dart';
import '../../core/result.dart';
import '../../domain/entities/document_group.dart';
import '../../domain/entities/document_group_with_templates.dart';
import '../../domain/repositories/document_group_repository.dart';
import '../models/document_group_api_model.dart';
import '../services/document_group_api_service.dart';

/// Concrete implementation of [DocumentGroupRepository] backed by
/// [DocumentGroupApiService].
///
/// All service calls are wrapped in try/catch. [DocumentGroupException]
/// subtypes are propagated as-is; all other errors are wrapped in
/// [DocumentGroupNetworkException].
class DocumentGroupRepositoryImpl implements DocumentGroupRepository {
  const DocumentGroupRepositoryImpl({required this.apiService});

  final DocumentGroupApiService apiService;

  @override
  Future<Result<List<DocumentGroup>>> getDocumentGroups(
      String companyId) async {
    try {
      final models = await apiService.getDocumentGroups(companyId);
      return Result.success(models.map((m) => m.toEntity()).toList());
    } on DocumentGroupException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DocumentGroupNetworkException(e));
    }
  }

  @override
  Future<Result<List<DocumentGroupWithTemplates>>>
      getDocumentGroupsWithTemplates(String companyId) async {
    try {
      final models =
          await apiService.getDocumentGroupsWithTemplates(companyId);
      return Result.success(models.map((m) => m.toEntity()).toList());
    } on DocumentGroupException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DocumentGroupNetworkException(e));
    }
  }

  @override
  Future<Result<String>> createDocumentGroup(
    String companyId, {
    required String name,
    required String description,
  }) async {
    try {
      final model = DocumentGroupApiModel(
        id: '',
        name: name,
        description: description,
      );
      final id = await apiService.createDocumentGroup(companyId, model);
      return Result.success(id);
    } on DocumentGroupException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DocumentGroupNetworkException(e));
    }
  }

  @override
  Future<Result<String>> updateDocumentGroup(
    String companyId, {
    required String id,
    required String name,
    required String description,
  }) async {
    try {
      final model = DocumentGroupApiModel(
        id: id,
        name: name,
        description: description,
      );
      final returnedId =
          await apiService.updateDocumentGroup(companyId, model);
      return Result.success(returnedId);
    } on DocumentGroupException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DocumentGroupNetworkException(e));
    }
  }
}
