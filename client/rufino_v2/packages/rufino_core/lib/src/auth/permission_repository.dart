import '../monitoring/error_reporter.dart';
import '../result.dart';
import 'permission.dart';
import 'permission_api_service.dart';
import 'permission_cache_service.dart';
import 'permission_exception.dart';

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

/// Concrete implementation of [PermissionRepository] that delegates remote
/// fetching to [PermissionApiService] and local caching to
/// [PermissionCacheService].
class PermissionRepositoryImpl implements PermissionRepository {
  /// Creates the repository over [permissionApiService] and
  /// [permissionCacheService], reporting unexpected failures to [reporter].
  PermissionRepositoryImpl({
    required this.permissionApiService,
    required this.permissionCacheService,
    required this.reporter,
  });

  /// Remote source of permissions for one audience.
  final PermissionApiService permissionApiService;

  /// Local cache for the same audience.
  final PermissionCacheService permissionCacheService;

  /// Where unexpected failures are reported.
  final ErrorReporter reporter;

  @override
  Future<Result<List<Permission>>> fetchPermissions() async {
    try {
      final permissions = await permissionApiService.fetchPermissions();
      return Result.success(permissions);
    } on PermissionException catch (e, st) {
      return reporter.failure(e, st);
    } catch (e, st) {
      return reporter.failure(PermissionFetchException(e), st);
    }
  }

  @override
  Future<List<Permission>?> getCachedPermissions() async {
    return permissionCacheService.loadCached();
  }

  @override
  Future<void> cachePermissions(List<Permission> permissions) async {
    await permissionCacheService.save(permissions);
  }

  @override
  Future<void> clearCachedPermissions() async {
    await permissionCacheService.clear();
  }
}
