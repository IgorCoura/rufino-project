import 'package:bill_payment/bill_payment.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:rufino_core/rufino_core.dart';

import '../fakes/fakes.dart';

class MockPayeeApiService extends Mock implements PayeeApiService {}

void main() {
  late MockPayeeApiService apiService;
  late FakeErrorReporter reporter;
  late PayeeRepositoryImpl repository;

  setUp(() {
    apiService = MockPayeeApiService();
    reporter = FakeErrorReporter();
    repository =
        PayeeRepositoryImpl(apiService: apiService, reporter: reporter);
  });

  group('PayeeRepositoryImpl error classification', () {
    test('a 4xx with a domain message becomes a rule exception with its code '
        'and is not reported', () async {
      when(() => apiService.deletePayee(any())).thenThrow(
        const HttpException(
          statusCode: 409,
          message: 'HTTP 409',
          serverMessages: ['Beneficiário em uso.'],
          domainErrorId: 'BLP.PYE10',
        ),
      );

      final result = await repository.deletePayee('payee-1');

      result.fold(
        onSuccess: (_) => fail('should have failed'),
        onError: (error, _) {
          final rule = error as BillPaymentRuleException;
          expect(rule.message, 'Beneficiário em uso.');
          expect(rule.code, 'BLP.PYE10');
        },
      );
      expect(reporter.capturedErrors, isEmpty);
    });

    test('a 5xx becomes a network exception and is reported', () async {
      when(() => apiService.listPayees(limit: any(named: 'limit'))).thenThrow(
        const HttpException(statusCode: 500, message: 'HTTP 500'),
      );

      final result = await repository.listPayees();

      result.fold(
        onSuccess: (_) => fail('should have failed'),
        onError: (error, _) =>
            expect(error, isA<BillPaymentNetworkException>()),
      );
      expect(reporter.capturedErrors, hasLength(1));
    });

    test('an unexpected exception is wrapped and reported', () async {
      when(() => apiService.getPayee(any()))
          .thenThrow(StateError('parse blew up'));

      final result = await repository.getPayee('payee-1');

      result.fold(
        onSuccess: (_) => fail('should have failed'),
        onError: (error, _) =>
            expect(error, isA<BillPaymentNetworkException>()),
      );
      expect(reporter.capturedErrors, hasLength(1));
    });
  });

  group('PayeeRepositoryImpl passthrough', () {
    test('a payee not found by tax id resolves to null, not an error',
        () async {
      when(() => apiService.findByTaxId(any())).thenAnswer((_) async => null);

      final result = await repository.findByTaxId('11.222.333/0001-81');

      result.fold(
        onSuccess: (payee) => expect(payee, isNull),
        onError: (error, _) => fail('204 is a state, not an error: $error'),
      );
      expect(reporter.capturedErrors, isEmpty);
    });

    test('registering returns the new id', () async {
      when(
        () => apiService.registerPayee(
          legalName: any(named: 'legalName'),
          taxId: any(named: 'taxId'),
          amountPolicy: any(named: 'amountPolicy'),
        ),
      ).thenAnswer((_) async => 'payee-9');

      final result = await repository.registerPayee(
        legalName: 'EDP',
        taxId: '02.302.100/0001-06',
        amountPolicy: const AmountPolicyInput(kind: AmountPolicyKinds.range),
      );

      result.fold(
        onSuccess: (id) => expect(id, 'payee-9'),
        onError: (error, _) => fail('should have succeeded: $error'),
      );
    });
  });

  setUpAll(() {
    registerFallbackValue(
      const AmountPolicyInput(kind: AmountPolicyKinds.unbounded),
    );
  });
}
