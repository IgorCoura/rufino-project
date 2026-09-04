import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/bills/bill_list_screen.dart';
import 'package:bill_payment/src/ui/bills/bill_list_viewmodel.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../fakes/fakes.dart';

/// Os filtros da fase 3 (Agendados/Pagos/Falhou) e a linha "pagar em" — sem
/// eles um boleto agendado sumia da vista e obrigava a abrir o detalhe.
void main() {
  late FakeBillRepository repository;

  setUp(() {
    repository = FakeBillRepository();
  });

  Future<void> pumpList(WidgetTester tester) async {
    final viewModel = BillListViewModel(
      repository: repository,
      initialStatus: BillStatuses.awaitingApproval,
    );
    addTearDown(viewModel.dispose);
    final permissions = await billPaymentPermissions([
      const Permission(
        resource: BillPaymentResources.bill,
        scopes: ['view', 'import'],
      ),
    ]);
    addTearDown(permissions.dispose);

    tester.view.physicalSize = const Size(1000, 1600);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);
    await tester.pumpWidget(
      ChangeNotifierProvider<BillPaymentPermissionNotifier>.value(
        value: permissions,
        child: MaterialApp(
          home: BillListScreen(
            viewModel: viewModel,
            backFallback: '/bill-payment/pending',
            onOpenBill: (_) {},
            onImportBill: () {},
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('phase 3 filters', () {
    testWidgets('the Agendados chip filters on the server side',
        (tester) async {
      repository.bills = [
        bill(id: 'b1', status: BillStatuses.scheduled, amount: 100),
        bill(id: 'b2', status: BillStatuses.paid, amount: 200),
      ];

      await pumpList(tester);
      await tester.tap(find.text('Agendados'));
      await tester.pumpAndSettle();

      expect(repository.lastStatusFilter, BillStatuses.scheduled);
      expect(find.textContaining('100,00'), findsOneWidget);
      expect(find.textContaining('200,00'), findsNothing);
    });

    testWidgets('the Falhou chip is the operational queue of the payment',
        (tester) async {
      repository.bills = [
        bill(id: 'b1', status: BillStatuses.failed, amount: 300),
      ];

      await pumpList(tester);
      await tester.tap(find.text('Falhou'));
      await tester.pumpAndSettle();

      expect(repository.lastStatusFilter, BillStatuses.failed);
      expect(find.textContaining('300,00'), findsOneWidget);
    });

    testWidgets('the Pagos chip asks the server for paid bills',
        (tester) async {
      repository.bills = [];

      await pumpList(tester);
      await tester.tap(find.text('Pagos'));
      await tester.pumpAndSettle();

      expect(repository.lastStatusFilter, BillStatuses.paid);
      expect(find.text('Nenhum boleto neste estado.'), findsOneWidget);
    });
  });

  group('pagar em line', () {
    testWidgets('a scheduled bill shows its payment date on the row',
        (tester) async {
      repository.bills = [
        bill(
          id: 'b1',
          status: BillStatuses.scheduled,
          scheduledFor: DateTime(2026, 9, 11),
        ),
      ];

      await pumpList(tester);
      await tester.tap(find.text('Agendados'));
      await tester.pumpAndSettle();

      expect(find.textContaining('pagar em'), findsOneWidget);
    });

    testWidgets('a bill without a schedule keeps the row clean',
        (tester) async {
      repository.bills = [bill(id: 'b1')];

      await pumpList(tester);

      expect(find.textContaining('pagar em'), findsNothing);
    });
  });
}
