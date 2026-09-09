import 'dart:async';
import 'dart:typed_data';

import 'package:bill_payment/src/ui/shared/artifact_viewer_viewmodel.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../fakes/fakes.dart';

void main() {
  group('ArtifactViewerViewModel', () {
    late FakeCaptureItemRepository repository;

    setUp(() => repository = FakeCaptureItemRepository());

    ArtifactViewerViewModel viewModelFor(String id) => ArtifactViewerViewModel(
          load: () => repository.getArtifact(id),
          onSave: ({required fileName, required bytes}) async => true,
          reporter: const NoopErrorReporter(),
        );

    test('starts loading before anything is asked', () {
      final viewModel = viewModelFor('item-1');
      addTearDown(viewModel.dispose);

      expect(viewModel.status, ArtifactViewerStatus.loading);
      expect(viewModel.artifact, isNull);
    });

    test('holds the document once the loader answers', () async {
      final viewModel = viewModelFor('item-1');
      addTearDown(viewModel.dispose);

      await viewModel.load();

      expect(viewModel.status, ArtifactViewerStatus.loaded);
      expect(viewModel.artifact!.isPdf, isTrue);
      expect(repository.calls, contains('getArtifact:item-1'));
    });

    // O 404 do servidor cobre "não há arquivo" e "você não pode ver este item"
    // com a mesma resposta, de propósito — então a tela diz a única coisa
    // verdadeira nos dois casos, em vez de inventar um motivo.
    test('shows an honest message when the document is unavailable', () async {
      repository.setShouldFail(true);
      final viewModel = viewModelFor('item-1');
      addTearDown(viewModel.dispose);

      await viewModel.load();

      expect(viewModel.status, ArtifactViewerStatus.error);
      expect(viewModel.errorMessage, isNotNull);
      expect(viewModel.artifact, isNull);
    });

    test('a retry after a failure clears the message and loads', () async {
      repository.setShouldFail(true);
      final viewModel = viewModelFor('item-1');
      addTearDown(viewModel.dispose);
      await viewModel.load();

      repository.setShouldFail(false);
      await viewModel.load();

      expect(viewModel.status, ArtifactViewerStatus.loaded);
      expect(viewModel.errorMessage, isNull);
    });

    group('download', () {
      /// Grava o que a casca receberia, e permite forçar desistência e falha.
      _RecordingSaver saver() => _RecordingSaver();

      ArtifactViewerViewModel downloadable(
        _RecordingSaver saver, {
        String? suggestedFileName,
        ErrorReporter? reporter,
      }) =>
          ArtifactViewerViewModel(
            load: () => repository.getArtifact('item-1'),
            onSave: saver.call,
            reporter: reporter ?? const NoopErrorReporter(),
            suggestedFileName: suggestedFileName,
          );

      // O download entrega ao salvador da casca os bytes que já estão na tela,
      // sob o nome amigável — não há requisição nova.
      test('hands the loaded bytes to the saver under the suggested name',
          () async {
        final recorder = saver();
        final viewModel = downloadable(
          recorder,
          suggestedFileName: 'comprovante-SECONCI-SP-2026-09-25',
        );
        addTearDown(viewModel.dispose);
        await viewModel.load();

        await viewModel.save();

        expect(recorder.calls, 1);
        expect(
          recorder.lastFileName,
          'comprovante-SECONCI-SP-2026-09-25.pdf',
        );
        expect(recorder.lastBytes, viewModel.artifact!.bytes);
        expect(viewModel.infoMessage, 'Arquivo salvo.');
      });

      // Sem nome sugerido, vale o que o servidor mandou no Content-Disposition.
      test('falls back to the name the server suggested', () async {
        final recorder = saver();
        final viewModel = downloadable(recorder);
        addTearDown(viewModel.dispose);
        await viewModel.load();

        await viewModel.save();

        expect(recorder.lastFileName, 'boleto.pdf');
      });

      // Fechar a caixa de diálogo do sistema não é erro e NÃO vira "salvo" —
      // anunciar sucesso para quem desistiu é a mentira que o bool evita.
      test('says nothing when the person dismisses the save dialog', () async {
        final recorder = saver()..outcome = false;
        final viewModel = downloadable(recorder);
        addTearDown(viewModel.dispose);
        await viewModel.load();

        await viewModel.save();

        expect(viewModel.infoMessage, isNull);
        expect(viewModel.isSaving, isFalse);
      });

      // Falha do plugin vira aviso e NÃO derruba a tela: o documento continua
      // na frente da pessoa, que é o que ela veio ver.
      test('keeps the document on screen when saving fails', () async {
        final recorder = saver()..shouldThrow = true;
        final viewModel = downloadable(recorder);
        addTearDown(viewModel.dispose);
        await viewModel.load();

        await viewModel.save();

        expect(viewModel.infoMessage, 'Não foi possível salvar o arquivo.');
        expect(viewModel.status, ArtifactViewerStatus.loaded);
        expect(viewModel.isSaving, isFalse);
      });

      // O nome do arquivo carrega beneficiário e conta: ele NÃO pode chegar ao
      // relatório de erro. É a regra de PII do módulo, e sem este teste ela
      // volta na primeira vez que alguém achar o contexto pobre demais.
      test('never sends the file name to the error reporter', () async {
        final recorder = saver()..shouldThrow = true;
        final reporter = _CapturingReporter();
        final viewModel = downloadable(
          recorder,
          suggestedFileName: 'comprovante-SECONCI-SP-2026-09-25',
          reporter: reporter,
        );
        addTearDown(viewModel.dispose);
        await viewModel.load();

        await viewModel.save();

        expect(reporter.captures, 1);
        expect(reporter.lastContext, isNot(contains('fileName')));
        expect(
          reporter.lastContext.toString(),
          isNot(contains('SECONCI')),
        );
      });

      // Toque duplo não salva duas vezes — o segundo toque cai enquanto o
      // primeiro ainda está com a caixa de diálogo aberta.
      test('ignores a second tap while the first is still saving', () async {
        final recorder = saver()..hold = true;
        final viewModel = downloadable(recorder);
        addTearDown(viewModel.dispose);
        await viewModel.load();

        final first = viewModel.save();
        await viewModel.save();
        recorder.release();
        await first;

        expect(recorder.calls, 1);
      });

      // Sem artefato não há o que baixar, e o botão não deve nem aparecer.
      test('cannot save before the document arrives', () {
        final viewModel = downloadable(saver());
        addTearDown(viewModel.dispose);

        expect(viewModel.canSave, isFalse);
      });

      // O artefato que a tela NÃO renderiza é o que mais precisa do download:
      // baixar é a única saída que sobra.
      test('can save a document the viewer cannot render', () async {
        final viewModel = ArtifactViewerViewModel(
          load: () async =>
              Result.success(artifact(contentType: 'text/html')),
          onSave: saver().call,
          reporter: const NoopErrorReporter(),
        );
        addTearDown(viewModel.dispose);

        await viewModel.load();

        expect(viewModel.artifact!.isViewable, isFalse);
        expect(viewModel.canSave, isTrue);
      });
    });

    // A mesma tela serve item de quarentena e boleto: o que muda é o loader,
    // e é isso que a impede de conhecer os dois repositórios.
    test('serves a bill through the same view model', () async {
      final bills = FakeBillRepository();
      final viewModel = ArtifactViewerViewModel(
        load: () => bills.getArtifact('bill-1'),
        onSave: ({required fileName, required bytes}) async => true,
        reporter: const NoopErrorReporter(),
      );
      addTearDown(viewModel.dispose);

      await viewModel.load();

      expect(viewModel.status, ArtifactViewerStatus.loaded);
      expect(bills.calls, contains('getArtifact:bill-1'));
    });
  });
}

/// Salvador de teste: conta chamadas, guarda o que recebeu e sabe desistir,
/// falhar e ficar pendurado (para o teste do toque duplo).
class _RecordingSaver {
  int calls = 0;
  String? lastFileName;
  Uint8List? lastBytes;
  bool outcome = true;
  bool shouldThrow = false;
  bool hold = false;

  final _gate = Completer<void>();

  void release() => _gate.complete();

  Future<bool> call({
    required String fileName,
    required Uint8List bytes,
  }) async {
    calls++;
    lastFileName = fileName;
    lastBytes = bytes;
    if (hold) await _gate.future;
    if (shouldThrow) throw Exception('save failed');
    return outcome;
  }
}

/// Reporter que guarda o contexto, para o teste de PII poder olhá-lo.
class _CapturingReporter extends NoopErrorReporter {
  int captures = 0;
  Map<String, Object?> lastContext = const {};

  @override
  void capture(
    Object error,
    StackTrace? stackTrace, {
    Map<String, Object?>? context,
  }) {
    captures++;
    lastContext = context ?? const {};
  }
}
