import 'dart:collection';

/// A granted permission on a Keycloak-managed resource.
///
/// Each permission pairs a [resource] name (e.g. `"employee"`, `"tenant"`)
/// with the [scopes] the current user is allowed to perform on it
/// (e.g. `"create"`, `"view"`, `"edit"`).
///
/// Permissions are fetched from Keycloak Authorization Services via an RPT
/// (Requesting Party Token) request and cached in `PermissionNotifier`. They
/// are always scoped to a single resource server (audience) — the same
/// resource name under two audiences is two different permissions, and the
/// notifier that holds them is what keeps them apart.
class Permission {
  /// Creates a permission for the given [resource] with the granted [scopes].
  const Permission({
    required this.resource,
    required List<String> scopes,
  }) : _scopes = scopes;

  /// The Keycloak resource name, matching the backend's
  /// `[ProtectedResource("resource", ...)]` attribute.
  final String resource;

  final List<String> _scopes;

  /// The scopes granted on this [resource].
  UnmodifiableListView<String> get scopes => UnmodifiableListView(_scopes);

  /// Whether the given [scope] is granted on this resource.
  bool hasScope(String scope) => _scopes.contains(scope);
}

/// DTO for serializing [Permission] to and from JSON.
///
/// Used by `PermissionCacheService` to persist permissions. Not used directly
/// by the UI layer.
class PermissionModel {
  /// Creates a model with the given [resource] and [scopes].
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
      scopes: (json['scopes'] as List<dynamic>).map((s) => s as String).toList(),
    );
  }

  /// The Keycloak resource name.
  final String resource;

  /// The scopes granted on this [resource].
  final List<String> scopes;

  /// Converts this model to the domain [Permission] entity.
  Permission toEntity() => Permission(resource: resource, scopes: scopes);

  /// Serializes this model to a JSON-encodable map.
  Map<String, dynamic> toJson() => {'resource': resource, 'scopes': scopes};
}
