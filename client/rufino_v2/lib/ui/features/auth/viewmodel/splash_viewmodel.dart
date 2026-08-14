import 'package:flutter/foundation.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:tenant_management/tenant_management.dart';

import '../../../../core/tenant/tenant_session_bridge.dart';
import '../../../../domain/repositories/auth_repository.dart';

/// Where the splash decided the app should open.
enum SplashDestination {
  /// No usable session.
  login,

  /// A tenant is already in context.
  home,

  /// The user has to pick a tenant — or has none and needs to be told so.
  selectTenant,
}

/// Stage of the splash.
enum SplashStatus {
  /// Still deciding.
  loading,

  /// [SplashViewModel.destination] is ready.
  decided,
}

/// Decides where the app opens.
///
/// The order matters and is the whole point: credentials first, then the
/// permissions of **both** audiences, then the tenants the person belongs to.
/// Only with all three in hand can the app tell "has nowhere to go" apart from
/// "has exactly one place and should go straight in".
class SplashViewModel extends ChangeNotifier {
  /// Creates the view model.
  SplashViewModel({
    required AuthRepository authRepository,
    required TenantRepository tenantRepository,
    required TenantContextNotifier tenantContext,
    required TenantSessionBridge tenantSessionBridge,
    required PermissionNotifier permissionNotifier,
    required TenantPermissionNotifier tenantPermissionNotifier,
    required ErrorReporter errorReporter,
  })  : _authRepository = authRepository,
        _tenantRepository = tenantRepository,
        _tenantContext = tenantContext,
        _tenantSessionBridge = tenantSessionBridge,
        _permissionNotifier = permissionNotifier,
        _tenantPermissionNotifier = tenantPermissionNotifier,
        _errorReporter = errorReporter;

  final AuthRepository _authRepository;
  final TenantRepository _tenantRepository;
  final TenantContextNotifier _tenantContext;
  final TenantSessionBridge _tenantSessionBridge;
  final PermissionNotifier _permissionNotifier;
  final TenantPermissionNotifier _tenantPermissionNotifier;
  final ErrorReporter _errorReporter;

  SplashStatus _status = SplashStatus.loading;
  SplashDestination? _destination;

  /// Whether the decision is ready.
  SplashStatus get status => _status;

  /// Where to go. Null while [status] is loading.
  SplashDestination? get destination => _destination;

  /// Runs the decision once.
  Future<void> initialize() async {
    if (_status != SplashStatus.loading) return;

    // Credencial primeiro, e num try próprio: qualquer falha aqui é falta de
    // sessão, e mandar quem não está autenticado para o seletor de cliente
    // seria pedir que escolha algo que ele não pode ver.
    try {
      final credentials = await _authRepository.hasValidCredentials();
      if (credentials.isError || credentials.valueOrNull == false) {
        _decide(SplashDestination.login);
        return;
      }
    } catch (_) {
      _decide(SplashDestination.login);
      return;
    }

    try {
      // As duas audiências em paralelo. Um 403 numa delas significa "sem
      // permissão nenhuma ali", e não impede a outra de responder.
      await Future.wait([
        _permissionNotifier.loadPermissions(),
        _tenantPermissionNotifier.loadPermissions(),
        _tenantContext.restore(),
      ]);

      final myTenants = await _tenantRepository.getMyTenants();
      final tenants = myTenants.valueOrNull;

      if (tenants == null) {
        // A lista não veio. Com um tenant já escolhido antes, seguir é melhor
        // do que trancar o app numa tela de erro; sem ele, a tela de seleção
        // é quem mostra o erro e oferece tentar de novo.
        _decide(
          _tenantContext.hasTenant
              ? SplashDestination.home
              : SplashDestination.selectTenant,
        );
        return;
      }

      final restored = _tenantContext.current;
      if (restored != null) {
        MyTenant? match;
        for (final tenant in tenants) {
          if (tenant.id == restored.id && tenant.isSelectable) {
            match = tenant;
            break;
          }
        }

        if (match != null) {
          // Reentrar pelo servidor: produtos e papel podem ter mudado desde a
          // última sessão, e seguir com a cópia velha é operar com um menu que
          // não corresponde ao contrato do cliente.
          await _enter(match);
          _decide(SplashDestination.home);
          return;
        }

        // O tenant guardado não serve mais: perdeu o acesso, ou foi suspenso.
        await _tenantContext.clear();
        await _tenantSessionBridge.clear();
      }

      final selectable = tenants.where((t) => t.isSelectable).toList();
      if (selectable.length == 1) {
        await _enter(selectable.single);
        _decide(SplashDestination.home);
        return;
      }

      _decide(SplashDestination.selectTenant);
    } catch (_) {
      _decide(SplashDestination.selectTenant);
    }
  }

  Future<void> _enter(MyTenant tenant) async {
    final selected = tenant.toSelectedTenant();
    await _tenantContext.select(selected);
    await _tenantSessionBridge.syncPeopleManagementCompany(selected);
    _errorReporter.setUser(userId: null, companyId: selected.id);
  }

  void _decide(SplashDestination destination) {
    _destination = destination;
    _status = SplashStatus.decided;
    notifyListeners();
  }
}
