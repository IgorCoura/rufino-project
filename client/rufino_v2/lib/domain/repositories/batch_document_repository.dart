import '../../core/result.dart';
import '../entities/batch_document_unit.dart';

/// Contract for batch document management operations.
///
/// All methods return [Result] — errors are values, never thrown.
abstract class BatchDocumentRepository {
  /// Fetches pending document units across all employees for a given template.
  ///
  /// Supports filtering by employee status, name, and exact period selection.
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
    int pageSize,
    int pageNumber,
  });

  /// Fetches employees who do not have a pending document for the template.
  Future<Result<List<EmployeeMissingDocument>>> getMissingEmployees(
    String companyId,
    String documentTemplateId, {
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
}
