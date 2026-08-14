import 'package:flutter/widgets.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../tenant_permissions.dart';
import 'select/tenant_selection_viewmodel.dart';
import 'tenant_pages.dart';

/// Route paths this module owns.
///
/// They are constants so the shell can send the user here without spelling a
/// string that only breaks at runtime.
abstract final class TenantRoutes {
  /// The app's front door: pick the current customer.
  static const String select = '/tenant/select';

  /// Back-office listing of every tenant on the platform.
  static const String list = '/tenant';

  /// Cadastro of a new tenant — reachable only from [select].
  static const String create = '/tenant/create';

  /// Full cadastro of one tenant.
  static String detail(String id) => '/tenant/$id';
}

/// Builds the routes of this module.
///
/// The shell supplies what belongs to it: where "home" is, what to do once a
/// tenant becomes the context, and how to sign out. The module supplies the
/// screens. Neither knows the other's internals.
List<RouteBase> tenantManagementRoutes({
  required String homeRoute,
  required TenantSelectedCallback onTenantSelected,
  required Future<void> Function(BuildContext context) onLogout,
  CepLookupService? cepService,
}) {
  return [
    GoRoute(
      path: TenantRoutes.select,
      builder: (context, state) => TenantSelectionPage(
        onTenantSelected: onTenantSelected,
        homeRoute: homeRoute,
        onSelected: () => context.go(homeRoute),
        onCreateTenant: () => context.push(TenantRoutes.create),
        onOpenBackOffice: () => context.push(TenantRoutes.list),
        onLogout: () => onLogout(context),
      ),
    ),
    // Declarada ANTES de `/tenant/:id`: go_router casa na ordem, e um `:id`
    // à frente engoliria `create` como se fosse um identificador.
    GoRoute(
      path: TenantRoutes.create,
      redirect: (context, state) => _requireTenantScope(
        context,
        scope: TenantScopes.create,
        fallback: TenantRoutes.select,
      ),
      builder: (context, state) => TenantFormPage(
        cepService: cepService,
        backFallback: TenantRoutes.select,
        // O cadastro responde só com o id; o estado do convite está no
        // detalhe, e é lá que a pessoa descobre se o acesso chegou.
        onRegistered: (id) => context.pushReplacement(TenantRoutes.detail(id)),
      ),
    ),
    GoRoute(
      path: TenantRoutes.list,
      redirect: (context, state) => _requireTenantScope(
        context,
        scope: TenantScopes.view,
        fallback: homeRoute,
      ),
      builder: (context, state) => TenantListPage(
        backFallback: homeRoute,
        onOpenTenant: (id) => context.push(TenantRoutes.detail(id)),
      ),
    ),
    GoRoute(
      path: '/tenant/:id',
      redirect: (context, state) => _requireTenantScope(
        context,
        scope: TenantScopes.view,
        fallback: homeRoute,
      ),
      builder: (context, state) => TenantDetailPage(
        tenantId: state.pathParameters['id']!,
        cepService: cepService,
        backFallback: TenantRoutes.list,
      ),
    ),
  ];
}

/// Guards a route behind a scope of the **tenant** audience.
///
/// A guard in the widget tree hides a button; it does nothing about a URL
/// typed into the browser. Returns the path to redirect to, or `null` to let
/// the navigation through.
///
/// While permissions have not loaded yet the navigation is **allowed**: on
/// the web the router opens straight at the deep link, without passing
/// through the splash, and refusing at that moment would throw the operator
/// out on every refresh. The screens behind these routes are read-only until
/// the server answers, and the server is the one that decides anyway.
String? _requireTenantScope(
  BuildContext context, {
  required String scope,
  required String fallback,
}) {
  final permissions = context.read<TenantPermissionNotifier>();
  if (permissions.status != PermissionStatus.loaded) return null;
  if (permissions.hasPermission(TenantResources.tenant, scope)) return null;
  return fallback;
}
