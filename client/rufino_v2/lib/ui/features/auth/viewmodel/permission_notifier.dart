import 'package:flutter/widgets.dart';

import '../../../../domain/entities/permission.dart';
import '../../../../domain/repositories/permission_repository.dart';

/// Status of the permission loading process.
enum PermissionStatus { loading, loaded, error }

/// Holds the current user's Keycloak permissions and exposes query methods.
///
/// Permissions are fetched after login via [loadPermissions] and cached
/// in memory (and optionally persisted through the [PermissionRepository]).
/// Widgets use [hasPermission] or [hasAnyScope] (typically through
/// `PermissionGuard` / `ModuleGuard`) to decide what to render.
///
/// Also observes the app lifecycle — when the app resumes from background,
/// permissions are automatically reloaded (throttled to once every 30 seconds).
class PermissionNotifier extends ChangeNotifier
    with WidgetsBindingObserver {
  PermissionNotifier({required PermissionRepository permissionRepository})
      : _permissionRepository = permissionRepository {
    WidgetsBinding.instance.addObserver(this);
  }

  final PermissionRepository _permissionRepository;

  List<Permission> _permissions = const [];
  PermissionStatus _status = PermissionStatus.loading;
  Object? _lastError;
  DateTime? _lastLoadTime;

  static const _reloadThrottle = Duration(seconds: 30);

  /// The current loading status.
  PermissionStatus get status => _status;

  /// The last error that occurred during [loadPermissions], or `null`.
  ///
  /// When a reload fails but previous permissions are still cached, this
  /// holds the error while [status] remains [PermissionStatus.loaded].
  Object? get lastError => _lastError;

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
  ///
  /// If a previous set of permissions is already loaded and the fetch fails,
  /// the existing permissions are preserved (stale data is better than no
  /// data). The error is still stored in [lastError] for optional display.
  Future<void> loadPermissions() async {
    _status = PermissionStatus.loading;
    notifyListeners();

    try {
      // Hydrate from cache if we have no permissions in memory yet.
      if (_permissions.isEmpty) {
        final cached =
            await _permissionRepository.getCachedPermissions();
        if (cached != null && cached.isNotEmpty) {
          _permissions = cached;
          _status = PermissionStatus.loaded;
          notifyListeners();
        }
      }

      final result = await _permissionRepository.fetchPermissions();
      result.fold(
        onSuccess: (permissions) {
          _permissions = permissions;
          _status = PermissionStatus.loaded;
          _lastError = null;
          _lastLoadTime = DateTime.now();
          _permissionRepository.cachePermissions(permissions);
        },
        onError: (error) {
          _lastError = error;
          if (_permissions.isEmpty) {
            _status = PermissionStatus.error;
          } else {
            // Keep stale permissions — better than losing access.
            _status = PermissionStatus.loaded;
          }
        },
      );
    } catch (error) {
      _lastError = error;
      if (_permissions.isEmpty) {
        _status = PermissionStatus.error;
      } else {
        _status = PermissionStatus.loaded;
      }
    } finally {
      notifyListeners();
    }
  }

  /// Clears all cached permissions (both in-memory and persisted).
  ///
  /// Call this on logout so stale permissions are not visible to the next user.
  Future<void> clear() async {
    _permissions = const [];
    _status = PermissionStatus.loading;
    _lastError = null;
    _lastLoadTime = null;
    await _permissionRepository.clearCachedPermissions();
    notifyListeners();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) {
      _reloadIfThrottleAllows();
    }
  }

  void _reloadIfThrottleAllows() {
    if (_permissions.isEmpty) return;
    final last = _lastLoadTime;
    if (last != null && DateTime.now().difference(last) < _reloadThrottle) {
      return;
    }
    loadPermissions();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }
}
