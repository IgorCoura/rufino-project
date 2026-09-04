import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:bill_payment/bill_payment.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:rufino_v2/core/tenant/tenant_session_bridge.dart';
import 'package:people_management/people_management.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/splash_viewmodel.dart';
import 'package:tenant_management/tenant_management.dart';

import '../../../../testing/fakes/fake_auth_repository.dart';
import '../../../../testing/fakes/fake_company_repository.dart';
import '../../../../testing/fakes/fake_error_reporter.dart';
import '../../../../testing/fakes/fake_permission_repository.dart';
import '../../../../testing/fakes/fake_secure_storage.dart';
import '../../../../testing/fakes/fake_tenant_repository.dart';

MyTenant _tenant({
  String id = 'tenant-1',
  String status = TenantStatuses.active,
  List<String> products = const [TenantProducts.peopleManagement],
}) {
  return MyTenant(
    id: id,
    kind: TenantKinds.company,
    legalName: 'Padaria do Zé LTDA',
    tradeName: 'Pão Quente',
    status: status,
    role: MembershipRoles.owner,
    activeProducts: products,
  );
}

void main() {
  WidgetsFlutterBinding.ensureInitialized();

  late FakeAuthRepository authRepository;
  late FakeCompanyRepository companyRepository;
  late FakeTenantRepository tenantRepository;
  late FakeSecureStorage storage;
  late TenantContextNotifier tenantContext;
  late PermissionNotifier permissionNotifier;
  late TenantPermissionNotifier tenantPermissionNotifier;
  late BillPaymentPermissionNotifier billPaymentPermissionNotifier;
  late TenantSessionBridge bridge;
  late SplashViewModel viewModel;

  SplashViewModel buildViewModel() {
    return SplashViewModel(
      authRepository: authRepository,
      tenantRepository: tenantRepository,
      tenantContext: tenantContext,
      tenantSessionBridge: bridge,
      permissionNotifier: permissionNotifier,
      tenantPermissionNotifier: tenantPermissionNotifier,
      billPaymentPermissionNotifier: billPaymentPermissionNotifier,
      errorReporter: FakeErrorReporter(),
    );
  }

  setUp(() {
    authRepository = FakeAuthRepository();
    companyRepository = FakeCompanyRepository();
    tenantRepository = FakeTenantRepository();
    storage = FakeSecureStorage();
    tenantContext = TenantContextNotifier(storage: storage);
    permissionNotifier = PermissionNotifier(
      permissionRepository: FakePermissionRepository(),
    );
    tenantPermissionNotifier = TenantPermissionNotifier(
      permissionRepository: FakePermissionRepository(),
    );
    billPaymentPermissionNotifier = BillPaymentPermissionNotifier(
      permissionRepository: FakePermissionRepository(),
    );
    bridge = TenantSessionBridge(
      companyRepository: companyRepository,
      permissionNotifier: permissionNotifier,
      tenantPermissionNotifier: tenantPermissionNotifier,
      billPaymentPermissionNotifier: billPaymentPermissionNotifier,
      errorReporter: FakeErrorReporter(),
    );
    viewModel = buildViewModel();
  });

  tearDown(() {
    viewModel.dispose();
    permissionNotifier.dispose();
    tenantPermissionNotifier.dispose();
    billPaymentPermissionNotifier.dispose();
  });

  group('SplashViewModel', () {
    test('starts undecided', () {
      expect(viewModel.status, SplashStatus.loading);
      expect(viewModel.destination, isNull);
    });

    test('sends an unauthenticated visitor to the login screen', () async {
      authRepository.setAuthenticated(false);

      await viewModel.initialize();

      expect(viewModel.destination, SplashDestination.login);
    });

    test('enters straight into the only tenant the person belongs to',
        () async {
      authRepository.setAuthenticated(true);
      tenantRepository.setMyTenants([_tenant()]);

      await viewModel.initialize();

      expect(viewModel.destination, SplashDestination.home);
      expect(tenantContext.tenantId, 'tenant-1');
    });

    test('asks the user to choose when there is more than one tenant',
        () async {
      authRepository.setAuthenticated(true);
      tenantRepository.setMyTenants([
        _tenant(),
        _tenant(id: 'tenant-2'),
      ]);

      await viewModel.initialize();

      expect(viewModel.destination, SplashDestination.selectTenant);
      expect(tenantContext.hasTenant, isFalse);
    });

    test('a single suspended tenant is not entered automatically', () async {
      authRepository.setAuthenticated(true);
      tenantRepository.setMyTenants([
        _tenant(status: TenantStatuses.suspended),
      ]);

      await viewModel.initialize();

      expect(viewModel.destination, SplashDestination.selectTenant);
      expect(tenantContext.hasTenant, isFalse);
    });

    test('someone with no tenant at all lands on the selection screen',
        () async {
      authRepository.setAuthenticated(true);
      tenantRepository.setMyTenants(const []);

      await viewModel.initialize();

      expect(viewModel.destination, SplashDestination.selectTenant);
    });

    test('reuses the tenant chosen in the previous session', () async {
      authRepository.setAuthenticated(true);
      await tenantContext.select(_tenant(id: 'tenant-2').toSelectedTenant());
      tenantRepository.setMyTenants([
        _tenant(),
        _tenant(id: 'tenant-2'),
      ]);

      await viewModel.initialize();

      expect(viewModel.destination, SplashDestination.home);
      expect(tenantContext.tenantId, 'tenant-2');
    });

    test('drops a stored tenant the person no longer has access to', () async {
      authRepository.setAuthenticated(true);
      await tenantContext.select(_tenant(id: 'gone').toSelectedTenant());
      tenantRepository.setMyTenants([
        _tenant(),
        _tenant(id: 'tenant-2'),
      ]);

      await viewModel.initialize();

      expect(viewModel.destination, SplashDestination.selectTenant);
      expect(tenantContext.hasTenant, isFalse);
    });

    test('drops a stored tenant that was suspended since the last session',
        () async {
      authRepository.setAuthenticated(true);
      await tenantContext.select(_tenant().toSelectedTenant());
      tenantRepository.setMyTenants([
        _tenant(status: TenantStatuses.suspended),
      ]);

      await viewModel.initialize();

      expect(viewModel.destination, SplashDestination.selectTenant);
      expect(tenantContext.hasTenant, isFalse);
    });

    test('keeps going with the stored tenant when the list cannot be loaded',
        () async {
      authRepository.setAuthenticated(true);
      await tenantContext.select(_tenant().toSelectedTenant());
      tenantRepository.setMyTenantsShouldFail(true);

      await viewModel.initialize();

      expect(viewModel.destination, SplashDestination.home);
    });

    test('a failed list with nothing stored falls back to the selection screen',
        () async {
      authRepository.setAuthenticated(true);
      tenantRepository.setMyTenantsShouldFail(true);

      await viewModel.initialize();

      expect(viewModel.destination, SplashDestination.selectTenant);
    });

    test('resolves the People Management company when entering a tenant',
        () async {
      authRepository.setAuthenticated(true);
      tenantRepository.setMyTenants([_tenant()]);

      await viewModel.initialize();

      expect(bridge.isPeopleManagementReady, isTrue);
      final selected = await companyRepository.getSelectedCompany();
      expect(selected.valueOrNull?.id, 'tenant-1');
    });

    test('a tenant without People Management leaves no company behind',
        () async {
      authRepository.setAuthenticated(true);
      companyRepository.setSelectedCompany(
        const Company(
          id: 'outro',
          corporateName: 'Outra',
          fantasyName: '',
          cnpj: '11222333000181',
        ),
      );
      tenantRepository.setMyTenants([
        _tenant(products: const [TenantProducts.billPayment]),
      ]);

      await viewModel.initialize();

      expect(bridge.isPeopleManagementReady, isFalse);
      expect(companyRepository.selectionCleared, isTrue);
    });

    test('People Management stays unavailable when no company answers',
        () async {
      authRepository.setAuthenticated(true);
      companyRepository.setDetailShouldFail(true);
      tenantRepository.setMyTenants([_tenant()]);

      await viewModel.initialize();

      expect(viewModel.destination, SplashDestination.home);
      expect(bridge.isPeopleManagementReady, isFalse);
    });

    test('decides only once', () async {
      authRepository.setAuthenticated(false);
      await viewModel.initialize();

      authRepository.setAuthenticated(true);
      tenantRepository.setMyTenants([_tenant()]);
      await viewModel.initialize();

      expect(viewModel.destination, SplashDestination.login);
    });
  });
}
