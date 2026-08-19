import 'package:bill_payment/bill_payment.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:rufino_core/rufino_core.dart';

import '../fakes/fakes.dart';

class MockExpectationApiService extends Mock
    implements ExpectationApiService {}

void main() {
  late MockExpectationApiService apiService;
  late FakeErrorReporter reporter;
  late ExpectationRepositoryImpl repository;

  setUp(() {
    apiService = MockExpectationApiService();
    reporter = FakeErrorReporter();
    repository = ExpectationRepositoryImpl(
      apiService: apiService,
      reporter: reporter,
    );
  });

  group('ExpectationRepositoryImpl', () {
    test('a duplicate account reference surfaces the domain rule without '
        'reporting', () async {
      when(
        () => apiService.registerExpectation(
          payeeId: any(named: 'payeeId'),
          label: any(named: 'label'),
          recurrence: any(named: 'recurrence'),
          expectedDueDay: any(named: 'expectedDueDay'),
          observedLeadDays: any(named: 'observedLeadDays'),
          accountReference: any(named: 'accountReference'),
          alertLeadDays: any(named: 'alertLeadDays'),
        ),
      ).thenThrow(
        const HttpException(
          statusCode: 409,
          message: 'HTTP 409',
          serverMessages: ['Já existe expectativa para esta conta.'],
          domainErrorId: 'BLP.EXP01',
        ),
      );

      final result = await repository.registerExpectation(
        payeeId: 'payee-1',
        label: 'EDP — Casa Florentino',
        recurrence: Recurrences.monthly,
        expectedDueDay: 10,
        observedLeadDays: 7,
      );

      result.fold(
        onSuccess: (_) => fail('should have failed'),
        onError: (error, _) =>
            expect((error as BillPaymentRuleException).code, 'BLP.EXP01'),
      );
      expect(reporter.capturedErrors, isEmpty);
    });

    test('the pending panel keeps its three lists apart', () async {
      when(
        () => apiService.getPending(
          dueSoonWindowDays: any(named: 'dueSoonWindowDays'),
        ),
      ).thenAnswer(
        (_) async => ExpectationMapper.pendingViewFromJson({
          'missing': [
            {
              'expectationId': 'exp-1',
              'cycleId': 'cycle-1',
              'label': 'EDP — Casa Florentino',
              'competence': '2026-08',
              'expectedDueDate': '2026-08-20T00:00:00Z',
              'status': 'Missing',
              'missReason': 'NeverArrived',
              'arrived': false,
              'lastAlertLevel': 'Warning',
            },
          ],
          'captureFailed': [
            {
              'expectationId': 'exp-2',
              'cycleId': 'cycle-2',
              'label': 'Vivo Fibra',
              'competence': '2026-08',
              'expectedDueDate': '2026-08-22T00:00:00Z',
              'status': 'PartiallyCaptured',
              'missReason': 'Locked',
              'arrived': true,
              'blockedByCaptureItemId': 'item-9',
            },
          ],
          'dueSoon': [],
        }),
      );

      final result = await repository.getPending();

      result.fold(
        onSuccess: (view) {
          expect(view.missing, hasLength(1));
          expect(view.captureFailed.single.blockedByCaptureItemId, 'item-9');
          expect(view.dueSoon, isEmpty);
          expect(view.actionableCount, 2);
        },
        onError: (error, _) => fail('should have succeeded: $error'),
      );
    });

    test('the mapper reads an expectation with its cycles', () {
      final expectation = ExpectationMapper.fromJson({
        'id': 'exp-1',
        'payeeId': 'payee-1',
        'accountReference': 'instalacao-748299879',
        'label': 'EDP — Casa Florentino',
        'recurrence': 'Monthly',
        'expectedDueDay': 20,
        'observedLeadDays': 7,
        'alertLeadDays': 5,
        'origin': 'Learned',
        'observationCount': 6,
        'isActive': true,
        'pausedUntil': null,
        'cycles': [
          {
            'id': 'cycle-1',
            'competence': '2026-08',
            'expectedDueDate': '2026-08-20T00:00:00Z',
            'alertAt': '2026-08-15T00:00:00Z',
            'status': 'Waiting',
            'missReason': null,
            'arrived': null,
            'fulfilledByBillId': null,
            'blockedByCaptureItemId': null,
            'lastAlertLevel': null,
          },
        ],
      });

      expect(expectation.accountReference, 'instalacao-748299879');
      expect(expectation.origin, 'Learned');
      expect(expectation.cycles.single.isOpen, isTrue);
    });

    test('an outage on the panel is wrapped and reported', () async {
      when(
        () => apiService.getPending(
          dueSoonWindowDays: any(named: 'dueSoonWindowDays'),
        ),
      ).thenThrow(const HttpException(statusCode: 503, message: 'HTTP 503'));

      final result = await repository.getPending();

      result.fold(
        onSuccess: (_) => fail('should have failed'),
        onError: (error, _) =>
            expect(error, isA<BillPaymentNetworkException>()),
      );
      expect(reporter.capturedErrors, hasLength(1));
    });
  });
}
