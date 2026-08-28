import 'package:flutter/material.dart';
import 'package:flutter_widget_from_html_core/flutter_widget_from_html_core.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../domain/bill_payment_exception.dart';
import '../../domain/email_message.dart';
import '../bill_payment_back_button.dart';
import 'formats.dart';
import 'message_panel.dart';

/// Stage of the e-mail viewer.
enum EmailViewerStatus {
  /// The e-mail is on its way.
  loading,

  /// The e-mail is on screen.
  loaded,

  /// The e-mail could not be loaded.
  error,
}

/// Drives the e-mail viewer for whatever fetches the message.
class EmailViewerViewModel extends ChangeNotifier {
  /// Creates the view model over [load].
  EmailViewerViewModel({required Future<Result<EmailMessage>> Function() load})
      : _load = load;

  final Future<Result<EmailMessage>> Function() _load;

  EmailViewerStatus _status = EmailViewerStatus.loading;
  EmailMessage? _message;
  String? _errorMessage;
  bool _showRemoteImages = false;

  /// The stage of the viewer.
  EmailViewerStatus get status => _status;

  /// The e-mail, once loaded.
  EmailMessage? get message => _message;

  /// The message of the last failure.
  String? get errorMessage => _errorMessage;

  /// Whether remote images may load.
  ///
  /// Off by default on purpose: a tracking pixel confirms the read to the
  /// sender, and the viewer must not leak that silently.
  bool get showRemoteImages => _showRemoteImages;

  /// Lets remote images load, by explicit user choice.
  void allowRemoteImages() {
    if (_showRemoteImages) return;
    _showRemoteImages = true;
    notifyListeners();
  }

  /// Loads the e-mail.
  Future<void> load() async {
    _status = EmailViewerStatus.loading;
    _errorMessage = null;
    notifyListeners();

    final result = await _load();
    result.fold(
      onSuccess: (message) {
        _message = message;
        _status = EmailViewerStatus.loaded;
      },
      onError: (error, _) {
        _status = EmailViewerStatus.error;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'O e-mail original não está disponível.',
        );
      },
    );
    notifyListeners();
  }
}

/// The captured e-mail, full screen: subject, sender and rendered body.
class EmailViewerScreen extends StatefulWidget {
  /// Creates the screen.
  const EmailViewerScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    this.title = 'E-mail do boleto',
  });

  /// Drives the screen.
  final EmailViewerViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// What the bar says — the origin of the e-mail, in the user's words.
  final String title;

  @override
  State<EmailViewerScreen> createState() => _EmailViewerScreenState();
}

class _EmailViewerScreenState extends State<EmailViewerScreen> {
  @override
  void initState() {
    super.initState();
    widget.viewModel.load();
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final viewModel = widget.viewModel;
        return Scaffold(
          appBar: AppBar(
            title: Text(widget.title),
            leading: BillPaymentBackButton(fallback: widget.backFallback),
            actions: [
              if (viewModel.status == EmailViewerStatus.loaded &&
                  viewModel.message!.isHtml &&
                  !viewModel.showRemoteImages)
                IconButton(
                  tooltip: 'Carregar imagens',
                  icon: const Icon(Symbols.image),
                  onPressed: viewModel.allowRemoteImages,
                ),
            ],
          ),
          body: SafeArea(child: _body(viewModel)),
        );
      },
    );
  }

  Widget _body(EmailViewerViewModel viewModel) {
    switch (viewModel.status) {
      case EmailViewerStatus.loading:
        return const Center(child: CircularProgressIndicator());
      case EmailViewerStatus.error:
        return MessagePanel(
          icon: Symbols.error,
          title:
              viewModel.errorMessage ?? 'O e-mail original não está disponível.',
          action: FilledButton.tonal(
            onPressed: viewModel.load,
            child: const Text('Tentar novamente'),
          ),
        );
      case EmailViewerStatus.loaded:
        return _Message(viewModel: viewModel);
    }
  }
}

class _Message extends StatelessWidget {
  const _Message({required this.viewModel});

  final EmailViewerViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final message = viewModel.message!;

    return SingleChildScrollView(
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Card.outlined(
            child: Padding(
              padding: const EdgeInsets.all(AppSpacing.md),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    message.subject ?? '(sem assunto)',
                    style: theme.textTheme.titleMedium,
                  ),
                  const SizedBox(height: AppSpacing.xs),
                  Text(
                    message.sender,
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                  Text(
                    formatDateTime(message.receivedAt),
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: AppSpacing.md),
          if (message.isHtml && !viewModel.showRemoteImages)
            Padding(
              padding: const EdgeInsets.only(bottom: AppSpacing.sm),
              child: Text(
                'Imagens externas bloqueadas — carregá-las avisa o remetente '
                'que o e-mail foi aberto.',
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
            ),
          if (message.isHtml)
            HtmlWidget(
              message.content,
              // Bloqueia imagem remota até o usuário pedir: pixel de
              // rastreamento confirma leitura ao remetente.
              customWidgetBuilder: viewModel.showRemoteImages
                  ? null
                  : (element) => element.localName == 'img'
                      ? const SizedBox.shrink()
                      : null,
            )
          else
            SelectableText(message.content, style: theme.textTheme.bodyMedium),
        ],
      ),
    );
  }
}
