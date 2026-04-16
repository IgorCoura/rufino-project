import 'dart:typed_data';

import '../../core/result.dart';
import '../entities/batch_download.dart';

/// Contract for batch document download operations.
///
/// All methods return [Result] — errors are values, never thrown.
abstract class BatchDownloadRepository {
  /// Fetches employees available for document download with optional filters.
  ///
  /// Supports filtering by [name], [statusId], [workplaceId], and [roleId].
  /// Returns a paginated [BatchDownloadEmployeesPage].
  Future<Result<BatchDownloadEmployeesPage>> getEmployeesForDownload(
    String companyId, {
    String? name,
    int? statusId,
    String? workplaceId,
    String? roleId,
    int pageSize,
    int pageNumber,
  });

  /// Fetches document units for the selected employees with optional filters.
  ///
  /// Supports filtering by document group, template, unit status, and period.
  /// Returns a paginated [BatchDownloadUnitsPage].
  Future<Result<BatchDownloadUnitsPage>> getDocumentUnitsForDownload(
    String companyId, {
    required List<String> employeeIds,
    String? documentGroupId,
    String? documentTemplateId,
    int? unitStatusId,
    String? dateFrom,
    String? dateTo,
    int? periodTypeId,
    int? periodYear,
    int? periodMonth,
    int? periodDay,
    int? periodWeek,
    int pageSize,
    int pageNumber,
  });

  /// Downloads the selected document units as a ZIP file.
  ///
  /// Returns the raw ZIP bytes.
  Future<Result<Uint8List>> downloadBatch(
    String companyId,
    List<BatchDownloadItem> items,
  );

  /// Downloads a single document unit file.
  ///
  /// Returns the raw file bytes for the unit identified by [documentUnitId]
  /// within [documentId] for [employeeId].
  Future<Result<Uint8List>> downloadDocumentUnit(
    String companyId,
    String employeeId,
    String documentId,
    String documentUnitId,
  );
}
