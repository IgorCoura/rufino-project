import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';
import 'package:provider/single_child_widget.dart';
import 'package:rufino_core/rufino_core.dart';

import 'data/tenant_api_service.dart';
import 'data/tenant_repository_impl.dart';
import 'domain/tenant_repository.dart';
import 'tenant_permissions.dart';
import 'ui/tenant_routes.dart';
import 'ui/select/tenant_selection_viewmodel.dart';

/// A identidade do cliente da plataforma, plugada na casca.
///
/// Não é produto: não tem porteira de produto no tenant, porque ele **é** o
/// tenant. Quem decide se o grupo aparece no menu é só a permissão — na
/// prática, quem opera o back-office da plataforma.
class TenantManagementModule extends AppModule {
  /// Cria o módulo.
  const TenantManagementModule({
    required this.client,
    required this.baseUrl,
    required this.getAuthHeader,
    required this.errorReporter,
    required this.homeRoute,
    required this.onTenantSelected,
    required this.onLogout,
    this.cepService,
  });

  /// O cliente HTTP da casca.
  final http.Client client;

  /// Origem da API deste BC.
  final String baseUrl;

  /// Devolve o cabeçalho `Authorization` já pronto.
  final Future<String> Function() getAuthHeader;

  /// Para onde os erros vão.
  final ErrorReporter errorReporter;

  /// Rota do Home.
  final String homeRoute;

  /// O que a casca faz quando um tenant é escolhido.
  final TenantSelectedCallback onTenantSelected;

  /// Como a casca encerra a sessão.
  final Future<void> Function(BuildContext context) onLogout;

  /// Consulta de CEP, opcional, para o cadastro.
  final CepLookupService? cepService;

  @override
  String get menuTitle => 'ADMINISTRAÇÃO DA PLATAFORMA';

  @override
  List<RouteBase> routes() => tenantManagementRoutes(
        homeRoute: homeRoute,
        onTenantSelected: onTenantSelected,
        onLogout: onLogout,
        cepService: cepService,
      );

  @override
  List<SingleChildWidget> providers() => [
        Provider<TenantRepository>.value(
          value: TenantRepositoryImpl(
            apiService: TenantApiService(
              client: client,
              baseUrl: baseUrl,
              getAuthHeader: getAuthHeader,
            ),
            reporter: errorReporter,
          ),
        ),
      ];

  @override
  List<HomeEntry> visibleEntries(BuildContext context) {
    final permissions = context.watch<TenantPermissionNotifier>();
    return _entries
        .where((e) => permissions.hasAnyScope(e.resource))
        .toList();
  }
}

const _entries = <HomeEntry>[
  HomeEntry(
    icon: Icons.manage_accounts_outlined,
    label: 'Clientes',
    route: TenantRoutes.list,
    resource: TenantResources.tenant,
  ),
];
