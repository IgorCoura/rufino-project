import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/expectations/expectation_detail_viewmodel.dart';
import 'package:bill_payment/src/ui/expectations/expectation_form_viewmodel.dart';
import 'package:bill_payment/src/ui/expectations/expectation_list_viewmodel.dart';
import 'package:bill_payment/src/ui/pending/pending_viewmodel.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../fakes/fakes.dart';

void main() {
  group('ExpectationListViewModel', () {
    test('loads the expectations and lands on loaded', () async {
      final repository = FakeExpectationRepository()
        ..expectations = [expectation()];
      final viewModel = ExpectationListViewModel(repository: repository);

      await viewModel.load();

      expect(viewModel.status, ExpectationListStatus.loaded);
      expect(viewModel.items, hasLength(1));
      viewModel.dispose();
    });
  });

  group('ExpectationFormViewModel', () {
    late FakeExpectationRepository repository;
    late FakePayeeRepository payeeRepository;
    late ExpectationFormViewModel viewModel;

    setUp(() {
      repository = FakeExpectationRepository();
      payeeRepository = FakePayeeRepository()
        ..payees = [payee(), payee(id: 'p2', isActive: false)];
      viewModel = ExpectationFormViewModel(
        repository: repository,
        payeeRepository: payeeRepository,
      );
    });

    tearDown(() => viewModel.dispose());

    test('the payee picker offers only active payees', () async {
      await viewModel.loadPayees();

      expect(viewModel.payeeOptions, hasLength(1));
      expect(viewModel.payeeOptions.single.id, 'payee-1');
    });

    test('registering without a payee refuses locally', () async {
      await viewModel.loadPayees();

      final id = await viewModel.register(
        label: 'EDP',
        expectedDueDay: 10,
        observedLeadDays: 7,
      );

      expect(id, isNull);
      expect(viewModel.errorMessage, 'Escolha o beneficiário.');
    });

    test('registering with a payee resolves to the new id', () async {
      await viewModel.loadPayees();
      viewModel.selectPayee('payee-1');

      final id = await viewModel.register(
        label: 'EDP — Casa',
        expectedDueDay: 10,
        observedLeadDays: 7,
        accountReference: 'instalacao-1',
      );

      expect(id, 'exp-new');
      expect(repository.calls, contains('registerExpectation:EDP — Casa'));
    });
  });

  group('ExpectationDetailViewModel', () {
    late FakeExpectationRepository repository;
    late ExpectationDetailViewModel viewModel;

    setUp(() {
      repository = FakeExpectationRepository()
        ..expectations = [expectation()];
      viewModel = ExpectationDetailViewModel(
        repository: repository,
        expectationId: 'exp-1',
      );
    });

    tearDown(() => viewModel.dispose());

    test('pausing records the until date and reloads', () async {
      await viewModel.load();

      final paused = await viewModel.pause(DateTime(2026, 12, 1));

      expect(paused, isTrue);
      expect(
        repository.calls.single,
        startsWith('alterWatch:true:2026-12-01'),
      );
    });

    test('waiving a cycle records the cycle id', () async {
      await viewModel.load();

      final waived = await viewModel.waiveCycle('cycle-9', 'férias');

      expect(waived, isTrue);
      expect(repository.calls, contains('waiveCycle:cycle-9'));
    });

    test('a refused mutation keeps the expectation on screen', () async {
      await viewModel.load();
      repository.setShouldFail(true);

      final deactivated = await viewModel.deactivate(null);

      expect(deactivated, isFalse);
      expect(viewModel.errorMessage, 'regra disse não');
      expect(viewModel.expectation, isNotNull);
    });
  });

  group('PendingViewModel', () {
    test('loads the three lists, the queue count and the onboarding nudge',
        () async {
      final expectations = FakeExpectationRepository()
        ..pending = PendingExpectationsView(
          missing: [pendingExpectation()],
          captureFailed: [
            pendingExpectation(blockedByCaptureItemId: 'item-9'),
          ],
          dueSoon: const [],
        );
      final bills = FakeBillRepository()..bills = [bill(), bill(id: 'b2')];
      final profiles = FakePayerProfileRepository();
      final viewModel = PendingViewModel(
        expectationRepository: expectations,
        billRepository: bills,
        payerProfileRepository: profiles,
      );

      await viewModel.load();

      expect(viewModel.status, PendingStatus.loaded);
      expect(viewModel.view.missing, hasLength(1));
      expect(viewModel.view.captureFailed, hasLength(1));
      expect(viewModel.awaitingApprovalCount, 2);
      expect(viewModel.missingPayerProfile, isTrue,
          reason: 'no profile registered means the banner shows');
      viewModel.dispose();
    });

    test('an existing profile silences the onboarding banner', () async {
      final viewModel = PendingViewModel(
        expectationRepository: FakeExpectationRepository(),
        billRepository: FakeBillRepository(),
        payerProfileRepository: FakePayerProfileRepository()
          ..profile = payerProfile(),
      );

      await viewModel.load();

      expect(viewModel.missingPayerProfile, isFalse);
      viewModel.dispose();
    });

    test('a failed pending load lands on error', () async {
      final viewModel = PendingViewModel(
        expectationRepository: FakeExpectationRepository()
          ..setShouldFail(true),
        billRepository: FakeBillRepository(),
        payerProfileRepository: FakePayerProfileRepository(),
      );

      await viewModel.load();

      expect(viewModel.status, PendingStatus.error);
      viewModel.dispose();
    });
  });
}
