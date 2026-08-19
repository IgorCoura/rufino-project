import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/capture_sources/capture_source_connect_viewmodel.dart';
import 'package:bill_payment/src/ui/capture_sources/capture_source_detail_viewmodel.dart';
import 'package:bill_payment/src/ui/capture_sources/capture_source_list_viewmodel.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../fakes/fakes.dart';

void main() {
  group('CaptureSourceListViewModel', () {
    test('loads the sources and lands on loaded', () async {
      final repository = FakeCaptureSourceRepository()
        ..sources = [captureSource()];
      final viewModel = CaptureSourceListViewModel(repository: repository);

      await viewModel.load();

      expect(viewModel.status, CaptureSourceListStatus.loaded);
      expect(viewModel.items, hasLength(1));
      viewModel.dispose();
    });

    test('no source connected lands on empty', () async {
      final viewModel = CaptureSourceListViewModel(
        repository: FakeCaptureSourceRepository(),
      );

      await viewModel.load();

      expect(viewModel.status, CaptureSourceListStatus.empty);
      viewModel.dispose();
    });
  });

  group('CaptureSourceConnectViewModel', () {
    test('connecting resolves to the new id and carries the shared-mailbox '
        'warning', () async {
      final repository = FakeCaptureSourceRepository()
        ..alreadyMonitored = true;
      final viewModel =
          CaptureSourceConnectViewModel(repository: repository);

      final id = await viewModel.connect(
        displayName: 'Contas',
        address: 'contas@empresa.com.br',
        directoryId: 'dir',
        clientId: 'client',
        clientSecret: 'secret',
      );

      expect(id, 'src-new');
      expect(viewModel.sharedMailboxWarning, isTrue);
      viewModel.dispose();
    });

    test('a refused access proof surfaces the rule message and resolves to '
        'null', () async {
      final repository = FakeCaptureSourceRepository()..setShouldFail(true);
      final viewModel =
          CaptureSourceConnectViewModel(repository: repository);

      final id = await viewModel.connect(
        displayName: 'Contas',
        address: 'contas@empresa.com.br',
        directoryId: 'dir',
        clientId: 'client',
        clientSecret: 'secret',
      );

      expect(id, isNull);
      expect(viewModel.errorMessage, 'regra disse não');
      viewModel.dispose();
    });
  });

  group('CaptureSourceDetailViewModel', () {
    late FakeCaptureSourceRepository repository;
    late CaptureSourceDetailViewModel viewModel;

    setUp(() {
      repository = FakeCaptureSourceRepository()
        ..sources = [captureSource()];
      viewModel = CaptureSourceDetailViewModel(
        repository: repository,
        sourceId: 'src-1',
      );
    });

    tearDown(() => viewModel.dispose());

    test('loads the source', () async {
      await viewModel.load();

      expect(viewModel.status, CaptureSourceDetailStatus.loaded);
      expect(viewModel.source!.address, 'contas@empresa.com.br');
    });

    test('a successful sync records the ingestion counters for the snackbar',
        () async {
      await viewModel.load();

      final synced = await viewModel.syncNow();

      expect(synced, isTrue);
      expect(viewModel.infoMessage, contains('3 novos'));
      expect(repository.calls, contains('syncSource:src-1'));
    });

    test('a denied sync outcome is translated for the person to act on',
        () async {
      repository.syncOutcome = const SyncOutcome(
        id: 'src-1',
        status: SyncStatuses.denied,
        ingestedItems: 0,
        skippedAsAlreadyIngested: 0,
      );
      await viewModel.load();

      await viewModel.syncNow();

      expect(viewModel.infoMessage, 'Acesso negado');
    });

    test('a rescan reports how many cursors were discarded', () async {
      await viewModel.load();

      final rescanned = await viewModel.rescan();

      expect(rescanned, isTrue);
      expect(viewModel.infoMessage, contains('2 cursores'));
    });

    test('a refused folder removal keeps the source on screen with the '
        'message', () async {
      await viewModel.load();
      repository.setShouldFail(true);

      final removed = await viewModel.removeFolder('Contas');

      expect(removed, isFalse);
      expect(viewModel.errorMessage, 'regra disse não');
      expect(viewModel.source, isNotNull);
    });
  });
}
