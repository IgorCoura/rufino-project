import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../bill_payment_permissions.dart';
import '../../domain/bill_payment_enums.dart';
import '../../domain/captured_message.dart';
import '../bill_payment_back_button.dart';
import '../shared/formats.dart';
import '../shared/message_panel.dart';
import '../shared/status_badge.dart';
import 'captured_message_list_screen_parts.dart';
import 'captured_message_list_viewmodel.dart';

/// O histórico de tudo que a captura leu — inclusive o que ela descartou.
///
/// É a tela que responde "o que aconteceu com o e-mail que eu mandei". A
/// quarentena não responde isso: ela é fila de trabalho e só mostra o que ficou
/// pendente; o que a triagem descarta não deixa item.
class CapturedMessageListScreen extends StatefulWidget {
  /// Creates the screen.
  const CapturedMessageListScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    required this.onOpenBill,
    required this.onOpenCaptureItem,
  });

  /// Drives the screen.
  final CapturedMessageListViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Abre o boleto que o e-mail produziu.
  final void Function(String billId) onOpenBill;

  /// Abre o item da quarentena, quando ele ainda existe.
  final void Function(String itemId) onOpenCaptureItem;

  @override
  State<CapturedMessageListScreen> createState() =>
      _CapturedMessageListScreenState();
}

