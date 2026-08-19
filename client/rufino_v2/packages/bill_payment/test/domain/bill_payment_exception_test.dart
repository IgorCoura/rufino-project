import 'package:bill_payment/bill_payment.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';

void main() {
  group('BillPaymentRuleException', () {
    test('is an expected failure so it never reaches the error monitor', () {
      const exception = BillPaymentRuleException('regra disse não');

      expect(exception, isA<ExpectedFailure>());
    });

    test('carries the domain error code when the response had one', () {
      const exception =
          BillPaymentRuleException('duplicado', code: 'BLP.BIL02');

      expect(exception.code, 'BLP.BIL02');
      expect(exception.toString(), contains('BLP.BIL02'));
    });
  });

  group('BillPaymentNetworkException', () {
    test('is not an expected failure so it gets reported', () {
      const exception = BillPaymentNetworkException('timeout');

      expect(exception, isNot(isA<ExpectedFailure>()));
    });
  });

  group('billPaymentErrorMessage', () {
    test('prefers the rule message the server wrote', () {
      const error = BillPaymentRuleException('Este boleto já está sob gestão.');

      final message = billPaymentErrorMessage(error, fallback: 'Falhou.');

      expect(message, 'Este boleto já está sob gestão.');
    });

    test('translates access denied into a permission message', () {
      final message = billPaymentErrorMessage(
        const AccessDeniedException(),
        fallback: 'Falhou.',
      );

      expect(message, 'Você não tem permissão para esta ação.');
    });

    test('translates session expiry into a login message', () {
      final message = billPaymentErrorMessage(
        const SessionExpiredException(),
        fallback: 'Falhou.',
      );

      expect(message, 'Sua sessão expirou. Entre novamente.');
    });

    test('unwraps the cause of a network exception before falling back', () {
      const error = BillPaymentNetworkException(AccessDeniedException());

      final message = billPaymentErrorMessage(error, fallback: 'Falhou.');

      expect(message, 'Você não tem permissão para esta ação.');
    });

    test('uses the fallback when there is nothing better to show', () {
      final message = billPaymentErrorMessage(
        const BillPaymentNetworkException('socket closed'),
        fallback: 'Não foi possível carregar.',
      );

      expect(message, 'Não foi possível carregar.');
    });

    test('surfaces the first server message of an http exception', () {
      const error = HttpException(
        statusCode: 409,
        message: 'HTTP 409: Conflict',
        serverMessages: ['Já existe.'],
      );

      final message = billPaymentErrorMessage(error, fallback: 'Falhou.');

      expect(message, 'Já existe.');
    });
  });
}
