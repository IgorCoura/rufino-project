import '../../domain/entities/document_content_status.dart';

/// Wire shape that addresses a single document unit.
///
/// Mirrors the API's `DocumentUnitRef` and is used as the request item by
/// both the outdated check and the snapshot refresh.
class DocumentUnitRefApiModel {
  /// Creates a [DocumentUnitRefApiModel].
  const DocumentUnitRefApiModel({
    required this.documentUnitId,
    required this.documentId,
    required this.employeeId,
  });

  /// Identifier of the document unit.
  final String documentUnitId;

  /// Identifier of the document the unit belongs to.
  final String documentId;

  /// Identifier of the employee the document belongs to.
  final String employeeId;

  /// Serializes this reference to the API request shape.
  Map<String, String> toJson() => {
        'documentUnitId': documentUnitId,
        'documentId': documentId,
        'employeeId': employeeId,
      };
}

/// One entry of the outdated-check response.
class DocumentContentStatusApiModel {
  /// Creates a [DocumentContentStatusApiModel].
  const DocumentContentStatusApiModel({
    required this.documentUnitId,
    required this.isOutdated,
    required this.checkFailed,
  });

  /// Identifier of the checked document unit.
  final String documentUnitId;

  /// Whether the stored snapshot diverges from the employee's current data.
  final bool isOutdated;

  /// Whether the comparison could not be completed.
  final bool checkFailed;

  /// Builds a [DocumentContentStatusApiModel] from the API payload.
  factory DocumentContentStatusApiModel.fromJson(Map<String, dynamic> json) {
    return DocumentContentStatusApiModel(
      documentUnitId: json['documentUnitId']?.toString() ?? '',
      isOutdated: json['isOutdated'] as bool? ?? false,
      checkFailed: json['checkFailed'] as bool? ?? false,
    );
  }

  /// Converts this DTO into its domain entity.
  DocumentContentStatus toEntity() => DocumentContentStatus(
        documentUnitId: documentUnitId,
        isOutdated: isOutdated,
        checkFailed: checkFailed,
      );
}

/// Envelope of the outdated-check response.
class DocumentContentStatusResponse {
  /// Creates a [DocumentContentStatusResponse].
  const DocumentContentStatusResponse({required this.items});

  /// One status per requested document unit.
  final List<DocumentContentStatusApiModel> items;

  /// Builds a [DocumentContentStatusResponse] from the API payload.
  factory DocumentContentStatusResponse.fromJson(Map<String, dynamic> json) {
    final rawItems = json['items'] as List<dynamic>? ?? const [];
    return DocumentContentStatusResponse(
      items: rawItems
          .map((e) =>
              DocumentContentStatusApiModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}
