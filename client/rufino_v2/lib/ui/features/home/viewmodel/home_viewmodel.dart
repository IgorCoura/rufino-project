import 'package:flutter/foundation.dart';
import 'package:bill_payment/bill_payment.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:tenant_management/tenant_management.dart';

import '../../../../core/tenant/tenant_session_bridge.dart';
import '../../../../domain/repositories/auth_repository.dart';

/// Stage of the home screen.
enum HomeStatus {
  /// Waiting for the context to be known.
  loading,

  /// A tenant is in context.
  loaded,

  /// No tenant is in context — nothing can be shown.
  noTenant,
}

/// Drives the hub the app opens on.
///
/// The home shows the features of the products the **selected tenant** has
/// enabled and the person is allowed to use. There is no company to load here
/// anymore: the context is the tenant, and it is already in memory by the
/// time this screen is built.
class HomeViewModel extends ChangeNotifier {
  /// Creates the view model.
  HomeViewModel({
    required AuthRepository authRepository,
    required TenantContextNotifier tenantContext,
    required TenantSessionBridge tenantSessionBridge,
    required PermissionNotifier permissionNotifier,
    required TenantPermissionNotifier tenantPermissionNotifier,
    required BillPaymentPermissionNotifier billPaymentPermissionNotifier,
    required ErrorReporter errorReporter,
  })  : _authRepository = authRepository,
        _tenantContext = tenantContext,
        _tenantSessionBridge = tenantSessionBridge,
        _permissionNotifier = permissionNotifier,
        _tenantPermissionNotifier = tenantPermissionNotifier,
        _billPaymentPermissionNotifier = billPaymentPermissionNotifier,
        _errorReporter = errorReporter;

  final AuthRepository _authRepository;
  final TenantContextNotifier _tenantContext;
  final TenantSessionBridge _tenantSessionBridge;
  final PermissionNotifier _permissionNotifier;
  final TenantPermissionNotifier _tenantPermissionNotifier;
  final BillPaymentPermissionNotifier _billPaymentPermissionNotifier;
  final ErrorReporter _errorReporter;

  /// The tenant currently in context.
  SelectedTenant? get tenant => _tenantContext.current;

  /// The stage of the screen.
  HomeStatus get status =>
      _tenantContext.hasTenant ? HomeStatus.loaded : HomeStatus.noTenant;

  /// Whether the screen is waiting for something.
  bool get isLoading => false;

  /// The name to show in the app bar.
  String get tenantDisplayName => tenant?.displayName ?? 'Rufino';

  /// The document-less subtitle: the kind of customer, in words.
  ///
  /// `GET /me/tenants` does not return the document, and asking the
  /// back-office for it would need a permission a customer does not have —
  /// so the subtitle says what is actually known.
  String get tenantSubtitle {
    final current = tenant;
    if (current == null) return '';
    final kind = TenantKinds.label(current.kind);
    return current.isSuspended ? '$kind · Suspenso' : kind;
  }

  /// Whether People Management can be used for the current tenant.
  bool get isPeopleManagementReady =>
      _tenantSessionBridge.isPeopleManagementReady;

  /// Whether the tenant has People Management enabled but no company answers
  /// for it — a migration that has not finished.
  bool get isPeopleManagementPending =>
      (tenant?.hasProduct(TenantProducts.peopleManagement) ?? false) &&
      !isPeopleManagementReady;

  /// Reloads the permissions of both audiences.
  Future<void> refreshPermissions() async {
    await Future.wait([
      _permissionNotifier.loadPermissions(),
      _tenantPermissionNotifier.loadPermissions(),
      _billPaymentPermissionNotifier.loadPermissions(),
    ]);
  }

  /// Signs out, clearing every trace of the session.
  Future<void> logout() async {
    await _authRepository.logout();
    await _permissionNotifier.clear();
    await _tenantPermissionNotifier.clear();
    await _billPaymentPermissionNotifier.clear();
    await _tenantContext.clear();
    await _tenantSessionBridge.clear();
    _errorReporter.clearUser();
  }
}
