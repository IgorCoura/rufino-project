import '../../domain/entities/batch_download.dart';
import 'period_api_model.dart';

/// API response model for an employee in batch download context.
///
/// Parses the JSON returned by
/// `GET /api/v1/{company}/batch-download/employees`.
class BatchDownloadEmployeeApiModel {
  const BatchDownloadEmployeeApiModel({
    required this.id,
    required this.name,
    required this.statusId,
    required this.statusName,
    required this.roleName,
    required this.workplaceName,
  });

  final String id;
  final String name;
  final int statusId;
  final String statusName;
  final String roleName;
  final String workplaceName;

  /// Deserializes from the API JSON structure.
  factory BatchDownloadEmployeeApiModel.fromJson(Map<String, dynamic> json) {
    final status = json['status'] as Map<String, dynamic>?;
    return BatchDownloadEmployeeApiModel(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      statusId: status?['id'] as int? ?? 0,
      statusName: status?['name'] as String? ?? '',
      roleName: json['roleName'] as String? ?? '',
      workplaceName: json['workplaceName'] as String? ?? '',
    );
  }

  /// Converts this DTO to a domain [BatchDownloadEmployee] entity.
  BatchDownloadEmployee toEntity() {
    return BatchDownloadEmployee(
      id: id,
      name: name,
      statusId: statusId,
      statusName: statusName,
      roleName: roleName,
      workplaceName: workplaceName,
    );
  }
}

/// API response model for a document unit in batch download context.
///
/// Parses the JSON returned by
/// `POST /api/v1/{company}/batch-download/units`.
class BatchDownloadUnitApiModel {
  const BatchDownloadUnitApiModel({
    required this.documentUnitId,
    required this.documentId,
    required this.employeeId,
    required this.employeeName,
    required this.documentTemplateName,
    required this.documentGroupName,
    required this.date,
    required this.statusId,
    required this.statusName,
    this.period,
    required this.hasFile,
  });

  final String documentUnitId;
  final String documentId;
  final String employeeId;
  final String employeeName;
  final String documentTemplateName;
  final String documentGroupName;
  final String date;
  final int statusId;
  final String statusName;
  final PeriodApiModel? period;
  final bool hasFile;

  /// Deserializes from the API JSON structure.
  factory BatchDownloadUnitApiModel.fromJson(Map<String, dynamic> json) {
    final status = json['status'] as Map<String, dynamic>?;
    final periodJson = json['period'] as Map<String, dynamic>?;

    return BatchDownloadUnitApiModel(
      documentUnitId: json['documentUnitId'] as String? ?? '',
      documentId: json['documentId'] as String? ?? '',
      employeeId: json['employeeId'] as String? ?? '',
      employeeName: json['employeeName'] as String? ?? '',
      documentTemplateName: json['documentTemplateName'] as String? ?? '',
      documentGroupName: json['documentGroupName'] as String? ?? '',
      date: json['date'] as String? ?? '',
      statusId: status?['id'] as int? ?? 0,
      statusName: status?['name'] as String? ?? '',
      period:
          periodJson != null ? PeriodApiModel.fromJson(periodJson) : null,
      hasFile: json['hasFile'] as bool? ?? false,
    );
  }

  /// Converts this DTO to a domain [BatchDownloadUnit] entity.
  ///
  /// Transforms the date from `yyyy-MM-dd` API format to `dd/MM/yyyy`
  /// display format.
  BatchDownloadUnit toEntity() {
    return BatchDownloadUnit(
      documentUnitId: documentUnitId,
      documentId: documentId,
      employeeId: employeeId,
      employeeName: employeeName,
      documentTemplateName: documentTemplateName,
      documentGroupName: documentGroupName,
      date: _dateToDisplay(date),
      statusId: statusId,
      statusName: statusName,
      period: period?.toEntity(),
      hasFile: hasFile,
    );
  }

  static String _dateToDisplay(String apiDate) {
    if (apiDate.isEmpty) return '';
    final parts = apiDate.split('-');
    if (parts.length != 3) return apiDate;
    return '${parts[2]}/${parts[1]}/${parts[0]}';
  }
}

/// Paginated API response for batch download employees.
class BatchDownloadEmployeesResponse {
  const BatchDownloadEmployeesResponse({
    required this.items,
    required this.totalCount,
  });

  final List<BatchDownloadEmployeeApiModel> items;
  final int totalCount;

  /// Deserializes from the API JSON structure.
  factory BatchDownloadEmployeesResponse.fromJson(Map<String, dynamic> json) {
    final list = json['items'] as List<dynamic>? ?? [];
    return BatchDownloadEmployeesResponse(
      items: list
          .map((e) => BatchDownloadEmployeeApiModel.fromJson(
              e as Map<String, dynamic>))
          .toList(),
      totalCount: json['totalCount'] as int? ?? 0,
    );
  }
}

/// Paginated API response for batch download document units.
class BatchDownloadUnitsResponse {
  const BatchDownloadUnitsResponse({
    required this.items,
    required this.totalCount,
  });

  final List<BatchDownloadUnitApiModel> items;
  final int totalCount;

  /// Deserializes from the API JSON structure.
  factory BatchDownloadUnitsResponse.fromJson(Map<String, dynamic> json) {
    final list = json['items'] as List<dynamic>? ?? [];
    return BatchDownloadUnitsResponse(
      items: list
          .map((e) => BatchDownloadUnitApiModel.fromJson(
              e as Map<String, dynamic>))
          .toList(),
      totalCount: json['totalCount'] as int? ?? 0,
    );
  }
}
