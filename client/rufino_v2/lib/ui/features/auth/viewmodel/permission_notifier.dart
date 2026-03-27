import 'package:flutter/foundation.dart';

import '../../../../domain/entities/permission.dart';
import '../../../../domain/repositories/permission_repository.dart';

/// Status of the permission loading process.
enum PermissionStatus { loading, loaded, error }

/// Holds the current user's Keycloak permissions and exposes query methods.
///
/// Permissions are fetched once after login via [loadPermissions] and cached
/// in memory. Widgets use [hasPermission] or [hasAnyScope] (typically through
/// `PermissionGuard` / `ModuleGuard`) to decide what to render.
class PermissionNotifier extends ChangeNotifier {
  PermissionNotifier({required PermissionRepository permissionRepository})
      : _permissionRepository = permissionRepository;

  final PermissionRepository _permissionRepository;

  List<Permission> _permissions = const [];
  PermissionStatus _status = PermissionStatus.loading;

  /// The current loading status.
  PermissionStatus get status => _status;

  /// Whether the user has the given [scope] on the given [resource].
  ///
  /// Example: `hasPermission('employee', 'create')`.
  bool hasPermission(String resource, String scope) {
    return _permissions.any(
      (p) => p.resource == resource && p.hasScope(scope),
    );
  }

  /// Whether the user has any scope at all on the given [resource].
  ///
  /// Useful for module-level visibility — e.g. showing or hiding an entire
  /// menu section.
  bool hasAnyScope(String resource) {
    return _permissions.any((p) => p.resource == resource);
  }

  /// Fetches the user's permissions from Keycloak and caches them.
  Future<void> loadPermissions() async {
    _status = PermissionStatus.loading;
    notifyListeners();

    try {
      final result = await _permissionRepository.fetchPermissions();
      result.fold(
        onSuccess: (permissions) {
          _permissions = permissions;
          _status = PermissionStatus.loaded;
        },
        onError: (_) {
          _permissions = const [];
          _status = PermissionStatus.error;
        },
      );
    } catch (_) {
      _permissions = const [];
      _status = PermissionStatus.error;
    } finally {
      notifyListeners();
    }
  }

  /// Clears all cached permissions.
  ///
  /// Call this on logout so stale permissions are not visible to the next user.
  void clear() {
    _permissions = const [];
    _status = PermissionStatus.loading;
    notifyListeners();
  }
}
