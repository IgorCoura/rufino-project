import 'package:bill_payment/bill_payment.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:rufino_core/rufino_core.dart';

import '../fakes/fakes.dart';

class MockBillApiService extends Mock implements BillApiService {}

void main() {
  late MockBillApiService apiService;
  late FakeErrorReporter reporter;
  late BillRepositoryImpl repository;

  setUp(() {
    apiService = MockBillApiService();
    reporter = FakeErrorReporter();
    repository =
        BillRepositoryImpl(apiService: apiService, reporter: reporter);
  });

  group('BillRepositoryImpl', () {
    test('a duplicate import surfaces BLP.BIL02 without reporting', () async {
      when(
        () => apiService.importBill(
          digitableLine: any(named: 'digitableLine'),
          pixPayload: any(named: 'pixPayload'),
        ),
      ).thenThrow(
        const HttpException(
          statusCode: 409,
          message: 'HTTP 409',
          serverMessages: ['Este boleto já está sob gestão.'],
          domainErrorId: 'BLP.BIL02',
        ),
      );

      final result = await repository.importBill(digitableLine: '3419...');

      result.fold(
        onSuccess: (_) => fail('should have failed'),
        onError: (error, _) =>
            expect((error as BillPaymentRuleException).code, 'BLP.BIL02'),
      );
      expect(reporter.capturedErrors, isEmpty);
    });

    test('a stale-snapshot approval surfaces the domain rule without '
        'reporting', () async {
      when(
        () => apiService.approveBill(
          any(),
          scheduleFor: any(named: 'scheduleFor'),
          note: any(named: 'note'),
        ),
      ).thenThrow(
        const HttpException(
          statusCode: 409,
          message: 'HTTP 409',
          serverMessages: ['O retrato da consulta está velho. Revalide.'],
          domainErrorId: 'BLP.BIL21',
        ),
      );

      final result = await repository.approveBill(
        'bill-1',
        scheduleFor: DateTime(2026, 8, 20),
      );

      result.fold(
        onSuccess: (_) => fail('should have failed'),
        onError: (error, _) =>
            expect((error as BillPaymentRuleException).code, 'BLP.BIL21'),
      );
      expect(reporter.capturedErrors, isEmpty);
    });

    test('a revalidation resolves to the run outcome', () async {
      when(() => apiService.revalidateBill(any())).thenAnswer(
        (_) async => const ValidationRunOutcome(
          id: 'bill-1',
          status: 'AwaitingApproval',
          blockingFailures: 0,
          attentionItems: 2,
        ),
      );

      final result = await repository.revalidateBill('bill-1');

      result.fold(
        onSuccess: (outcome) {
          expect(outcome.status, 'AwaitingApproval');
          expect(outcome.attentionItems, 2);
        },
        onError: (error, _) => fail('should have succeeded: $error'),
      );
    });

    test('the schedule preview flows through as the entity', () async {
      when(() => apiService.previewSchedule(any(), any())).thenAnswer(
        (_) async => SchedulePreview(
          requestedDate: DateTime(2026, 9, 10),
          effectiveDate: DateTime(2026, 9, 11),
          slid: true,
          immediate: false,
        ),
      );

      final result =
          await repository.previewSchedule('bill-1', DateTime(2026, 9, 10));

      result.fold(
        onSuccess: (preview) {
          expect(preview.effectiveDate, DateTime(2026, 9, 11));
          expect(preview.slid, isTrue);
        },
        onError: (error, _) => fail('should have succeeded: $error'),
      );
      expect(reporter.capturedErrors, isEmpty);
    });

    test('a 5xx on the list is wrapped and reported', () async {
      when(
        () => apiService.listBills(
          status: any(named: 'status'),
          cursor: any(named: 'cursor'),
          limit: any(named: 'limit'),
        ),
      ).thenThrow(const HttpException(statusCode: 500, message: 'HTTP 500'));

      final result = await repository.listBills();

      result.fold(
        onSuccess: (_) => fail('should have failed'),
        onError: (error, _) =>
            expect(error, isA<BillPaymentNetworkException>()),
      );
      expect(reporter.capturedErrors, hasLength(1));
    });
  });

  group('BillApiService helpers', () {
    test('dates travel as yyyy-MM-dd for the DateOnly fields', () {
      expect(BillApiService.dateOnly(DateTime(2026, 8, 3)), '2026-08-03');
      expect(BillApiService.dateOnly(DateTime(2026, 12, 25)), '2026-12-25');
    });
  });

  group('BillMapper', () {
    test('maps the detail with checks, approval and origin', () {
      final detail = BillMapper.detailFromJson({
        'id': 'bill-1',
        'status': 'Approved',
        'kind': 'BankSlip',
        'rail': 'Pix',
        'beneficiary': {
          'name': 'EDP SAO PAULO SA',
          'tradingName': 'EDP',
          'taxId': '02.302.100/0001-06',
        },
        'amount': 615.07,
        'originalAmount': 600.0,
        'dueDate': '2026-08-20T00:00:00Z',
        'bankCode': '033',
        'minimumScheduleDate': '2026-08-18T00:00:00Z',
        'lastConsultedAt': '2026-08-17T08:00:00Z',
        'checks': [
          {
            'type': 'PayeeMatch',
            'outcome': 'Passed',
            'severity': 'Blocking',
            'reasonCode': null,
            'evidence': null,
            'isBlockingFailure': false,
            'evaluatedAt': '2026-08-17T08:00:00Z',
          },
        ],
        'approval': {
          'decidedBy': '0195a1f0-0000-7000-8000-00000000000a',
          'decision': 'Approved',
          'decidedAt': '2026-08-17T09:00:00Z',
          'note': 'ok',
        },
        'scheduledFor': '2026-08-20T00:00:00Z',
        'origin': {
          'sourceKind': 'Mailbox',
          'sourceId': 'src-1',
          'senderAddress': 'cobranca@edp.com.br',
          'receivedAt': '2026-08-15T10:00:00Z',
        },
        'createdAt': '2026-08-15T10:05:00Z',
      });

      expect(detail.beneficiary!.displayName, 'EDP');
      expect(detail.checks, hasLength(1));
      expect(detail.approval!.decision, 'Approved');
      expect(detail.scheduledFor, isNotNull);
      expect(detail.origin.senderAddress, 'cobranca@edp.com.br');
    });

    test('carries the reading status so the detail can say it is queued', () {
      final detail = BillMapper.detailFromJson({
        'id': 'bill-3',
        'status': 'AwaitingApproval',
        'kind': 'BankSlip',
        'rail': 'Boleto',
        'readingStatus': 'Queued',
        'reading': null,
        'checks': const [],
        'origin': {
          'sourceKind': 'Mailbox',
          'sourceId': 'src-1',
          'senderAddress': 'noreply@omie.com.br',
          'receivedAt': '2026-08-27T14:54:56Z',
        },
        'createdAt': '2026-08-28T13:49:37Z',
      });

      expect(detail.readingStatus, ReadingStatuses.queued);
      expect(detail.isReadingQueued, isTrue);
      expect(detail.reading, isNull);
    });

    // The field only reached the detail contract on 2026-08-28. A response from
    // before that must not read as "queued forever" — absent means the server
    // has nothing to say, which is what notApplicable expresses.
    test('an absent reading status falls back to not applicable', () {
      final detail = BillMapper.detailFromJson({
        'id': 'bill-4',
        'status': 'AwaitingApproval',
        'kind': 'BankSlip',
        'rail': 'Boleto',
        'checks': const [],
        'origin': {
          'sourceKind': 'ManualUpload',
          'sourceId': null,
          'senderAddress': null,
          'receivedAt': '2026-08-15T10:00:00Z',
        },
        'createdAt': '2026-08-15T10:05:00Z',
      });

      expect(detail.readingStatus, ReadingStatuses.notApplicable);
      expect(detail.isReadingQueued, isFalse);
    });

    test('maps a bill whose optional fields are all absent', () {
      final bill = BillMapper.fromJson({
        'id': 'bill-2',
        'status': 'Captured',
        'kind': 'Utility',
        'rail': 'Boleto',
        'amount': null,
        'dueDate': null,
        'bankCode': null,
        'origin': {
          'sourceKind': 'ManualUpload',
          'sourceId': null,
          'senderAddress': null,
          'receivedAt': '2026-08-15T10:00:00Z',
        },
        'createdAt': '2026-08-15T10:05:00Z',
      });

      expect(bill.amount, isNull);
      expect(bill.bankCode, isNull);
      expect(bill.isTerminal, isFalse);
    });
  });
}
