import 'package:flutter/widgets.dart';
import 'package:provider/provider.dart';

import 'permission_notifier.dart';

/// Conditionally renders [child] only when the user has the given [scope]
/// on the given [resource].
///
/// When the permission is not granted, renders [SizedBox.shrink] — the
/// element is completely hidden, not disabled.
///
/// The type parameter selects **which resource server** answers the
/// question. Omitting it resolves to [PermissionNotifier], the app's default
/// audience; a second audience passes its own notifier subclass:
///
/// ```dart
/// PermissionGuard(                      // people-management-api
///   resource: 'employee',
///   scope: 'create',
///   child: FloatingActionButton(...),
/// )
///
/// PermissionGuard<TenantPermissionNotifier>(   // tenant-management-api
///   resource: 'tenant',
///   scope: 'create',
///   child: FloatingActionButton(...),
/// )
/// ```
class PermissionGuard<T extends PermissionNotifier> extends StatelessWidget {
  /// Creates a guard for [resource] + [scope] around [child].
  const PermissionGuard({
    super.key,
    required this.resource,
    required this.scope,
    required this.child,
  });

  /// The Keycloak resource name (e.g. `"employee"`, `"tenant"`).
  final String resource;

  /// The required scope (e.g. `"create"`, `"edit"`, `"view"`).
  final String scope;

  /// The widget to render when the permission is granted.
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final notifier = context.watch<T>();
    if (notifier.hasPermission(resource, scope)) {
      return child;
    }
    return const SizedBox.shrink();
  }
}

/// Conditionally renders [child] only when the user has any scope at all
/// on the given [resource].
///
/// Useful for hiding entire feature sections (e.g. a home-screen menu card)
/// when the user has no access to that module whatsoever. The type parameter
/// works exactly as in [PermissionGuard].
class ModuleGuard<T extends PermissionNotifier> extends StatelessWidget {
  /// Creates a guard around [child] for any scope on [resource].
  const ModuleGuard({
    super.key,
    required this.resource,
    required this.child,
  });

  /// The Keycloak resource name (e.g. `"employee"`, `"tenant"`).
  final String resource;

  /// The widget to render when the user has at least one scope on
  /// this resource.
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final notifier = context.watch<T>();
    if (notifier.hasAnyScope(resource)) {
      return child;
    }
    return const SizedBox.shrink();
  }
}
