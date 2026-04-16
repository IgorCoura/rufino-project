import 'dart:typed_data';

import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/domain/entities/batch_download.dart';
import 'package:rufino_v2/domain/repositories/batch_download_repository.dart';

/// In-memory fake implementation of [BatchDownloadRepository] for testing.
///
/// Exposes setters to configure the data returned by each method, and
/// optional error injection to test failure paths.
class FakeBatchDownloadRepository implements BatchDownloadRepository {
  /// The employee page to return from [getEmployeesForDownload].
  BatchDownloadEmployeesPage employeesPage = const BatchDownloadEmployeesPage(
    items: [],
    totalCount: 0,
  );

  /// The unit page to return from [getDocumentUnitsForDownload].
  BatchDownloadUnitsPage unitsPage = const BatchDownloadUnitsPage(
    items: [],
    totalCount: 0,
  );

  /// The ZIP bytes to return from [downloadBatch].
  Uint8List downloadBytes = Uint8List(0);

  /// Bytes to return from [downloadDocumentUnit], keyed by `documentId:documentUnitId`.
  Map<String, Uint8List> documentUnitBytes = {};

  /// Tracks the keys of units downloaded via [downloadDocumentUnit].
  List<String> downloadedUnitKeys = [];

  /// When non-null, all methods return [Result.error] with this value.
  Object? errorToThrow;

  /// Tracks the last arguments passed to [getEmployeesForDownload].
  Map<String, dynamic>? lastEmployeeFilters;

  /// Tracks the last arguments passed to [getDocumentUnitsForDownload].
  Map<String, dynamic>? lastUnitFilters;

  /// Tracks the last items passed to [downloadBatch].
  List<BatchDownloadItem>? lastDownloadItems;

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
    lastEmployeeFilters = {
      'companyId': companyId,
      'name': name,
      'statusId': statusId,
      'workplaceId': workplaceId,
      'roleId': roleId,
      'pageSize': pageSize,
      'pageNumber': pageNumber,
    };
    if (errorToThrow != null) return Result.error(errorToThrow!);
    return Result.success(employeesPage);
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
    lastUnitFilters = {
      'companyId': companyId,
      'employeeIds': employeeIds,
      'documentGroupId': documentGroupId,
      'documentTemplateId': documentTemplateId,
      'unitStatusId': unitStatusId,
      'dateFrom': dateFrom,
      'dateTo': dateTo,
      'periodTypeId': periodTypeId,
      'periodYear': periodYear,
      'periodMonth': periodMonth,
      'pageSize': pageSize,
      'pageNumber': pageNumber,
    };
    if (errorToThrow != null) return Result.error(errorToThrow!);
    return Result.success(unitsPage);
  }

  @override
  Future<Result<Uint8List>> downloadBatch(
    String companyId,
    List<BatchDownloadItem> items,
  ) async {
    lastDownloadItems = items;
    if (errorToThrow != null) return Result.error(errorToThrow!);
    return Result.success(downloadBytes);
  }

  @override
  Future<Result<Uint8List>> downloadDocumentUnit(
    String companyId,
    String employeeId,
    String documentId,
    String documentUnitId,
  ) async {
    final key = '$documentId:$documentUnitId';
    downloadedUnitKeys.add(key);
    if (errorToThrow != null) return Result.error(errorToThrow!);
    final bytes = documentUnitBytes[key] ?? Uint8List(0);
    return Result.success(bytes);
  }
}
