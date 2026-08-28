import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../bill_payment_permissions.dart';
import '../../domain/bill_payment_enums.dart';
import '../../domain/expectation.dart';
import '../bill_payment_back_button.dart';
import '../shared/formats.dart';
import '../shared/message_panel.dart';
import '../shared/status_badge.dart';
import 'expectation_detail_viewmodel.dart';

/// The expectation detail: watch controls and the cycle timeline.
class ExpectationDetailScreen extends StatefulWidget {
  /// Creates the screen.
  const ExpectationDetailScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    required this.onOpenBill,
    required this.onOpenCaptureItem,
    required this.onEdit,
    required this.onDeleted,
  });

  /// Drives the screen.
  final ExpectationDetailViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Opens the bill that fulfilled a cycle.
  final void Function(String billId) onOpenBill;

  /// Opens the quarantine item blocking a cycle.
  final void Function(String itemId) onOpenCaptureItem;

  /// Opens the edit form for this expectation.
  final VoidCallback onEdit;

  /// Called after the expectation is deleted — there is no screen left.
  final VoidCallback onDeleted;

  @override
  State<ExpectationDetailScreen> createState() =>
      _ExpectationDetailScreenState();
}

class _ExpectationDetailScreenState extends State<ExpectationDetailScreen> {
  @override
  void initState() {
    super.initState();
    widget.viewModel.load();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Expectativa'),
        leading: BillPaymentBackButton(fallback: widget.backFallback),
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) {
            final viewModel = widget.viewModel;
            switch (viewModel.status) {
              case ExpectationDetailStatus.loading:
                return const Center(child: CircularProgressIndicator());
              case ExpectationDetailStatus.error:
                return MessagePanel(
                  icon: Symbols.error,
                  title: viewModel.errorMessage ??
                      'Não foi possível carregar a expectativa.',
                  action: FilledButton.tonal(
                    onPressed: viewModel.load,
                    child: const Text('Tentar novamente'),
                  ),
                );
              case ExpectationDetailStatus.loaded:
                return _Body(viewModel: viewModel, widget: widget);
            }
          },
        ),
      ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({required this.viewModel, required this.widget});

  final ExpectationDetailViewModel viewModel;
  final ExpectationDetailScreen widget;

  @override
  Widget build(BuildContext context) {
    final expectation = viewModel.expectation!;
    final canManage = context.watch<BillPaymentPermissionNotifier>()
        .hasPermission(
      BillPaymentResources.expectation,
      BillPaymentScopes.manage,
    );

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
              title: expectation.label,
              child: Column(
                children: [
                  InfoRow(
                    icon: Symbols.repeat,
                    label: 'Recorrência',
                    value:
                        '${Recurrences.label(expectation.recurrence)} · dia '
                        '${expectation.expectedDueDay}',
                  ),
                  if (expectation.accountReference != null)
                    InfoRow(
                      icon: Symbols.tag,
                      label: 'Conta / referência',
                      value: expectation.accountReference!,
                    ),
                  InfoRow(
                    icon: Symbols.notifications,
                    label: 'Aviso',
                    value: '${expectation.alertLeadDays} dia(s) antes do '
                        'vencimento',
                  ),
                  InfoRow(
                    icon: Symbols.school,
                    label: 'Origem',
                    value: ExpectationOrigins.label(expectation.origin) +
                        (expectation.origin == ExpectationOrigins.learned
                            ? ' (${expectation.observationCount} '
                                'observações)'
                            : ''),
                  ),
                  if (canManage) ...[
                    const SizedBox(height: AppSpacing.sm),
                    Wrap(
                      spacing: AppSpacing.sm,
                      runSpacing: AppSpacing.xs,
                      children: [
                        FilledButton.tonalIcon(
                          onPressed:
                              viewModel.isMutating ? null : widget.onEdit,
                          icon: const Icon(Symbols.edit, size: 18),
                          label: const Text('Editar'),
                        ),
                        TextButton.icon(
                          onPressed: viewModel.isMutating
                              ? null
                              : () => _confirmDelete(context),
                          icon: const Icon(Symbols.delete, size: 18),
                          label: const Text('Excluir'),
                          style: TextButton.styleFrom(
                            foregroundColor:
                                Theme.of(context).colorScheme.error,
                          ),
                        ),
                      ],
                    ),
                  ],
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            SectionCard(
              title: 'Vigilância',
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      StatusBadge(
                        label: !expectation.isActive
                            ? 'Desativada'
                            : expectation.isPausedAt(DateTime.now())
                                ? 'Pausada até '
                                    '${formatDate(expectation.pausedUntil)}'
                                : 'Ativa',
                        tone: expectation.isActive
                            ? BadgeTone.positive
                            : BadgeTone.neutral,
                      ),
                    ],
                  ),
                  if (canManage) ...[
                    const SizedBox(height: AppSpacing.sm),
                    Wrap(
                      spacing: AppSpacing.sm,
                      runSpacing: AppSpacing.xs,
                      children: [
                        FilledButton.tonal(
                          onPressed: viewModel.isMutating
                              ? null
                              : () => _pickPauseDate(context),
                          child: const Text('Pausar até…'),
                        ),
                        OutlinedButton(
                          onPressed: viewModel.isMutating
                              ? null
                              : viewModel.resume,
                          child: const Text('Retomar'),
                        ),
                        TextButton(
                          onPressed: viewModel.isMutating
                              ? null
                              : () => viewModel.deactivate(null),
                          child: const Text('Desativar'),
                        ),
                      ],
                    ),
                  ],
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            SectionCard(
              title: 'Ciclos',
              child: expectation.cycles.isEmpty
                  ? const Text('Nenhum ciclo aberto ainda.')
                  : Column(
                      children: [
                        for (final cycle in expectation.cycles)
                          _CycleTile(
                            cycle: cycle,
                            viewModel: viewModel,
                            widget: widget,
                          ),
                      ],
                    ),
            ),
          ],
        ),
      ),
    );
  }

  /// O beneficiário não é editável, então excluir é o caminho de trocá-lo — e
  /// o diálogo precisa dizer o que vai junto e o que Desativar faria no lugar.
  Future<void> _confirmDelete(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Excluir expectativa?'),
        content: const Text(
          'Os ciclos e o histórico de alertas vão junto. Para apenas parar '
          'de monitorar, use Desativar.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Excluir'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    final deleted = await viewModel.deleteExpectation();
    if (deleted) widget.onDeleted();
  }

  Future<void> _pickPauseDate(BuildContext context) async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: now.add(const Duration(days: 30)),
      firstDate: now,
      lastDate: now.add(const Duration(days: 365)),
    );
    if (picked != null) await viewModel.pause(picked);
  }
}

