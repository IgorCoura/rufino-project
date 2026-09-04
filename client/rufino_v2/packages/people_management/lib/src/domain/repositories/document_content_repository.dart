import 'package:rufino_core/rufino_core.dart';
import '../../data/models/document_content_status_api_model.dart';
import '../entities/document_content_status.dart';

/// Contract for checking and refreshing the data snapshot stored in
/// document units.
abstract class DocumentContentRepository {
  /// Returns the snapshot status of each unit in [items].
  ///
  /// A unit that has no snapshot yet, or whose template produces no document,
  /// comes back as up to date — there is nothing to warn about.
  Future<Result<List<DocumentContentStatus>>> checkOutdated(
    String companyId,
    List<DocumentUnitRefApiModel> items,
  );

  /// Rewrites the snapshot of each unit in [items] with the current data,
  /// keeping each unit's own date.
  Future<Result<void>> refresh(
    String companyId,
    List<DocumentUnitRefApiModel> items,
  );
}
