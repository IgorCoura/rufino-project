import 'package:bill_payment/bill_payment.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  final now = DateTime(2026, 8, 17, 12);

  group('BillDetail snapshot staleness', () {
    test('a snapshot older than twelve hours is stale', () {
      final detail = _detail(
        lastConsultedAt: now.subtract(const Duration(hours: 12, minutes: 1)),
      );

      expect(detail.isSnapshotStaleAt(now), isTrue);
    });

    test('a snapshot exactly twelve hours old is still fresh', () {
      final detail = _detail(
        lastConsultedAt: now.subtract(const Duration(hours: 12)),
      );

      expect(detail.isSnapshotStaleAt(now), isFalse);
    });

    test('a bill never consulted counts as stale', () {
      final detail = _detail(lastConsultedAt: null);

      expect(detail.isSnapshotStaleAt(now), isTrue);
    });

    test('the deadline the server resolved wins over the local constant', () {
      // O retrato tem 1 hora pela conta local, e ainda assim venceu: quem manda é a
      // política do servidor, que pode ter sido encurtada.
      final detail = _detail(
        lastConsultedAt: now.subtract(const Duration(hours: 1)),
        snapshotExpiresAt: now.subtract(const Duration(minutes: 5)),
      );

      expect(detail.isSnapshotStaleAt(now), isTrue);
    });

    test('a deadline still ahead keeps the snapshot fresh past twelve hours',
        () {
      final detail = _detail(
        lastConsultedAt: now.subtract(const Duration(hours: 20)),
        snapshotExpiresAt: now.add(const Duration(hours: 4)),
      );

      expect(detail.isSnapshotStaleAt(now), isFalse);
    });
  });

  group('BillDetail schedule gate', () {
    test('schedule is enabled on an approved bill with a fresh snapshot', () {
      final detail = _detail(
        status: BillStatuses.approved,
        lastConsultedAt: now.subtract(const Duration(hours: 1)),
      );

      expect(detail.canScheduleAt(now), isTrue);
    });

    // A regressão relatada: o boleto aprovado ficava com "Agendar…" habilitado e o
    // clique voltava BLP.BIL06, porque o aviso de retrato velho era gated em
    // acceptsDecision — que um aprovado não satisfaz.
    test('schedule is disabled when the snapshot went stale', () {
      final detail = _detail(
        status: BillStatuses.approved,
        lastConsultedAt: now.subtract(const Duration(hours: 13)),
      );

      expect(detail.acceptsScheduling, isTrue,
          reason: 'status alone still allows it — the snapshot is what blocks');
      expect(detail.canScheduleAt(now), isFalse);
      expect(detail.acceptsValidation, isTrue,
          reason: 'revalidation must be offered as the way back');
    });

    test('schedule is disabled once a date was already picked', () {
      final detail = _detail(
        status: BillStatuses.approved,
        lastConsultedAt: now,
        scheduledFor: DateTime(2026, 8, 20),
      );

      expect(detail.canScheduleAt(now), isFalse);
    });
  });

  group('BillDetail approval gate', () {
    test('approve is enabled on awaiting approval with a fresh snapshot', () {
      final detail = _detail(
        status: BillStatuses.awaitingApproval,
        lastConsultedAt: now.subtract(const Duration(hours: 1)),
      );

      expect(detail.canApproveAt(now), isTrue);
    });

    test('approve is disabled when the snapshot went stale', () {
      final detail = _detail(
        status: BillStatuses.awaitingApproval,
        lastConsultedAt: now.subtract(const Duration(hours: 13)),
      );

      expect(detail.canApproveAt(now), isFalse);
      expect(detail.acceptsValidation, isTrue,
          reason: 'revalidation must be offered as the way back');
    });

    test('approve is disabled outside awaiting approval whatever the '
        'snapshot age', () {
      final detail = _detail(
        status: BillStatuses.approved,
        lastConsultedAt: now,
      );

      expect(detail.canApproveAt(now), isFalse);
    });
  });

  group('BillDetail schedule date', () {
    test('today wins when the provider minimum is in the past', () {
      final detail = _detail(
        minimumScheduleDate: now.subtract(const Duration(days: 3)),
      );

      expect(detail.earliestScheduleDate(now), now);
    });

    test('the provider minimum wins when it is later than today', () {
      final min = now.add(const Duration(days: 2));
      final detail = _detail(minimumScheduleDate: min);

      expect(detail.earliestScheduleDate(now), min);
    });

    test('today is the floor when the provider declared no minimum', () {
      final detail = _detail(minimumScheduleDate: null);

      expect(detail.earliestScheduleDate(now), now);
    });
  });

  group('BillDetail checks summary', () {
    test('counts blocking failures and filters attention checks', () {
      final detail = _detail(checks: [
        _check(CheckTypes.duplicate, CheckOutcomes.passed, blocking: false),
        _check(CheckTypes.payeeMatch, CheckOutcomes.failed, blocking: true),
        _check(CheckTypes.amountMatch, CheckOutcomes.inconclusive,
            blocking: false),
      ]);

      expect(detail.blockingFailures, 1);
      expect(detail.attentionChecks, hasLength(2));
    });
  });

  group('BillPage', () {
    test('has more pages exactly while a cursor exists', () {
      const withCursor = BillPage(items: [], nextCursor: 'abc');
      const lastPage = BillPage(items: []);

      expect(withCursor.hasMore, isTrue);
      expect(lastPage.hasMore, isFalse);
    });
  });
}

BillDetail _detail({
  String status = BillStatuses.awaitingApproval,
  DateTime? lastConsultedAt,
  DateTime? snapshotExpiresAt,
  DateTime? minimumScheduleDate,
  DateTime? scheduledFor,
  List<BillCheck> checks = const [],
}) {
  return BillDetail(
    id: 'bill-1',
    status: status,
    kind: BillKinds.bankSlip,
    rail: PaymentRails.boleto,
    checks: checks,
    origin: BillOrigin(
      sourceKind: BillSourceKinds.manualUpload,
      receivedAt: DateTime(2026, 8, 1),
    ),
    createdAt: DateTime(2026, 8, 1),
    lastConsultedAt: lastConsultedAt,
    snapshotExpiresAt: snapshotExpiresAt,
    minimumScheduleDate: minimumScheduleDate,
    scheduledFor: scheduledFor,
  );
}

BillCheck _check(String type, String outcome, {required bool blocking}) {
  return BillCheck(
    type: type,
    outcome: outcome,
    severity: blocking ? CheckSeverities.blocking : CheckSeverities.advisory,
    isBlockingFailure: blocking && outcome == CheckOutcomes.failed,
    evaluatedAt: DateTime(2026, 8, 1),
  );
}
