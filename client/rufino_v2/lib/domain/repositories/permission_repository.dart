import '../../core/result.dart';
import '../entities/permission.dart';

/// Contract for fetching the current user's permissions from Keycloak
/// Authorization Services.
abstract class PermissionRepository {
  /// Fetches all permissions the current user has.
  ///
  /// Returns a list of [Permission] objects, each containing a resource name
  /// and the scopes granted on that resource.
  Future<Result<List<Permission>>> fetchPermissions();
}
