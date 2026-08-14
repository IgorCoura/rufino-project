import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:tenant_management/tenant_management.dart';

import '../fakes/fakes.dart';

const _adminPermissions = [
  Permission(
    resource: TenantResources.tenant,
    scopes: ['view', 'create', 'edit', 'suspend'],
  ),
  Permission(
    resource: TenantResources.tenantAccess,
    scopes: ['view', 'edit'],
  ),
  Permission(
    resource: TenantResources.tenantProduct,
    scopes: ['view', 'edit'],
  ),
];

const _supportPermissions = [
  Permission(resource: TenantResources.tenant, scopes: ['view']),
  Permission(resource: TenantResources.tenantAccess, scopes: ['view']),
  Permission(resource: TenantResources.tenantProduct, scopes: ['view']),
];

void main() {
  late FakeTenantRepository repository;

  setUp(() => repository = FakeTenantRepository());

  Future<void> pumpDetail(
    WidgetTester tester, {
    List<Permission> permissions = _adminPermissions,
  }) async {
    final notifier = await tenantPermissions(permissions);

    await tester.pumpWidget(
      ChangeNotifierProvider<TenantPermissionNotifier>.value(
        value: notifier,
        child: MaterialApp(
          home: TenantDetailScreen(
            backFallback: '/tenant',
            viewModel: TenantDetailViewModel(
              repository: repository,
              tenantId: 'tenant-1',
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  Future<void> openTab(WidgetTester tester, String label) async {
    await tester.tap(find.text(label));
    await tester.pumpAndSettle();
  }

  group('TenantDetailScreen — cadastro', () {
    testWidgets('shows the cadastro in read mode', (tester) async {
      repository.setTenant(tenant());

      await pumpDetail(tester);

      expect(find.text('Padaria do Zé LTDA'), findsOneWidget);
      expect(find.text('Pão Quente'), findsWidgets);
      // Cabeçalho e bloco de identificação mostram o documento.
      expect(find.text('11.222.333/0001-81'), findsNWidgets(2));
      expect(find.text('contato@paoquente.com.br'), findsOneWidget);
      expect(find.text('(31) 99999-0000'), findsOneWidget);
      expect(find.text('Editar'), findsWidgets);
    });

    testWidgets('editing swaps the block in place, without leaving the screen',
        (tester) async {
      repository.setTenant(tenant());

      await pumpDetail(tester);
      await tester.tap(find.text('Editar').first);
      await tester.pumpAndSettle();

      expect(find.text('Salvar'), findsOneWidget);
      expect(find.text('Cancelar'), findsOneWidget);
      expect(find.byType(TextFormField), findsWidgets);
      // Continua na mesma tela: nada abriu por cima, nada trocou de rota.
      expect(find.text('Acessos'), findsOneWidget);
    });

    testWidgets('cancelling discards the draft without calling the server',
        (tester) async {
      repository.setTenant(tenant());

      await pumpDetail(tester);
      await tester.tap(find.text('Editar').first);
      await tester.pumpAndSettle();

      await tester.enterText(find.byType(TextFormField).first, 'Outro nome');
      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();

      expect(repository.calls, isEmpty);
      expect(find.text('Padaria do Zé LTDA'), findsOneWidget);
    });

    testWidgets('saving one block calls only its own endpoint',
        (tester) async {
      repository.setTenant(tenant());

      await pumpDetail(tester);
      await tester.tap(find.text('Editar').first);
      await tester.pumpAndSettle();

      await tester.enterText(
        find.byType(TextFormField).first,
        'Padaria do Zé ME',
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(repository.calls, ['editDetails']);
      expect(find.text('Identificação atualizada.'), findsOneWidget);
    });

    testWidgets('a refused rule keeps the block open with the message',
        (tester) async {
      repository.setTenant(tenant());
      repository.setWriteShouldFail(
        true,
        message: 'Documento já cadastrado para outro cliente.',
      );

      await pumpDetail(tester);
      await tester.tap(find.text('Editar').first);
      await tester.pumpAndSettle();

      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(
        find.text('Documento já cadastrado para outro cliente.'),
        findsOneWidget,
      );
      // Segue em edição: o rascunho não se perde por causa da recusa.
      expect(find.text('Salvar'), findsOneWidget);
    });

    testWidgets('an individual has no trade name to edit', (tester) async {
      repository.setTenant(
        tenant(
          kind: TenantKinds.individual,
          legalName: 'José da Silva',
          tradeName: '',
          taxId: '52998224725',
        ),
      );

      await pumpDetail(tester);

      expect(find.text('Nome completo'), findsOneWidget);
      expect(find.text('Nome fantasia'), findsNothing);
      expect(find.text('529.982.247-25'), findsNWidgets(2));
    });

    testWidgets('a suspended cadastro disables editing and says why',
        (tester) async {
      repository.setTenant(
        tenant(
          status: TenantStatuses.suspended,
          suspensionReason: 'Inadimplência',
        ),
      );

      await pumpDetail(tester);

      expect(
        find.textContaining('Cliente suspenso — alterações bloqueadas'),
        findsOneWidget,
      );
      expect(find.textContaining('Inadimplência'), findsOneWidget);

      // Desabilitado, não escondido: a causa é o estado do cadastro, não
      // falta de permissão — some sem explicação seria esconder a razão.
      expect(find.text('Editar'), findsWidgets);

      await tester.tap(find.text('Editar').first);
      await tester.pumpAndSettle();

      expect(find.text('Salvar'), findsNothing);
    });

    testWidgets('read-only support sees no edit affordance at all',
        (tester) async {
      repository.setTenant(tenant());

      await pumpDetail(tester, permissions: _supportPermissions);

      expect(find.text('Editar'), findsNothing);
    });
  });

  group('TenantDetailScreen — acessos', () {
    testWidgets('the last responsible person cannot be revoked',
        (tester) async {
      repository.setTenant(tenant());

      await pumpDetail(tester);
      await openTab(tester, 'Acessos');

      expect(find.text('dono@paoquente.com.br'), findsOneWidget);
      expect(find.text('Revogar'), findsNothing);
      expect(
        find.text('Último responsável — não pode perder o acesso.'),
        findsOneWidget,
      );
    });

    testWidgets('a second responsible person makes revoking possible',
        (tester) async {
      repository.setTenant(
        tenant(
          memberships: const [
            TenantMembership(
              email: 'dono@paoquente.com.br',
              role: MembershipRoles.owner,
              isActive: true,
              provisioning: ProvisioningStatuses.done,
            ),
            TenantMembership(
              email: 'socio@paoquente.com.br',
              role: MembershipRoles.owner,
              isActive: true,
              provisioning: ProvisioningStatuses.done,
            ),
          ],
        ),
      );

      await pumpDetail(tester);
      await openTab(tester, 'Acessos');

      expect(find.text('Revogar'), findsNWidgets(2));
    });

    testWidgets('granting access expands inline instead of opening a dialog',
        (tester) async {
      repository.setTenant(tenant());

      await pumpDetail(tester);
      await openTab(tester, 'Acessos');
      await tester.tap(find.text('Conceder acesso'));
      await tester.pumpAndSettle();

      expect(find.byType(Dialog), findsNothing);
      expect(find.text('Papel'), findsOneWidget);

      await tester.enterText(
        find.byType(TextFormField).first,
        'novo@paoquente.com.br',
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(repository.calls, ['grantMembership:novo@paoquente.com.br']);
    });

    testWidgets('a failed provisioning offers to send it again',
        (tester) async {
      repository.setTenant(
        tenant(
          provisioning: ProvisioningStatuses.failed,
          memberships: const [
            TenantMembership(
              email: 'dono@paoquente.com.br',
              role: MembershipRoles.owner,
              isActive: true,
              provisioning: ProvisioningStatuses.failed,
            ),
          ],
        ),
      );

      await pumpDetail(tester);
      await openTab(tester, 'Acessos');

      expect(
        find.text('O acesso não chegou ao provedor de identidade.'),
        findsOneWidget,
      );

      await tester.tap(find.text('Reenviar acessos'));
      await tester.pumpAndSettle();

      expect(repository.calls, ['reprovisionAccess']);
    });

    testWidgets('support cannot grant or revoke anything', (tester) async {
      repository.setTenant(tenant());

      await pumpDetail(tester, permissions: _supportPermissions);
      await openTab(tester, 'Acessos');

      expect(find.text('Conceder acesso'), findsNothing);
      expect(find.text('Revogar'), findsNothing);
    });
  });

  group('TenantDetailScreen — produtos', () {
    testWidgets('shows every product with its history', (tester) async {
      repository.setTenant(tenant());

      await pumpDetail(tester);
      await openTab(tester, 'Produtos');

      expect(find.text('Gestão de Pessoas'), findsOneWidget);
      expect(find.text('Contas a Pagar'), findsOneWidget);
      expect(find.text('Habilitado em 01/01/2026'), findsOneWidget);
      expect(find.text('Nunca habilitado'), findsOneWidget);
    });

    testWidgets('turning a product on reaches the server', (tester) async {
      repository.setTenant(tenant());

      await pumpDetail(tester);
      await openTab(tester, 'Produtos');
      await tester.tap(find.byType(Switch).last);
      await tester.pumpAndSettle();

      expect(
        repository.calls,
        ['activateProduct:${TenantProducts.billPayment}'],
      );
    });

    testWidgets('without permission the state is text, not a dead switch',
        (tester) async {
      repository.setTenant(tenant());

      await pumpDetail(tester, permissions: _supportPermissions);
      await openTab(tester, 'Produtos');

      expect(find.byType(Switch), findsNothing);
      expect(find.text('Habilitado'), findsOneWidget);
      expect(find.text('Desabilitado'), findsOneWidget);
    });
  });
}
