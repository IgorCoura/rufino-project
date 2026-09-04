import '../../domain/entities/document_group.dart';

/// DTO that mirrors the JSON shape of a document group returned by the
/// people-management API.
class DocumentGroupApiModel {
  const DocumentGroupApiModel({
    required this.id,
    required this.name,
    required this.description,
  });

  /// Unique identifier.
  final String id;

  /// Group name.
  final String name;

  /// Group description.
  final String description;

  /// Deserialises a [DocumentGroupApiModel] from a JSON map.
  factory DocumentGroupApiModel.fromJson(Map<String, dynamic> json) {
    return DocumentGroupApiModel(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      description: json['description'] as String? ?? '',
    );
  }

  /// Converts this DTO to the domain [DocumentGroup] entity.
  DocumentGroup toEntity() {
    return DocumentGroup(
      id: id,
      name: name,
      description: description,
    );
  }

  /// Serialises this model to JSON for create requests (no [id] field).
  Map<String, dynamic> toCreateJson() {
    return {
      'name': name,
      'description': description,
    };
  }

  /// Serialises this model to JSON including the [id] field (for updates).
  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'description': description,
    };
  }
}
