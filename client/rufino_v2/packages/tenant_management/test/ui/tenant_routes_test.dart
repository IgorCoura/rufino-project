import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:tenant_management/tenant_management.dart';

import '../fakes/fakes.dart';

void main() {
  late FakeTenantRepository repository;
  late TenantContextNotifier tenantContext;

  setUp(() {
    repository = FakeTenantRepository();
    tenantContext = TenantContextNotifier(storage: FakeSecureStorage());
  });

  /// Pumps the module's routes with [permissions], opening at [location].
  ///
  /// [permissionsLoaded] false reproduces the web deep link: the router opens
  /// straight at the URL, before the permission request has answered.
  Future<void> pumpAt(
    WidgetTester tester,
    String location, {
    List<Permission> permissions = const [],
    bool permissionsLoaded = true,
  }) async {
    final repo = FakePermissionRepository()..setPermissions(permissions);
    final notifier = TenantPermissionNotifier(permissionRepository: repo);
    if (permissionsLoaded) await notifier.loadPermissions();

    final router = GoRouter(
      initialLocation: location,
      routes: [
        GoRoute(
          path: '/home',
          builder: (_, __) => const Scaffold(body: Text('home-screen')),
        ),
        ...tenantManagementRoutes(
          homeRoute: '/home',
          onTenantSelected: (_) async {},
          onLogout: (_) async {},
        ),
      ],
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<TenantPermissionNotifier>.value(
            value: notifier,
          ),
          ChangeNotifierProvider<TenantContextNotifier>.value(
            value: tenantContext,
          ),
          Provider<TenantRepository>.value(value: repository),
        ],
        child: MaterialApp.router(routerConfig: router),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('tenantManagementRoutes', () {
    testWidgets('a deep link into the back-office without permission goes '
        'home', (tester) async {
      await pumpAt(tester, TenantRoutes.list);

      expect(find.text('home-screen'), findsOneWidget);
      expect(find.text('Clientes da plataforma'), findsNothing);
    });

    testWidgets('the back-office opens for whoever may view it',
        (tester) async {
      await pumpAt(
        tester,
        TenantRoutes.list,
        permissions: const [
          Permission(resource: TenantResources.tenant, scopes: ['view']),
        ],
      );

      expect(find.text('Clientes da plataforma'), findsOneWidget);
    });

    testWidgets('viewing is not enough to reach the cadastro', (tester) async {
      await pumpAt(
        tester,
        TenantRoutes.create,
        permissions: const [
          Permission(resource: TenantResources.tenant, scopes: ['view']),
        ],
      );

      // Cai na listagem, que o `view` alcança — e lá o FAB não aparece.
      expect(find.byType(FloatingActionButton), findsNothing);
      expect(find.text('Cadastrar cliente'), findsNothing);
      expect(find.text('Clientes da plataforma'), findsOneWidget);
    });

    testWidgets('the cadastro opens for whoever may create', (tester) async {
      await pumpAt(
        tester,
        TenantRoutes.create,
        permissions: const [
          Permission(
            resource: TenantResources.tenant,
            scopes: ['view', 'create'],
          ),
        ],
      );

      expect(find.text('Cadastrar cliente'), findsWidgets);
    });

    testWidgets('a refresh before permissions load is not thrown out',
        (tester) async {
      // Na web o router abre direto na URL, sem passar pelo splash. Recusar
      // nesse instante expulsaria o operador a cada F5.
      await pumpAt(
        tester,
        TenantRoutes.list,
        permissionsLoaded: false,
      );

      expect(find.text('home-screen'), findsNothing);
      expect(find.text('Clientes da plataforma'), findsOneWidget);
    });

    testWidgets('the detail route is reachable with view permission',
        (tester) async {
      repository.setTenant(tenant());

      await pumpAt(
        tester,
        TenantRoutes.detail('tenant-1'),
        permissions: const [
          Permission(resource: TenantResources.tenant, scopes: ['view']),
        ],
      );

      expect(find.text('Pão Quente'), findsWidgets);
    });

    testWidgets('the selection screen never asks for permission',
        (tester) async {
      await pumpAt(tester, TenantRoutes.select);

      expect(find.text('Selecionar cliente'), findsOneWidget);
    });
  });
}