class _CapturedMessageListScreenState extends State<CapturedMessageListScreen> {
  final _scrollController = ScrollController();
  final _searchController = TextEditingController();
  String? _lastInfoMessage;

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_onScroll);
    widget.viewModel.addListener(_onViewModelChanged);
    widget.viewModel.load();
  }

  @override
  void dispose() {
    _scrollController
      ..removeListener(_onScroll)
      ..dispose();
    _searchController.dispose();
    widget.viewModel.removeListener(_onViewModelChanged);
    super.dispose();
  }

  void _onScroll() {
    if (!_scrollController.hasClients) return;
    final position = _scrollController.position;
    if (position.pixels >= position.maxScrollExtent - 240) {
      widget.viewModel.loadMore();
    }
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
        title: const Text('E-mails capturados'),
        leading: BillPaymentBackButton(fallback: widget.backFallback),
        actions: [
          IconButton(
            icon: const Icon(Symbols.refresh),
            tooltip: 'Atualizar',
            onPressed: widget.viewModel.load,
          ),
        ],
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) => Center(
            child: ConstrainedBox(
              constraints:
                  const BoxConstraints(maxWidth: AppBreakpoints.desktop),
              child: Column(
                children: [
                  CaptureSyncHeader(status: widget.viewModel.syncStatus),
                  RetentionControl(viewModel: widget.viewModel),
                  CapturedMessageFilters(
                    viewModel: widget.viewModel,
                    searchController: _searchController,
                  ),
                  Expanded(
                    child: _Results(
                      viewModel: widget.viewModel,
                      scrollController: _scrollController,
                      onOpenBill: widget.onOpenBill,
                      onOpenCaptureItem: widget.onOpenCaptureItem,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _Results extends StatelessWidget {
  const _Results({
    required this.viewModel,
    required this.scrollController,
    required this.onOpenBill,
    required this.onOpenCaptureItem,
  });

  final CapturedMessageListViewModel viewModel;
  final ScrollController scrollController;
  final void Function(String billId) onOpenBill;
  final void Function(String itemId) onOpenCaptureItem;

  @override
  Widget build(BuildContext context) {
    switch (viewModel.status) {
      case CapturedMessageListStatus.loading:
        return const Center(child: CircularProgressIndicator());
      case CapturedMessageListStatus.error:
        return MessagePanel(
          icon: Symbols.error,
          title: viewModel.errorMessage ??
              'Não foi possível carregar os e-mails capturados.',
          action: FilledButton.tonal(
            onPressed: viewModel.load,
            child: const Text('Tentar novamente'),
          ),
        );
      case CapturedMessageListStatus.empty:
        return MessagePanel(
          icon: Symbols.mark_email_read,
          title: viewModel.filter.isEmpty
              ? 'Nenhum e-mail lido ainda.\nO histórico se enche sozinho a '
                  'cada sincronização.'
              : 'Nenhum e-mail com esses filtros.',
          action: viewModel.filter.isEmpty
              ? null
              : FilledButton.tonal(
                  onPressed: viewModel.clearFilters,
                  child: const Text('Limpar filtros'),
                ),
        );
      case CapturedMessageListStatus.loaded:
      case CapturedMessageListStatus.loadingMore:
        return ListView.builder(
          controller: scrollController,
          padding: const EdgeInsets.fromLTRB(
            AppSpacing.md,
            0,
            AppSpacing.md,
            AppSpacing.lg,
          ),
          itemCount: viewModel.items.length + (viewModel.hasMore ? 1 : 0),
          itemBuilder: (context, index) {
            if (index >= viewModel.items.length) {
              return const Padding(
                padding: EdgeInsets.all(AppSpacing.md),
                child: Center(child: CircularProgressIndicator()),
              );
            }
            return _MessageRow(
              message: viewModel.items[index],
              viewModel: viewModel,
              onOpenBill: onOpenBill,
              onOpenCaptureItem: onOpenCaptureItem,
            );
          },
        );
    }
  }
}

class _MessageRow extends StatelessWidget {
  const _MessageRow({
    required this.message,
    required this.viewModel,
    required this.onOpenBill,
    required this.onOpenCaptureItem,
  });

  final CapturedMessage message;
  final CapturedMessageListViewModel viewModel;
  final void Function(String billId) onOpenBill;
  final void Function(String itemId) onOpenCaptureItem;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final billId = message.billId;
    final itemId = message.captureItemId;

    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: Card.outlined(
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Icon(Symbols.mail, color: theme.colorScheme.primary),
                  const SizedBox(width: AppSpacing.md),
                  Expanded(
                    child: Text(
                      message.subject ?? '(sem assunto)',
                      style: theme.textTheme.titleMedium,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  Text(
                    message.artifactCount == 1
                        ? '1 anexo'
                        : '${message.artifactCount} anexos',
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppSpacing.xs),
              Text(
                message.sender,
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
              Text(
                'Recebido ${formatDateTime(message.receivedAt)}'
                '${message.isProcessed ? ' · Processado ${formatDateTime(message.processedAt)}' : ''}',
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
              const SizedBox(height: AppSpacing.sm),
              Wrap(
                spacing: AppSpacing.sm,
                runSpacing: AppSpacing.xs,
                crossAxisAlignment: WrapCrossAlignment.center,
                children: [
                  StatusBadge(
                    label: ArtifactOutcomes.label(message.outcome),
                    tone: ArtifactOutcomes.needsAttention(message.outcome)
                        ? BadgeTone.attention
                        : message.outcome == ArtifactOutcomes.promoted
                            ? BadgeTone.positive
                            : BadgeTone.neutral,
                  ),
                  if (billId != null)
                    TextButton(
                      onPressed: () => onOpenBill(billId),
                      child: const Text('Abrir boleto'),
                    ),
                  if (billId == null && itemId != null)
                    TextButton(
                      onPressed: () => onOpenCaptureItem(itemId),
                      child: const Text('Abrir na quarentena'),
                    ),
                  BillPaymentPermissionGuard(
                    resource: BillPaymentResources.capturedMessage,
                    scope: BillPaymentScopes.recapture,
                    child: OutlinedButton(
                      onPressed: viewModel.isMutating || !message.canRecapture
                          ? null
                          : () => _confirmRecapture(context),
                      child: const Text('Reprocessar'),
                    ),
                  ),
                ],
              ),
              if (message.artifactCount > 1) _Artifacts(message: message),
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _confirmRecapture(BuildContext context) async {
    final producedBill = message.billId != null;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Reprocessar este e-mail?'),
        content: Text(
          'Tudo que a captura produziu para ele é apagado e o e-mail é lido de '
          'novo, do zero, como se tivesse acabado de chegar.'
          '${producedBill ? '\n\nEste e-mail já virou boleto. Se ele ainda aguarda aprovação, o boleto é cancelado e recriado pela nova leitura; se já foi aprovado, agendado ou pago, o reprocessamento é bloqueado.' : ''}',
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

    if (confirmed == true) await viewModel.recapture(message.id);
  }
}

/// Os anexos, quando há mais de um — o desfecho da linha é o dominante, e aqui
/// aparecem os individuais.
class _Artifacts extends StatelessWidget {
  const _Artifacts({required this.message});

  final CapturedMessage message;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Padding(
      padding: const EdgeInsets.only(top: AppSpacing.sm),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          for (final artifact in message.artifacts)
            Padding(
              padding: const EdgeInsets.only(top: AppSpacing.xs),
              child: Text(
                '${artifact.fileName ?? '(sem nome)'} — '
                '${ArtifactOutcomes.label(artifact.outcome)}',
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
            ),
        ],
      ),
    );
  }
}
