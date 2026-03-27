import 'dart:collection';

/// A granted permission on a Keycloak-managed resource.
///
/// Each permission pairs a [resource] name (e.g. `"employee"`, `"Document"`)
/// with the [scopes] the current user is allowed to perform on it
/// (e.g. `"create"`, `"view"`, `"edit"`).
///
/// Permissions are fetched from Keycloak Authorization Services via an RPT
/// (Requesting Party Token) request and cached in [PermissionNotifier].
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
