import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/bills/bill_detail_screen.dart';
import 'package:bill_payment/src/ui/bills/bill_detail_viewmodel.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../fakes/fakes.dart';

/// A seção "Execução do pagamento" do detalhe (fase 3): a janela do outbox,
/// as ações por status/retenção sob guard, o deslize da data efetiva, o
/// reabrir do falhado e o aceite do vencido na folha de aprovar.
void main() {
  late FakeBillRepository repository;
  late FakePaymentRepository payments;

  setUp(() {
    repository = FakeBillRepository();
    payments = FakePaymentRepository();
  });

  Future<void> pumpDetail(
    WidgetTester tester, {
    List<String> billScopes = const ['view', 'approve', 'cancel'],
    VoidCallback? onOpenReceipt,
  }) async {
    final viewModel = BillDetailViewModel(
      repository: repository,
      paymentRepository: payments,
      billId: 'bill-1',
    );
    addTearDown(viewModel.dispose);
    final permissions = await billPaymentPermissions([
      Permission(resource: BillPaymentResources.bill, scopes: billScopes),
    ]);
    addTearDown(permissions.dispose);

    tester.view.physicalSize = const Size(800, 3200);
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
            onOpenReceipt: onOpenReceipt,
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('outbox window', () {
    testWidgets('an approved bill without an order says it is processing, '
        'not an error', (tester) async {
      repository.detail = billDetail(status: BillStatuses.approved);
      payments.order = null;

      await pumpDetail(tester);

      expect(find.text('Agendamento em processamento…'), findsOneWidget);
    });

    // Falha ao ler o pagamento NUNCA derruba o detalhe: o boleto está na
    // tela de qualquer jeito, e a seção fica na janela de processamento.
    testWidgets('a payment read failure never breaks the loaded detail',
        (tester) async {
      repository.detail = billDetail(status: BillStatuses.scheduled);
      payments.setShouldFail(true);

      await pumpDetail(tester);

      expect(find.text('Resumo'), findsOneWidget);
      expect(find.text('Agendamento em processamento…'), findsOneWidget);
    });
  });

  group('cancel action', () {
    testWidgets('a pending order in the reaction window offers the cancel '
        'under the bill:cancel guard', (tester) async {
      repository.detail = billDetail(status: BillStatuses.scheduled);
      payments.order = paymentOrder(status: PaymentOrderStatuses.pending);

      await pumpDetail(tester);

      expect(find.text('Cancelar agendamento'), findsOneWidget);
    });

    testWidgets('without the cancel scope the button does not exist',
        (tester) async {
      repository.detail = billDetail(status: BillStatuses.scheduled);
      payments.order = paymentOrder(status: PaymentOrderStatuses.pending);

      await pumpDetail(tester, billScopes: const ['view', 'approve']);

      expect(find.text('Cancelar agendamento'), findsNothing);
    });

    // Depois do desfecho não há o que cancelar — o botão some, não desabilita.
    testWidgets('a paid order offers no cancel even with the scope',
        (tester) async {
      repository.detail = billDetail(status: BillStatuses.paid);
      payments.order = paymentOrder(status: PaymentOrderStatuses.paid);

      await pumpDetail(tester);

      expect(find.text('Cancelar agendamento'), findsNothing);
    });

    testWidgets('confirming the dialog cancels the order', (tester) async {
      repository.detail = billDetail(status: BillStatuses.scheduled);
      payments.order = paymentOrder(status: PaymentOrderStatuses.pending);

      await pumpDetail(tester);
      await tester.ensureVisible(find.text('Cancelar agendamento'));
      await tester.tap(find.text('Cancelar agendamento'));
      await tester.pumpAndSettle();

      expect(find.text('Cancelar o agendamento?'), findsOneWidget);

      await tester.tap(
        find.widgetWithText(FilledButton, 'Cancelar agendamento'),
      );
      await tester.pumpAndSettle();

      expect(payments.calls, contains('cancel:order-1'));
    });
  });

  group('immediate confirmation hold', () {
    testWidgets('an order awaiting confirmation shows the confirm button and '
        'delegates on dialog confirmation', (tester) async {
      repository.detail = billDetail(status: BillStatuses.approved);
      payments.order = paymentOrder(
        status: PaymentOrderStatuses.draft,
        hold: PaymentOrderHolds.awaitingConfirmation,
        requiresConfirmation: true,
      );

      await pumpDetail(tester);

      expect(find.text('Confirmar pagamento imediato'), findsOneWidget);

      await tester.ensureVisible(find.text('Confirmar pagamento imediato'));
      await tester.tap(find.text('Confirmar pagamento imediato'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Pagar agora'));
      await tester.pumpAndSettle();

      expect(payments.calls, contains('confirmImmediate:order-1'));
    });

    testWidgets('an unheld order shows no confirm button', (tester) async {
      repository.detail = billDetail(status: BillStatuses.scheduled);
      payments.order = paymentOrder(status: PaymentOrderStatuses.pending);

      await pumpDetail(tester);

      expect(find.text('Confirmar pagamento imediato'), findsNothing);
    });
  });

  group('effective date', () {
    testWidgets('a slid effective date is labelled as such', (tester) async {
      repository.detail = billDetail(status: BillStatuses.scheduled);
      payments.order = paymentOrder(
        requestedScheduleDate: DateTime(2026, 9, 10),
        effectiveScheduleDate: DateTime(2026, 9, 11),
      );

      await pumpDetail(tester);

      expect(find.text('Data efetiva (deslizou)'), findsOneWidget);
    });

    testWidgets('an honoured date is labelled plainly', (tester) async {
      repository.detail = billDetail(status: BillStatuses.scheduled);
      payments.order = paymentOrder(
        requestedScheduleDate: DateTime(2026, 9, 10),
        effectiveScheduleDate: DateTime(2026, 9, 10),
      );

      await pumpDetail(tester);

      expect(find.text('Data efetiva'), findsOneWidget);
      expect(find.text('Data efetiva (deslizou)'), findsNothing);
    });
  });

  group('receipt button', () {
    testWidgets('an order with a receipt offers the viewer when the route '
        'exists', (tester) async {
      repository.detail = billDetail(status: BillStatuses.paid);
      payments.order = paymentOrder(
        status: PaymentOrderStatuses.paid,
        hasReceipt: true,
      );
      var opened = false;

      await pumpDetail(tester, onOpenReceipt: () => opened = true);
      await tester.ensureVisible(find.text('Ver comprovante'));
      await tester.tap(find.text('Ver comprovante'));

      expect(opened, isTrue);
    });

    testWidgets('without a receipt the button does not exist', (tester) async {
      repository.detail = billDetail(status: BillStatuses.paid);
      payments.order = paymentOrder(status: PaymentOrderStatuses.paid);

      await pumpDetail(tester, onOpenReceipt: () {});

      expect(find.text('Ver comprovante'), findsNothing);
    });
  });

  group('reopen action', () {
    testWidgets('only a FAILED bill offers the reopen, and confirming '
        'delegates to the repository', (tester) async {
      repository.detail = billDetail(status: BillStatuses.failed);
      payments.order = paymentOrder(status: PaymentOrderStatuses.failed);

      await pumpDetail(tester);

      expect(find.text('Reabrir para nova tentativa'), findsOneWidget);

      await tester.ensureVisible(find.text('Reabrir para nova tentativa'));
      await tester.tap(find.text('Reabrir para nova tentativa'));
      await tester.pumpAndSettle();
      await tester.tap(find.widgetWithText(FilledButton, 'Reabrir'));
      await tester.pumpAndSettle();

      expect(repository.calls, contains('reopenBill:bill-1'));
    });

    testWidgets('a scheduled bill offers no reopen — it is not an undo',
        (tester) async {
      repository.detail = billDetail(status: BillStatuses.scheduled);
      payments.order = paymentOrder(status: PaymentOrderStatuses.pending);

      await pumpDetail(tester);

      expect(find.text('Reabrir para nova tentativa'), findsNothing);
    });
  });

  group('approve sheet — overdue acknowledgement (ADR-017)', () {
    testWidgets('an overdue bill demands the immediate-execution box before '
        'authorizing', (tester) async {
      repository.detail = billDetail(
        status: BillStatuses.awaitingApproval,
        dueDate: DateTime(2020, 1, 10),
        lastConsultedAt: DateTime.now(),
      );

      await pumpDetail(tester);
      await tester.ensureVisible(find.text('Aprovar…'));
      await tester.tap(find.text('Aprovar…'));
      await tester.pumpAndSettle();

      expect(
        find.textContaining('Este boleto está vencido'),
        findsOneWidget,
      );
      final authorize = tester.widget<FilledButton>(
        find.widgetWithText(FilledButton, 'Autorizar'),
      );
      expect(authorize.onPressed, isNull);

      await tester.tap(find.textContaining('Este boleto está vencido'));
      await tester.pumpAndSettle();

      final armed = tester.widget<FilledButton>(
        find.widgetWithText(FilledButton, 'Autorizar'),
      );
      expect(armed.onPressed, isNotNull);
    });

    testWidgets('a future due date shows no box and authorizes freely',
        (tester) async {
      repository.detail = billDetail(
        status: BillStatuses.awaitingApproval,
        dueDate: DateTime.now().add(const Duration(days: 30)),
        lastConsultedAt: DateTime.now(),
      );

      await pumpDetail(tester);
      await tester.ensureVisible(find.text('Aprovar…'));
      await tester.tap(find.text('Aprovar…'));
      await tester.pumpAndSettle();

      expect(find.textContaining('Este boleto está vencido'), findsNothing);
      final authorize = tester.widget<FilledButton>(
        find.widgetWithText(FilledButton, 'Autorizar'),
      );
      expect(authorize.onPressed, isNotNull);
    });

    // O cinto extra do descompasso de relógio (UTC × local na virada do
    // dia): o relógio da tela diz "não vencido", o servidor recusa com
    // BLP.BIL35 — a caixa aparece no lugar, o formulário sobrevive, e o
    // reenvio com o aceite marcado é aprovado.
    testWidgets('a BIL35 refusal from the server reveals the box in place '
        'and the resubmit carries the acknowledgement', (tester) async {
      repository.detail = billDetail(
        status: BillStatuses.awaitingApproval,
        dueDate: DateTime.now().add(const Duration(days: 30)),
        lastConsultedAt: DateTime.now(),
      );
      repository.scriptedApproveRefusals.add(
        const BillPaymentRuleException(
          'Boleto vencido exige o aceite explícito.',
          code: 'BLP.BIL35',
        ),
      );

      await pumpDetail(tester);
      await tester.ensureVisible(find.text('Aprovar…'));
      await tester.tap(find.text('Aprovar…'));
      await tester.pumpAndSettle();

      await tester.tap(find.widgetWithText(FilledButton, 'Autorizar'));
      await tester.pumpAndSettle();

      // A folha continua aberta, com o aviso do servidor e a caixa.
      expect(
        find.textContaining('O servidor considera este boleto vencido'),
        findsOneWidget,
      );
      expect(
        find.textContaining('Este boleto está vencido'),
        findsOneWidget,
      );
      final disarmed = tester.widget<FilledButton>(
        find.widgetWithText(FilledButton, 'Autorizar'),
      );
      expect(disarmed.onPressed, isNull);

      await tester.tap(find.textContaining('Este boleto está vencido'));
      await tester.pumpAndSettle();
      await tester.tap(find.widgetWithText(FilledButton, 'Autorizar'));
      await tester.pumpAndSettle();

      expect(
        repository.calls.where((c) => c == 'approveBill:bill-1').length,
        2,
      );
      expect(repository.lastApproveImmediateAck, isTrue);
      expect(find.text('Autorizar pagamento'), findsNothing);
    });
  });

  group('approve sheet — schedule preview (informative)', () {
    testWidgets('the sheet shows when the payment will execute, naming the '
        'slide', (tester) async {
      repository.detail = billDetail(
        status: BillStatuses.awaitingApproval,
        dueDate: DateTime.now().add(const Duration(days: 30)),
        lastConsultedAt: DateTime.now(),
      );
      repository.schedulePreview = SchedulePreview(
        requestedDate: DateTime(2026, 9, 10),
        effectiveDate: DateTime(2026, 9, 11),
        slid: true,
        immediate: false,
      );

      await pumpDetail(tester);
      await tester.ensureVisible(find.text('Aprovar…'));
      await tester.tap(find.text('Aprovar…'));
      await tester.pumpAndSettle();

      expect(
        find.textContaining('Pagamento será executado em 11/09/2026'),
        findsOneWidget,
      );
      expect(
        find.textContaining('(deslizou do dia pedido)'),
        findsOneWidget,
      );
    });

    testWidgets('an honoured date shows no slide suffix', (tester) async {
      repository.detail = billDetail(
        status: BillStatuses.awaitingApproval,
        dueDate: DateTime.now().add(const Duration(days: 30)),
        lastConsultedAt: DateTime.now(),
      );
      repository.schedulePreview = SchedulePreview(
        requestedDate: DateTime(2026, 9, 10),
        effectiveDate: DateTime(2026, 9, 10),
        slid: false,
        immediate: false,
      );

      await pumpDetail(tester);
      await tester.ensureVisible(find.text('Aprovar…'));
      await tester.tap(find.text('Aprovar…'));
      await tester.pumpAndSettle();

      expect(
        find.textContaining('Pagamento será executado em 10/09/2026'),
        findsOneWidget,
      );
      expect(find.textContaining('deslizou do dia pedido'), findsNothing);
    });

    // A prévia é informativa: sem ela a folha funciona exatamente como
    // antes — nada de linha, e o Autorizar segue habilitado.
    testWidgets('a preview failure draws nothing and never blocks the '
        'authorization', (tester) async {
      repository.detail = billDetail(
        status: BillStatuses.awaitingApproval,
        dueDate: DateTime.now().add(const Duration(days: 30)),
        lastConsultedAt: DateTime.now(),
      );
      repository.previewShouldFail = true;

      await pumpDetail(tester);
      await tester.ensureVisible(find.text('Aprovar…'));
      await tester.tap(find.text('Aprovar…'));
      await tester.pumpAndSettle();

      expect(find.textContaining('Pagamento será executado'), findsNothing);
      final authorize = tester.widget<FilledButton>(
        find.widgetWithText(FilledButton, 'Autorizar'),
      );
      expect(authorize.onPressed, isNotNull);

      await tester.tap(find.widgetWithText(FilledButton, 'Autorizar'));
      await tester.pumpAndSettle();

      expect(repository.calls, contains('approveBill:bill-1'));
    });

    // Quando o servidor calcula execução imediata (o relógio dele manda), a
    // prévia conecta com a caixa do ADR-017: o aceite passa a ser exigido
    // mesmo que o relógio da tela ainda não veja o vencimento.
    testWidgets('an immediate preview reveals the acknowledgement box even '
        'when the local clock disagrees', (tester) async {
      repository.detail = billDetail(
        status: BillStatuses.awaitingApproval,
        dueDate: DateTime.now().add(const Duration(days: 30)),
        lastConsultedAt: DateTime.now(),
      );
      repository.schedulePreview = SchedulePreview(
        requestedDate: DateTime(2026, 9, 1),
        effectiveDate: DateTime(2026, 9, 1),
        slid: false,
        immediate: true,
      );

      await pumpDetail(tester);
      await tester.ensureVisible(find.text('Aprovar…'));
      await tester.tap(find.text('Aprovar…'));
      await tester.pumpAndSettle();

      expect(
        find.textContaining('Pagamento será executado imediatamente'),
        findsOneWidget,
      );
      expect(
        find.textContaining('Este boleto está vencido'),
        findsOneWidget,
      );
      final authorize = tester.widget<FilledButton>(
        find.widgetWithText(FilledButton, 'Autorizar'),
      );
      expect(authorize.onPressed, isNull);
    });
  });
}
