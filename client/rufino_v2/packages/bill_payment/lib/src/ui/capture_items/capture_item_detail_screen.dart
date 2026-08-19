import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../bill_payment_permissions.dart';
import '../../domain/bill_payment_enums.dart';
import '../bill_payment_back_button.dart';
import '../shared/formats.dart';
import '../shared/message_panel.dart';
import '../shared/status_badge.dart';
import 'capture_item_detail_viewmodel.dart';

/// The quarantine item detail: what arrived, what happened to it, and the
/// two ways a person can act.
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
  });

  /// Drives the screen.
  final CaptureItemDetailViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Called with a bill id — the promoted bill, or the one a claim created.
  final void Function(String billId) onOpenBill;

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
                );
            }
          },
        ),
      ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({required this.viewModel, required this.onOpenBill});

  final CaptureItemDetailViewModel viewModel;
  final void Function(String billId) onOpenBill;

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
                    child: const Text('Reprocessar'),
                  ),
                ),
              ),
          ],
        ),
      ),
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
