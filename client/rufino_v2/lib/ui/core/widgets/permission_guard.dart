import 'package:flutter/widgets.dart';
import 'package:provider/provider.dart';

import '../../features/auth/viewmodel/permission_notifier.dart';

/// Conditionally renders [child] only when the user has the given [scope]
/// on the given [resource].
///
/// When the permission is not granted, renders [SizedBox.shrink] — the
/// element is completely hidden, not disabled.
///
/// Example:
/// ```dart
/// PermissionGuard(
///   resource: 'employee',
///   scope: 'create',
///   child: FloatingActionButton(...),
/// )
/// ```
class PermissionGuard extends StatelessWidget {
  const PermissionGuard({
    super.key,
    required this.resource,
    required this.scope,
    required this.child,
  });

  /// The Keycloak resource name (e.g. `"employee"`, `"Document"`).
  final String resource;

  /// The required scope (e.g. `"create"`, `"edit"`, `"view"`).
  final String scope;

  /// The widget to render when the permission is granted.
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final notifier = context.watch<PermissionNotifier>();
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
/// when the user has no access to that module whatsoever.
///
/// Example:
/// ```dart
/// ModuleGuard(
///   resource: 'employee',
///   child: _MenuCard(label: 'Funcionarios', ...),
/// )
/// ```
class ModuleGuard extends StatelessWidget {
  const ModuleGuard({
    super.key,
    required this.resource,
    required this.child,
  });

  /// The Keycloak resource name (e.g. `"employee"`, `"Document"`).
  final String resource;

  /// The widget to render when the user has at least one scope on
  /// this resource.
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final notifier = context.watch<PermissionNotifier>();
    if (notifier.hasAnyScope(resource)) {
      return child;
    }
    return const SizedBox.shrink();
  }
}
