import 'package:bill_payment/bill_payment.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

void main() {
  group('billPaymentRoutes', () {
    final routes = billPaymentRoutes(homeRoute: '/home')
        .whereType<GoRoute>()
        .map((r) => r.path)
        .toList();

    test('declares every screen of the module', () {
      expect(routes, contains(BillPaymentRoutes.pending));
      expect(routes, contains(BillPaymentRoutes.bills));
      expect(routes, contains(BillPaymentRoutes.billImport));
      expect(routes, contains('/bill-payment/bills/:id'));
      expect(routes, contains(BillPaymentRoutes.captureItems));
      expect(routes, contains('/bill-payment/capture-items/:id'));
      expect(routes, contains(BillPaymentRoutes.captureSources));
      expect(routes, contains(BillPaymentRoutes.captureSourceConnect));
      expect(routes, contains('/bill-payment/capture-sources/:id'));
      expect(routes, contains(BillPaymentRoutes.payees));
      expect(routes, contains(BillPaymentRoutes.payeeCreate));
      expect(routes, contains('/bill-payment/payees/:id'));
      expect(routes, contains(BillPaymentRoutes.payerProfile));
      expect(routes, contains(BillPaymentRoutes.trustedOrigins));
      expect(routes, contains(BillPaymentRoutes.expectations));
      expect(routes, contains(BillPaymentRoutes.expectationCreate));
      expect(routes, contains('/bill-payment/expectations/:id'));
    });

    test('every literal route comes before its :id sibling — go_router '
        'matches in order and an :id first would swallow it', () {
      void literalBeforeParam(String literal, String param) {
        expect(
          routes.indexOf(literal),
          lessThan(routes.indexOf(param)),
          reason: '$literal must be declared before $param',
        );
      }

      literalBeforeParam(
        BillPaymentRoutes.billImport,
        '/bill-payment/bills/:id',
      );
      literalBeforeParam(
        BillPaymentRoutes.captureSourceConnect,
        '/bill-payment/capture-sources/:id',
      );
      literalBeforeParam(
        BillPaymentRoutes.payeeCreate,
        '/bill-payment/payees/:id',
      );
      literalBeforeParam(
        BillPaymentRoutes.expectationCreate,
        '/bill-payment/expectations/:id',
      );
    });
  });
}
