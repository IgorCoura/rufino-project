import 'package:people_management/people_management.dart';
import 'period_api_model.dart';

/// API response model for the dashboard summary.
///
/// Parses the JSON returned by
/// `GET /api/v1/{company}/document-dashboard/summary`.
class DashboardSummaryApiModel {
  const DashboardSummaryApiModel({
    required this.expired,
    required this.expiring,
    required this.pending,
    required this.awaitingSignature,
    required this.requiresValidation,
  });

  final int expired;
  final int expiring;
  final int pending;
  final int awaitingSignature;
  final int requiresValidation;

  /// Deserializes from the API JSON structure.
  factory DashboardSummaryApiModel.fromJson(Map<String, dynamic> json) {
    return DashboardSummaryApiModel(
      expired: json['expired'] as int? ?? 0,
      expiring: json['expiring'] as int? ?? 0,
      pending: json['pending'] as int? ?? 0,
      awaitingSignature: json['awaitingSignature'] as int? ?? 0,
      requiresValidation: json['requiresValidation'] as int? ?? 0,
    );
  }

  /// Converts this DTO to a domain [DashboardSummary] entity.
  DashboardSummary toEntity() {
    return DashboardSummary(
      expired: expired,
      expiring: expiring,
      pending: pending,
      awaitingSignature: awaitingSignature,
      requiresValidation: requiresValidation,
    );
  }
}

/// API response model for a document unit row of the dashboard list.
///
/// Parses the JSON returned by
/// `GET /api/v1/{company}/document-dashboard/units`.
class DashboardUnitApiModel {
  const DashboardUnitApiModel({
    required this.documentUnitId,
    required this.documentId,
    required this.employeeId,
    required this.employeeName,
    required this.employeeStatusId,
    required this.employeeStatusName,
    required this.documentTemplateName,
    required this.documentGroupName,
    required this.date,
    required this.validity,
    required this.statusId,
    required this.statusName,
    this.period,
    required this.hasFile,
  });

  final String documentUnitId;
  final String documentId;
  final String employeeId;
  final String employeeName;
  final int employeeStatusId;
  final String employeeStatusName;
  final String documentTemplateName;
  final String documentGroupName;
  final String date;
  final String validity;
  final int statusId;
  final String statusName;
  final PeriodApiModel? period;
  final bool hasFile;

  /// Deserializes from the API JSON structure.
  factory DashboardUnitApiModel.fromJson(Map<String, dynamic> json) {
    final status = json['status'] as Map<String, dynamic>?;
    final employeeStatus = json['employeeStatus'] as Map<String, dynamic>?;
    final periodJson = json['period'] as Map<String, dynamic>?;

    return DashboardUnitApiModel(
      documentUnitId: json['documentUnitId'] as String? ?? '',
      documentId: json['documentId'] as String? ?? '',
      employeeId: json['employeeId'] as String? ?? '',
      employeeName: json['employeeName'] as String? ?? '',
      employeeStatusId: employeeStatus?['id'] as int? ?? 0,
      employeeStatusName: employeeStatus?['name'] as String? ?? '',
      documentTemplateName: json['documentTemplateName'] as String? ?? '',
      documentGroupName: json['documentGroupName'] as String? ?? '',
      date: json['date'] as String? ?? '',
      validity: json['validity'] as String? ?? '',
      statusId: status?['id'] as int? ?? 0,
      statusName: status?['name'] as String? ?? '',
      period:
          periodJson != null ? PeriodApiModel.fromJson(periodJson) : null,
      hasFile: json['hasFile'] as bool? ?? false,
    );
  }

  /// Converts this DTO to a domain [DashboardUnitItem] entity.
  ///
  /// Transforms both dates from `yyyy-MM-dd` API format to `dd/MM/yyyy`
  /// display format.
  DashboardUnitItem toEntity() {
    return DashboardUnitItem(
      documentUnitId: documentUnitId,
      documentId: documentId,
      employeeId: employeeId,
      employeeName: employeeName,
      employeeStatusId: employeeStatusId.toString(),
      employeeStatusName: employeeStatusName,
      documentTemplateName: documentTemplateName,
      documentGroupName: documentGroupName,
      date: _dateToDisplay(date),
      validity: _dateToDisplay(validity),
      statusId: statusId.toString(),
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

/// Paginated API response for dashboard document units.
class DashboardUnitsResponse {
  const DashboardUnitsResponse({
    required this.items,
    required this.totalCount,
  });

  final List<DashboardUnitApiModel> items;
  final int totalCount;

  /// Deserializes from the API JSON structure.
  factory DashboardUnitsResponse.fromJson(Map<String, dynamic> json) {
    final list = json['items'] as List<dynamic>? ?? [];
    return DashboardUnitsResponse(
      items: list
          .map((e) =>
              DashboardUnitApiModel.fromJson(e as Map<String, dynamic>))
          .toList(),
      totalCount: json['totalCount'] as int? ?? 0,
    );
  }
}
