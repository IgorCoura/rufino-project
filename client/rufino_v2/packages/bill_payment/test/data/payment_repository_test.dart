import 'package:bill_payment/bill_payment.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:rufino_core/rufino_core.dart';

import '../fakes/fakes.dart';

class MockPaymentApiService extends Mock implements PaymentApiService {}

void main() {
  late MockPaymentApiService apiService;
  late FakeErrorReporter reporter;
  late PaymentRepositoryImpl repository;

  setUp(() {
    apiService = MockPaymentApiService();
    reporter = FakeErrorReporter();
    repository =
        PaymentRepositoryImpl(apiService: apiService, reporter: reporter);
  });

  group('PaymentRepositoryImpl error classification', () {
    test('a 4xx with a domain message becomes a rule exception with its code '
        'and is not reported', () async {
      when(() => apiService.cancel(any())).thenThrow(
        const HttpException(
          statusCode: 409,
          message: 'HTTP 409',
          serverMessages: ['Ordem já em processamento bancário.'],
          domainErrorId: 'BLP.PMO09',
        ),
      );

      final result = await repository.cancel('order-1');

      result.fold(
        onSuccess: (_) => fail('should have failed'),
        onError: (error, _) {
          final rule = error as BillPaymentRuleException;
          expect(rule.message, 'Ordem já em processamento bancário.');
          expect(rule.code, 'BLP.PMO09');
        },
      );
      expect(reporter.capturedErrors, isEmpty);
    });

    test('a 5xx becomes a network exception and is reported', () async {
      when(() => apiService.getByBill(any())).thenThrow(
        const HttpException(statusCode: 500, message: 'HTTP 500'),
      );

      final result = await repository.getForBill('bill-1');

      result.fold(
        onSuccess: (_) => fail('should have failed'),
        onError: (error, _) =>
            expect(error, isA<BillPaymentNetworkException>()),
      );
      expect(reporter.capturedErrors, hasLength(1));
    });

    test('an unexpected exception is wrapped and reported', () async {
      when(() => apiService.getByBill(any()))
          .thenThrow(StateError('parse blew up'));

      final result = await repository.getForBill('bill-1');

      result.fold(
        onSuccess: (_) => fail('should have failed'),
        onError: (error, _) =>
            expect(error, isA<BillPaymentNetworkException>()),
      );
      expect(reporter.capturedErrors, hasLength(1));
    });
  });

  group('PaymentRepositoryImpl passthrough', () {
    // 404 é resposta normal: a janela do outbox entre aprovar e a ordem
    // existir. O serviço traduz para null e o repositório o repassa.
    test('a bill without an order yet resolves to null, not an error',
        () async {
      when(() => apiService.getByBill(any())).thenAnswer((_) async => null);

      final result = await repository.getForBill('bill-1');

      result.fold(
        onSuccess: (order) => expect(order, isNull),
        onError: (error, _) => fail('404 is a state, not an error: $error'),
      );
      expect(reporter.capturedErrors, isEmpty);
    });

    test('the order entity flows through untouched', () async {
      final order = paymentOrder(hasReceipt: true);
      when(() => apiService.getByBill('bill-1'))
          .thenAnswer((_) async => order);

      final result = await repository.getForBill('bill-1');

      result.fold(
        onSuccess: (loaded) {
          expect(loaded!.id, 'order-1');
          expect(loaded.status, PaymentOrderStatuses.pending);
          expect(loaded.hasReceipt, isTrue);
        },
        onError: (error, _) => fail('should have succeeded: $error'),
      );
    });

    test('confirmImmediate delegates to the service', () async {
      when(() => apiService.confirmImmediate(any())).thenAnswer((_) async {});

      final result = await repository.confirmImmediate('order-1');

      expect(result.isSuccess, isTrue);
      verify(() => apiService.confirmImmediate('order-1')).called(1);
    });
  });

  group('PaymentRepositoryImpl getReceiptForBill', () {
    // A regra vive no repositório: sem ordem (janela do outbox) ou sem
    // comprovante ainda, a recusa é de REGRA — silenciosa no monitor.
    test('a bill whose order has no receipt yet fails with a rule message '
        'and is not reported', () async {
      when(() => apiService.getByBill('bill-1'))
          .thenAnswer((_) async => paymentOrder(hasReceipt: false));

      final result = await repository.getReceiptForBill('bill-1');

      result.fold(
        onSuccess: (_) => fail('should have failed'),
        onError: (error, _) {
          final rule = error as BillPaymentRuleException;
          expect(rule.message, contains('comprovante'));
        },
      );
      expect(reporter.capturedErrors, isEmpty);
      verifyNever(() => apiService.getReceipt(any()));
    });

    test('a bill with no order at all fails with the same rule message',
        () async {
      when(() => apiService.getByBill('bill-1')).thenAnswer((_) async => null);

      final result = await repository.getReceiptForBill('bill-1');

      result.fold(
        onSuccess: (_) => fail('should have failed'),
        onError: (error, _) =>
            expect(error, isA<BillPaymentRuleException>()),
      );
      expect(reporter.capturedErrors, isEmpty);
    });

    test('resolves the order first and downloads by the ORDER id', () async {
      when(() => apiService.getByBill('bill-1'))
          .thenAnswer((_) async => paymentOrder(hasReceipt: true));
      when(() => apiService.getReceipt('order-1'))
          .thenAnswer((_) async => artifact(fileName: 'comprovante.pdf'));

      final result = await repository.getReceiptForBill('bill-1');

      result.fold(
        onSuccess: (receipt) {
          expect(receipt.fileName, 'comprovante.pdf');
          expect(receipt.contentType, 'application/pdf');
          expect(receipt.bytes, isNotEmpty);
        },
        onError: (error, _) => fail('should have succeeded: $error'),
      );
      verify(() => apiService.getReceipt('order-1')).called(1);
    });
  });
}
