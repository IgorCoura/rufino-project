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
import '../../../testing/fakes/fake_permission_repository.dart';

void main() {
  late FakeAuthRepository fakeAuth;
  late FakeCompanyRepository fakeCompany;
  late PermissionNotifier permissionNotifier;
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
    viewModel = CompanySelectionViewModel(
      authRepository: fakeAuth,
      companyRepository: fakeCompany,
      permissionNotifier: permissionNotifier,
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
  });
}
