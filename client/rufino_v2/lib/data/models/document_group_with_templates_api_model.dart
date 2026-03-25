import '../../domain/entities/document_group_with_templates.dart';

/// DTO that mirrors the JSON shape returned by the
/// `GET /api/v1/{companyId}/documentgroup/withtemplates` endpoint.
class DocumentGroupWithTemplatesApiModel {
  const DocumentGroupWithTemplatesApiModel({
    required this.id,
    required this.name,
    required this.description,
    required this.companyId,
    required this.documentTemplates,
  });

  /// Unique identifier.
  final String id;

  /// Group name.
  final String name;

  /// Group description.
  final String description;

  /// The company this group belongs to.
  final String companyId;

  /// Nested template summaries.
  final List<DocumentTemplateSummaryApiModel> documentTemplates;

  /// Deserialises from a JSON map.
  factory DocumentGroupWithTemplatesApiModel.fromJson(
      Map<String, dynamic> json) {
    final templates = (json['documentTemplates'] as List<dynamic>?)
            ?.map((e) => DocumentTemplateSummaryApiModel.fromJson(
                e as Map<String, dynamic>))
            .toList() ??
        [];

    return DocumentGroupWithTemplatesApiModel(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      description: json['description'] as String? ?? '',
      companyId: json['companyId'] as String? ?? '',
      documentTemplates: templates,
    );
  }

  /// Converts this DTO to the domain entity.
  DocumentGroupWithTemplates toEntity() {
    return DocumentGroupWithTemplates(
      id: id,
      name: name,
      description: description,
      templates: documentTemplates.map((t) => t.toEntity()).toList(),
    );
  }
}

/// DTO for a simplified document template nested inside a group.
class DocumentTemplateSummaryApiModel {
  const DocumentTemplateSummaryApiModel({
    required this.id,
    required this.name,
    required this.description,
  });

  /// Unique identifier.
  final String id;

  /// Template name.
  final String name;

  /// Template description.
  final String description;

  /// Deserialises from a JSON map.
  factory DocumentTemplateSummaryApiModel.fromJson(Map<String, dynamic> json) {
    return DocumentTemplateSummaryApiModel(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      description: json['description'] as String? ?? '',
    );
  }

  /// Converts this DTO to the domain entity.
  DocumentTemplateSummary toEntity() {
    return DocumentTemplateSummary(
      id: id,
      name: name,
      description: description,
    );
  }
}
