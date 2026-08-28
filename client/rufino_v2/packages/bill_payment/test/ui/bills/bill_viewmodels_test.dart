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
}
