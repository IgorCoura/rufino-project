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
    test('connecting resolves to the new id', () async {
      final repository = FakeCaptureSourceRepository();
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

  group('CaptureSource capture floor', () {
    test('connecting hands the chosen floor to the repository', () async {
      final repository = FakeCaptureSourceRepository();
      final viewModel = CaptureSourceConnectViewModel(repository: repository);

      await viewModel.connect(
        displayName: 'Contas',
        address: 'contas@empresa.com.br',
        directoryId: 'dir',
        clientId: 'app',
        clientSecret: 'segredo',
        captureSince: DateTime(2026, 5, 27),
      );

      expect(repository.lastCaptureSince, DateTime(2026, 5, 27));
      viewModel.dispose();
    });

    test('connecting without a floor reads the whole mailbox', () async {
      final repository = FakeCaptureSourceRepository();
      final viewModel = CaptureSourceConnectViewModel(repository: repository);

      await viewModel.connect(
        displayName: 'Contas',
        address: 'contas@empresa.com.br',
        directoryId: 'dir',
        clientId: 'app',
        clientSecret: 'segredo',
      );

      expect(repository.lastCaptureSince, isNull);
      viewModel.dispose();
    });

    test('changing the floor reaches the repository', () async {
      final repository = FakeCaptureSourceRepository()
        ..sources = [captureSource()];
      final viewModel = CaptureSourceDetailViewModel(
        repository: repository,
        sourceId: 'src-1',
      );
      await viewModel.load();

      final changed = await viewModel.changeCaptureSince(DateTime(2026, 5, 27));

      expect(changed, isTrue);
      expect(repository.lastCaptureSince, DateTime(2026, 5, 27));
      viewModel.dispose();
    });

    test('clearing the floor returns the source to the whole mailbox',
        () async {
      final repository = FakeCaptureSourceRepository()
        ..sources = [captureSource(captureSince: DateTime(2026, 5, 27))];
      final viewModel = CaptureSourceDetailViewModel(
        repository: repository,
        sourceId: 'src-1',
      );
      await viewModel.load();

      final changed = await viewModel.changeCaptureSince(null);

      expect(changed, isTrue);
      expect(repository.calls, contains('changeCaptureSince:null'));
      viewModel.dispose();
    });

    test('a refused change keeps the source on screen with the message',
        () async {
      final repository = FakeCaptureSourceRepository()
        ..sources = [captureSource()];
      final viewModel = CaptureSourceDetailViewModel(
        repository: repository,
        sourceId: 'src-1',
      );
      await viewModel.load();
      repository.setShouldFail(true);

      final changed = await viewModel.changeCaptureSince(DateTime(2027, 1, 1));

      expect(changed, isFalse);
      expect(viewModel.errorMessage, 'regra disse não');
      expect(viewModel.source, isNotNull);
      viewModel.dispose();
    });

    test('a source with no floor reads the entire mailbox', () {
      expect(captureSource().readsEntireMailbox, isTrue);
      expect(
        captureSource(captureSince: DateTime(2026, 5, 27)).readsEntireMailbox,
        isFalse,
      );
    });
  });
}
