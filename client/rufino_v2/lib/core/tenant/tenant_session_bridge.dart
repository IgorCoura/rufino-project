import 'package:flutter/foundation.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:tenant_management/tenant_management.dart';

import '../../domain/repositories/company_repository.dart';

/// Turns "this tenant is now the context" into whatever each product needs.
///
/// The selector knows only about tenants. People Management still works off
/// its own `Company` record (ADR-002) — same Guid, its own registry — and the
/// nineteen view models that read `getSelectedCompany()` were left untouched
/// on purpose: this class writes the company they already read, so switching
/// the app's front door did not become a rewrite of the product behind it.
///
/// It is a [ChangeNotifier] because the home screen has to say something
/// truthful when a tenant has People Management enabled but no company answers
/// for it — the state of the migration, made visible instead of surfacing as a
/// 403 three taps later.
class TenantSessionBridge extends ChangeNotifier {
  /// Creates the bridge.
  TenantSessionBridge({
    required CompanyRepository companyRepository,
    required PermissionNotifier permissionNotifier,
    required TenantPermissionNotifier tenantPermissionNotifier,
    required ErrorReporter errorReporter,
  })  : _companyRepository = companyRepository,
        _permissionNotifier = permissionNotifier,
        _tenantPermissionNotifier = tenantPermissionNotifier,
        _errorReporter = errorReporter;

  final CompanyRepository _companyRepository;
  final PermissionNotifier _permissionNotifier;
  final TenantPermissionNotifier _tenantPermissionNotifier;
  final ErrorReporter _errorReporter;

  bool _isPeopleManagementReady = false;

  /// Whether People Management can actually be used for the current tenant.
  ///
  /// False both when the product is off for this customer and when it is on
  /// but no company record answers for the tenant id — the second case is a
  /// migration that has not finished, and the home screen says so.
  bool get isPeopleManagementReady => _isPeopleManagementReady;

  /// Runs everything that has to happen when [tenant] becomes the context.
  ///
  /// Order matters: the company is resolved first, so a screen that opens
  /// right after this returns already finds the right one.
  Future<void> onTenantSelected(SelectedTenant tenant) async {
    await syncPeopleManagementCompany(tenant);

    // Both audiences: permissions on one resource server say nothing about
    // the other, and a stale set is what makes a button appear for someone
    // who cannot use it.
    await Future.wait([
      _permissionNotifier.loadPermissions(),
      _tenantPermissionNotifier.loadPermissions(),
    ]);

    _errorReporter.setUser(userId: null, companyId: tenant.id);
  }

  /// Resolves the People Management company for [tenant], if any.
  ///
  /// Returns whether the product is usable. The tenant id **is** the company
  /// id — that is what the backfill preserves, and what makes one selector
  /// enough for both products.
  Future<bool> syncPeopleManagementCompany(SelectedTenant tenant) async {
    if (!tenant.hasProduct(TenantProducts.peopleManagement)) {
      await _companyRepository.clearSelectedCompany();
      _setReady(false);
      return false;
    }

    final detail = await _companyRepository.getCompanyDetail(tenant.id);
    final company = detail.valueOrNull;
    if (company == null) {
      // The product is on for this customer but its company record is not
      // reachable — an unfinished migration, not a bug to hide.
      await _companyRepository.clearSelectedCompany();
      _setReady(false);
      return false;
    }

    final selected = await _companyRepository.selectCompany(company);
    _setReady(selected.isSuccess);
    return selected.isSuccess;
  }

  /// Drops the People Management selection when the session ends.
  Future<void> clear() async {
    await _companyRepository.clearSelectedCompany();
    _setReady(false);
  }

  void _setReady(bool value) {
    if (_isPeopleManagementReady == value) return;
    _isPeopleManagementReady = value;
    notifyListeners();
  }
}
