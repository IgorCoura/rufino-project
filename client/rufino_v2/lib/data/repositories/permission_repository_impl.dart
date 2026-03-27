import '../../core/errors/permission_exception.dart';
import '../../core/result.dart';
import '../../domain/entities/permission.dart';
import '../../domain/repositories/permission_repository.dart';
import '../services/permission_api_service.dart';

/// Concrete implementation of [PermissionRepository] that delegates to
/// [PermissionApiService] and wraps results in [Result].
class PermissionRepositoryImpl implements PermissionRepository {
  const PermissionRepositoryImpl({required this.permissionApiService});

  final PermissionApiService permissionApiService;

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
}
