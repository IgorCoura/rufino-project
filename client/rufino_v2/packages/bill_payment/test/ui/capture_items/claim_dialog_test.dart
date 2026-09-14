import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/bills/bill_detail_screen.dart';
import 'package:bill_payment/src/ui/bills/bill_detail_viewmodel.dart';
import 'package:bill_payment/src/ui/capture_items/claim_dialog.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../fakes/fakes.dart';

/// "Lembrar desta conta" (ADR-026): the claim teaches the account number, so
/// next month's bill arrives routed instead of back in the claim queue.
void main() {
  Future<List<String?>> pumpDialog(
    WidgetTester tester, {
    String? suggestion,
    ClaimAttempt Function(String?)? answer,
  }) async {
    final requests = <String?>[];

    await tester.pumpWidget(
      MaterialApp(
        home: Builder(
          builder: (context) => Scaffold(
            body: Center(
              child: FilledButton(
                onPressed: () => showDialog<bool>(
                  context: context,
                  builder: (_) => ClaimDialog(
                    suggestedAccountReference: suggestion,
                    onClaim: (account) async {
                      requests.add(account);
                      return (answer ?? (_) => const ClaimAttempt.finished())(account);
                    },
                  ),
                ),
                child: const Text('abrir'),
              ),
            ),
          ),
        ),
      ),
    );
    await tester.tap(find.text('abrir'));
    await tester.pumpAndSettle();
    return requests;
  }

  group('ClaimDialog', () {
    testWidgets('comes ticked with the suggested account and sends it',
        (tester) async {
      final requests = await pumpDialog(tester, suggestion: '1123004411');

      final box = tester.widget<CheckboxListTile>(
        find.byKey(const Key('claim-remember-account')),
      );
      expect(box.value, isTrue);
      expect(find.text('1123004411'), findsOneWidget);

      await tester.tap(find.byKey(const Key('claim-confirm')));
      await tester.pumpAndSettle();

      expect(requests, ['1123004411']);
      expect(find.byType(ClaimDialog), findsNothing);
    });

    testWidgets('without a suggestion comes unticked and claims without '
        'remembering', (tester) async {
      final requests = await pumpDialog(tester);

      expect(
        tester
            .widget<CheckboxListTile>(
              find.byKey(const Key('claim-remember-account')),
            )
            .value,
        isFalse,
      );
      expect(find.byKey(const Key('claim-account-reference')), findsNothing);

      await tester.tap(find.byKey(const Key('claim-confirm')));
      await tester.pumpAndSettle();

      expect(requests, [null]);
    });

    testWidgets('ticked with an empty number asks for it and does not claim',
        (tester) async {
      final requests = await pumpDialog(tester);

      await tester.tap(find.byKey(const Key('claim-remember-account')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('claim-confirm')));
      await tester.pumpAndSettle();

      expect(requests, isEmpty);
      expect(
        find.text('Informe o número da conta ou desmarque a opção.'),
        findsOneWidget,
      );
    });

    testWidgets('keeps the dialog open with the reason when the server refuses '
        'the account', (tester) async {
      await pumpDialog(
        tester,
        suggestion: '555555555',
        answer: (_) => const ClaimAttempt.accountRefused(
          'O número da conta informado não aparece neste boleto.',
        ),
      );

      await tester.tap(find.byKey(const Key('claim-confirm')));
      await tester.pumpAndSettle();

      expect(find.byType(ClaimDialog), findsOneWidget);
      expect(find.byKey(const Key('claim-account-refusal')), findsOneWidget);
    });
  });

  group('bill detail — pending "lembrar desta conta"', () {
    testWidgets('says the payee must be registered for the account to be '
        'remembered', (tester) async {
      final permissions = await billPaymentPermissions(const [
        Permission(resource: BillPaymentResources.bill, scopes: ['view']),
      ]);
      addTearDown(permissions.dispose);

      final viewModel = BillDetailViewModel(
        repository: FakeBillRepository()
          ..detail = billDetail(pendingAccountReference: '1123004411'),
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

      expect(find.byKey(const Key('bill-pending-account-notice')), findsOneWidget);
    });
  });
}
