import 'dart:collection';

import 'package:flutter/foundation.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../domain/my_tenant.dart';
import '../../domain/tenant_exception.dart';
import '../../domain/tenant_repository.dart';

/// What the shell has to do once a tenant becomes the current context.
///
/// The module knows nothing about People Management or bill payment: it hands
/// the choice over and the shell resolves whatever each product needs from
/// it.
typedef TenantSelectedCallback = Future<void> Function(SelectedTenant tenant);

/// Stage of the selection screen.
enum TenantSelectionStatus {
  /// Asking the server which tenants the person belongs to.
  loading,

  /// The list is on screen.
  loaded,

  /// A tenant was tapped and the context is being switched.
  selecting,

  /// The person belongs to no tenant at all.
  empty,

  /// The list could not be loaded.
  error,
}

/// Drives the screen where the user picks which customer they are operating
/// as.
///
/// This is the app's front door: every product reads the tenant chosen here,
/// so the choice is made once and not once per product.
class TenantSelectionViewModel extends ChangeNotifier {
  /// Creates the view model.
  TenantSelectionViewModel({
    required TenantRepository repository,
    required TenantContextNotifier tenantContext,
    required TenantSelectedCallback onTenantSelected,
  })  : _repository = repository,
        _tenantContext = tenantContext,
        _onTenantSelected = onTenantSelected;

  final TenantRepository _repository;
  final TenantContextNotifier _tenantContext;
  final TenantSelectedCallback _onTenantSelected;

  List<MyTenant> _tenants = const [];
  TenantSelectionStatus _status = TenantSelectionStatus.loading;
  String? _errorMessage;

  /// The tenants the person has access to.
  UnmodifiableListView<MyTenant> get tenants => UnmodifiableListView(_tenants);

  /// The current stage.
  TenantSelectionStatus get status => _status;

  /// The message to show when something failed.
  String? get errorMessage => _errorMessage;

  /// Whether the screen is busy.
  bool get isBusy =>
      _status == TenantSelectionStatus.loading ||
      _status == TenantSelectionStatus.selecting;

  /// The tenant currently in context, if any.
  String? get currentTenantId => _tenantContext.tenantId;

  /// Loads the tenants the signed-in person belongs to.
  Future<void> load() async {
    _status = TenantSelectionStatus.loading;
    _errorMessage = null;
    notifyListeners();

    try {
      final result = await _repository.getMyTenants();
      result.fold(
        onSuccess: (tenants) {
          _tenants = tenants;
          _status = tenants.isEmpty
              ? TenantSelectionStatus.empty
              : TenantSelectionStatus.loaded;
        },
        onError: (error, _) {
          _status = TenantSelectionStatus.error;
          _errorMessage = tenantErrorMessage(
            error,
            fallback: 'Não foi possível carregar seus clientes.',
          );
        },
      );
    } finally {
      notifyListeners();
    }
  }

  /// Makes [tenant] the current context.
  ///
  /// Returns whether the switch completed. A suspended tenant is refused
  /// here — its cadastro exists, but nothing inside it can be operated.
  Future<bool> select(MyTenant tenant) async {
    if (!tenant.isSelectable) return false;

    _status = TenantSelectionStatus.selecting;
    _errorMessage = null;
    notifyListeners();

    try {
      final selected = tenant.toSelectedTenant();
      await _tenantContext.select(selected);
      await _onTenantSelected(selected);
      _status = TenantSelectionStatus.loaded;
      return true;
    } catch (error) {
      _status = TenantSelectionStatus.loaded;
      _errorMessage = tenantErrorMessage(
        error,
        fallback: 'Não foi possível entrar neste cliente.',
      );
      return false;
    } finally {
      notifyListeners();
    }
  }

  /// Selects the only tenant available, when there is exactly one.
  ///
  /// Returns whether the app can move straight on to the home screen. Asking
  /// somebody to choose from a list of one is asking for nothing.
  Future<bool> selectIfSingle() async {
    if (_tenants.length != 1) return false;
    final only = _tenants.single;
    if (!only.isSelectable) return false;
    return select(only);
  }
}
