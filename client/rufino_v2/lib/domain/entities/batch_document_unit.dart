import 'dart:typed_data';

import 'period.dart';
export 'period.dart';

/// A pending [DocumentUnit] in the context of batch document management.
///
/// Each item represents a single document unit across any employee for a
/// given document template, enriched with the owning employee's name and
/// status for display in the batch management screen.
class BatchDocumentUnitItem {
  const BatchDocumentUnitItem({
    required this.documentUnitId,
    required this.documentId,
    required this.employeeId,
    required this.employeeName,
    required this.employeeStatusId,
    required this.employeeStatusName,
    required this.date,
    required this.statusId,
    required this.statusName,
    this.period,
    required this.isSignable,
    required this.canGenerateDocument,
  });

  /// The unique identifier of the document unit.
  final String documentUnitId;

  /// The identifier of the parent document aggregate.
  final String documentId;

  /// The identifier of the employee who owns this document.
  final String employeeId;

  /// The display name of the owning employee.
  final String employeeName;

  /// The employee status identifier (1–5).
  final String employeeStatusId;

  /// The employee status display name.
  final String employeeStatusName;

  /// The document unit date in `dd/MM/yyyy` display format.
  final String date;

  /// The document unit status identifier (1–8).
  final String statusId;

  /// The document unit status display name.
  final String statusName;

  /// The competency period, if the document has one.
  final Period? period;

  /// Whether the document supports digital signature.
  final bool isSignable;

  /// Whether the document can be auto-generated from a template.
  final bool canGenerateDocument;

  /// Whether the document unit is in pending status.
  bool get isPending => statusId == '1';

  /// Human-readable employee status label in Portuguese.
  String get employeeStatusLabel => switch (employeeStatusId) {
        '1' => 'Pendente',
        '2' => 'Ativo',
        '3' => 'Férias',
        '4' => 'Afastado',
        '5' => 'Inativo',
        _ => employeeStatusName,
      };

  /// Whether the document unit date is a valid `dd/MM/yyyy` date.
  bool get hasValidDate {
    if (date.isEmpty) return false;
    final parts = date.split('/');
    if (parts.length != 3) return false;
    final day = int.tryParse(parts[0]);
    final month = int.tryParse(parts[1]);
    final year = int.tryParse(parts[2]);
    if (day == null || month == null || year == null) return false;
    if (year < 1900 || month < 1 || month > 12 || day < 1 || day > 31) {
      return false;
    }
    return true;
  }

  /// Human-readable status label.
  String get statusLabel => switch (statusId) {
        '1' => 'Pendente',
        '2' => 'OK',
        '3' => 'Obsoleto',
        '4' => 'Inválido',
        '5' => 'Requer Validação',
        '6' => 'Não Aplicável',
        '7' => 'Aguardando Assinatura',
        _ => statusName.isNotEmpty ? statusName : statusId,
      };
}

/// An employee who does not have a pending document unit for the selected template.
class EmployeeMissingDocument {
  const EmployeeMissingDocument({
    required this.employeeId,
    required this.employeeName,
    required this.employeeStatusId,
    required this.employeeStatusName,
  });

  /// The employee identifier.
  final String employeeId;

  /// The employee display name.
  final String employeeName;

  /// The employee status identifier.
  final String employeeStatusId;

  /// The employee status display name.
  final String employeeStatusName;
}

/// A file staged for batch upload, associated with a specific document unit.
class BatchUploadItem {
  const BatchUploadItem({
    required this.documentUnitId,
    required this.documentId,
    required this.employeeId,
    required this.fileBytes,
    required this.fileName,
  });

  /// The document unit this file will be uploaded to.
  final String documentUnitId;

  /// The parent document aggregate identifier.
  final String documentId;

  /// The employee who owns this document.
  final String employeeId;

  /// The raw file bytes.
  final Uint8List fileBytes;

  /// The original file name including extension.
  final String fileName;
}

/// The result of a single document unit upload within a batch.
class BatchUploadResult {
  const BatchUploadResult({
    required this.documentUnitId,
    required this.success,
    this.errorMessage,
  });

  /// The document unit identifier.
  final String documentUnitId;

  /// Whether the upload succeeded.
  final bool success;

  /// Error message when [success] is false.
  final String? errorMessage;
}

/// An item created by the batch create operation.
class BatchCreatedItem {
  const BatchCreatedItem({
    required this.employeeId,
    required this.documentId,
    required this.documentUnitId,
  });

  /// The employee for whom the document unit was created.
  final String employeeId;

  /// The parent document aggregate identifier.
  final String documentId;

  /// The newly created document unit identifier.
  final String documentUnitId;
}

/// Paginated result of batch document units.
class BatchDocumentUnitsPage {
  const BatchDocumentUnitsPage({
    required this.items,
    required this.totalCount,
  });

  /// The document units on the current page.
  final List<BatchDocumentUnitItem> items;

  /// The total number of matching document units across all pages.
  final int totalCount;
}
