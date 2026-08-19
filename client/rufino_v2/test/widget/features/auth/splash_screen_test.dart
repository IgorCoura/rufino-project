import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:bill_payment/bill_payment.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:rufino_v2/core/tenant/tenant_session_bridge.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/splash_viewmodel.dart';
import 'package:rufino_v2/ui/features/auth/widgets/splash_screen.dart';
import 'package:tenant_management/tenant_management.dart';

import '../../../testing/fakes/fake_auth_repository.dart';
import '../../../testing/fakes/fake_company_repository.dart';
import '../../../testing/fakes/fake_error_reporter.dart';
import '../../../testing/fakes/fake_permission_repository.dart';
import '../../../testing/fakes/fake_secure_storage.dart';
import '../../../testing/fakes/fake_tenant_repository.dart';

const _tenant = MyTenant(
  id: 'tenant-1',
  kind: TenantKinds.company,
  legalName: 'Padaria do Zé LTDA',
  tradeName: 'Pão Quente',
  status: TenantStatuses.active,
  role: MembershipRoles.owner,
  activeProducts: [TenantProducts.peopleManagement],
);

void main() {
  late FakeAuthRepository authRepository;
  late FakeCompanyRepository companyRepository;
  late FakeTenantRepository tenantRepository;
  late PermissionNotifier permissionNotifier;
  late TenantPermissionNotifier tenantPermissionNotifier;
  late BillPaymentPermissionNotifier billPaymentPermissionNotifier;
  late SplashViewModel viewModel;

  setUp(() {
    authRepository = FakeAuthRepository();
    companyRepository = FakeCompanyRepository();
    tenantRepository = FakeTenantRepository();
    permissionNotifier =
        PermissionNotifier(permissionRepository: FakePermissionRepository());
    tenantPermissionNotifier = TenantPermissionNotifier(
      permissionRepository: FakePermissionRepository(),
    );
    billPaymentPermissionNotifier = BillPaymentPermissionNotifier(
      permissionRepository: FakePermissionRepository(),
    );
    viewModel = SplashViewModel(
      authRepository: authRepository,
      tenantRepository: tenantRepository,
      tenantContext: TenantContextNotifier(storage: FakeSecureStorage()),
      tenantSessionBridge: TenantSessionBridge(
        companyRepository: companyRepository,
        permissionNotifier: permissionNotifier,
        tenantPermissionNotifier: tenantPermissionNotifier,
        billPaymentPermissionNotifier: billPaymentPermissionNotifier,
        errorReporter: FakeErrorReporter(),
      ),
      permissionNotifier: permissionNotifier,
      tenantPermissionNotifier: tenantPermissionNotifier,
      billPaymentPermissionNotifier: billPaymentPermissionNotifier,
      errorReporter: FakeErrorReporter(),
    );
  });

  Widget buildApp() {
    final router = GoRouter(
      initialLocation: '/',
      routes: [
        GoRoute(
          path: '/',
          builder: (_, __) => SplashScreen(viewModel: viewModel),
        ),
        GoRoute(
          path: '/login',
          builder: (_, __) => const Scaffold(body: Text('login-screen')),
        ),
        GoRoute(
          path: '/home',
          builder: (_, __) => const Scaffold(body: Text('home-screen')),
        ),
        GoRoute(
          path: TenantRoutes.select,
          builder: (_, __) => const Scaffold(body: Text('select-tenant')),
        ),
      ],
    );
    return MaterialApp.router(routerConfig: router);
  }

  group('SplashScreen', () {
    testWidgets(
        'navigates to the login screen when there are no valid credentials',
        (tester) async {
      authRepository.setAuthenticated(false);

      await tester.pumpWidget(buildApp());
      await tester.pumpAndSettle();

      expect(find.text('login-screen'), findsOne);
    });

    testWidgets(
        'navigates to the login screen when the credential check fails '
        'unexpectedly', (tester) async {
      authRepository.setThrowOnHasValidCredentials(true);

      await tester.pumpWidget(buildApp());
      await tester.pumpAndSettle();

      expect(find.text('login-screen'), findsOne);
    });

    testWidgets('goes straight home when the person has a single tenant',
        (tester) async {
      authRepository.setAuthenticated(true);
      tenantRepository.setMyTenants([_tenant]);

      await tester.pumpWidget(buildApp());
      await tester.pumpAndSettle();

      expect(find.text('home-screen'), findsOne);
    });

    testWidgets('opens the selection screen when there is more than one tenant',
        (tester) async {
      authRepository.setAuthenticated(true);
      tenantRepository.setMyTenants([
        _tenant,
        const MyTenant(
          id: 'tenant-2',
          kind: TenantKinds.individual,
          legalName: 'José da Silva',
          tradeName: '',
          status: TenantStatuses.active,
          role: MembershipRoles.member,
          activeProducts: [TenantProducts.billPayment],
        ),
      ]);

      await tester.pumpWidget(buildApp());
      await tester.pumpAndSettle();

      expect(find.text('select-tenant'), findsOne);
    });
  });
}
