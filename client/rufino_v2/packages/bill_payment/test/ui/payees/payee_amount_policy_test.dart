import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/payees/payee_detail_screen.dart';
import 'package:bill_payment/src/ui/payees/payee_detail_viewmodel.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../fakes/fakes.dart';

/// A política de valor na tela de detalhe: ler tudo que o usuário informou, e
/// poder mudar.
///
/// O sintoma que motivou estes testes: a tela mostrava uma linha só — "Faixa de
/// valores", sem número nenhum — e não havia como editar. Os dois primeiros
/// casos são testes de regressão desse relato.
void main() {
  late FakePayeeRepository repository;
  late PayeeDetailViewModel viewModel;

  setUp(() {
    repository = FakePayeeRepository();
    viewModel = PayeeDetailViewModel(repository: repository, payeeId: 'payee-1');
  });

  tearDown(() => viewModel.dispose());

  Future<BillPaymentPermissionNotifier> managerAsync() =>
      billPaymentPermissions(const [
        Permission(
          resource: BillPaymentResources.payee,
          scopes: [BillPaymentScopes.view, BillPaymentScopes.manage],
        ),
      ]);

  Future<void> pumpScreen(
    WidgetTester tester,
    BillPaymentPermissionNotifier permissions,
  ) async {
    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<BillPaymentPermissionNotifier>.value(
            value: permissions,
          ),
        ],
        child: MaterialApp(
          home: PayeeDetailScreen(
            viewModel: viewModel,
            backFallback: '/home',
            onDeleted: () {},
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('Leitura da política de valor', () {
    // REGRESSÃO: valor fixo mostrava só o valor. A tolerância é o que decide se
    // o boleto do mês passa, e ficava invisível.
    testWidgets('valor fixo mostra o valor, a tolerância e a janela aceita',
        (tester) async {
      repository.payees = [
        payee(
          amountPolicy: const AmountPolicy(
            kind: AmountPolicyKinds.fixed,
            isConclusive: true,
            expectedAmount: 1500,
            tolerancePercent: 5,
          ),
        ),
      ];
      final permissions = await managerAsync();

      await pumpScreen(tester, permissions);

      expect(find.text('Valor esperado'), findsOneWidget);
      expect(find.textContaining('1.500,00'), findsWidgets);
      expect(find.text('Tolerância'), findsOneWidget);
      // A janela derivada: 1500 ± 5% = 1.425,00 a 1.575,00.
      expect(find.textContaining('±5%'), findsOneWidget);
      expect(find.textContaining('1.425,00'), findsOneWidget);
      expect(find.textContaining('1.575,00'), findsOneWidget);

      permissions.dispose();
    });

    // REGRESSÃO: faixa de preço mostrava a frase "Faixa de valores" e mais
    // nada — nem o mínimo nem o máximo que o usuário havia informado.
    testWidgets('faixa mostra o valor mínimo e o máximo', (tester) async {
      repository.payees = [
        payee(
          amountPolicy: const AmountPolicy(
            kind: AmountPolicyKinds.range,
            isConclusive: true,
            minAmount: 80,
            maxAmount: 400,
          ),
        ),
      ];
      final permissions = await managerAsync();

      await pumpScreen(tester, permissions);

      expect(find.text('Valor mínimo'), findsOneWidget);
      expect(find.text('Valor máximo'), findsOneWidget);
      expect(find.textContaining('80,00'), findsOneWidget);
      expect(find.textContaining('400,00'), findsOneWidget);

      permissions.dispose();
    });

    // Tolerância zero é válida no domínio, e "±0%" não diz o que ela significa.
    testWidgets('tolerância zero é escrita por extenso, não como ±0%',
        (tester) async {
      repository.payees = [
        payee(
          amountPolicy: const AmountPolicy(
            kind: AmountPolicyKinds.fixed,
            isConclusive: true,
            expectedAmount: 1500,
            tolerancePercent: 0,
          ),
        ),
      ];
      final permissions = await managerAsync();

      await pumpScreen(tester, permissions);

      expect(find.textContaining('exato'), findsOneWidget);
      expect(find.textContaining('±0%'), findsNothing);

      permissions.dispose();
    });

    // `isConclusive` vinha do servidor e não era mostrado em lugar nenhum:
    // política sem limite enfraquece a verificação de valor em silêncio.
    testWidgets('sem limite avisa que a verificação fica inconclusiva',
        (tester) async {
      repository.payees = [payee()];
      final permissions = await managerAsync();

      await pumpScreen(tester, permissions);

      expect(find.textContaining('inconclusiva'), findsOneWidget);

      permissions.dispose();
    });
  });

  group('Edição da política de valor', () {
    testWidgets('o lápis abre o editor já preenchido com o que está valendo',
        (tester) async {
      repository.payees = [
        payee(
          amountPolicy: const AmountPolicy(
            kind: AmountPolicyKinds.fixed,
            isConclusive: true,
            expectedAmount: 1500,
            tolerancePercent: 5,
          ),
        ),
      ];
      final permissions = await managerAsync();

      await pumpScreen(tester, permissions);
      await tester.tap(find.byTooltip('Editar política de valor'));
      await tester.pumpAndSettle();

      expect(find.widgetWithText(TextFormField, '1500,0'), findsOneWidget);
      expect(find.widgetWithText(TextFormField, '5,0'), findsOneWidget);

      permissions.dispose();
    });

    // TESTE-ÂNCORA: trocar o tipo e salvar manda o tipo novo e SÓ os campos
    // dele. Mandar min/max junto de um valor fixo descreveria uma política que
    // não existe.
    testWidgets('trocar de fixo para faixa envia só os campos da faixa',
        (tester) async {
      repository.payees = [
        payee(
          amountPolicy: const AmountPolicy(
            kind: AmountPolicyKinds.fixed,
            isConclusive: true,
            expectedAmount: 1500,
            tolerancePercent: 5,
          ),
        ),
      ];
      final permissions = await managerAsync();

      await pumpScreen(tester, permissions);
      await tester.tap(find.byTooltip('Editar política de valor'));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Faixa'));
      await tester.pumpAndSettle();

      await tester.enterText(
        find.widgetWithText(TextFormField, 'Valor mínimo (R\$)'),
        '80',
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Valor máximo (R\$)'),
        '400',
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(repository.calls, contains('changeAmountPolicy:Range'));

      final sent = repository.lastPolicy!;
      expect(sent.kind, AmountPolicyKinds.range);
      expect(sent.minAmount, 80);
      expect(sent.maxAmount, 400);
      expect(sent.expectedAmount, isNull);
      expect(sent.tolerancePercent, isNull);

      permissions.dispose();
    });

    // A tolerância é obrigatória no valor fixo — AmountPolicy.From a exige
    // (BLP.PYE07). Em branco, a recusa tem que ser no campo, não no servidor.
    testWidgets('valor fixo sem tolerância é barrado antes de sair da tela',
        (tester) async {
      repository.payees = [payee()];
      final permissions = await managerAsync();

      await pumpScreen(tester, permissions);
      await tester.tap(find.byTooltip('Editar política de valor'));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Fixo'));
      await tester.pumpAndSettle();

      await tester.enterText(
        find.widgetWithText(TextFormField, 'Valor esperado (R\$)'),
        '1500',
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(find.text('Informe o valor.'), findsOneWidget);
      expect(repository.calls, isEmpty);

      permissions.dispose();
    });

    testWidgets('Cancelar fecha o editor e não escreve nada', (tester) async {
      repository.payees = [payee()];
      final permissions = await managerAsync();

      await pumpScreen(tester, permissions);
      await tester.tap(find.byTooltip('Editar política de valor'));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();

      expect(find.text('Salvar'), findsNothing);
      expect(repository.calls, isEmpty);

      permissions.dispose();
    });

    // Quem só lê não vê o lápis — o elemento some, nunca fica desabilitado.
    testWidgets('o lápis não aparece para quem só tem view', (tester) async {
      repository.payees = [payee()];
      final permissions = await billPaymentPermissions(const [
        Permission(
          resource: BillPaymentResources.payee,
          scopes: [BillPaymentScopes.view],
        ),
      ]);

      await pumpScreen(tester, permissions);

      expect(find.byTooltip('Editar política de valor'), findsNothing);

      permissions.dispose();
    });
  });
}
