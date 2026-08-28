import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../bill_payment_permissions.dart';
import '../../domain/bill_payment_enums.dart';
import '../bill_payment_back_button.dart';
import '../shared/document_picker.dart';
import '../shared/formats.dart';
import '../shared/message_panel.dart';
import '../shared/status_badge.dart';
import 'capture_item_detail_viewmodel.dart';

// Os tipos do seletor de arquivos moraram aqui até a importação manual de
// boleto também passar a anexar documento. Dois consumidores no módulo é o
// momento de eles saírem da tela que os usou primeiro — quem importa boleto
// não deveria depender do arquivo da quarentena para nomear um callback.

/// The quarantine item detail: what arrived, what happened to it, and the
/// ways a person can act — open the issuer's link, attach the bill by hand,
/// claim it, or dismiss it.
///
/// The financial fields render only when the server sent them — the
/// visibility rule is the domain's, never this screen's.
class CaptureItemDetailScreen extends StatefulWidget {
  /// Creates the screen.
  const CaptureItemDetailScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    required this.onOpenBill,
    required this.onOpenArtifact,
    required this.onOpenEmail,
    required this.onPickDocument,
    required this.onOpenLink,
  });

  /// Drives the screen.
  final CaptureItemDetailViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Called with a bill id — the promoted bill, or the one a claim created.
  final void Function(String billId) onOpenBill;

  /// Opens the original document of this item.
  final VoidCallback onOpenArtifact;

  /// Abre o e-mail que trouxe o item.
  final VoidCallback onOpenEmail;

  /// Abre o seletor de arquivos do sistema. Nulo quando a pessoa desiste.
  ///
  /// Injetado em vez de embutido: escolher arquivo é capacidade da casca, e a
  /// tela fica testável sem seletor de verdade.
  final DocumentPicker onPickDocument;

  /// Abre o endereco do documento no navegador.
  final LinkOpener onOpenLink;

  @override
  State<CaptureItemDetailScreen> createState() =>
      _CaptureItemDetailScreenState();
}

class _CaptureItemDetailScreenState extends State<CaptureItemDetailScreen> {
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
    return Scaffold(
      appBar: AppBar(
        title: const Text('Item da quarentena'),
        leading: BillPaymentBackButton(fallback: widget.backFallback),
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) {
            final viewModel = widget.viewModel;
            switch (viewModel.status) {
              case CaptureItemDetailStatus.loading:
                return const Center(child: CircularProgressIndicator());
              case CaptureItemDetailStatus.error:
                return MessagePanel(
                  icon: Symbols.error,
                  title: viewModel.errorMessage ??
                      'Não foi possível carregar o item.',
                  action: FilledButton.tonal(
                    onPressed: viewModel.load,
                    child: const Text('Tentar novamente'),
                  ),
                );
              case CaptureItemDetailStatus.loaded:
                return _Body(
                  viewModel: viewModel,
                  onOpenBill: widget.onOpenBill,
                  onOpenArtifact: widget.onOpenArtifact,
                  onOpenEmail: widget.onOpenEmail,
                  onPickDocument: widget.onPickDocument,
                  onOpenLink: widget.onOpenLink,
                );
            }
          },
        ),
      ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({
    required this.viewModel,
    required this.onOpenBill,
    required this.onOpenArtifact,
    required this.onOpenEmail,
    required this.onPickDocument,
    required this.onOpenLink,
  });

  final CaptureItemDetailViewModel viewModel;
  final void Function(String billId) onOpenBill;
  final VoidCallback onOpenArtifact;

  /// Abre o e-mail que trouxe o item.
  final VoidCallback onOpenEmail;
  final DocumentPicker onPickDocument;

  /// Abre o endereco do documento no navegador.
  final LinkOpener onOpenLink;

