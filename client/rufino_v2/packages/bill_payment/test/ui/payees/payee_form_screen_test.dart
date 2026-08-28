import 'package:bill_payment/src/ui/payees/payee_form_screen.dart';
import 'package:bill_payment/src/ui/payees/payee_form_viewmodel.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../fakes/fakes.dart';

/// O campo do documento, localizado pelo rótulo que a pessoa lê.
Finder taxIdField() => find.widgetWithText(TextFormField, 'CPF ou CNPJ');

void main() {
  late FakePayeeRepository repository;
  late PayeeFormViewModel viewModel;

  setUp(() {
    repository = FakePayeeRepository();
    viewModel = PayeeFormViewModel(repository: repository);
  });

  tearDown(() => viewModel.dispose());

  Future<void> pumpForm(WidgetTester tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: PayeeFormScreen(
          viewModel: viewModel,
          backFallback: '/bill-payment/payees',
          onRegistered: (_) {},
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('PayeeFormScreen', () {
    // Teste de regressão. Bug relatado em 2026-08-19: o campo nascia com a
    // máscara de CPF, de 11 posições, e o formatador engolia a 12ª tecla — a
    // tela recusava CNPJ e não era possível cadastrar empresa nenhuma.
    testWidgets('accepts a CNPJ and formats it as one', (tester) async {
      await pumpForm(tester);

      await tester.enterText(taxIdField(), '11222333000181');
      await tester.pump();

      expect(find.text('11.222.333/0001-81'), findsOneWidget);
    });

    testWidgets('still formats a CPF as a CPF', (tester) async {
      await pumpForm(tester);

      await tester.enterText(taxIdField(), '12345678901');
      await tester.pump();

      expect(find.text('123.456.789-01'), findsOneWidget);
    });

    testWidgets('registers a company with the document intact',
        (tester) async {
      await pumpForm(tester);

      await tester.enterText(
        find.widgetWithText(TextFormField, 'Razão social'),
        'EDP SAO PAULO SA',
      );
      await tester.enterText(taxIdField(), '11222333000181');
      await tester.tap(find.text('Cadastrar'));
      await tester.pumpAndSettle();

      expect(repository.calls, contains('registerPayee:EDP SAO PAULO SA'));
      expect(repository.lastRegisteredTaxId, '11.222.333/0001-81');
    });

    // O domínio exige tolerância no valor fixo (AmountPolicy.From, BLP.PYE07).
    // O formulário a tratava como opcional, e o cadastro voltava do servidor
    // com "valor obrigatório" sem dizer qual campo faltava.
    testWidgets('valor fixo sem tolerancia e barrado no formulario',
        (tester) async {
      await pumpForm(tester);

      await tester.enterText(
        find.widgetWithText(TextFormField, 'Razão social'),
        'EDP SAO PAULO SA',
      );
      await tester.enterText(taxIdField(), '11222333000181');
      await tester.tap(find.text('Fixo'));
      await tester.pumpAndSettle();
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Valor esperado (R\$)'),
        '1500',
      );
      await tester.tap(find.text('Cadastrar'));
      await tester.pumpAndSettle();

      expect(find.text('Informe o valor.'), findsOneWidget);
      expect(repository.calls, isEmpty);
    });

    // CONTRAPROVA: com a tolerância preenchida o cadastro passa. Sem ela, o
    // teste acima passaria mesmo se o formulário barrasse tudo.
    testWidgets('valor fixo com tolerancia preenchida e aceito',
        (tester) async {
      await pumpForm(tester);

      await tester.enterText(
        find.widgetWithText(TextFormField, 'Razão social'),
        'EDP SAO PAULO SA',
      );
      await tester.enterText(taxIdField(), '11222333000181');
      await tester.tap(find.text('Fixo'));
      await tester.pumpAndSettle();
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Valor esperado (R\$)'),
        '1500',
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Tolerância (%)'),
        '0',
      );
      await tester.tap(find.text('Cadastrar'));
      await tester.pumpAndSettle();

      expect(repository.calls, contains('registerPayee:EDP SAO PAULO SA'));
    });

    // O validador continua exigindo documento completo — a máscara maior não
    // pode ter virado permissão para meio CNPJ.
    testWidgets('refuses an incomplete document', (tester) async {
      await pumpForm(tester);

      await tester.enterText(
        find.widgetWithText(TextFormField, 'Razão social'),
        'EDP SAO PAULO SA',
      );
      await tester.enterText(taxIdField(), '112223330001');
      await tester.tap(find.text('Cadastrar'));
      await tester.pumpAndSettle();

      expect(find.text('Informe um CPF (11) ou CNPJ (14 dígitos).'),
          findsOneWidget);
      expect(repository.calls, isEmpty);
    });
  });
}