class _CycleTile extends StatelessWidget {
  const _CycleTile({
    required this.cycle,
    required this.viewModel,
    required this.widget,
  });

  final ExpectationCycle cycle;
  final ExpectationDetailViewModel viewModel;
  final ExpectationDetailScreen widget;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return ListTile(
      contentPadding: EdgeInsets.zero,
      title: Row(
        children: [
          Text('Competência ${cycle.competence}',
              style: theme.textTheme.bodyLarge),
          const SizedBox(width: AppSpacing.sm),
          StatusBadge(
            label: CycleStatuses.label(cycle.status),
            tone: switch (cycle.status) {
              CycleStatuses.missing => BadgeTone.problem,
              CycleStatuses.partiallyCaptured => BadgeTone.attention,
              CycleStatuses.fulfilled => BadgeTone.positive,
              _ => BadgeTone.neutral,
            },
          ),
        ],
      ),
      subtitle: Text(
        'Venc. previsto ${formatDate(cycle.expectedDueDate)}'
        '${cycle.missReason == null ? '' : ' · ${MissReasons.label(cycle.missReason!)}'}',
      ),
      trailing: Wrap(
        spacing: AppSpacing.xs,
        children: [
          if (cycle.fulfilledByBillId != null)
            IconButton(
              icon: const Icon(Symbols.receipt_long),
              tooltip: 'Abrir o boleto',
              onPressed: () => widget.onOpenBill(cycle.fulfilledByBillId!),
            ),
          if (cycle.blockedByCaptureItemId != null)
            IconButton(
              icon: const Icon(Symbols.inbox),
              tooltip: 'Abrir o item da quarentena',
              onPressed: () =>
                  widget.onOpenCaptureItem(cycle.blockedByCaptureItemId!),
            ),
          if (cycle.isOpen)
            BillPaymentPermissionGuard(
              resource: BillPaymentResources.expectation,
              scope: BillPaymentScopes.waive,
              child: IconButton(
                icon: const Icon(Symbols.notifications_off),
                tooltip: 'Dispensar este ciclo',
                onPressed: viewModel.isMutating
                    ? null
                    : () => _confirmWaive(context),
              ),
            ),
        ],
      ),
    );
  }

  Future<void> _confirmWaive(BuildContext context) async {
    final controller = TextEditingController();
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text('Dispensar a competência ${cycle.competence}?'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Text(
              'A rede de segurança silencia só para este ciclo — os '
              'próximos continuam vigiados.',
            ),
            const SizedBox(height: AppSpacing.md),
            TextField(
              controller: controller,
              decoration: const InputDecoration(
                labelText: 'Motivo (opcional)',
                border: OutlineInputBorder(),
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Cancelar'),
          ),
          FilledButton.tonal(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Dispensar'),
          ),
        ],
      ),
    );
    if (confirmed == true) {
      await viewModel.waiveCycle(
        cycle.id,
        controller.text.trim().isEmpty ? null : controller.text.trim(),
      );
    }
    controller.dispose();
  }
}