  @override
  Widget build(BuildContext context) {
    final item = viewModel.item!;
    return Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: AppBreakpoints.tablet),
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.md),
          children: [
            if (viewModel.errorMessage != null)
              Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.md),
                child: Text(
                  viewModel.errorMessage!,
                  style:
                      TextStyle(color: Theme.of(context).colorScheme.error),
                ),
              ),
            SectionCard(
              title: 'Mensagem',
              child: Column(
                children: [
                  InfoRow(
                    icon: Symbols.person,
                    label: 'Remetente',
                    value: item.sender ?? '—',
                  ),
                  InfoRow(
                    icon: Symbols.subject,
                    label: 'Assunto',
                    value: item.subject ?? '—',
                  ),
                  InfoRow(
                    icon: Symbols.schedule,
                    label: 'Recebido em',
                    value: formatDateTime(item.receivedAt),
                  ),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            SectionCard(
              title: 'Desfecho',
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Wrap(
                    spacing: AppSpacing.xs,
                    runSpacing: AppSpacing.xs,
                    children: [
                      StatusBadge.captureItemStatus(item.status),
                      if (item.extractionMethod != null)
                        StatusBadge(
                          label: ExtractionMethods.label(
                            item.extractionMethod!,
                          ),
                        ),
                      if (item.routingConfidence != null)
                        StatusBadge(
                          label: 'Confiança: '
                              '${RoutingConfidences.label(item.routingConfidence!)}',
                        ),
                    ],
                  ),
                  if (item.reason != null) ...[
                    const SizedBox(height: AppSpacing.sm),
                    Text(
                      item.reason!,
                      style: Theme.of(context).textTheme.bodyMedium,
                    ),
                  ],
                  // A URL de onde o documento veio — ou viria. Só aparece nos
                  // estados em que uma pessoa ainda decide; o servidor é quem
                  // aplica esse recorte, aqui só se mostra o que chegou.
                  if (item.sourceUrl != null) ...[
                    const SizedBox(height: AppSpacing.sm),
                    Text(
                      item.linkHost == null
                          ? 'Documento publicado em:'
                          : 'Documento publicado por ${item.linkHost}:',
                      style: Theme.of(context).textTheme.bodySmall,
                    ),
                    // Clicável: o endereço costuma ser longo e opaco (token de
                    // capability), e copiar à mão convida a erro de seleção.
                    // Quem abre é a casca — abrir navegador é capacidade de
                    // plataforma, e o módulo não carrega plugin.
                    InkWell(
                      onTap: () => _openLink(context, item.sourceUrl!),
                      child: Padding(
                        padding: const EdgeInsets.symmetric(
                          vertical: AppSpacing.xs,
                        ),
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Expanded(
                              child: Text(
                                item.sourceUrl!,
                                style: Theme.of(context)
                                    .textTheme
                                    .bodySmall
                                    ?.copyWith(
                                      color: Theme.of(context)
                                          .colorScheme
                                          .primary,
                                      decoration: TextDecoration.underline,
                                    ),
                              ),
                            ),
                            const SizedBox(width: AppSpacing.xs),
                            Icon(
                              Symbols.open_in_new,
                              size: 16,
                              color: Theme.of(context).colorScheme.primary,
                            ),
                          ],
                        ),
                      ),
                    ),
                    Text(
                      'O sistema não conseguiu baixar este documento. Abra o '
                      'endereço, salve o boleto e anexe abaixo.',
                      style: Theme.of(context).textTheme.bodySmall,
                    ),
                  ],
                  if (item.unlockedBy != null)
                    Padding(
                      padding: const EdgeInsets.only(top: AppSpacing.xs),
                      child: Text(
                        'Aberto pela senha derivada de: ${item.unlockedBy}',
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                    ),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.md),

            // Vem ANTES de reivindicar: sem ver o papel, a pessoa confirma
            // que a conta é dela olhando remetente e assunto — que é
            // exatamente o que o roteamento automático já não conseguiu usar
            // para decidir.
            if (item.hasArtifact || item.sender != null)
              Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.sm),
                child: Wrap(
                  spacing: AppSpacing.sm,
                  runSpacing: AppSpacing.xs,
                  children: [
                    if (item.hasArtifact)
                      OutlinedButton.icon(
                        onPressed: onOpenArtifact,
                        icon: const Icon(Symbols.description),
                        label: const Text('Ver documento'),
                      ),
                    // Só item vindo de caixa tem e-mail por trás — anexo
                    // manual não mostra o botão, como o de documento.
                    if (item.sender != null)
                      OutlinedButton.icon(
                        onPressed: onOpenEmail,
                        icon: const Icon(Symbols.mail),
                        label: const Text('Ver e-mail'),
                      ),
                  ],
                ),
              ),
            if (item.hasBill)
              FilledButton.tonal(
                onPressed: () => onOpenBill(item.billId!),
                child: const Text('Abrir o boleto deste item'),
              ),
            if (item.acceptsClaim)
              BillPaymentPermissionGuard(
                resource: BillPaymentResources.captureItem,
                scope: BillPaymentScopes.claim,
                child: Padding(
                  padding: const EdgeInsets.only(top: AppSpacing.sm),
                  child: FilledButton(
                    onPressed: viewModel.isMutating
                        ? null
                        : () => _confirmClaim(context),
                    child: const Text('Reivindicar este boleto'),
                  ),
                ),
              ),
            if (item.acceptsReprocess)
              BillPaymentPermissionGuard(
                resource: BillPaymentResources.captureItem,
                scope: BillPaymentScopes.reprocess,
                child: Padding(
                  padding: const EdgeInsets.only(top: AppSpacing.sm),
                  child: OutlinedButton(
                    onPressed: viewModel.isMutating
                        ? null
                        : () => _confirmReprocess(context),
                    child: Text(
                      item.status == CaptureItemStatuses.dismissed
                          ? 'Reabrir'
                          : 'Reprocessar',
                    ),
                  ),
                ),
              ),

            // Anexar vem depois de "ver documento" e antes de reprovar: é a ação
            // que RESOLVE o item, e reprovar é a que desiste dele.
            if (item.acceptsReprocess)
              BillPaymentPermissionGuard(
                resource: BillPaymentResources.captureItem,
                scope: BillPaymentScopes.reprocess,
                child: Padding(
                  padding: const EdgeInsets.only(top: AppSpacing.sm),
                  child: OutlinedButton.icon(
                    onPressed: viewModel.isMutating ? null : () => _attach(context),
                    icon: const Icon(Symbols.upload_file),
                    label: const Text('Anexar boleto'),
                  ),
                ),
              ),

            if (CaptureItemStatuses.acceptsDismiss(item.status))
              BillPaymentPermissionGuard(
                resource: BillPaymentResources.captureItem,
                scope: BillPaymentScopes.claim,
                child: Padding(
                  padding: const EdgeInsets.only(top: AppSpacing.sm),
                  child: TextButton(
                    onPressed: viewModel.isMutating
                        ? null
                        : () => _confirmDismiss(context),
                    style: TextButton.styleFrom(
                      foregroundColor: Theme.of(context).colorScheme.error,
                    ),
                    child: const Text('Não reconheço esta cobrança'),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }

  /// O diálogo da reprovação.
  ///
  /// Diz **o que acontece** em vez de só perguntar "tem certeza?", e diz que dá
  /// para desfazer — uma reprovação que parecesse final faria a pessoa hesitar,
  /// e uma fila que ninguém esvazia é o problema que esta ação existe para
  /// resolver. "Cancelar" é o padrão do teclado, porque a ação é destrutiva.
  Future<void> _confirmDismiss(BuildContext context) async {
    final noteController = TextEditingController();

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Não reconhece esta cobrança?'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'De: ${viewModel.item!.sender ?? "remetente desconhecido"}'
              '\nAssunto: ${viewModel.item!.subject ?? "(sem assunto)"}',
              style: Theme.of(dialogContext).textTheme.bodyMedium,
            ),
            const SizedBox(height: AppSpacing.md),
            const Text(
              'Este item sai da lista de pendências. Ele não será pago e nenhum '
              'boleto será criado.\n\nDá para reabrir depois, a qualquer momento.',
            ),
            const SizedBox(height: AppSpacing.md),
            TextField(
              controller: noteController,
              decoration: const InputDecoration(
                labelText: 'Observação (opcional)',
                hintText: 'ex.: não é nosso, é do vizinho',
              ),
              maxLength: 200,
            ),
          ],
        ),
        actions: [
          TextButton(
            autofocus: true,
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(
              backgroundColor: Theme.of(dialogContext).colorScheme.error,
            ),
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Não reconheço'),
          ),
        ],
      ),
    );

    final note = noteController.text.trim();
    noteController.dispose();

    if (confirmed == true) {
      await viewModel.dismiss(note: note.isEmpty ? null : note);
    }
  }

  /// Abre o endereço do documento no navegador.
  ///
  /// Avisa quando não dá: um toque que não faz nada deixaria a pessoa achando
  /// que o link está quebrado, quando o que faltou foi navegador disponível.
  Future<void> _openLink(BuildContext context, String url) async {
    final messenger = ScaffoldMessenger.of(context);
    final opened = await onOpenLink(url);

    if (!opened) {
      messenger.showSnackBar(
        const SnackBar(
          content: Text('Não foi possível abrir o endereço neste dispositivo.'),
        ),
      );
    }
  }

  /// Escolhe o arquivo e o entrega ao sistema.
  ///
  /// É o caminho para o emissor que a escada de link não alcança: a pessoa abre
  /// a URL que a quarentena mostra, baixa o boleto e o devolve aqui.
  Future<void> _attach(BuildContext context) async {
    final picked = await onPickDocument();
    if (picked == null) return;

    await viewModel.attachArtifact(
      picked.bytes,
      fileName: picked.fileName,
      contentType: picked.contentType,
    );
  }

  Future<void> _confirmClaim(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Reivindicar este boleto?'),
        content: const Text(
          'O documento passa a ser deste cliente e vira um boleto na fila '
          'de verificação. O sistema relê o artefato pelos mesmos dígitos '
          'verificadores do caminho automático.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Cancelar'),
          ),
          FilledButton.tonal(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Reivindicar'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    final claimed = await viewModel.claim();
    if (claimed && viewModel.claimedBillId != null) {
      onOpenBill(viewModel.claimedBillId!);
    }
  }

  Future<void> _confirmReprocess(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Reprocessar o item?'),
        content: const Text(
          'O item volta ao início da cascata de leitura. Quando o degrau de '
          'visão é usado, consome a cota diária do extrator — por isso é um '
          'item por vez, não a fila inteira.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Cancelar'),
          ),
          FilledButton.tonal(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Reprocessar'),
          ),
        ],
      ),
    );
    if (confirmed == true) await viewModel.reprocess();
  }
}
