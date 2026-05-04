import 'dart:typed_data';

import '../../core/errors/batch_document_exception.dart';
import '../../core/monitoring/error_reporter.dart';
import '../../core/result.dart';
import '../../domain/entities/batch_document_unit.dart';
import '../../domain/repositories/batch_document_repository.dart';
import '../services/batch_document_api_service.dart';

/// Concrete implementation of [BatchDocumentRepository] backed by
/// [BatchDocumentApiService].
///
/// All service calls are wrapped in try/catch. [BatchDocumentException]
/// subtypes are propagated as-is; all other errors are wrapped in
/// [BatchDocumentNetworkException].
class BatchDocumentRepositoryImpl implements BatchDocumentRepository {
  BatchDocumentRepositoryImpl({
    required this.apiService,
    required this.reporter,
  });

  final BatchDocumentApiService apiService;
  final ErrorReporter reporter;

  @override
  Future<Result<BatchDocumentUnitsPage>> getPendingDocumentUnits(
    String companyId,
    String documentTemplateId, {
    int? employeeStatusId,
    String? employeeName,
    int? periodTypeId,
    int? periodYear,
    int? periodMonth,
    int? periodDay,
    int? periodWeek,
    int pageSize = 50,
    int pageNumber = 1,
  }) async {
    try {
      final response = await apiService.getPendingDocumentUnits(
        companyId,
        documentTemplateId,
        employeeStatusId: employeeStatusId,
        employeeName: employeeName,
        periodTypeId: periodTypeId,
        periodYear: periodYear,
        periodMonth: periodMonth,
        periodDay: periodDay,
        periodWeek: periodWeek,
        pageSize: pageSize,
        pageNumber: pageNumber,
      );
      return Result.success(BatchDocumentUnitsPage(
        items: response.items.map((m) => m.toEntity()).toList(),
        totalCount: response.totalCount,
      ));
    } on BatchDocumentException catch (e, st) {
      return reporter.failure(e, st);
    } catch (e, st) {
      return reporter.failure(BatchDocumentNetworkException(e), st);
    }
  }

  @override
  Future<Result<List<EmployeeMissingDocument>>> getMissingEmployees(
    String companyId,
    String documentTemplateId, {
    int? employeeStatusId,
    String? employeeName,
  }) async {
    try {
      final models = await apiService.getMissingEmployees(
        companyId,
        documentTemplateId,
        employeeStatusId: employeeStatusId,
        employeeName: employeeName,
      );
      return Result.success(models.map((m) => m.toEntity()).toList());
    } on BatchDocumentException catch (e, st) {
      return reporter.failure(e, st);
    } catch (e, st) {
      return reporter.failure(BatchDocumentNetworkException(e), st);
    }
  }

  @override
  Future<Result<List<BatchCreatedItem>>> batchCreateDocumentUnits(
    String companyId,
    String documentTemplateId,
    List<String> employeeIds,
  ) async {
    try {
      final response = await apiService.batchCreateDocumentUnits(
          companyId, documentTemplateId, employeeIds);
      return Result.success(
          response.createdItems.map((m) => m.toEntity()).toList());
    } on BatchDocumentException catch (e, st) {
      return reporter.failure(e, st);
    } catch (e, st) {
      return reporter.failure(BatchDocumentNetworkException(e), st);
    }
  }

  @override
  Future<Result<int>> batchUpdateDate(
    String companyId,
    List<BatchDocumentUnitItem> items,
    String date,
  ) async {
    try {
      final itemMaps = items
          .map((i) => {
                'documentUnitId': i.documentUnitId,
                'documentId': i.documentId,
                'employeeId': i.employeeId,
              })
          .toList();
      final count =
          await apiService.batchUpdateDate(companyId, itemMaps, date);
      return Result.success(count);
    } on BatchDocumentException catch (e, st) {
      return reporter.failure(e, st);
    } catch (e, st) {
      return reporter.failure(BatchDocumentNetworkException(e), st);
    }
  }

  @override
  Future<Result<List<BatchUploadResult>>> uploadDocumentRange(
    String companyId,
    List<BatchUploadItem> items,
  ) async {
    try {
      final response =
          await apiService.uploadDocumentRange(companyId, items);
      return Result.success(
          response.results.map((m) => m.toEntity()).toList());
    } on BatchDocumentException catch (e, st) {
      return reporter.failure(e, st);
    } catch (e, st) {
      return reporter.failure(BatchDocumentNetworkException(e), st);
    }
  }

  @override
  Future<Result<List<BatchUploadResult>>> uploadDocumentRangeToSign(
    String companyId,
    List<BatchUploadItem> items,
    String dateLimitToSign,
    int reminderEveryNDays,
  ) async {
    try {
      final response = await apiService.uploadDocumentRangeToSign(
          companyId, items, dateLimitToSign, reminderEveryNDays);
      return Result.success(
          response.results.map((m) => m.toEntity()).toList());
    } on BatchDocumentException catch (e, st) {
      return reporter.failure(e, st);
    } catch (e, st) {
      return reporter.failure(BatchDocumentNetworkException(e), st);
    }
  }

  @override
  Future<Result<Uint8List>> generatePdfRange(
    String companyId,
    List<BatchDocumentUnitItem> items,
  ) async {
    try {
      final itemMaps = items
          .map((i) => {
                'documentUnitId': i.documentUnitId,
                'documentId': i.documentId,
                'employeeId': i.employeeId,
              })
          .toList();
      final bytes = await apiService.generatePdfRange(companyId, itemMaps);
      return Result.success(bytes);
    } on BatchDocumentException catch (e, st) {
      return reporter.failure(e, st);
    } catch (e, st) {
      return reporter.failure(BatchDocumentNetworkException(e), st);
    }
  }

  @override
  Future<Result<void>> generateAndSignRange(
    String companyId,
    List<BatchDocumentUnitItem> items,
    String dateLimitToSign,
    int reminderEveryNDays,
  ) async {
    try {
      final itemMaps = items
          .map((i) => {
                'documentUnitId': i.documentUnitId,
                'documentId': i.documentId,
                'employeeId': i.employeeId,
              })
          .toList();
      await apiService.generateAndSignRange(
          companyId, itemMaps, dateLimitToSign, reminderEveryNDays);
      return const Result.success(null);
    } on BatchDocumentException catch (e, st) {
      return reporter.failure(e, st);
    } catch (e, st) {
      return reporter.failure(BatchDocumentNetworkException(e), st);
    }
  }
}
