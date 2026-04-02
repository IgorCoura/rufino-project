import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/domain/entities/permission.dart';
import 'package:rufino_v2/domain/repositories/permission_repository.dart';

class FakePermissionRepository implements PermissionRepository {
  List<Permission> _permissions = const [];
  List<Permission>? _cachedPermissions;
  bool _shouldFail = false;

  void setPermissions(List<Permission> permissions) =>
      _permissions = permissions;

  void setShouldFail(bool value) => _shouldFail = value;

  void setCachedPermissions(List<Permission>? permissions) =>
      _cachedPermissions = permissions;

  @override
  Future<Result<List<Permission>>> fetchPermissions() async {
    if (_shouldFail) {
      return Result.error(Exception('Permission fetch failed'));
    }
    return Result.success(_permissions);
  }

  @override
  Future<List<Permission>?> getCachedPermissions() async {
    return _cachedPermissions;
  }

  @override
  Future<void> cachePermissions(List<Permission> permissions) async {
    _cachedPermissions = permissions;
  }

  @override
  Future<void> clearCachedPermissions() async {
    _cachedPermissions = null;
  }
}
