import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:tenant_management/tenant_management.dart';

import '../fakes/fakes.dart';

void main() {
  late FakeTenantRepository repository;
  late List<String> opened;
  late List<String> navigations;

  setUp(() {
    repository = FakeTenantRepository();
    opened = [];
    navigations = [];
  });

  Future<void> pumpList(
    WidgetTester tester, {
    List<Permission> permissions = const [
      Permission(resource: TenantResources.tenant, scopes: ['view']),
    ],
  }) async {
    final notifier = await tenantPermissions(permissions);

    await tester.pumpWidget(
      ChangeNotifierProvider<TenantPermissionNotifier>.value(
        value: notifier,
        child: MaterialApp(
          home: TenantListScreen(
            viewModel: TenantListViewModel(repository: repository),
            backFallback: '/home',
            onOpenTenant: opened.add,
            onCreateTenant: () => navigations.add('create'),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('TenantListScreen', () {
    testWidgets('shows the customer, the document and the products',
        (tester) async {
      repository.setPage(TenantPage(items: [tenantSummary()]));

      await pumpList(tester);

      expect(find.text('Pão Quente'), findsOneWidget);
      expect(find.text('Padaria do Zé LTDA'), findsOneWidget);
      expect(find.text('11.222.333/0001-81'), findsOneWidget);
      // O rótulo também é um chip de filtro no topo: o que importa é que a
      // linha carrega o produto.
      expect(find.text('Gestão de Pessoas'), findsNWidgets(2));
    });

    testWidgets('a failed access grant is called out on the row',
        (tester) async {
      repository.setPage(
        TenantPage(
          items: [tenantSummary(provisioning: ProvisioningStatuses.failed)],
        ),
      );

      await pumpList(tester);

      expect(find.text('Acesso falhou'), findsOneWidget);
    });

    testWidgets('a pending grant is shown apart from a failed one',
        (tester) async {
      repository.setPage(
        TenantPage(
          items: [tenantSummary(provisioning: ProvisioningStatuses.pending)],
        ),
      );

      await pumpList(tester);

      expect(find.text('Acesso pendente'), findsOneWidget);
      expect(find.text('Acesso falhou'), findsNothing);
    });

    testWidgets('a suspended cadastro is flagged', (tester) async {
      repository.setPage(
        TenantPage(items: [tenantSummary(status: TenantStatuses.suspended)]),
      );

      await pumpList(tester);

      expect(find.text('Suspenso'), findsOneWidget);
    });

    testWidgets('quem pode criar cadastra a partir do back-office',
        (tester) async {
      repository.setPage(TenantPage(items: [tenantSummary()]));

      await pumpList(
        tester,
        permissions: const [
          Permission(
            resource: TenantResources.tenant,
            scopes: ['view', 'create'],
          ),
        ],
      );

      expect(find.text('Cadastrar cliente'), findsOneWidget);

      await tester.tap(find.text('Cadastrar cliente'));
      await tester.pumpAndSettle();

      expect(navigations, contains('create'));
    });

    testWidgets('sem permissão de criar não há como cadastrar', (tester) async {
      repository.setPage(TenantPage(items: [tenantSummary()]));

      await pumpList(tester);

      expect(find.byType(FloatingActionButton), findsNothing);
      expect(find.text('Cadastrar cliente'), findsNothing);
    });

    testWidgets('opening a row hands the id over', (tester) async {
      repository.setPage(TenantPage(items: [tenantSummary()]));

      await pumpList(tester);
      await tester.tap(find.text('Pão Quente'));
      await tester.pumpAndSettle();

      expect(opened, ['tenant-1']);
    });

    testWidgets('an empty result offers to clear the filters', (tester) async {
      await pumpList(tester);

      expect(find.text('Nenhum cliente encontrado.'), findsOneWidget);
      expect(find.text('Limpar filtros'), findsNothing);

      await tester.enterText(find.byType(TextField), 'padaria');
      await tester.testTextInput.receiveAction(TextInputAction.search);
      await tester.pumpAndSettle();

      expect(find.text('Limpar filtros'), findsWidgets);
    });

    testWidgets('a filter reaches the server', (tester) async {
      repository.setPage(TenantPage(items: [tenantSummary()]));

      await pumpList(tester);
      expect(repository.calls.where((c) => c == 'listTenants'), hasLength(1));

      await tester.tap(find.text('Suspensos'));
      await tester.pumpAndSettle();

      expect(repository.calls.where((c) => c == 'listTenants'), hasLength(2));
    });
  });
}
