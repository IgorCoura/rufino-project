import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/document_template.dart';
import 'package:rufino_v2/ui/features/document_template/viewmodel/document_template_form_viewmodel.dart';
import 'package:rufino_v2/ui/features/document_template/widgets/document_template_form_screen.dart';

import '../../../testing/fakes/fake_company_repository.dart';
import '../../../testing/fakes/fake_document_template_repository.dart';

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeDocumentTemplateRepository templateRepository;
  late DocumentTemplateFormViewModel viewModel;

  const fakeCompany = Company(
    id: 'company-1',
    corporateName: 'Acme Corp',
    fantasyName: 'Acme',
    cnpj: '00000000000000',
  );

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(fakeCompany);
    templateRepository = FakeDocumentTemplateRepository();
    viewModel = DocumentTemplateFormViewModel(
      companyRepository: companyRepository,
      documentTemplateRepository: templateRepository,
    );
  });

  tearDown(() => viewModel.dispose());

  Widget buildSubject() => MaterialApp.router(
        routerConfig: GoRouter(
          routes: [
            GoRoute(
              path: '/',
              builder: (_, __) =>
                  DocumentTemplateFormScreen(viewModel: viewModel),
            ),
          ],
        ),
      );

  group('DocumentTemplateFormScreen rules section', () {
    testWidgets('shows a switch for each rule', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Regras'), findsOneWidget);
      expect(find.text('Vencimento'), findsOneWidget);
      expect(find.text('Carga horária'), findsOneWidget);
    });

    testWidgets('hides the duration field while the rule is off',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.byKey(const ValueKey('rule-field-expiration')), findsNothing);
    });

    testWidgets('reveals the duration field when the rule is turned on',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.ensureVisible(
          find.byKey(const ValueKey('rule-switch-expiration')));
      await tester.tap(find.byKey(const ValueKey('rule-switch-expiration')));
      await tester.pumpAndSettle();

      expect(find.byKey(const ValueKey('rule-field-expiration')),
          findsOneWidget);
    });

    testWidgets('shows the legacy field as not applicable while the rule is off',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Não se aplica'), findsNWidgets(2));
    });

    // The legacy field has no input of its own: it must follow whatever the
    // rule says, which is what "somente visualização" means here.
    testWidgets('mirrors the typed duration into the legacy field',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.ensureVisible(
          find.byKey(const ValueKey('rule-switch-expiration')));
      await tester.tap(find.byKey(const ValueKey('rule-switch-expiration')));
      await tester.pumpAndSettle();
      await tester.enterText(
          find.byKey(const ValueKey('rule-field-expiration')), '365');
      await tester.pumpAndSettle();

      expect(find.text('365'), findsNWidgets(2));
    });

    testWidgets('the legacy field is read-only and points to the rules section',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Definido em Regras'), findsNWidgets(2));
    });

    testWidgets('shows the competência switch', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Competência'), findsOneWidget);
    });

    testWidgets('hides the granularity dropdown while the rule is off',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.byKey(const ValueKey('rule-field-period')), findsNothing);
    });

    testWidgets('reveals the granularity dropdown when the rule is turned on',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester
          .ensureVisible(find.byKey(const ValueKey('rule-switch-period')));
      await tester.tap(find.byKey(const ValueKey('rule-switch-period')));
      await tester.pumpAndSettle();

      expect(find.byKey(const ValueKey('rule-field-period')), findsOneWidget);
    });
  });

  group('DocumentTemplateFormScreen rules loaded from a template', () {
    testWidgets('turns the switches on for the rules the template carries',
        (tester) async {
      templateRepository.setTemplate(const DocumentTemplate(
        id: 'tpl-1',
        name: 'NR01',
        description: 'Descrição',
        policies: TemplatePolicies(
          expiration: ExpirationRule(durationInDays: 365),
        ),
        acceptsSignature: false,
      ));

      await tester.pumpWidget(MaterialApp.router(
        routerConfig: GoRouter(
          routes: [
            GoRoute(
              path: '/',
              builder: (_, __) => DocumentTemplateFormScreen(
                viewModel: viewModel,
                templateId: 'tpl-1',
              ),
            ),
          ],
        ),
      ));
      await tester.pumpAndSettle();

      expect(viewModel.expirationEnabled, isTrue);
      expect(viewModel.workloadEnabled, isFalse);
      expect(find.byKey(const ValueKey('rule-field-expiration')),
          findsOneWidget);
      expect(find.byKey(const ValueKey('rule-field-workload')), findsNothing);
    });
  });
}
