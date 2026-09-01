import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/bills/bill_detail_screen.dart';
import 'package:bill_payment/src/ui/bills/bill_detail_viewmodel.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../fakes/fakes.dart';

/// Os quatro níveis de risco na tela de decisão, e a alçada de aprovação —
/// o espelho de BLP.BIL32/BIL27 do lado de quem aprova.
void main() {
  late FakeBillRepository repository;

  setUp(() => repository = FakeBillRepository());

  Future<void> pumpBill(
    WidgetTester tester, {
    String? riskLevel,
    required List<String> billScopes,
  }) async {
    // Retrato fresco: sem lastConsultedAt o botão desabilitaria por
    // "revalide antes", e o que está sob teste aqui é a ALÇADA.
    repository.detail = billDetail(
      riskLevel: riskLevel,
      lastConsultedAt: DateTime.now(),
    );
    final viewModel = BillDetailViewModel(
      repository: repository,
      billId: 'bill-1',
    );
    addTearDown(viewModel.dispose);
    final permissions = await billPaymentPermissions([
      Permission(resource: BillPaymentResources.bill, scopes: billScopes),
    ]);
    addTearDown(permissions.dispose);

    tester.view.physicalSize = const Size(800, 2400);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);
    await tester.pumpWidget(
      ChangeNotifierProvider<BillPaymentPermissionNotifier>.value(
        value: permissions,
        child: MaterialApp(
          home: BillDetailScreen(
            viewModel: viewModel,
            backFallback: '/bill-payment/bills',
            onOpenArtifact: () {},
            onOpenEmail: () {},
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  FilledButton approveButton(WidgetTester tester) => tester.widget<FilledButton>(
        find.widgetWithText(FilledButton, 'Aprovar…'),
      );

  group('banner de risco', () {
    testWidgets('extremo perigo tem banner próprio, mais duro que o Perigo',
        (tester) async {
      await pumpBill(
        tester,
        riskLevel: RiskLevels.extremeDanger,
        billScopes: const ['view', 'approve'],
      );

      expect(find.text('Extremo Perigo'), findsOneWidget);
      expect(find.textContaining('lista de bloqueio'), findsOneWidget);
    });

    // REGRESSÃO: nível desconhecido caía no default do switch e desenhava
    // "Seguro" — um servidor mais novo mentiria verde para o aprovador.
    testWidgets('nível desconhecido nunca desenha "Seguro"', (tester) async {
      await pumpBill(
        tester,
        riskLevel: 'CatastrophicDanger',
        billScopes: const ['view', 'approve'],
      );

      expect(find.text('Seguro'), findsNothing);
      expect(find.text('Nível de risco desconhecido'), findsOneWidget);
    });
  });

  group('alçada de aprovação', () {
    // Espelho do BLP.BIL32: sem o escopo do nível, o botão desabilita com o
    // motivo à vista — o servidor recusaria com 403 de qualquer jeito.
    testWidgets('boleto em Perigo desabilita o Aprovar de quem só tem approve',
        (tester) async {
      await pumpBill(
        tester,
        riskLevel: RiskLevels.danger,
        billScopes: const ['view', 'approve'],
      );

      expect(approveButton(tester).onPressed, isNull);
      // O motivo vive no Tooltip do botão — só aparece como texto ao segurar.
      expect(
        find.byWidgetPredicate(
          (w) => w is Tooltip && w.message!.contains('acima da sua alçada'),
        ),
        findsOneWidget,
      );
    });

    // A alçada é hierárquica: approve-danger cobre Perigo (e os níveis abaixo).
    testWidgets('com approve-danger o mesmo boleto habilita o Aprovar',
        (tester) async {
      await pumpBill(
        tester,
        riskLevel: RiskLevels.danger,
        billScopes: const ['view', 'approve', 'approve-danger'],
      );

      expect(approveButton(tester).onPressed, isNotNull);
    });

    // Extremo fica ACIMA de Perigo: approve-danger não basta.
    testWidgets('extremo perigo exige approve-extreme', (tester) async {
      await pumpBill(
        tester,
        riskLevel: RiskLevels.extremeDanger,
        billScopes: const ['view', 'approve', 'approve-danger'],
      );

      expect(approveButton(tester).onPressed, isNull);
    });

    // Com a alçada máxima, o Extremo abre a folha e exige o aceite nomeando
    // o nível — o botão Autorizar não habilita sem a caixa.
    testWidgets('o aceite do Extremo nomeia o nível e trava sem a caixa',
        (tester) async {
      await pumpBill(
        tester,
        riskLevel: RiskLevels.extremeDanger,
        billScopes: const ['view', 'approve', 'approve-extreme'],
      );

      await tester.tap(find.text('Aprovar…'));
      await tester.pumpAndSettle();

      expect(
        find.textContaining('Vi o alerta de Extremo Perigo'),
        findsOneWidget,
      );
      final authorize = tester.widget<FilledButton>(
        find.widgetWithText(FilledButton, 'Autorizar'),
      );
      expect(authorize.onPressed, isNull);
    });
  });
}
