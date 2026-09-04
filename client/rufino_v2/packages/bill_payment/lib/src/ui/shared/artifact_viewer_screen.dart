import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:syncfusion_flutter_pdfviewer/pdfviewer.dart';

import '../bill_payment_back_button.dart';
import 'artifact_viewer_viewmodel.dart';
import 'message_panel.dart';

/// The original document, full screen.
///
/// A route and not a dialog: a bank slip inside a dialog is unreadable on a
/// phone, and the route gives the back button its behaviour for free.
class ArtifactViewerScreen extends StatefulWidget {
  /// Creates the screen.
  const ArtifactViewerScreen({
    super.key,
    required this.viewModel,
    required this.title,
    required this.backFallback,
  });

  /// Drives the screen.
  final ArtifactViewerViewModel viewModel;

  /// What the bar says — the origin of the document, in the user's words.
  final String title;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  @override
  State<ArtifactViewerScreen> createState() => _ArtifactViewerScreenState();
}

class _ArtifactViewerScreenState extends State<ArtifactViewerScreen> {
  @override
  void initState() {
    super.initState();
    widget.viewModel.load();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.title),
        leading: BillPaymentBackButton(fallback: widget.backFallback),
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) {
            final viewModel = widget.viewModel;
            switch (viewModel.status) {
              case ArtifactViewerStatus.loading:
                return const Center(child: CircularProgressIndicator());
              case ArtifactViewerStatus.error:
                return MessagePanel(
                  icon: Symbols.error,
                  title: viewModel.errorMessage ??
                      'O documento original não está disponível.',
                  action: FilledButton.tonal(
                    onPressed: viewModel.load,
                    child: const Text('Tentar novamente'),
                  ),
                );
              case ArtifactViewerStatus.loaded:
                return _Document(viewModel: viewModel);
            }
          },
        ),
      ),
    );
  }
}

class _Document extends StatelessWidget {
  const _Document({required this.viewModel});

  final ArtifactViewerViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    final artifact = viewModel.artifact!;

    if (artifact.isPdf) {
      return SfPdfViewer.memory(
        artifact.bytes,
        canShowPaginationDialog: false,
      );
    }

    if (artifact.isImage) {
      // A caixa traz foto de boleto e página escaneada mais do que se
      // imagina, e nesses o zoom é o que torna a leitura possível.
      return InteractiveViewer(
        maxScale: 6,
        child: Center(child: Image.memory(artifact.bytes)),
      );
    }

    // Nem PDF nem imagem: dizer o que é vale mais do que uma tela em branco.
    return MessagePanel(
      icon: Symbols.description,
      title: 'Este documento chegou em um formato que o app não exibe '
          '(${artifact.contentType}).',
    );
  }
}
