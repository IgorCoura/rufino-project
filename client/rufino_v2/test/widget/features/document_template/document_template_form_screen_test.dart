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

    testWidgets('shows the renewal-limit switch once expiration is on',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Limitar renovações'), findsNothing);

      await tester
          .ensureVisible(find.byKey(const ValueKey('rule-switch-expiration')));
      await tester.tap(find.byKey(const ValueKey('rule-switch-expiration')));
      await tester.pumpAndSettle();

      expect(find.text('Limitar renovações'), findsOneWidget);
      expect(find.byKey(const ValueKey('rule-field-maxRenewals')), findsNothing);
    });

    testWidgets('reveals the renewal field when the limit is turned on',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester
          .ensureVisible(find.byKey(const ValueKey('rule-switch-expiration')));
      await tester.tap(find.byKey(const ValueKey('rule-switch-expiration')));
      await tester.pumpAndSettle();

      await tester
          .ensureVisible(find.byKey(const ValueKey('rule-switch-maxRenewals')));
      await tester.tap(find.byKey(const ValueKey('rule-switch-maxRenewals')));
      await tester.pumpAndSettle();

      expect(
          find.byKey(const ValueKey('rule-field-maxRenewals')), findsOneWidget);
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

    testWidgets('shows the signature switch', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Assinatura'), findsOneWidget);
    });

    testWidgets('hides the placements while the signature rule is off',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.byKey(const ValueKey('rule-field-signature')), findsNothing);
    });

    testWidgets('reveals the placements when the signature rule is turned on',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester
          .ensureVisible(find.byKey(const ValueKey('rule-switch-signature')));
      await tester.tap(find.byKey(const ValueKey('rule-switch-signature')));
      await tester.pumpAndSettle();

      expect(
          find.byKey(const ValueKey('rule-field-signature')), findsOneWidget);
      expect(find.text('Adicionar Assinatura'), findsOneWidget);
    });

    testWidgets('hides the placements again when the rule is turned off',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester
          .ensureVisible(find.byKey(const ValueKey('rule-switch-signature')));
      await tester.tap(find.byKey(const ValueKey('rule-switch-signature')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const ValueKey('rule-switch-signature')));
      await tester.pumpAndSettle();

      expect(find.byKey(const ValueKey('rule-field-signature')), findsNothing);
    });
  });

  group('DocumentTemplateFormScreen signature placements', () {
    // Router com pilha popável: o save de sucesso chama context.pop().
    Widget buildPoppableSubject() => MaterialApp.router(
          routerConfig: GoRouter(
            initialLocation: '/home',
            routes: [
              GoRoute(
                path: '/home',
                builder: (context, __) => Scaffold(
                  body: Center(
                    child: ElevatedButton(
                      onPressed: () => context.push('/form'),
                      child: const Text('ir'),
                    ),
                  ),
                ),
              ),
              GoRoute(
                path: '/form',
                builder: (_, __) =>
                    DocumentTemplateFormScreen(viewModel: viewModel),
              ),
            ],
          ),
        );

    Future<void> openForm(WidgetTester tester) async {
      await tester.pumpWidget(buildPoppableSubject());
      await tester.pumpAndSettle();
      await tester.tap(find.text('ir'));
      await tester.pumpAndSettle();
      viewModel.nameController.text = 'Novo Template';
      viewModel.descriptionController.text = 'Descrição do template';
    }

    Future<void> addFirstPlacement(WidgetTester tester) async {
      await tester
          .ensureVisible(find.byKey(const ValueKey('rule-switch-signature')));
      await tester.tap(find.byKey(const ValueKey('rule-switch-signature')));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.text('Adicionar Assinatura'));
      await tester.tap(find.text('Adicionar Assinatura'));
      await tester.pumpAndSettle();
    }

    Future<void> fill(WidgetTester tester, String label, String value) async {
      final field = find.widgetWithText(TextFormField, label);
      await tester.ensureVisible(field);
      await tester.enterText(field, value);
      await tester.pumpAndSettle();
    }

    Future<void> fillAllNumbers(WidgetTester tester) async {
      await fill(tester, 'Página', '1');
      await fill(tester, 'Pos. Inferior', '10');
      await fill(tester, 'Pos. Esquerda', '20');
      await fill(tester, 'Tam. Horizontal', '30');
      await fill(tester, 'Tam. Vertical', '40');
    }

    Future<void> tapSave(WidgetTester tester) async {
      await tester.ensureVisible(find.text('Salvar'));
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();
    }

    // Reprodução do relato: adicionar o primeiro local a uma lista vazia, com tipo
    // selecionado, e salvar. O local preenchido precisa chegar ao repositório.
    testWidgets('sends the first placement added to an empty list',
        (tester) async {
      await openForm(tester);
      await addFirstPlacement(tester);

      await tester.ensureVisible(find.text('Tipo de Assinatura *'));
      await tester.tap(find.text('Tipo de Assinatura *'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Visto').last);
      await tester.pumpAndSettle();

      await fillAllNumbers(tester);
      await tapSave(tester);

      expect(templateRepository.lastSentAcceptsSignature, isTrue);
      expect(templateRepository.lastSentPlaceSignatures, hasLength(1));
      final sent = templateRepository.lastSentPlaceSignatures!.first;
      expect(sent.typeSignatureId, '2');
      expect(sent.page, '1');
      expect(sent.positionBottom, '10');
      expect(sent.positionLeft, '20');
      expect(sent.sizeX, '30');
      expect(sent.sizeY, '40');
    });

    // A causa raiz do relato: sem o tipo selecionado, o app mandava type:0 e o
    // backend derrubava o save inteiro. Agora o tipo é obrigatório: o save é
    // bloqueado com erro inline e nada é enviado.
    testWidgets('blocks the save when the placement has no type', (tester) async {
      await openForm(tester);
      await addFirstPlacement(tester);
      await fillAllNumbers(tester); // números válidos, mas sem escolher o tipo
      await tapSave(tester);

      expect(find.text('Selecione o tipo de assinatura.'), findsOneWidget);
      expect(templateRepository.lastSentPlaceSignatures, isNull);
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
