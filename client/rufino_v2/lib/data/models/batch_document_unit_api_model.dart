import '../../domain/entities/batch_document_unit.dart';
import 'period_api_model.dart';

/// API response model for a pending document unit in batch context.
///
/// Parses the JSON returned by
/// `GET /api/v1/{company}/batch-document/pending-units/{documentTemplateId}`.
class BatchDocumentUnitApiModel {
  const BatchDocumentUnitApiModel({
    required this.documentUnitId,
    required this.documentId,
    required this.employeeId,
    required this.employeeName,
    required this.employeeStatusId,
    required this.employeeStatusName,
    required this.documentUnitDate,
    required this.statusId,
    required this.statusName,
    this.period,
    required this.isSignable,
    required this.canGenerateDocument,
  });

  final String documentUnitId;
  final String documentId;
  final String employeeId;
  final String employeeName;
  final int employeeStatusId;
  final String employeeStatusName;
  final String documentUnitDate;
  final int statusId;
  final String statusName;
  final PeriodApiModel? period;
  final bool isSignable;
  final bool canGenerateDocument;

  /// Deserializes from the API JSON structure.
  factory BatchDocumentUnitApiModel.fromJson(Map<String, dynamic> json) {
    final empStatus = json['employeeStatus'] as Map<String, dynamic>?;
    final unitStatus =
        json['documentUnitStatus'] as Map<String, dynamic>?;
    final periodJson = json['period'] as Map<String, dynamic>?;

    return BatchDocumentUnitApiModel(
      documentUnitId: json['documentUnitId'] as String? ?? '',
      documentId: json['documentId'] as String? ?? '',
      employeeId: json['employeeId'] as String? ?? '',
      employeeName: json['employeeName'] as String? ?? '',
      employeeStatusId: empStatus?['id'] as int? ?? 0,
      employeeStatusName: empStatus?['name'] as String? ?? '',
      documentUnitDate: json['documentUnitDate'] as String? ?? '',
      statusId: unitStatus?['id'] as int? ?? 0,
      statusName: unitStatus?['name'] as String? ?? '',
      period:
          periodJson != null ? PeriodApiModel.fromJson(periodJson) : null,
      isSignable: json['isSignable'] as bool? ?? false,
      canGenerateDocument: json['canGenerateDocument'] as bool? ?? false,
    );
  }

  /// Converts this DTO to a domain entity.
  ///
  /// Transforms the date from `yyyy-MM-dd` API format to `dd/MM/yyyy`
  /// display format.
  BatchDocumentUnitItem toEntity() {
    return BatchDocumentUnitItem(
      documentUnitId: documentUnitId,
      documentId: documentId,
      employeeId: employeeId,
      employeeName: employeeName,
      employeeStatusId: employeeStatusId.toString(),
      employeeStatusName: employeeStatusName,
      date: _dateToDisplay(documentUnitDate),
      statusId: statusId.toString(),
      statusName: statusName,
      period: period?.toEntity(),
      isSignable: isSignable,
      canGenerateDocument: canGenerateDocument,
    );
  }

  static String _dateToDisplay(String apiDate) {
    if (apiDate.isEmpty) return '';
    final parts = apiDate.split('-');
    if (parts.length != 3) return apiDate;
    return '${parts[2]}/${parts[1]}/${parts[0]}';
  }
}

// PeriodApiModel is now in period_api_model.dart.

/// API response model for an employee missing the selected document.
class EmployeeMissingDocumentApiModel {
  const EmployeeMissingDocumentApiModel({
    required this.employeeId,
    required this.employeeName,
    required this.employeeStatusId,
    required this.employeeStatusName,
  });

  final String employeeId;
  final String employeeName;
  final int employeeStatusId;
  final String employeeStatusName;

  /// Deserializes from the API JSON structure.
  factory EmployeeMissingDocumentApiModel.fromJson(
      Map<String, dynamic> json) {
    final empStatus = json['employeeStatus'] as Map<String, dynamic>?;
    return EmployeeMissingDocumentApiModel(
      employeeId: json['employeeId'] as String? ?? '',
      employeeName: json['employeeName'] as String? ?? '',
      employeeStatusId: empStatus?['id'] as int? ?? 0,
      employeeStatusName: empStatus?['name'] as String? ?? '',
    );
  }

