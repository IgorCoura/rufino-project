import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/bills/bill_detail_screen.dart';
import 'package:bill_payment/src/ui/bills/bill_detail_viewmodel.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../fakes/fakes.dart';

/// The approval screen telling the user where the AI reading stands.
///
/// A bill waiting in the queue and a bill whose document has nothing to read
/// used to look identical here: the summary simply lacked the competência and
/// descrição rows, and check 13 reported "Não se aplica — Sem leitura por IA".
/// Both facts were true and neither was the whole truth, so the user read the
/// pending state as a verdict.
void main() {
  late BillPaymentPermissionNotifier permissions;

  setUp(() async {
    permissions = await billPaymentPermissions(const [
      Permission(
        resource: BillPaymentResources.bill,
        scopes: ['view', 'validate', 'approve', 'deny', 'cancel'],
      ),
    ]);
  });

  tearDown(() => permissions.dispose());

  Future<void> pumpBill(WidgetTester tester, String readingStatus) async {
    final repository = FakeBillRepository()
      ..detail = billDetail(
        readingStatus: readingStatus,
        checks: [
          BillCheck(
            type: CheckTypes.documentConsistency,
            outcome: CheckOutcomes.skipped,
            severity: CheckSeverities.advisory,
            reasonCode: 'reading_not_available',
            evidence: 'Sem leitura por IA para comparar.',
            isBlockingFailure: false,
            evaluatedAt: DateTime(2026, 8, 28),
          ),
        ],
      );

    final viewModel = BillDetailViewModel(
      repository: repository,
      billId: 'bill-1',
    );
    addTearDown(viewModel.dispose);

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

  group('bill approval screen — AI reading status', () {
    testWidgets('says the bill is queued for the AI lookup', (tester) async {
      await pumpBill(tester, ReadingStatuses.queued);

      expect(find.text('Na fila para consulta com IA'), findsWidgets);
    });

    // The check is right to be skipped — there is nothing to compare yet — but
    // "Não se aplica" reads as a verdict, and the user acted on it as one.
    testWidgets('the document check reads as pending, not as settled', (
      tester,
    ) async {
      await pumpBill(tester, ReadingStatuses.queued);

      expect(find.text('Aguardando'), findsOneWidget);
      expect(find.text('Não se aplica'), findsNothing);
    });

    // Nothing to read is a different fact from not read yet, and the screen
    // must not invent a pending state for a bill that has none.
    testWidgets('stays silent when there is nothing to read', (tester) async {
      await pumpBill(tester, ReadingStatuses.notApplicable);

      expect(find.text('Na fila para consulta com IA'), findsNothing);
      expect(find.text('Aguardando'), findsNothing);
      expect(find.text('Não se aplica'), findsOneWidget);
    });

    // Gave up is not the same as pending either: it is actionable in a
    // different way, and it must not be dressed as a queue that will drain.
    testWidgets('says so when the reading gave up', (tester) async {
      await pumpBill(tester, ReadingStatuses.unavailable);

      expect(find.text('Consulta com IA indisponível'), findsWidgets);
      expect(find.text('Aguardando'), findsNothing);
    });
  });
}
