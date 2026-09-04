import 'package:bill_payment/bill_payment.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';

import 'fakes/fakes.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('BillPaymentPermissionNotifier', () {
    test('answers for scopes granted on the bill payment audience', () async {
      final notifier = await billPaymentPermissions(const [
        Permission(
          resource: BillPaymentResources.bill,
          scopes: [BillPaymentScopes.view, BillPaymentScopes.import],
        ),
      ]);

      expect(
        notifier.hasPermission(
          BillPaymentResources.bill,
          BillPaymentScopes.view,
        ),
        isTrue,
      );
      expect(
        notifier.hasPermission(
          BillPaymentResources.bill,
          BillPaymentScopes.approve,
        ),
        isFalse,
      );
      expect(notifier.hasAnyScope(BillPaymentResources.payee), isFalse);

      notifier.dispose();
    });

    test('canDecide reflects the approve scope on the bill resource',
        () async {
      final approver = await billPaymentPermissions(const [
        Permission(
          resource: BillPaymentResources.bill,
          scopes: [BillPaymentScopes.view, BillPaymentScopes.approve],
        ),
      ]);
      final viewer = await billPaymentPermissions(const [
        Permission(
          resource: BillPaymentResources.bill,
          scopes: [BillPaymentScopes.view],
        ),
      ]);

      expect(approver.canDecide, isTrue);
      expect(viewer.canDecide, isFalse);

      approver.dispose();
      viewer.dispose();
    });
  });

  group('BillPaymentResources', () {
    test('uses the exact strings the backend declares in ProtectedResource',
        () {
      expect(BillPaymentResources.bill, 'bill');
      expect(BillPaymentResources.captureItem, 'capture-item');
      expect(BillPaymentResources.captureSource, 'capture-source');
      expect(BillPaymentResources.expectation, 'expectation');
      expect(BillPaymentResources.payee, 'payee');
      expect(BillPaymentResources.payerProfile, 'payer-profile');
      expect(BillPaymentResources.origin, 'origin');
    });
  });
}
