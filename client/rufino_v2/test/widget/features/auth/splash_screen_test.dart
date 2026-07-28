import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/permission_notifier.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/splash_viewmodel.dart';
import 'package:rufino_v2/ui/features/auth/widgets/splash_screen.dart';

import '../../../testing/fakes/fake_auth_repository.dart';
import '../../../testing/fakes/fake_company_repository.dart';
import '../../../testing/fakes/fake_error_reporter.dart';
import '../../../testing/fakes/fake_permission_repository.dart';

void main() {
  late FakeAuthRepository authRepository;
  late FakeCompanyRepository companyRepository;
  late PermissionNotifier permissionNotifier;
  late SplashViewModel viewModel;

  setUp(() {
    authRepository = FakeAuthRepository();
    companyRepository = FakeCompanyRepository();
    permissionNotifier =
        PermissionNotifier(permissionRepository: FakePermissionRepository());
    viewModel = SplashViewModel(
      authRepository: authRepository,
      companyRepository: companyRepository,
      permissionNotifier: permissionNotifier,
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
          path: '/company',
          builder: (_, __) => const Scaffold(body: Text('company-screen')),
        ),
      ],
    );
    return MaterialApp.router(routerConfig: router);
  }

  group('SplashScreen', () {
    testWidgets('navigates to the login screen when there are no valid credentials',
        (tester) async {
      authRepository.setAuthenticated(false);

      await tester.pumpWidget(buildApp());
      await tester.pumpAndSettle();

      expect(find.text('login-screen'), findsOne);
    });

    testWidgets(
        'navigates to the login screen when the credential check fails unexpectedly',
        (tester) async {
      authRepository.setThrowOnHasValidCredentials(true);

      await tester.pumpWidget(buildApp());
      await tester.pumpAndSettle();

      expect(find.text('login-screen'), findsOne);
    });

    testWidgets(
        'navigates to the home screen when credentials are valid and a company is selected',
        (tester) async {
      authRepository.setAuthenticated(true);
      authRepository.setCompanyIds(['company-1']);
      companyRepository.setVerifyResult(true);

      await tester.pumpWidget(buildApp());
      await tester.pumpAndSettle();

      expect(find.text('home-screen'), findsOne);
    });
  });
}
