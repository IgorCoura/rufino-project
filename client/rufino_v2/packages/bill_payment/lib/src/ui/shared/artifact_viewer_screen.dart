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
  String? _lastInfoMessage;

  @override
  void initState() {
    super.initState();
    widget.viewModel.addListener(_onViewModelChanged);
    widget.viewModel.load();
  }

  @override
  void dispose() {
    widget.viewModel.removeListener(_onViewModelChanged);
    super.dispose();
  }

  /// Mostra o desfecho do download uma vez só.
  ///
  /// Comparar com a última mensagem é o que impede o SnackBar de reaparecer a
  /// cada `notifyListeners` — é o molde repetido nas outras telas do módulo.
  void _onViewModelChanged() {
    final message = widget.viewModel.infoMessage;
    if (message != null && message != _lastInfoMessage && mounted) {
      _lastInfoMessage = message;
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(message)));
    }
  }

  @override
  Widget build(BuildContext context) {
    // O ListenableBuilder envolve o Scaffold INTEIRO, e não só o body: o botão
    // de baixar vive na barra e precisa enxergar o estado do ViewModel.
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final viewModel = widget.viewModel;

        return Scaffold(
          appBar: AppBar(
            title: Text(widget.title),
            leading: BillPaymentBackButton(fallback: widget.backFallback),
            actions: [
              if (viewModel.canSave)
                _DownloadButton(viewModel: viewModel),
            ],
          ),
          body: SafeArea(
            child: switch (viewModel.status) {
              ArtifactViewerStatus.loading =>
                const Center(child: CircularProgressIndicator()),
              ArtifactViewerStatus.error => MessagePanel(
                  icon: Symbols.error,
                  title: viewModel.errorMessage ??
                      'O documento original não está disponível.',
                  action: FilledButton.tonal(
                    onPressed: viewModel.load,
                    child: const Text('Tentar novamente'),
                  ),
                ),
              ArtifactViewerStatus.loaded => _Document(viewModel: viewModel),
            },
          ),
        );
      },
    );
  }
}

/// O botão de baixar, com o giro enquanto o sistema decide onde salvar.
class _DownloadButton extends StatelessWidget {
  const _DownloadButton({required this.viewModel});

  final ArtifactViewerViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    if (viewModel.isSaving) {
      return const Padding(
        padding: EdgeInsets.symmetric(horizontal: 16),
        child: Center(
          child: SizedBox(
            width: 20,
            height: 20,
            child: CircularProgressIndicator(strokeWidth: 2),
          ),
        ),
      );
    }

    return IconButton(
      tooltip: 'Baixar',
      icon: const Icon(Symbols.download),
      onPressed: viewModel.save,
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

    // Nem PDF nem imagem: dizer o que é vale mais do que uma tela em branco —
    // e baixar é a ÚNICA saída que sobra, então o botão aparece aqui também,
    // por extenso. Escondê-lo só na barra seria esconder a resposta.
    return MessagePanel(
      icon: Symbols.description,
      title: 'Este documento chegou em um formato que o app não exibe '
          '(${artifact.contentType}).',
      action: FilledButton.tonal(
        onPressed: viewModel.isSaving ? null : viewModel.save,
        child: const Text('Baixar arquivo'),
      ),
    );
  }
}
