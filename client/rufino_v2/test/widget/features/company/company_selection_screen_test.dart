import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/ui/features/company/viewmodel/company_selection_viewmodel.dart';
import 'package:rufino_v2/ui/features/company/widgets/company_selection_screen.dart';

import '../../../testing/fakes/fake_auth_repository.dart';
import '../../../testing/fakes/fake_company_repository.dart';

void main() {
  late FakeAuthRepository fakeAuth;
  late FakeCompanyRepository fakeCompany;
  late CompanySelectionViewModel viewModel;

  const fakeCompanyEntity = Company(
    id: 'company-1',
    corporateName: 'Acme Corp S.A.',
    fantasyName: 'Acme',
    cnpj: '12.345.678/0001-90',
  );

  setUp(() {
    fakeAuth = FakeAuthRepository();
    fakeCompany = FakeCompanyRepository();
    fakeCompany.setCompanies([fakeCompanyEntity]);
    viewModel = CompanySelectionViewModel(
      authRepository: fakeAuth,
      companyRepository: fakeCompany,
    );
  });

  tearDown(() => viewModel.dispose());

  Widget buildSubject() => MaterialApp.router(
        routerConfig: GoRouter(
          routes: [
            GoRoute(
              path: '/',
              builder: (_, __) => CompanySelectionScreen(viewModel: viewModel),
            ),
          ],
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
