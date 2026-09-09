import 'dart:async';
import 'dart:typed_data';

import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/shared/artifact_viewer_screen.dart';
import 'package:bill_payment/src/ui/shared/artifact_viewer_viewmodel.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';

/// O botão de baixar dentro do visualizador.
///
/// Os bytes já vieram para renderizar, então baixar é salvar o que está na
/// tela — e é a única saída que sobra quando o app não sabe exibir o documento.
void main() {
  late List<String> saved;

  setUp(() => saved = []);

  Future<bool> saver({
    required String fileName,
    required Uint8List bytes,
  }) async {
    saved.add(fileName);
    return true;
  }

  ArtifactViewerViewModel viewModelFor(
    Future<Result<CapturedArtifact>> Function() load, {
    String? suggestedFileName,
  }) =>
      ArtifactViewerViewModel(
        load: load,
        onSave: saver,
        reporter: const NoopErrorReporter(),
        suggestedFileName: suggestedFileName,
      );

  Future<void> pump(WidgetTester tester, ArtifactViewerViewModel viewModel) =>
      tester.pumpWidget(
        MaterialApp(
          home: ArtifactViewerScreen(
            viewModel: viewModel,
            title: 'Comprovante de pagamento',
            backFallback: '/bill-payment/bills/bill-1',
          ),
        ),
      );

  CapturedArtifact textArtifact([String contentType = 'text/plain']) =>
      CapturedArtifact(
        bytes: Uint8List.fromList('documento'.codeUnits),
        contentType: contentType,
        fileName: 'boleto.txt',
      );

  group('ArtifactViewerScreen', () {
    // O documento carregou: o botão está na barra, e baixar entrega o nome
    // amigável ao salvador da casca.
    testWidgets('downloads the document under the suggested name',
        (tester) async {
      final viewModel = viewModelFor(
        () async => Result.success(textArtifact()),
        suggestedFileName: 'comprovante-Enel-2026-09-25',
      );
      addTearDown(viewModel.dispose);

      await pump(tester, viewModel);
      await tester.pumpAndSettle();

      await tester.tap(find.byTooltip('Baixar'));
      await tester.pumpAndSettle();

      expect(saved, ['comprovante-Enel-2026-09-25.txt']);
      expect(find.text('Arquivo salvo.'), findsOneWidget);
    });

    // Enquanto o documento não chegou não há o que baixar — o botão some em
    // vez de ficar desabilitado, como manda a doutrina do módulo.
    testWidgets('hides the button while the document is on its way',
        (tester) async {
      final gate = Completer<Result<CapturedArtifact>>();
      final viewModel = viewModelFor(() => gate.future);
      addTearDown(viewModel.dispose);

      await pump(tester, viewModel);
      await tester.pump();

      expect(find.byTooltip('Baixar'), findsNothing);

      gate.complete(Result.success(textArtifact()));
      await tester.pumpAndSettle();

      expect(find.byTooltip('Baixar'), findsOneWidget);
    });

    // Documento indisponível não tem o que baixar.
    testWidgets('hides the button when the document failed to load',
        (tester) async {
      final viewModel = viewModelFor(
        () async => Result.error(Exception('sem documento')),
      );
      addTearDown(viewModel.dispose);

      await pump(tester, viewModel);
      await tester.pumpAndSettle();

      expect(find.byTooltip('Baixar'), findsNothing);
      expect(find.text('Tentar novamente'), findsOneWidget);
    });

    // O CASO QUE MAIS IMPORTA: formato que o app não exibe deixava a pessoa
    // sem saída nenhuma. Agora o download aparece por extenso, no painel, além
    // do ícone na barra — escondê-lo ali seria esconder a resposta.
    testWidgets('offers the download inside the panel it cannot render',
        (tester) async {
      final viewModel = viewModelFor(
        () async => Result.success(textArtifact('application/octet-stream')),
      );
      addTearDown(viewModel.dispose);

      await pump(tester, viewModel);
      await tester.pumpAndSettle();

      expect(find.textContaining('formato que o app não exibe'), findsOneWidget);

      await tester.tap(find.text('Baixar arquivo'));
      await tester.pumpAndSettle();

      expect(saved, ['boleto.txt']);
    });
  });
}
