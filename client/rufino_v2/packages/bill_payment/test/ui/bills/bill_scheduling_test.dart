import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/bills/bill_detail_screen.dart';
import 'package:bill_payment/src/ui/bills/bill_detail_viewmodel.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../fakes/fakes.dart';

/// A separação entre aprovar e agendar (ADR-018), e a reversão de decisão
/// terminal — do lado de quem usa a tela.
void main() {
  late FakeBillRepository repository;

  setUp(() => repository = FakeBillRepository());

  Future<BillDetailViewModel> pumpBill(
    WidgetTester tester, {
    required String status,
    required List<String> billScopes,
    DateTime? scheduledFor,
    List<BillHistoryEntry> history = const [],
  }) async {
    repository.detail = billDetail(
      status: status,
      scheduledFor: scheduledFor,
      history: history,
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
    return viewModel;
  }

  group('aprovar sem agendar', () {
    testWidgets('offers both approve and approve-and-schedule while awaiting',
        (tester) async {
      await pumpBill(
        tester,
        status: BillStatuses.awaitingApproval,
        billScopes: const ['view', 'approve'],
      );

      expect(find.widgetWithText(OutlinedButton, 'Aprovar'), findsOneWidget);
      expect(
        find.widgetWithText(FilledButton, 'Aprovar e agendar…'),
        findsOneWidget,
      );
    });

    testWidgets('approving alone sends no date to the server', (tester) async {
      await pumpBill(
        tester,
        status: BillStatuses.awaitingApproval,
        billScopes: const ['view', 'approve'],
      );

      await tester.tap(find.widgetWithText(OutlinedButton, 'Aprovar'));
      await tester.pumpAndSettle();
      await tester.tap(find.widgetWithText(FilledButton, 'Aprovar'));
      await tester.pumpAndSettle();

      expect(repository.calls, contains('approveBill:bill-1'));
      expect(repository.lastApproveScheduleFor, isNull);
    });
  });

  group('agendar um boleto aprovado', () {
    testWidgets('an approved bill without a date offers Agendar',
        (tester) async {
      await pumpBill(
        tester,
        status: BillStatuses.approved,
        billScopes: const ['view', 'schedule'],
      );

      expect(find.widgetWithText(FilledButton, 'Agendar…'), findsOneWidget);
    });

    // Aprovado COM data já tem ordem a caminho: agendar de novo criaria um
    // segundo pagamento para o mesmo compromisso.
    testWidgets('an approved bill already scheduled offers no Agendar',
        (tester) async {
      await pumpBill(
        tester,
        status: BillStatuses.approved,
        scheduledFor: DateTime(2026, 9, 20),
        billScopes: const ['view', 'schedule'],
      );

      expect(find.widgetWithText(FilledButton, 'Agendar…'), findsNothing);
    });

    // Sem a alçada de agendamento o botão SOME — a de aprovar não a implica.
    testWidgets('without the scheduling clearance the button is not rendered',
        (tester) async {
      await pumpBill(
        tester,
        status: BillStatuses.approved,
        billScopes: const ['view', 'approve'],
      );

      expect(find.widgetWithText(FilledButton, 'Agendar…'), findsNothing);
    });

    testWidgets('scheduling sends the date through the schedule endpoint',
        (tester) async {
      await pumpBill(
        tester,
        status: BillStatuses.approved,
        billScopes: const ['view', 'schedule'],
      );

      await tester.tap(find.widgetWithText(FilledButton, 'Agendar…'));
      await tester.pumpAndSettle();
      await tester.tap(find.widgetWithText(FilledButton, 'Agendar'));
      await tester.pumpAndSettle();

      expect(repository.calls, contains('scheduleBill:bill-1'));
      expect(repository.lastApproveScheduleFor, isNotNull);
    });
  });

  group('reverter decisão terminal', () {
    testWidgets('a denied bill offers Reverter with the undo clearance',
        (tester) async {
      await pumpBill(
        tester,
        status: BillStatuses.denied,
        billScopes: const ['view', 'undo-decision'],
      );

      expect(find.widgetWithText(FilledButton, 'Reverter'), findsOneWidget);
    });

    testWidgets('a cancelled bill offers it too', (tester) async {
      await pumpBill(
        tester,
        status: BillStatuses.cancelled,
        billScopes: const ['view', 'undo-decision'],
      );

      expect(find.widgetWithText(FilledButton, 'Reverter'), findsOneWidget);
    });

    // Aprovar não dá o poder de desfazer o que outra pessoa decidiu.
    testWidgets('without the undo clearance nothing is offered',
        (tester) async {
      await pumpBill(
        tester,
        status: BillStatuses.denied,
        billScopes: const ['view', 'approve', 'deny', 'cancel'],
      );

      expect(find.widgetWithText(FilledButton, 'Reverter'), findsNothing);
    });

    // Pago é terminal de verdade — dinheiro que saiu não volta por decisão
    // nossa, e a barra inteira continua escondida.
    testWidgets('a paid bill offers no undo', (tester) async {
      await pumpBill(
        tester,
        status: BillStatuses.paid,
        billScopes: const ['view', 'undo-decision'],
      );

      expect(find.widgetWithText(FilledButton, 'Reverter'), findsNothing);
    });

    testWidgets('the reason is mandatory and travels to the server',
        (tester) async {
      await pumpBill(
        tester,
        status: BillStatuses.denied,
        billScopes: const ['view', 'undo-decision'],
      );

      await tester.tap(find.widgetWithText(FilledButton, 'Reverter'));
      await tester.pumpAndSettle();

      await tester.tap(find.widgetWithText(FilledButton, 'Confirmar'));
      await tester.pumpAndSettle();
      expect(find.text('Informe o motivo.'), findsOneWidget);

      await tester.enterText(find.byType(TextFormField), 'recusa por engano');
      await tester.tap(find.widgetWithText(FilledButton, 'Confirmar'));
      await tester.pumpAndSettle();

      expect(
        repository.calls,
        contains('undoBillDecision:bill-1:recusa por engano'),
      );
    });
  });

  group('histórico', () {
    testWidgets('shows the trail collapsed, newest first', (tester) async {
      await pumpBill(
        tester,
        status: BillStatuses.awaitingApproval,
        billScopes: const ['view'],
        history: [
          BillHistoryEntry(
            action: BillActions.captured,
            origin: BillActionOrigins.system,
            occurredAt: DateTime(2026, 9, 1, 10),
            actorName: 'Sistema',
            toStatus: BillStatuses.captured,
          ),
          BillHistoryEntry(
            action: BillActions.denied,
            origin: BillActionOrigins.user,
            occurredAt: DateTime(2026, 9, 2, 11),
            actorUserId: 'user-1',
            actorName: 'João',
            fromStatus: BillStatuses.awaitingApproval,
            toStatus: BillStatuses.denied,
            note: 'cobrança indevida',
          ),
        ],
      );

      expect(find.text('Histórico'), findsOneWidget);
      expect(find.text('2 registro(s)'), findsOneWidget);

      // Recolhido por padrão: o caso comum é decidir, não auditar.
      expect(find.text('Negado por João'), findsNothing);

      await tester.tap(find.text('Histórico'));
      await tester.pumpAndSettle();

      expect(find.text('Negado por João'), findsOneWidget);
      expect(find.text('Capturado por Sistema'), findsOneWidget);
      expect(find.text('cobrança indevida'), findsOneWidget);
    });

    // O selo que responde "quem cancelou isto?": sem ele, um cancelamento feito
    // no painel do provedor lia-se igual a um pedido por alguém aqui dentro.
    testWidgets('flags an entry that came from the provider', (tester) async {
      await pumpBill(
        tester,
        status: BillStatuses.approved,
        billScopes: const ['view'],
        history: [
          BillHistoryEntry(
            action: BillActions.unscheduled,
            origin: BillActionOrigins.provider,
            occurredAt: DateTime(2026, 9, 3, 9),
            actorName: 'Provedor de pagamento',
            fromStatus: BillStatuses.scheduled,
            toStatus: BillStatuses.approved,
          ),
        ],
      );

      await tester.tap(find.text('Histórico'));
      await tester.pumpAndSettle();

      expect(
        find.text('Agendamento cancelado por Provedor de pagamento'),
        findsOneWidget,
      );
      expect(find.text('no provedor'), findsOneWidget);
    });

    // Ação de uma pessoa NÃO leva o selo — ele existe para o que veio de fora.
    testWidgets('does not flag an entry made by a person', (tester) async {
      await pumpBill(
        tester,
        status: BillStatuses.approved,
        billScopes: const ['view'],
        history: [
          BillHistoryEntry(
            action: BillActions.approved,
            origin: BillActionOrigins.user,
            occurredAt: DateTime(2026, 9, 3, 9),
            actorUserId: 'user-1',
            actorName: 'João',
            fromStatus: BillStatuses.awaitingApproval,
            toStatus: BillStatuses.approved,
          ),
        ],
      );

      await tester.tap(find.text('Histórico'));
      await tester.pumpAndSettle();

      expect(find.text('Aprovado por João'), findsOneWidget);
      expect(find.text('no provedor'), findsNothing);
    });

    testWidgets('draws nothing when the bill has no trail', (tester) async {
      await pumpBill(
        tester,
        status: BillStatuses.awaitingApproval,
        billScopes: const ['view'],
      );

      expect(find.text('Histórico'), findsNothing);
    });
  });
}
