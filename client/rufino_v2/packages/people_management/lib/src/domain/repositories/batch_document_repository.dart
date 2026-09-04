import 'dart:typed_data';

import 'package:rufino_core/rufino_core.dart';
import '../entities/batch_document_unit.dart';

/// Contract for batch document management operations.
///
/// All methods return [Result] — errors are values, never thrown.
abstract class BatchDocumentRepository {
  /// Fetches the company's pending document units.
  ///
  /// [documentGroupId], [documentTemplateId] and [employeeId] are independent
  /// scope filters — none is required and any combination is valid. Also
  /// supports filtering by employee status, name and exact period selection.
  Future<Result<BatchDocumentUnitsPage>> getPendingDocumentUnits(
    String companyId, {
    String? documentGroupId,
    String? documentTemplateId,
    String? employeeId,
    int? employeeStatusId,
    String? employeeName,
    int? periodTypeId,
    int? periodYear,
    int? periodMonth,
    int? periodDay,
    int? periodWeek,
    int pageSize,
    int pageNumber,
  });

  /// Fetches the employee x template pairs without a pending document unit.
  ///
  /// Requires [documentTemplateId] or [documentGroupId]; without either the
  /// result is empty.
  Future<Result<List<EmployeeMissingDocument>>> getMissingEmployees(
    String companyId, {
    String? documentGroupId,
    String? documentTemplateId,
    String? employeeId,
    int? employeeStatusId,
    String? employeeName,
  });

  /// Creates document units in batch for the given employees and template.
  Future<Result<List<BatchCreatedItem>>> batchCreateDocumentUnits(
    String companyId,
    String documentTemplateId,
    List<String> employeeIds,
  );

  /// Updates the date of multiple document units at once.
  ///
  /// Returns the count of updated items.
  Future<Result<int>> batchUpdateDate(
    String companyId,
    List<BatchDocumentUnitItem> items,
    String date,
  );

  /// Uploads multiple files in a single multipart request.
  Future<Result<List<BatchUploadResult>>> uploadDocumentRange(
    String companyId,
    List<BatchUploadItem> items,
  );

  /// Uploads multiple files and sends them for digital signature.
  ///
  /// Uses a global [dateLimitToSign] and [reminderEveryNDays] for all items.
  Future<Result<List<BatchUploadResult>>> uploadDocumentRangeToSign(
    String companyId,
    List<BatchUploadItem> items,
    String dateLimitToSign,
    int reminderEveryNDays,
  );

  /// Generates PDFs for the selected document units and returns ZIP bytes.
  Future<Result<Uint8List>> generatePdfRange(
    String companyId,
    List<BatchDocumentUnitItem> items,
  );

  /// Generates PDFs and sends them for digital signature.
  Future<Result<void>> generateAndSignRange(
    String companyId,
    List<BatchDocumentUnitItem> items,
    String dateLimitToSign,
    int reminderEveryNDays,
  );
}
