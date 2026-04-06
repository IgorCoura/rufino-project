import '../../domain/entities/permission.dart';

/// DTO for serializing [Permission] to and from JSON.
///
/// Used by [PermissionCacheService] to persist permissions in
/// `SharedPreferences`. Not used directly by the UI layer.
class PermissionModel {
  const PermissionModel({required this.resource, required this.scopes});

  /// Creates a model from a domain [Permission] entity.
  factory PermissionModel.fromEntity(Permission permission) {
    return PermissionModel(
      resource: permission.resource,
      scopes: List<String>.from(permission.scopes),
    );
  }

  /// Deserializes a model from a JSON map.
  factory PermissionModel.fromJson(Map<String, dynamic> json) {
    return PermissionModel(
      resource: json['resource'] as String,
      scopes: (json['scopes'] as List<dynamic>)
          .map((s) => s as String)
          .toList(),
    );
  }

  /// The Keycloak resource name.
  final String resource;

  /// The scopes granted on this [resource].
  final List<String> scopes;

  /// Converts this model to the domain [Permission] entity.
  Permission toEntity() {
    return Permission(resource: resource, scopes: scopes);
  }

  /// Serializes this model to a JSON-encodable map.
  Map<String, dynamic> toJson() {
    return {'resource': resource, 'scopes': scopes};
  }
}