  /// Converts this DTO to a domain [EmployeeMissingDocument] entity.
  EmployeeMissingDocument toEntity() {
    return EmployeeMissingDocument(
      employeeId: employeeId,
      employeeName: employeeName,
      employeeStatusId: employeeStatusId.toString(),
      employeeStatusName: employeeStatusName,
    );
  }
}

/// Paginated API response for batch document units.
class BatchDocumentUnitsResponse {
  const BatchDocumentUnitsResponse({
    required this.items,
    required this.totalCount,
  });

  final List<BatchDocumentUnitApiModel> items;
  final int totalCount;

  /// Deserializes from the API JSON structure.
  factory BatchDocumentUnitsResponse.fromJson(Map<String, dynamic> json) {
    final list = json['items'] as List<dynamic>? ?? [];
    return BatchDocumentUnitsResponse(
      items: list
          .map((e) => BatchDocumentUnitApiModel.fromJson(
              e as Map<String, dynamic>))
          .toList(),
      totalCount: json['totalCount'] as int? ?? 0,
    );
  }
}

/// API response for a batch create operation.
class BatchCreateResponse {
  const BatchCreateResponse({required this.createdItems});

  final List<BatchCreatedItemApiModel> createdItems;

  /// Deserializes from the API JSON structure.
  factory BatchCreateResponse.fromJson(Map<String, dynamic> json) {
    final list = json['createdItems'] as List<dynamic>? ?? [];
    return BatchCreateResponse(
      createdItems: list
          .map((e) => BatchCreatedItemApiModel.fromJson(
              e as Map<String, dynamic>))
          .toList(),
    );
  }
}

/// API response model for a single item created by batch create.
class BatchCreatedItemApiModel {
  const BatchCreatedItemApiModel({
    required this.employeeId,
    required this.documentId,
    required this.documentUnitId,
  });

  final String employeeId;
  final String documentId;
  final String documentUnitId;

  /// Deserializes from the API JSON structure.
  factory BatchCreatedItemApiModel.fromJson(Map<String, dynamic> json) {
    return BatchCreatedItemApiModel(
      employeeId: json['employeeId'] as String? ?? '',
      documentId: json['documentId'] as String? ?? '',
      documentUnitId: json['documentUnitId'] as String? ?? '',
    );
  }

  /// Converts this DTO to a domain [BatchCreatedItem] entity.
  BatchCreatedItem toEntity() {
    return BatchCreatedItem(
      employeeId: employeeId,
      documentId: documentId,
      documentUnitId: documentUnitId,
    );
  }
}

/// API response for a batch upload (insert-range) operation.
class InsertDocumentRangeResponse {
  const InsertDocumentRangeResponse({required this.results});

  final List<InsertDocumentRangeResultItemApiModel> results;

  /// Deserializes from the API JSON structure.
  factory InsertDocumentRangeResponse.fromJson(Map<String, dynamic> json) {
    final list = json['results'] as List<dynamic>? ?? [];
    return InsertDocumentRangeResponse(
      results: list
          .map((e) => InsertDocumentRangeResultItemApiModel.fromJson(
              e as Map<String, dynamic>))
          .toList(),
    );
  }
}

/// API response model for a single upload result within a batch.
class InsertDocumentRangeResultItemApiModel {
  const InsertDocumentRangeResultItemApiModel({
    required this.documentUnitId,
    required this.success,
    this.errorMessage,
  });

  final String documentUnitId;
  final bool success;
  final String? errorMessage;

  /// Deserializes from the API JSON structure.
  factory InsertDocumentRangeResultItemApiModel.fromJson(
      Map<String, dynamic> json) {
    return InsertDocumentRangeResultItemApiModel(
      documentUnitId: json['documentUnitId'] as String? ?? '',
      success: json['success'] as bool? ?? false,
      errorMessage: json['errorMessage'] as String?,
    );
  }

  /// Converts this DTO to a domain [BatchUploadResult] entity.
  BatchUploadResult toEntity() {
    return BatchUploadResult(
      documentUnitId: documentUnitId,
      success: success,
      errorMessage: errorMessage,
    );
  }
}
