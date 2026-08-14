import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:rufino_v2/core/tenant/tenant_session_bridge.dart';
import 'package:rufino_v2/ui/features/home/viewmodel/home_viewmodel.dart';
import 'package:rufino_v2/ui/features/home/widgets/home_screen.dart';
import 'package:tenant_management/tenant_management.dart';

import '../../../testing/fakes/fake_auth_repository.dart';
import '../../../testing/fakes/fake_company_repository.dart';
import '../../../testing/fakes/fake_error_reporter.dart';
import '../../../testing/fakes/fake_permission_repository.dart';
import '../../../testing/fakes/fake_secure_storage.dart';

SelectedTenant _tenant({
  List<String> products = const [TenantProducts.peopleManagement],
}) {
  return SelectedTenant(
    id: 'tenant-1',
    kind: TenantKinds.company,
    legalName: 'Padaria do Zé LTDA',
    tradeName: 'Pão Quente',
    status: TenantStatuses.active,
    role: MembershipRoles.owner,
    activeProducts: products,
  );
}

void main() {
  late FakeCompanyRepository companyRepository;
  late TenantContextNotifier tenantContext;
  late TenantSessionBridge bridge;
  late PermissionNotifier permissionNotifier;
  late TenantPermissionNotifier tenantPermissionNotifier;

  setUp(() {
    companyRepository = FakeCompanyRepository();
    tenantContext = TenantContextNotifier(storage: FakeSecureStorage());
  });

  Future<void> pumpHome(
    WidgetTester tester, {
    required SelectedTenant tenant,
    List<Permission> permissions = const [],
    List<Permission> tenantPermissions = const [],
    bool companyResolves = true,
  }) async {
    companyRepository.setDetailShouldFail(!companyResolves);

    final permissionRepo = FakePermissionRepository()
      ..setPermissions(permissions);
    permissionNotifier =
        PermissionNotifier(permissionRepository: permissionRepo);
    await permissionNotifier.loadPermissions();

    final tenantPermissionRepo = FakePermissionRepository()
      ..setPermissions(tenantPermissions);
    tenantPermissionNotifier =
        TenantPermissionNotifier(permissionRepository: tenantPermissionRepo);
    await tenantPermissionNotifier.loadPermissions();

    bridge = TenantSessionBridge(
      companyRepository: companyRepository,
      permissionNotifier: permissionNotifier,
      tenantPermissionNotifier: tenantPermissionNotifier,
      errorReporter: FakeErrorReporter(),
    );

    await tenantContext.select(tenant);
    await bridge.syncPeopleManagementCompany(tenant);

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<PermissionNotifier>.value(
            value: permissionNotifier,
          ),
          ChangeNotifierProvider<TenantPermissionNotifier>.value(
            value: tenantPermissionNotifier,
          ),
          ChangeNotifierProvider<TenantContextNotifier>.value(
            value: tenantContext,
          ),
          ChangeNotifierProvider<ThemeNotifier>(create: (_) => ThemeNotifier()),
        ],
        child: MaterialApp(
          home: HomeScreen(
            viewModel: HomeViewModel(
              authRepository: FakeAuthRepository(),
              tenantContext: tenantContext,
              tenantSessionBridge: bridge,
              permissionNotifier: permissionNotifier,
              tenantPermissionNotifier: tenantPermissionNotifier,
              errorReporter: FakeErrorReporter(),
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('HomeScreen', () {
    testWidgets('shows the selected customer in the app bar', (tester) async {
      await pumpHome(
        tester,
        tenant: _tenant(),
        permissions: const [
          Permission(resource: 'employee', scopes: ['view']),
        ],
      );

      expect(find.text('Pão Quente'), findsOneWidget);
      expect(find.text('Pessoa jurídica'), findsOneWidget);
    });

    testWidgets('a feature needs the product enabled AND the permission',
        (tester) async {
      await pumpHome(
        tester,
        tenant: _tenant(),
        permissions: const [
          Permission(resource: 'employee', scopes: ['view']),
        ],
      );

      expect(find.text('GESTÃO DE PESSOAS'), findsOneWidget);
      expect(find.text('Funcionários'), findsOneWidget);
      // Permissão que a pessoa não tem: o card não existe.
      expect(find.text('Setores'), findsNothing);
    });

    testWidgets('permission without the product shows nothing', (tester) async {
      await pumpHome(
        tester,
        tenant: _tenant(products: const [TenantProducts.billPayment]),
        permissions: const [
          Permission(resource: 'employee', scopes: ['view']),
        ],
      );

      expect(find.text('GESTÃO DE PESSOAS'), findsNothing);
      expect(find.text('Funcionários'), findsNothing);
    });

    testWidgets('a group with no visible entry does not render its header',
        (tester) async {
      await pumpHome(tester, tenant: _tenant());

      expect(find.text('GESTÃO DE PESSOAS'), findsNothing);
      expect(find.text('ADMINISTRAÇÃO DA PLATAFORMA'), findsNothing);
      expect(
        find.text('Nenhuma funcionalidade disponível para este cliente.'),
        findsOneWidget,
      );
    });

    testWidgets('the back-office answers to the tenant audience, not the '
        'product one', (tester) async {
      await pumpHome(
        tester,
        tenant: _tenant(),
        // Mesmo nome de recurso na audiência errada não abre nada.
        permissions: const [
          Permission(resource: TenantResources.tenant, scopes: ['view']),
        ],
      );

      expect(find.text('ADMINISTRAÇÃO DA PLATAFORMA'), findsNothing);

      await pumpHome(
        tester,
        tenant: _tenant(),
        tenantPermissions: const [
          Permission(resource: TenantResources.tenant, scopes: ['view']),
        ],
      );

      expect(find.text('ADMINISTRAÇÃO DA PLATAFORMA'), findsOneWidget);
      expect(find.text('Clientes'), findsOneWidget);
    });

    testWidgets('says People Management is not released when no company '
        'answers for the tenant', (tester) async {
      await pumpHome(
        tester,
        tenant: _tenant(),
        companyResolves: false,
        permissions: const [
          Permission(resource: 'employee', scopes: ['view']),
        ],
      );

      expect(
        find.text(
          'Gestão de Pessoas ainda não está liberada para este cliente.',
        ),
        findsOneWidget,
      );
      expect(find.text('Funcionários'), findsNothing);
    });
  });
}
