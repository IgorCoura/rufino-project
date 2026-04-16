import 'dart:typed_data';

import '../../core/errors/batch_download_exception.dart';
import '../../core/result.dart';
import '../../domain/entities/batch_download.dart';
import '../../domain/repositories/batch_download_repository.dart';
import '../services/batch_download_api_service.dart';

/// Concrete implementation of [BatchDownloadRepository] backed by
/// [BatchDownloadApiService].
///
/// All service calls are wrapped in try/catch. [BatchDownloadException]
/// subtypes are propagated as-is; all other errors are wrapped in
/// [BatchDownloadNetworkException].
class BatchDownloadRepositoryImpl implements BatchDownloadRepository {
  const BatchDownloadRepositoryImpl({required this.apiService});

  final BatchDownloadApiService apiService;

  @override
  Future<Result<BatchDownloadEmployeesPage>> getEmployeesForDownload(
    String companyId, {
    String? name,
    int? statusId,
    String? workplaceId,
    String? roleId,
    int pageSize = 50,
    int pageNumber = 1,
  }) async {
    try {
      final response = await apiService.getEmployeesForDownload(
        companyId,
        name: name,
        statusId: statusId,
        workplaceId: workplaceId,
        roleId: roleId,
        pageSize: pageSize,
        pageNumber: pageNumber,
      );
      return Result.success(BatchDownloadEmployeesPage(
        items: response.items.map((m) => m.toEntity()).toList(),
        totalCount: response.totalCount,
      ));
    } on BatchDownloadException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(BatchDownloadNetworkException(e));
    }
  }

  @override
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
    int pageSize = 50,
    int pageNumber = 1,
  }) async {
    try {
      final response = await apiService.getDocumentUnitsForDownload(
        companyId,
        employeeIds: employeeIds,
        documentGroupId: documentGroupId,
        documentTemplateId: documentTemplateId,
        unitStatusId: unitStatusId,
        dateFrom: dateFrom,
        dateTo: dateTo,
        periodTypeId: periodTypeId,
        periodYear: periodYear,
        periodMonth: periodMonth,
        periodDay: periodDay,
        periodWeek: periodWeek,
        pageSize: pageSize,
        pageNumber: pageNumber,
      );
      return Result.success(BatchDownloadUnitsPage(
        items: response.items.map((m) => m.toEntity()).toList(),
        totalCount: response.totalCount,
      ));
    } on BatchDownloadException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(BatchDownloadNetworkException(e));
    }
  }

  @override
  Future<Result<Uint8List>> downloadBatch(
    String companyId,
    List<BatchDownloadItem> items,
  ) async {
    try {
      final bytes = await apiService.downloadBatch(
        companyId,
        items.map((i) => i.toJson()).toList(),
      );
      return Result.success(bytes);
    } on BatchDownloadException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(BatchDownloadNetworkException(e));
    }
  }

  @override
  Future<Result<Uint8List>> downloadDocumentUnit(
    String companyId,
    String employeeId,
    String documentId,
    String documentUnitId,
  ) async {
    try {
      final bytes = await apiService.downloadDocumentUnit(
        companyId,
        employeeId,
        documentId,
        documentUnitId,
      );
      return Result.success(bytes);
    } on BatchDownloadException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(BatchDownloadNetworkException(e));
    }
  }
}
