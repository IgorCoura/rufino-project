import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:tenant_management/tenant_management.dart';

import '../fakes/fakes.dart';

void main() {
  late FakeTenantRepository repository;
  late TenantContextNotifier tenantContext;
  late List<String> navigations;

  setUp(() {
    repository = FakeTenantRepository();
    tenantContext = TenantContextNotifier(storage: FakeSecureStorage());
    navigations = [];
  });

  Future<void> pumpScreen(
    WidgetTester tester, {
    List<Permission> permissions = const [],
  }) async {
    final notifier = await tenantPermissions(permissions);

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<TenantPermissionNotifier>.value(
            value: notifier,
          ),
          ChangeNotifierProvider<TenantContextNotifier>.value(
            value: tenantContext,
          ),
        ],
        child: MaterialApp(
          home: TenantSelectionScreen(
            backFallback: '/home',
            viewModel: TenantSelectionViewModel(
              repository: repository,
              tenantContext: tenantContext,
              onTenantSelected: (_) async {},
            ),
            onSelected: () => navigations.add('home'),
            onOpenBackOffice: () => navigations.add('back-office'),
            onLogout: () async => navigations.add('logout'),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('TenantSelectionScreen', () {
    testWidgets('lists the tenants the person belongs to', (tester) async {
      repository.setMyTenants([
        myTenant(),
        myTenant(
          id: 'tenant-2',
          legalName: 'José da Silva',
          tradeName: '',
          kind: TenantKinds.individual,
          role: MembershipRoles.member,
          products: const [TenantProducts.billPayment],
        ),
      ]);

      await pumpScreen(tester);

      expect(find.text('Pão Quente'), findsOneWidget);
      expect(find.text('José da Silva'), findsOneWidget);
      expect(find.text('Responsável'), findsOneWidget);
      expect(find.text('Membro'), findsOneWidget);
      expect(find.text('Gestão de Pessoas'), findsOneWidget);
      expect(find.text('Contas a Pagar'), findsOneWidget);
    });

    testWidgets('entering a tenant makes it the app context', (tester) async {
      repository.setMyTenants([myTenant(), myTenant(id: 'tenant-2')]);

      await pumpScreen(tester);
      await tester.tap(find.text('Pão Quente').first);
      await tester.pumpAndSettle();

      expect(tenantContext.tenantId, 'tenant-1');
      expect(navigations, contains('home'));
    });

    testWidgets('a suspended tenant is shown but cannot be entered',
        (tester) async {
      repository.setMyTenants([
        myTenant(
          id: 'suspenso',
          legalName: 'Mercado Central ME',
          tradeName: '',
          status: TenantStatuses.suspended,
        ),
      ]);

      await pumpScreen(tester);
      expect(find.text('Suspenso'), findsOneWidget);

      await tester.tap(find.text('Mercado Central ME'));
      await tester.pumpAndSettle();

      expect(tenantContext.hasTenant, isFalse);
      expect(navigations, isEmpty);
    });

    testWidgets('without permission there is no door to the back-office',
        (tester) async {
      repository.setMyTenants([myTenant()]);

      await pumpScreen(tester);

      expect(find.text('Clientes'), findsNothing);
    });

    testWidgets('an operator sees the door to the back-office', (tester) async {
      repository.setMyTenants(const []);

      await pumpScreen(
        tester,
        permissions: const [
          Permission(
            resource: TenantResources.tenant,
            scopes: ['view', 'create'],
          ),
        ],
      );

      expect(find.text('Clientes'), findsOneWidget);

      await tester.tap(find.text('Clientes'));
      await tester.pumpAndSettle();
      expect(navigations, contains('back-office'));
    });

    testWidgets('the cadastro is never offered here, not even to who may create',
        (tester) async {
      // O cadastro é trabalho de back-office: nasce em `/tenant`, não no
      // seletor. Ver a porta é o máximo que esta tela oferece.
      repository.setMyTenants(const []);

      await pumpScreen(
        tester,
        permissions: const [
          Permission(
            resource: TenantResources.tenant,
            scopes: ['view', 'create'],
          ),
        ],
      );

      expect(find.text('Cadastrar cliente'), findsNothing);
      expect(find.byType(FloatingActionButton), findsNothing);
    });

    testWidgets('read-only support reaches the back-office', (tester) async {
      repository.setMyTenants(const []);

      await pumpScreen(
        tester,
        permissions: const [
          Permission(resource: TenantResources.tenant, scopes: ['view']),
        ],
      );

      expect(find.text('Clientes'), findsOneWidget);
      expect(find.text('Cadastrar cliente'), findsNothing);
    });

    testWidgets('someone with no tenant and no permission is told so, with a '
        'way out', (tester) async {
      repository.setMyTenants(const []);

      await pumpScreen(tester);

      expect(
        find.text('Você ainda não faz parte de nenhum cliente.'),
        findsOneWidget,
      );
      expect(find.text('Sair'), findsOneWidget);

      await tester.tap(find.text('Sair'));
      await tester.pumpAndSettle();
      expect(navigations, contains('logout'));
    });

    testWidgets('a failed load offers to try again', (tester) async {
      repository.setMyTenantsShouldFail(true);

      await pumpScreen(tester);

      expect(find.text('Tentar novamente'), findsOneWidget);
    });
  });
}
