import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/captured_messages/captured_message_list_viewmodel.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../fakes/fakes.dart';

void main() {
  group('CapturedMessageListViewModel', () {
    late FakeCapturedMessageRepository repository;
    late CapturedMessageListViewModel viewModel;

    setUp(() {
      repository = FakeCapturedMessageRepository();
      viewModel = CapturedMessageListViewModel(repository: repository);
    });

    tearDown(() => viewModel.dispose());

    // O caso que justifica a tela: o e-mail descartado não deixa item, e é aqui
    // que ele continua visível.
    test('shows a discarded e-mail that left no quarantine item', () async {
      repository.messages = [capturedMessage()];

      await viewModel.load();

      final message = viewModel.items.single;
      expect(message.outcome, ArtifactOutcomes.discarded);
      expect(message.captureItemId, isNull);
      expect(message.billId, isNull);
    });

    // Cabeçalho e política vêm junto com a lista: sem "última sincronização" a
    // tela não responde "a varredura já rodou depois de eu mandar o e-mail?".
    test('loads the header and the retention policy with the first page',
        () async {
      repository.messages = [capturedMessage()];

      await viewModel.load();

      expect(viewModel.syncStatus, isNotNull);
      expect(viewModel.retention?.windowDays, 90);
      expect(viewModel.retention?.isEnabled, isFalse);
    });

    // Falha no cabeçalho não derruba a tela — a lista é o que interessa.
    test('keeps the list when only the header fails', () async {
      repository.messages = [capturedMessage()];
      await viewModel.load();

      expect(viewModel.status, CapturedMessageListStatus.loaded);
      expect(viewModel.items, hasLength(1));
    });

    test('an empty result reports the empty stage', () async {
      await viewModel.load();

      expect(viewModel.status, CapturedMessageListStatus.empty);
    });

    test('selecting an outcome reloads under that filter', () async {
      repository.messages = [
        capturedMessage(),
        capturedMessage(id: 'm2', outcome: ArtifactOutcomes.promoted),
      ];

      await viewModel.selectOutcome(ArtifactOutcomes.promoted);

      expect(repository.lastFilter?.outcome, ArtifactOutcomes.promoted);
      expect(viewModel.items, hasLength(1));
    });

    test('the search term travels to the server', () async {
      await viewModel.search('enel');

      expect(repository.lastFilter?.search, 'enel');
      expect(repository.lastFilter?.isEmpty, isFalse);
    });

    test('clearing filters empties the filter set', () async {
      await viewModel.search('enel');

      await viewModel.clearFilters();

      expect(viewModel.filter.isEmpty, isTrue);
    });

    test('the period filter travels to the server', () async {
      final from = DateTime(2026, 8, 1);
      final to = DateTime(2026, 8, 19);

      await viewModel.selectPeriod(from, to);

      expect(repository.lastFilter?.from, from);
      expect(repository.lastFilter?.to, to);
    });

    test('turning the purge on reports the window in the message', () async {
      await viewModel.load();

      final ok = await viewModel.configureRetention(
        isEnabled: true,
        windowDays: 30,
      );

      expect(ok, isTrue);
      expect(repository.calls, contains('configureRetention:true:30'));
      expect(viewModel.retention?.isEnabled, isTrue);
      expect(viewModel.infoMessage, contains('30 dias'));
    });

    // O PUT não devolve a faixa oferecida — mantê-la é o que impede a tela de
    // guardar uma segunda lista que envelhece sozinha.
    test('keeps the offered windows after configuring', () async {
      await viewModel.load();

      await viewModel.configureRetention(isEnabled: true, windowDays: 7);

      expect(viewModel.retention?.availableWindowDays, [7, 30, 90, 180]);
    });

    test('recapturing asks the server and reloads', () async {
      repository.messages = [capturedMessage()];
      await viewModel.load();

      final ok = await viewModel.recapture('msg-1');

      expect(ok, isTrue);
      expect(repository.calls, contains('recapture:msg-1'));
      expect(viewModel.infoMessage, isNotNull);
    });

    test('a failed listing surfaces the domain message', () async {
      repository.setShouldFail(true);

      await viewModel.load();

      expect(viewModel.status, CapturedMessageListStatus.error);
      expect(viewModel.errorMessage, 'regra disse não');
    });
  });
}
