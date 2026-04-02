import '../../core/errors/permission_exception.dart';
import '../../core/result.dart';
import '../../domain/entities/permission.dart';
import '../../domain/repositories/permission_repository.dart';
import '../services/permission_api_service.dart';
import '../services/permission_cache_service.dart';

/// Concrete implementation of [PermissionRepository] that delegates remote
/// fetching to [PermissionApiService] and local caching to
/// [PermissionCacheService].
class PermissionRepositoryImpl implements PermissionRepository {
  const PermissionRepositoryImpl({
    required this.permissionApiService,
    required this.permissionCacheService,
  });

  final PermissionApiService permissionApiService;
  final PermissionCacheService permissionCacheService;

  @override
  Future<Result<List<Permission>>> fetchPermissions() async {
    try {
      final permissions = await permissionApiService.fetchPermissions();
      return Result.success(permissions);
    } on PermissionException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(PermissionFetchException(e));
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
