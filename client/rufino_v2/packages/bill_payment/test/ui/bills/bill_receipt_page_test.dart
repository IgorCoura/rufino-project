import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/bill_payment_pages.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../fakes/fakes.dart';

/// Serves a receipt in a format the viewer shows as a plain message, so the
/// loaded branch is provable without the PDF plugin.
class _TextReceiptRepository extends FakePaymentRepository {
  @override
  Future<Result<CapturedArtifact>> getReceiptForBill(String billId) async {
    calls.add('getReceiptForBill:$billId');
    return Result.success(artifact(contentType: 'text/plain'));
  }
}

/// O comprovante é rota, não diálogo — e sem comprovante a mensagem é de
/// REGRA, não de rede (mesma doutrina do artefato).
void main() {
  Future<void> pumpPage(
    WidgetTester tester,
    PaymentRepository repository,
  ) async {
    await tester.pumpWidget(
      Provider<PaymentRepository>.value(
        value: repository,
        child: const MaterialApp(
          home: BillReceiptPage(
            billId: 'bill-1',
            backFallback: '/bill-payment/bills/bill-1',
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('BillReceiptPage', () {
    testWidgets('resolves the receipt through the repository and renders the '
        'viewer under its own title', (tester) async {
      final repository = _TextReceiptRepository();

      await pumpPage(tester, repository);

      expect(find.text('Comprovante de pagamento'), findsOneWidget);
      expect(repository.calls, contains('getReceiptForBill:bill-1'));
      // O formato não exibível prova que os bytes chegaram ao viewer.
      expect(find.textContaining('formato que o app não exibe'), findsOneWidget);
    });

    testWidgets('a rule refusal shows the rule message with a retry',
        (tester) async {
      final repository = FakePaymentRepository()..setShouldFail(true);

      await pumpPage(tester, repository);

      expect(find.text('Regra recusou.'), findsOneWidget);
      expect(find.text('Tentar novamente'), findsOneWidget);
    });
  });
}
