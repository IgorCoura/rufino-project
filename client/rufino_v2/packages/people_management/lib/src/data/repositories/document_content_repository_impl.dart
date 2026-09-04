import 'package:rufino_core/rufino_core.dart';
import '../../domain/errors/document_content_exception.dart';
import '../../domain/entities/document_content_status.dart';
import '../../domain/repositories/document_content_repository.dart';
import '../models/document_content_status_api_model.dart';
import '../services/document_content_api_service.dart';

/// Concrete implementation of [DocumentContentRepository] backed by
/// [DocumentContentApiService].
///
/// All service calls are wrapped in try/catch. [DocumentContentException]
/// subtypes are propagated as-is; all other errors are wrapped in
/// [DocumentContentNetworkException].
class DocumentContentRepositoryImpl implements DocumentContentRepository {
  /// Creates a [DocumentContentRepositoryImpl].
  DocumentContentRepositoryImpl({
    required this.apiService,
    required this.reporter,
  });

  final DocumentContentApiService apiService;
  final ErrorReporter reporter;

  @override
  Future<Result<List<DocumentContentStatus>>> checkOutdated(
    String companyId,
    List<DocumentUnitRefApiModel> items,
  ) async {
    try {
      final response = await apiService.checkOutdated(companyId, items);
      return Result.success(
        response.items.map((m) => m.toEntity()).toList(),
      );
    } on DocumentContentException catch (e, st) {
      return reporter.failure(e, st);
    } catch (e, st) {
      return reporter.failure(
        DocumentContentNetworkException(e),
        st,
        context: {'op': 'checkOutdatedContent', 'count': items.length},
      );
    }
  }

  @override
  Future<Result<void>> refresh(
    String companyId,
    List<DocumentUnitRefApiModel> items,
  ) async {
    try {
      await apiService.refresh(companyId, items);
      return const Result.success(null);
    } on DocumentContentException catch (e, st) {
      return reporter.failure(e, st);
    } catch (e, st) {
      return reporter.failure(
        DocumentContentNetworkException(e),
        st,
        context: {'op': 'refreshDocumentContent', 'count': items.length},
      );
    }
  }
}
