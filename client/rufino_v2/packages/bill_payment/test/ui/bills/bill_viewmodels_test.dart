import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/bills/bill_detail_viewmodel.dart';
import 'package:bill_payment/src/ui/bills/bill_import_viewmodel.dart';
import 'package:bill_payment/src/ui/bills/bill_list_viewmodel.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../fakes/fakes.dart';

void main() {
  final now = DateTime(2026, 8, 17, 12);

  group('BillListViewModel', () {
    late FakeBillRepository repository;
    late BillListViewModel viewModel;

    setUp(() {
      repository = FakeBillRepository();
      viewModel = BillListViewModel(
        repository: repository,
        initialStatus: BillStatuses.awaitingApproval,
      );
    });

    tearDown(() => viewModel.dispose());

    test('opens on the approval queue when told to', () async {
      repository.bills = [
        bill(),
        bill(id: 'b2', status: BillStatuses.approved),
      ];

      await viewModel.load();

      expect(repository.lastStatusFilter, BillStatuses.awaitingApproval);
      expect(viewModel.items, hasLength(1));
    });

    test('selecting null asks the server for everything', () async {
      repository.bills = [
        bill(),
        bill(id: 'b2', status: BillStatuses.approved),
      ];

      await viewModel.selectStatus(null);

      expect(repository.lastStatusFilter, isNull);
      expect(viewModel.items, hasLength(2));
    });

    test('a failed load lands on error with a message', () async {
      repository.setShouldFail(true);

      await viewModel.load();

      expect(viewModel.status, BillListStatus.error);
      expect(viewModel.errorMessage, isNotNull);
    });
  });

  group('BillDetailViewModel approval gate', () {
    late FakeBillRepository repository;
    late BillDetailViewModel viewModel;

    BillDetailViewModel build() => BillDetailViewModel(
          repository: repository,
          billId: 'bill-1',
          clock: () => now,
        );

    setUp(() => repository = FakeBillRepository());

    test('a fresh snapshot on awaiting approval enables the approve button',
        () async {
      repository.detail = billDetail(
        lastConsultedAt: now.subtract(const Duration(hours: 1)),
      );
      viewModel = build();

      await viewModel.load();

      expect(viewModel.canApprove, isTrue);
      expect(viewModel.isSnapshotStale, isFalse);
      viewModel.dispose();
    });

    test('a stale snapshot disables approve and leaves revalidate as the '
        'way back', () async {
      repository.detail = billDetail(
        lastConsultedAt: now.subtract(const Duration(hours: 13)),
      );
      viewModel = build();

      await viewModel.load();

      expect(viewModel.canApprove, isFalse);
      expect(viewModel.isSnapshotStale, isTrue);
      expect(viewModel.bill!.acceptsValidation, isTrue);
      viewModel.dispose();
    });

    test('revalidating reloads the bill', () async {
      repository.detail = billDetail(lastConsultedAt: now);
      viewModel = build();
      await viewModel.load();

      final revalidated = await viewModel.revalidate();

      expect(revalidated, isTrue);
      expect(repository.calls, contains('revalidateBill:bill-1'));
      viewModel.dispose();
    });

    test('a stale-snapshot 409 from the server surfaces the rule message',
        () async {
      repository.detail = billDetail(lastConsultedAt: now);
      viewModel = build();
      await viewModel.load();
      repository.setShouldFail(true);

      final approved = await viewModel.approve(
        scheduleFor: DateTime(2026, 8, 20),
      );

      expect(approved, isFalse);
      expect(viewModel.errorMessage, 'regra disse não');
      viewModel.dispose();
    });

    test('denying records the mandatory reason', () async {
      repository.detail = billDetail(lastConsultedAt: now);
      viewModel = build();
      await viewModel.load();

      await viewModel.deny('não reconheço este fornecedor');

      expect(
        repository.calls,
        contains('denyBill:não reconheço este fornecedor'),
      );
      viewModel.dispose();
    });

    test('the earliest schedule date respects the provider minimum',
        () async {
      final min = now.add(const Duration(days: 2));
      repository.detail = billDetail(
        lastConsultedAt: now,
        minimumScheduleDate: min,
      );
      viewModel = build();

      await viewModel.load();

      expect(viewModel.earliestScheduleDate, min);
      viewModel.dispose();
    });

    test('the schedule preview resolves the server answer', () async {
      repository.detail = billDetail(lastConsultedAt: now);
      repository.schedulePreview = SchedulePreview(
        requestedDate: DateTime(2026, 9, 10),
        effectiveDate: DateTime(2026, 9, 11),
        slid: true,
        immediate: false,
      );
      viewModel = build();
      await viewModel.load();

      final preview = await viewModel.previewSchedule(DateTime(2026, 9, 10));

      expect(preview!.effectiveDate, DateTime(2026, 9, 11));
      expect(preview.slid, isTrue);
      viewModel.dispose();
    });

    test('a preview failure resolves null and never touches the error '
        'message — it is informative only', () async {
      repository.detail = billDetail(lastConsultedAt: now);
      repository.previewShouldFail = true;
      viewModel = build();
      await viewModel.load();

      final preview = await viewModel.previewSchedule(DateTime(2026, 9, 10));

      expect(preview, isNull);
      expect(viewModel.errorMessage, isNull);
      viewModel.dispose();
    });

    test('a rule refusal exposes its domain code, and the next success '
        'clears it', () async {
      repository.detail = billDetail(lastConsultedAt: now);
      viewModel = build();
      await viewModel.load();
      repository.scriptedApproveRefusals.add(
        const BillPaymentRuleException(
          'Boleto vencido exige o aceite.',
          code: 'BLP.BIL35',
        ),
      );

      final refused = await viewModel.approve(
        scheduleFor: DateTime(2026, 8, 20),
      );

      expect(refused, isFalse);
      expect(viewModel.lastErrorCode, 'BLP.BIL35');

      final approved = await viewModel.approve(
        scheduleFor: DateTime(2026, 8, 20),
        acknowledgeImmediateExecution: true,
      );

      expect(approved, isTrue);
      expect(viewModel.lastErrorCode, isNull);
      viewModel.dispose();
    });
  });

  group('BillImportViewModel', () {
    test('importing resolves to the new id with its kind and rail',
        () async {
      final repository = FakeBillRepository();
      final viewModel = BillImportViewModel(repository: repository);

      final id = await viewModel.import(digitableLine: '3419...');

      expect(id, 'bill-new');
      expect(viewModel.outcome!.rail, 'Boleto');
      viewModel.dispose();
    });

    test('a duplicate import surfaces the rule message and resolves to null',
        () async {
      final repository = FakeBillRepository()..setShouldFail(true);
      final viewModel = BillImportViewModel(repository: repository);

      final id = await viewModel.import(digitableLine: '3419...');

      expect(id, isNull);
      expect(viewModel.errorMessage, 'regra disse não');
      viewModel.dispose();
    });

    test('sends the attached file along with the import', () async {
      final repository = FakeBillRepository();
      final viewModel = BillImportViewModel(repository: repository)
        ..setDocument((
          bytes: const [1, 2, 3],
          fileName: 'boleto.pdf',
          contentType: 'application/pdf',
        ));

      await viewModel.import();

      expect(repository.lastImport!.documentBytes, const [1, 2, 3]);
      expect(repository.lastImport!.documentFileName, 'boleto.pdf');
      expect(repository.lastImport!.documentContentType, 'application/pdf');
      viewModel.dispose();
    });

    test('imports with no file when the attachment was removed', () async {
      final repository = FakeBillRepository();
      final viewModel = BillImportViewModel(repository: repository)
        ..setDocument((
          bytes: const [1, 2, 3],
          fileName: 'boleto.pdf',
          contentType: 'application/pdf',
        ))
        ..setDocument(null);

      await viewModel.import(digitableLine: '3419...');

      expect(viewModel.document, isNull);
      expect(repository.lastImport!.documentBytes, isNull);
      viewModel.dispose();
    });

    test('attaching a file clears the error left by the previous attempt',
        () async {
      final repository = FakeBillRepository()..setShouldFail(true);
      final viewModel = BillImportViewModel(repository: repository);

      await viewModel.import(digitableLine: '3419...');
      expect(viewModel.errorMessage, isNotNull);

      viewModel.setDocument((
        bytes: const [1],
        fileName: 'boleto.pdf',
        contentType: 'application/pdf',
      ));

      expect(viewModel.errorMessage, isNull);
      viewModel.dispose();
    });
  });

  group('BillDetailViewModel payment section (phase 3)', () {
    late FakeBillRepository repository;
    late FakePaymentRepository paymentRepository;
    late BillDetailViewModel viewModel;

    BillDetailViewModel build() => BillDetailViewModel(
          repository: repository,
          billId: 'bill-1',
          paymentRepository: paymentRepository,
          clock: () => now,
        );

    setUp(() {
      repository = FakeBillRepository();
      paymentRepository = FakePaymentRepository();
    });

    tearDown(() => viewModel.dispose());

    test('loads the payment order once the bill status is committed',
        () async {
      repository.detail = billDetail(status: BillStatuses.scheduled);
      paymentRepository.order = paymentOrder();
      viewModel = build();

      await viewModel.load();

      expect(viewModel.payment?.id, 'order-1');
      expect(paymentRepository.calls, contains('getForBill:bill-1'));
    });

    test('skips the payment lookup while the bill is still under decision',
        () async {
      repository.detail = billDetail();
      paymentRepository.order = paymentOrder();
      viewModel = build();

      await viewModel.load();

      expect(viewModel.payment, isNull);
      expect(paymentRepository.calls, isEmpty);
    });

    // Falha na leitura do pagamento nunca derruba o detalhe: o boleto está
    // na tela de qualquer jeito, e a seção apenas silencia.
    test('a payment read failure never breaks the loaded detail', () async {
      repository.detail = billDetail(status: BillStatuses.paid);
      paymentRepository.setShouldFail(true);
      viewModel = build();

      await viewModel.load();

      expect(viewModel.status, BillDetailStatus.loaded);
      expect(viewModel.payment, isNull);
      expect(viewModel.errorMessage, isNull);
    });

    test('cancelPayment delegates to the order and reloads the detail',
        () async {
      repository.detail = billDetail(status: BillStatuses.scheduled);
      paymentRepository.order = paymentOrder();
      viewModel = build();
      await viewModel.load();

      final succeeded = await viewModel.cancelPayment();

      expect(succeeded, isTrue);
      expect(paymentRepository.calls, contains('cancel:order-1'));
      expect(
        paymentRepository.calls.where((c) => c == 'getForBill:bill-1'),
        hasLength(2),
      );
    });

    test('cancelPayment without a loaded order resolves false silently',
        () async {
      repository.detail = billDetail();
      viewModel = build();
      await viewModel.load();

      expect(await viewModel.cancelPayment(), isFalse);
      expect(paymentRepository.calls, isEmpty);
    });

    test('confirmImmediatePayment delegates to the order awaiting consent',
        () async {
      repository.detail = billDetail(status: BillStatuses.approved);
      paymentRepository.order = paymentOrder(
        hold: PaymentOrderHolds.awaitingConfirmation,
        requiresConfirmation: true,
      );
      viewModel = build();
      await viewModel.load();

      final succeeded = await viewModel.confirmImmediatePayment();

      expect(succeeded, isTrue);
      expect(paymentRepository.calls, contains('confirmImmediate:order-1'));
    });

    test('reopen sends the failed bill back to the decision queue', () async {
      repository.detail = billDetail(status: BillStatuses.failed);
      viewModel = build();
      await viewModel.load();

      final succeeded = await viewModel.reopen();

      expect(succeeded, isTrue);
      expect(repository.calls, contains('reopenBill:bill-1'));
    });

    // O aceite do vencido (ADR-017) precisa atravessar o ViewModel intacto.
    test('approve carries the immediate-execution acknowledgement', () async {
      repository.detail = billDetail(
        lastConsultedAt: now.subtract(const Duration(hours: 1)),
        dueDate: now.subtract(const Duration(days: 3)),
      );
      viewModel = build();
      await viewModel.load();

      await viewModel.approve(
        scheduleFor: now.add(const Duration(days: 2)),
        acknowledgeImmediateExecution: true,
      );

      expect(repository.lastApproveImmediateAck, isTrue);
    });

    test('isOverdue follows the due date against the clock', () async {
      repository.detail = billDetail(
        dueDate: now.subtract(const Duration(days: 1)),
      );
      viewModel = build();
      await viewModel.load();
      expect(viewModel.isOverdue, isTrue);

      final current = BillDetailViewModel(
        repository: repository..detail = billDetail(),
        billId: 'bill-1',
        clock: () => now,
      );
      await current.load();
      expect(current.isOverdue, isFalse);
      current.dispose();
    });
  });
}
