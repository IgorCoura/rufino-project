import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/permission.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/permission_notifier.dart';
import 'package:rufino_v2/ui/features/company/viewmodel/company_selection_viewmodel.dart';

import '../../../../testing/fakes/fake_auth_repository.dart';
import '../../../../testing/fakes/fake_company_repository.dart';
import '../../../../testing/fakes/fake_permission_repository.dart';

void main() {
  late FakeAuthRepository authRepository;
  late FakeCompanyRepository companyRepository;
  late FakePermissionRepository permissionRepository;
  late PermissionNotifier permissionNotifier;
  late CompanySelectionViewModel viewModel;

  const fakeCompany = Company(
    id: 'company-1',
    corporateName: 'Acme Corp S.A.',
    fantasyName: 'Acme',
    cnpj: '12.345.678/0001-90',
  );

  setUp(() {
    authRepository = FakeAuthRepository();
    companyRepository = FakeCompanyRepository();
    permissionRepository = FakePermissionRepository();
    permissionNotifier = PermissionNotifier(
      permissionRepository: permissionRepository,
    );
    viewModel = CompanySelectionViewModel(
      authRepository: authRepository,
      companyRepository: companyRepository,
      permissionNotifier: permissionNotifier,
    );
  });

  tearDown(() => viewModel.dispose());

  group('CompanySelectionViewModel', () {
    test('loadCompanies populates companies list', () async {
      companyRepository.setCompanies([fakeCompany]);

      await viewModel.loadCompanies();

      expect(viewModel.status, CompanySelectionStatus.loaded);
      expect(viewModel.companies, [fakeCompany]);
      expect(viewModel.selectedCompany, fakeCompany);
    });

    test('loadCompanies with empty list transitions to noCompanies', () async {
      companyRepository.setCompanies([]);

      await viewModel.loadCompanies();

      expect(viewModel.status, CompanySelectionStatus.noCompanies);
    });

    test('onCompanySelected updates selectedCompany', () async {
      companyRepository.setCompanies([fakeCompany]);
      await viewModel.loadCompanies();

      const other = Company(
        id: 'other',
        corporateName: 'Other',
        fantasyName: 'Other',
        cnpj: '00.000.000/0001-00',
      );
      viewModel.onCompanySelected(other);

      expect(viewModel.selectedCompany, other);
    });

    test('confirmSelection loads permissions and transitions to selected',
        () async {
      permissionRepository.setPermissions([
        const Permission(resource: 'employee', scopes: ['view', 'create']),
      ]);
      companyRepository.setCompanies([fakeCompany]);
      await viewModel.loadCompanies();

      await viewModel.confirmSelection();

      expect(viewModel.status, CompanySelectionStatus.selected);
      expect(permissionNotifier.status, PermissionStatus.loaded);
      expect(permissionNotifier.hasAnyScope('employee'), isTrue);
      expect(permissionNotifier.hasPermission('employee', 'view'), isTrue);
    });

    test('confirmSelection does not load permissions when selection fails',
        () async {
      companyRepository.setCompanies([fakeCompany]);
      await viewModel.loadCompanies();
      companyRepository.setSelectShouldFail(true);

      await viewModel.confirmSelection();

      expect(viewModel.status, CompanySelectionStatus.error);
      expect(permissionNotifier.status, PermissionStatus.loading);
    });
  });
}
