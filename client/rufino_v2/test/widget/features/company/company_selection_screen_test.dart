import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/permission.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/permission_notifier.dart';
import 'package:rufino_v2/ui/features/company/viewmodel/company_selection_viewmodel.dart';
import 'package:rufino_v2/ui/features/company/widgets/company_selection_screen.dart';

import '../../../testing/fakes/fake_auth_repository.dart';
import '../../../testing/fakes/fake_company_repository.dart';
import '../../../testing/fakes/fake_error_reporter.dart';
import '../../../testing/fakes/fake_permission_repository.dart';

void main() {
  late FakeAuthRepository fakeAuth;
  late FakeCompanyRepository fakeCompany;
  late PermissionNotifier permissionNotifier;
  late FakeErrorReporter fakeErrorReporter;
  late CompanySelectionViewModel viewModel;

  const fakeCompanyEntity = Company(
    id: 'company-1',
    corporateName: 'Acme Corp S.A.',
    fantasyName: 'Acme',
    cnpj: '12.345.678/0001-90',
  );

  setUp(() async {
    fakeAuth = FakeAuthRepository();
    fakeCompany = FakeCompanyRepository();
    fakeCompany.setCompanies([fakeCompanyEntity]);
    final fakePermRepo = FakePermissionRepository()
      ..setPermissions(const [
        Permission(resource: 'company', scopes: ['create', 'view', 'edit']),
      ]);
    permissionNotifier = PermissionNotifier(permissionRepository: fakePermRepo);
    await permissionNotifier.loadPermissions();
    fakeErrorReporter = FakeErrorReporter();
    viewModel = CompanySelectionViewModel(
      authRepository: fakeAuth,
      companyRepository: fakeCompany,
      permissionNotifier: permissionNotifier,
      errorReporter: fakeErrorReporter,
    );
  });

  tearDown(() {
    viewModel.dispose();
    permissionNotifier.dispose();
  });

  Widget buildSubject() => ChangeNotifierProvider<PermissionNotifier>.value(
        value: permissionNotifier,
        child: MaterialApp.router(
          routerConfig: GoRouter(
            routes: [
              GoRoute(
                path: '/',
                builder: (_, __) =>
                    CompanySelectionScreen(viewModel: viewModel),
              ),
              GoRoute(
                path: '/login',
                builder: (_, __) =>
                    const Scaffold(body: Text('login-screen')),
              ),
            ],
          ),
        ),
      );

  group('CompanySelectionScreen', () {
    testWidgets('shows company name in dropdown after loading', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Acme'), findsAtLeastNWidgets(1));
    });

    testWidgets('shows FAB to create company', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Criar Empresa'), findsOneWidget);
    });

    testWidgets('logout button signs the user out and navigates to /login',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.byTooltip('Sair'));
      await tester.pumpAndSettle();

      expect(find.text('login-screen'), findsOneWidget);
      expect(fakeErrorReporter.userCleared, isTrue);
      expect(permissionNotifier.status, PermissionStatus.loading);
    });
  });
}
