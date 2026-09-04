import 'package:bill_payment/bill_payment.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:rufino_core/rufino_core.dart';

import '../fakes/fakes.dart';

class MockTrustedOriginApiService extends Mock
    implements TrustedOriginApiService {}

void main() {
  late MockTrustedOriginApiService apiService;
  late FakeErrorReporter reporter;
  late TrustedOriginRepositoryImpl repository;

  setUp(() {
    apiService = MockTrustedOriginApiService();
    reporter = FakeErrorReporter();
    repository = TrustedOriginRepositoryImpl(
      apiService: apiService,
      reporter: reporter,
    );
  });

  group('TrustedOriginRepositoryImpl', () {
    test('an unknown sender resolves to null — a state, not an error',
        () async {
      when(() => apiService.resolveSender(any()))
          .thenAnswer((_) async => null);

      final result = await repository.resolveSender('x@desconhecido.com.br');

      result.fold(
        onSuccess: (origin) => expect(origin, isNull),
        onError: (error, _) => fail('204 is a state, not an error: $error'),
      );
      expect(reporter.capturedErrors, isEmpty);
    });

    test('a duplicate registration surfaces the domain rule without '
        'reporting', () async {
      when(
        () => apiService.registerOrigin(
          kind: any(named: 'kind'),
          value: any(named: 'value'),
          decision: any(named: 'decision'),
          note: any(named: 'note'),
        ),
      ).thenThrow(
        const HttpException(
          statusCode: 409,
          message: 'HTTP 409',
          serverMessages: ['Origem já cadastrada.'],
          domainErrorId: 'BLP.ORG01',
        ),
      );

      final result = await repository.registerOrigin(
        kind: OriginKinds.emailDomain,
        value: 'fornecedor.com.br',
        decision: TrustDecisions.trusted,
      );

      result.fold(
        onSuccess: (_) => fail('should have failed'),
        onError: (error, _) =>
            expect((error as BillPaymentRuleException).code, 'BLP.ORG01'),
      );
      expect(reporter.capturedErrors, isEmpty);
    });

    test('the mapper round-trips the read model including the decider',
        () async {
      final origin = TrustedOriginMapper.fromJson({
        'id': 'origin-1',
        'kind': 'EmailAddress',
        'value': 'financeiro@fornecedor.com.br',
        'decision': 'Blocked',
        'decidedBy': '0195a1f0-0000-7000-8000-0000000000a1',
        'decidedAt': '2026-08-01T09:00:00Z',
        'note': 'golpe recorrente',
      });

      expect(origin.isBlocked, isTrue);
      expect(origin.decidedBy, '0195a1f0-0000-7000-8000-0000000000a1');
      expect(origin.note, 'golpe recorrente');
    });

    test('an outage is wrapped as a network failure and reported', () async {
      when(() => apiService.getOrigin(any()))
          .thenThrow(const HttpException(statusCode: 502, message: 'HTTP 502'));

      final result = await repository.getOrigin('origin-1');

      result.fold(
        onSuccess: (_) => fail('should have failed'),
        onError: (error, _) =>
            expect(error, isA<BillPaymentNetworkException>()),
      );
      expect(reporter.capturedErrors, hasLength(1));
    });
  });
}
