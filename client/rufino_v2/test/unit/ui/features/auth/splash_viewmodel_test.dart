import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/splash_viewmodel.dart';

import '../../../../testing/fakes/fake_auth_repository.dart';
import '../../../../testing/fakes/fake_company_repository.dart';

void main() {
  late FakeAuthRepository authRepository;
  late FakeCompanyRepository companyRepository;
  late SplashViewModel viewModel;

  setUp(() {
    authRepository = FakeAuthRepository();
    companyRepository = FakeCompanyRepository();
    viewModel = SplashViewModel(
      authRepository: authRepository,
      companyRepository: companyRepository,
    );
  });

  tearDown(() => viewModel.dispose());

  group('SplashViewModel', () {
    test('initial status is loading', () {
      expect(viewModel.status, SplashStatus.loading);
    });

    test('goes to authenticated when credentials valid and company selected', () async {
      authRepository.setAuthenticated(true);
      authRepository.setCompanyIds(['company-1']);
      companyRepository.setVerifyResult(true);

      await viewModel.initialize();

      expect(viewModel.status, SplashStatus.authenticated);
    });

    test('goes to unauthenticated when no valid credentials', () async {
      authRepository.setAuthenticated(false);

      await viewModel.initialize();

      expect(viewModel.status, SplashStatus.unauthenticated);
    });

    test('goes to noCompany when credentials valid but no company selected', () async {
      authRepository.setAuthenticated(true);
      authRepository.setCompanyIds(['company-1']);
      companyRepository.setVerifyResult(false);

      await viewModel.initialize();

      expect(viewModel.status, SplashStatus.noCompany);
    });

    test('goes to error when verifyAndSelectCompany fails', () async {
      authRepository.setAuthenticated(true);
      authRepository.setCompanyIds(['company-1']);
      companyRepository.setVerifyError(true);

      await viewModel.initialize();

      expect(viewModel.status, SplashStatus.error);
    });

    test('goes to error when repository throws an unexpected exception',
        () async {
      authRepository.setThrowOnHasValidCredentials(true);

      await viewModel.initialize();

      expect(viewModel.status, SplashStatus.error);
    });

    test('initialize is idempotent after first completion', () async {
      authRepository.setAuthenticated(true);
      authRepository.setCompanyIds(['company-1']);
      companyRepository.setVerifyResult(true);

      await viewModel.initialize();
      expect(viewModel.status, SplashStatus.authenticated);

      // Second call should not reset status
      await viewModel.initialize();
      expect(viewModel.status, SplashStatus.authenticated);
    });
  });
}
