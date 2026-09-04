import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:tenant_management/tenant_management.dart';

import '../fakes/fakes.dart';

const _adminPermissions = [
  Permission(
    resource: TenantResources.tenant,
    scopes: ['view', 'create', 'edit', 'suspend'],
  ),
  Permission(resource: TenantResources.tenantAccess, scopes: ['view', 'edit']),
  Permission(resource: TenantResources.tenantProduct, scopes: ['view', 'edit']),
];

void main() {
  late FakeTenantRepository repository;
  late TenantContextNotifier tenantContext;

  setUp(() {
    repository = FakeTenantRepository()
      ..setMyTenants([myTenant(), myTenant(id: 'tenant-2')])
      ..setPage(TenantPage(items: [tenantSummary()]))
      ..setTenant(tenant());
    tenantContext = TenantContextNotifier(storage: FakeSecureStorage());
  });

  Future<void> pumpApp(WidgetTester tester, {required String at}) async {
    final notifier = await tenantPermissions(_adminPermissions);

    final router = GoRouter(
      initialLocation: at,
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

  group('Voltar', () {
    // REGRESSÃO: as telas eram alcançadas por `go` a partir do menu do Home e
    // ficavam sem saída — `pop` cru não tem para onde voltar.
    testWidgets('o back-office alcançado por substituição volta para o Home',
        (tester) async {
      await pumpApp(tester, at: TenantRoutes.list);
      expect(find.text('Clientes da plataforma'), findsOneWidget);

      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();

      expect(find.text('home-screen'), findsOneWidget);
    });

    testWidgets('o detalhe alcançado por link direto volta para a listagem',
        (tester) async {
      await pumpApp(tester, at: TenantRoutes.detail('tenant-1'));

      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();

      expect(find.text('Clientes da plataforma'), findsOneWidget);
    });

    testWidgets('o cadastro volta para a listagem', (tester) async {
      await pumpApp(tester, at: TenantRoutes.create);

      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();

      expect(find.text('Clientes da plataforma'), findsOneWidget);
    });

    testWidgets('o seletor não oferece voltar quando não há contexto nenhum',
        (tester) async {
      await pumpApp(tester, at: TenantRoutes.select);

      expect(find.text('Selecionar cliente'), findsOneWidget);
      // Sem cliente escolhido não existe tela anterior — a saída é o Sair.
      expect(find.byIcon(Icons.arrow_back), findsNothing);
      expect(find.byIcon(Icons.logout), findsOneWidget);
    });

    testWidgets('com um cliente já em contexto, o seletor volta para o Home',
        (tester) async {
      await tenantContext.select(myTenant().toSelectedTenant());

      await pumpApp(tester, at: TenantRoutes.select);
      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();

      expect(find.text('home-screen'), findsOneWidget);
    });
  });

  group('Voltar não deixa a tela anterior girando', () {
    // REGRESSÃO: o ViewModel era criado dentro do builder da rota. Ao voltar,
    // o go_router reexecutava o builder e criava um ViewModel novo em estado
    // `loading`; o `State` da tela sobrevivia ao rebuild, então o `initState`
    // que dispara o carregamento não rodava de novo — e a tela ficava com o
    // spinner para sempre.
    testWidgets('voltar do cadastro para a listagem mostra as linhas de novo',
        (tester) async {
      await pumpApp(tester, at: TenantRoutes.list);
      expect(find.text('Pão Quente'), findsOneWidget);

      await tester.tap(find.text('Cadastrar cliente'));
      await tester.pumpAndSettle();
      // Seção que só existe no formulário — o botão de enviar fica fora da
      // janela padrão de teste, porque o cadastro é longo.
      expect(find.text('Tipo de cliente'), findsOneWidget);

      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();

      expect(find.text('Clientes da plataforma'), findsOneWidget);
      expect(find.text('Pão Quente'), findsOneWidget);
      expect(find.byType(CircularProgressIndicator), findsNothing);
    });

    testWidgets('voltar do detalhe para a listagem mostra as linhas de novo',
        (tester) async {
      await pumpApp(tester, at: TenantRoutes.list);
      expect(find.text('Pão Quente'), findsOneWidget);

      await tester.tap(find.text('Pão Quente'));
      await tester.pumpAndSettle();
      expect(find.text('Cadastro'), findsOneWidget);

      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();

      expect(find.text('Clientes da plataforma'), findsOneWidget);
      expect(find.text('Pão Quente'), findsOneWidget);
      expect(find.byType(CircularProgressIndicator), findsNothing);
    });

    testWidgets('a listagem não recarrega do servidor ao voltar do detalhe',
        (tester) async {
      await pumpApp(tester, at: TenantRoutes.list);
      final afterFirstLoad =
          repository.calls.where((c) => c == 'listTenants').length;

      await tester.tap(find.text('Pão Quente'));
      await tester.pumpAndSettle();
      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();

      // Mesmo ViewModel, mesma pagina: o estado sobrevive a ida e volta.
      expect(
        repository.calls.where((c) => c == 'listTenants').length,
        afterFirstLoad,
      );
    });
  });
}
