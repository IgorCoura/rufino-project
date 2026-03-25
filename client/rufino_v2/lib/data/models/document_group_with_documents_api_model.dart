import '../../domain/entities/document_group_with_documents.dart';
import 'employee_document_api_model.dart';

/// DTO that mirrors the JSON shape returned by the
/// `GET /api/v1/{companyId}/documentgroup/withdocuments/{employeeId}` endpoint.
class DocumentGroupWithDocumentsApiModel {
  const DocumentGroupWithDocumentsApiModel({
    required this.id,
    required this.name,
    required this.description,
    required this.statusId,
    required this.statusName,
    required this.documents,
  });

  /// Unique identifier.
  final String id;

  /// Group name.
  final String name;

  /// Group description.
  final String description;

  /// The aggregate status id of this group.
  final String statusId;

  /// The aggregate status display name.
  final String statusName;

  /// Nested document summaries (without units).
  final List<EmployeeDocumentApiModel> documents;

  /// Deserialises from a JSON map.
  factory DocumentGroupWithDocumentsApiModel.fromJson(
      Map<String, dynamic> json) {
    final status =
        json['documentsStatus'] as Map<String, dynamic>? ?? {};
    final rawDocs = json['documents'] as List<dynamic>? ?? [];

    return DocumentGroupWithDocumentsApiModel(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      description: json['description'] as String? ?? '',
      statusId: (status['id'] ?? '').toString(),
      statusName: status['name'] as String? ?? '',
      documents: rawDocs
          .map((e) => EmployeeDocumentApiModel.fromJsonSimple(
              e as Map<String, dynamic>))
          .toList(),
    );
  }

  /// Converts this DTO to the domain entity.
  DocumentGroupWithDocuments toEntity() {
    return DocumentGroupWithDocuments(
      id: id,
      name: name,
      description: description,
      statusId: statusId,
      statusName: statusName,
      documents: documents.map((d) => d.toEntity()).toList(),
    );
  }
}
