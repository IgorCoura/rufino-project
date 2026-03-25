import '../../domain/entities/employee_document.dart';

/// Data Transfer Object for the employee document endpoints.
class EmployeeDocumentApiModel {
  const EmployeeDocumentApiModel({
    required this.id,
    required this.name,
    required this.description,
    required this.statusId,
    required this.statusName,
    required this.isSignable,
    required this.canGenerateDocument,
    required this.usePreviousPeriod,
    required this.totalUnitsCount,
    required this.units,
  });

  final String id;
  final String name;
  final String description;
  final String statusId;
  final String statusName;
  final bool isSignable;
  final bool canGenerateDocument;
  final bool usePreviousPeriod;
  final int totalUnitsCount;
  final List<DocumentUnitApiModel> units;

  /// Parses a simple document (from the list endpoint, no units).
  factory EmployeeDocumentApiModel.fromJsonSimple(Map<String, dynamic> json) {
    final status = json['status'] as Map<String, dynamic>? ?? {};
    return EmployeeDocumentApiModel(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      description: json['description'] as String? ?? '',
      statusId: (status['id'] ?? '').toString(),
      statusName: status['name'] as String? ?? '',
      isSignable: json['isSignable'] as bool? ?? false,
      canGenerateDocument: json['canGenerateDocument'] as bool? ?? false,
      usePreviousPeriod: json['usePreviousPeriod'] as bool? ?? false,
      totalUnitsCount: json['totalUnitsCount'] as int? ?? 0,
      units: const [],
    );
  }

  /// Parses a full document (from the detail endpoint, includes units).
  factory EmployeeDocumentApiModel.fromJson(Map<String, dynamic> json) {
    final status = json['status'] as Map<String, dynamic>? ?? {};
    final rawUnits = json['documentsUnits'] as List<dynamic>? ?? [];
    return EmployeeDocumentApiModel(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      description: json['description'] as String? ?? '',
      statusId: (status['id'] ?? '').toString(),
      statusName: status['name'] as String? ?? '',
      isSignable: json['isSignable'] as bool? ?? false,
      canGenerateDocument: json['canGenerateDocument'] as bool? ?? false,
      usePreviousPeriod: json['usePreviousPeriod'] as bool? ?? false,
      totalUnitsCount: json['totalUnitsCount'] as int? ?? 0,
      units: rawUnits
          .map((e) =>
              DocumentUnitApiModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  /// Converts to a domain entity.
  EmployeeDocument toEntity() {
    return EmployeeDocument(
      id: id,
      name: name,
      description: description,
      statusId: statusId,
      statusName: statusName,
      isSignable: isSignable,
      canGenerateDocument: canGenerateDocument,
      usePreviousPeriod: usePreviousPeriod,
      totalUnitsCount: totalUnitsCount,
      units: units.map((u) => u.toEntity()).toList(),
    );
  }
}

/// Data Transfer Object for a single document unit.
class DocumentUnitApiModel {
  const DocumentUnitApiModel({
    required this.id,
    required this.statusId,
    required this.statusName,
    required this.date,
    required this.validity,
    required this.createdAt,
    required this.content,
    required this.name,
    required this.extension,
  });

  final String id;
  final String statusId;
  final String statusName;
  final String date;
  final String validity;
  final String createdAt;
  final String content;
  final String name;
  final String extension;

  factory DocumentUnitApiModel.fromJson(Map<String, dynamic> json) {
    final status = json['status'] as Map<String, dynamic>? ?? {};
    return DocumentUnitApiModel(
      id: json['id'] as String? ?? '',
      statusId: (status['id'] ?? '').toString(),
      statusName: status['name'] as String? ?? '',
      date: json['date'] as String? ?? '',
      validity: json['validity'] as String? ?? '',
      createdAt: json['createAt'] as String? ?? '',
      content: json['content'] as String? ?? '',
      name: json['name'] as String? ?? '',
      extension: json['extension'] as String? ?? '',
    );
  }

  /// Converts to a domain entity.
  DocumentUnit toEntity() {
    final hasValidDate = date.isNotEmpty && date != '0001-01-01';
    return DocumentUnit(
      id: id,
      statusId: statusId,
      statusName: statusName,
      date: _dateToDisplay(date),
      validity: _dateToDisplay(validity),
      createdAt: _dateToDisplay(createdAt),
      hasFile: hasValidDate && extension.isNotEmpty,
      name: name,
    );
  }

  static String _dateToDisplay(String yyyyMMdd) {
    if (yyyyMMdd.isEmpty) return '';
    final datePart = yyyyMMdd.split('T').first;
    final parts = datePart.split('-');
    if (parts.length != 3) return yyyyMMdd;
    return '${parts[2]}/${parts[1]}/${parts[0]}';
  }

  /// Converts `dd/MM/yyyy` to `yyyy-MM-dd` for the API.
  static String dateToApi(String ddMMyyyy) {
    if (ddMMyyyy.isEmpty || ddMMyyyy.length != 10) return ddMMyyyy;
    final parts = ddMMyyyy.split('/');
    if (parts.length != 3) return ddMMyyyy;
    return '${parts[2]}-${parts[1]}-${parts[0]}';
  }
}
