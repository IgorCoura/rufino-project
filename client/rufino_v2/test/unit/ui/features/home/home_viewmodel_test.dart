import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:bill_payment/bill_payment.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:rufino_v2/core/tenant/tenant_session_bridge.dart';
import 'package:rufino_v2/ui/features/home/viewmodel/home_viewmodel.dart';
import 'package:tenant_management/tenant_management.dart';

import '../../../../testing/fakes/fake_auth_repository.dart';
import '../../../../testing/fakes/fake_company_repository.dart';
import '../../../../testing/fakes/fake_error_reporter.dart';
import '../../../../testing/fakes/fake_permission_repository.dart';
import '../../../../testing/fakes/fake_secure_storage.dart';

const _tenant = SelectedTenant(
  id: 'tenant-1',
  kind: TenantKinds.company,
  legalName: 'Padaria do Zé LTDA',
  tradeName: 'Pão Quente',
  status: TenantStatuses.active,
  role: MembershipRoles.owner,
  activeProducts: [TenantProducts.peopleManagement],
);

void main() {
  WidgetsFlutterBinding.ensureInitialized();

  late FakeAuthRepository authRepository;
  late FakeCompanyRepository companyRepository;
  late TenantContextNotifier tenantContext;
  late PermissionNotifier permissionNotifier;
  late TenantPermissionNotifier tenantPermissionNotifier;
  late BillPaymentPermissionNotifier billPaymentPermissionNotifier;
  late TenantSessionBridge bridge;
  late HomeViewModel viewModel;

  setUp(() {
    authRepository = FakeAuthRepository();
    companyRepository = FakeCompanyRepository();
    tenantContext = TenantContextNotifier(storage: FakeSecureStorage());
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
    viewModel = HomeViewModel(
      authRepository: authRepository,
      tenantContext: tenantContext,
      tenantSessionBridge: bridge,
      permissionNotifier: permissionNotifier,
      tenantPermissionNotifier: tenantPermissionNotifier,
      billPaymentPermissionNotifier: billPaymentPermissionNotifier,
      errorReporter: FakeErrorReporter(),
    );
  });

  tearDown(() {
    viewModel.dispose();
    permissionNotifier.dispose();
    tenantPermissionNotifier.dispose();
    billPaymentPermissionNotifier.dispose();
  });

  group('HomeViewModel', () {
    test('has nothing to show while no tenant is in context', () {
      expect(viewModel.status, HomeStatus.noTenant);
      expect(viewModel.tenantDisplayName, 'Rufino');
      expect(viewModel.tenantSubtitle, isEmpty);
    });

    test('shows the selected tenant in the app bar', () async {
      await tenantContext.select(_tenant);

      expect(viewModel.status, HomeStatus.loaded);
      expect(viewModel.tenantDisplayName, 'Pão Quente');
      expect(viewModel.tenantSubtitle, 'Pessoa jurídica');
    });

    test('says out loud when the customer is suspended', () async {
      await tenantContext.select(
        const SelectedTenant(
          id: 'tenant-1',
          kind: TenantKinds.individual,
          legalName: 'José da Silva',
          tradeName: '',
          status: TenantStatuses.suspended,
          role: MembershipRoles.owner,
          activeProducts: [],
        ),
      );

      expect(viewModel.tenantSubtitle, 'Pessoa física · Suspenso');
    });

    test('flags People Management as pending when no company answers',
        () async {
      await tenantContext.select(_tenant);
      companyRepository.setDetailShouldFail(true);
      await bridge.syncPeopleManagementCompany(_tenant);

      expect(viewModel.isPeopleManagementReady, isFalse);
      expect(viewModel.isPeopleManagementPending, isTrue);
    });

    test('nothing is pending when the product itself is off', () async {
      const withoutPm = SelectedTenant(
        id: 'tenant-1',
        kind: TenantKinds.company,
        legalName: 'Padaria do Zé LTDA',
        tradeName: '',
        status: TenantStatuses.active,
        role: MembershipRoles.owner,
        activeProducts: [TenantProducts.billPayment],
      );
      await tenantContext.select(withoutPm);
      await bridge.syncPeopleManagementCompany(withoutPm);

      expect(viewModel.isPeopleManagementPending, isFalse);
    });

    test('People Management is ready once its company resolves', () async {
      await tenantContext.select(_tenant);
      await bridge.syncPeopleManagementCompany(_tenant);

      expect(viewModel.isPeopleManagementReady, isTrue);
      expect(viewModel.isPeopleManagementPending, isFalse);
    });

    test('logging out leaves no context behind', () async {
      await tenantContext.select(_tenant);
      await bridge.syncPeopleManagementCompany(_tenant);

      await viewModel.logout();

      expect(tenantContext.hasTenant, isFalse);
      expect(companyRepository.selectionCleared, isTrue);
      expect(bridge.isPeopleManagementReady, isFalse);
    });
  });
}
