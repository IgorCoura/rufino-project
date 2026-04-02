import '../../core/result.dart';
import '../entities/permission.dart';

/// Contract for fetching and caching the current user's permissions from
/// Keycloak Authorization Services.
abstract class PermissionRepository {
  /// Fetches all permissions the current user has from the remote server.
  ///
  /// Returns a list of [Permission] objects, each containing a resource name
  /// and the scopes granted on that resource.
  Future<Result<List<Permission>>> fetchPermissions();

  /// Returns locally cached permissions, or `null` if no cache exists.
  Future<List<Permission>?> getCachedPermissions();

  /// Persists the given [permissions] to local cache.
  Future<void> cachePermissions(List<Permission> permissions);

  /// Removes all locally cached permissions.
  Future<void> clearCachedPermissions();
}
