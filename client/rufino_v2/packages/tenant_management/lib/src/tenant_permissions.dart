import 'package:rufino_core/rufino_core.dart';

/// The permissions of this module, as declared in Keycloak.
///
/// They belong to the **`tenant-management-api`** resource server, which is
/// not the one the People Management screens ask about. Same resource name
/// under two audiences would be two different permissions — which is exactly
/// why the guards below are typed.
abstract final class TenantResources {
  /// The cadastro itself. Scopes: view, create, edit, suspend.
  static const String tenant = 'tenant';

  /// Who has access to a tenant. Scopes: view, edit.
  static const String tenantAccess = 'tenant-access';

  /// Which products a tenant has enabled. Scopes: view, edit.
  static const String tenantProduct = 'tenant-product';
}

/// The scope names shared by the resources above.
abstract final class TenantScopes {
  /// Read.
  static const String view = 'view';

  /// Register a new tenant.
  static const String create = 'create';

  /// Change an existing one.
  static const String edit = 'edit';

  /// Freeze or unfreeze a cadastro.
  static const String suspend = 'suspend';
}

/// Holds the permissions granted on the `tenant-management-api` audience.
///
/// A subclass exists for one reason: `provider` resolves by type, and the app
/// keeps two notifiers of the same shape in the tree. Without the distinct
/// type, whichever was registered last would answer for both audiences.
class TenantPermissionNotifier extends PermissionNotifier {
  /// Creates the notifier over the tenant audience's repository.
  TenantPermissionNotifier({required super.permissionRepository});

  /// Whether the person can reach the back-office at all.
  bool get canSeeBackOffice => hasAnyScope(TenantResources.tenant);

  /// Whether the person can register a new tenant.
  bool get canCreateTenant =>
      hasPermission(TenantResources.tenant, TenantScopes.create);
}

/// Renders [child] only when the tenant audience grants [scope] on [resource].
typedef TenantPermissionGuard = PermissionGuard<TenantPermissionNotifier>;

/// Renders [child] only when the tenant audience grants any scope on
/// [resource].
typedef TenantModuleGuard = ModuleGuard<TenantPermissionNotifier>;
