import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/permission_notifier.dart';
import 'package:rufino_v2/ui/features/home/viewmodel/home_viewmodel.dart';

import '../../../../testing/fakes/fake_auth_repository.dart';
import '../../../../testing/fakes/fake_company_repository.dart';
import '../../../../testing/fakes/fake_permission_repository.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  late FakeAuthRepository authRepository;
  late FakeCompanyRepository companyRepository;
  late PermissionNotifier permissionNotifier;
  late HomeViewModel viewModel;

  setUp(() {
    authRepository = FakeAuthRepository();
    companyRepository = FakeCompanyRepository();
    permissionNotifier = PermissionNotifier(
      permissionRepository: FakePermissionRepository(),
    );
    viewModel = HomeViewModel(
      authRepository: authRepository,
      companyRepository: companyRepository,
      permissionNotifier: permissionNotifier,
    );
  });

  tearDown(() => viewModel.dispose());

  group('HomeViewModel', () {
    test('loadCompany sets company on success', () async {
      const expected = Company(
        id: 'company-1',
        corporateName: 'Acme Corp S.A.',
        fantasyName: 'Acme',
        cnpj: '12.345.678/0001-90',
      );
      companyRepository.setSelectedCompany(expected);

      await viewModel.loadCompany();

      expect(viewModel.status, HomeStatus.loaded);
      expect(viewModel.company?.id, expected.id);
    });

    test('loadCompany sets error when no company selected', () async {
      companyRepository.setSelectedCompany(null);

      await viewModel.loadCompany();

      expect(viewModel.status, HomeStatus.error);
      expect(viewModel.errorMessage, isNotNull);
    });
  });
}
