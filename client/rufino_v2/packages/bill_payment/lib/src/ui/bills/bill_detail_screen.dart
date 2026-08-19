import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../bill_payment_permissions.dart';
import '../../domain/bill_check.dart';
import '../../domain/bill_detail.dart';
import '../../domain/bill_payment_enums.dart';
import '../bill_payment_back_button.dart';
import '../shared/formats.dart';
import '../shared/message_panel.dart';
import '../shared/status_badge.dart';
import 'bill_detail_viewmodel.dart';

/// The approval screen: the bill, the twelve checks with evidence, and the
/// three decisions.
///
/// The digitable line and the Pix payload never appear here — the API does
/// not return them, by design: whoever has them, pays.
class BillDetailScreen extends StatefulWidget {
  /// Creates the screen.
  const BillDetailScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
  });

  /// Drives the screen.
  final BillDetailViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  @override
  State<BillDetailScreen> createState() => _BillDetailScreenState();
}

class _BillDetailScreenState extends State<BillDetailScreen> {
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
        title: const Text('Boleto'),
        leading: BillPaymentBackButton(fallback: widget.backFallback),
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) {
            final viewModel = widget.viewModel;
            switch (viewModel.status) {
              case BillDetailStatus.loading:
                return const Center(child: CircularProgressIndicator());
              case BillDetailStatus.error:
                return MessagePanel(
                  icon: Symbols.error,
                  title: viewModel.errorMessage ??
                      'Não foi possível carregar o boleto.',
                  action: FilledButton.tonal(
                    onPressed: viewModel.load,
                    child: const Text('Tentar novamente'),
                  ),
                );
              case BillDetailStatus.loaded:
                return _Body(viewModel: viewModel);
            }
          },
        ),
      ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({required this.viewModel});

  final BillDetailViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    final bill = viewModel.bill!;
    return Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: AppBreakpoints.tablet),
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.md),
          children: [
            Row(
              children: [
                StatusBadge.billStatus(bill.status),
                const SizedBox(width: AppSpacing.sm),
                StatusBadge(label: bill.rail),
                const SizedBox(width: AppSpacing.sm),
                StatusBadge(label: BillKinds.label(bill.kind)),
              ],
            ),
            const SizedBox(height: AppSpacing.md),
            if (viewModel.errorMessage != null)
              Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.md),
                child: Text(
                  viewModel.errorMessage!,
                  style:
                      TextStyle(color: Theme.of(context).colorScheme.error),
                ),
              ),
            _SummarySection(bill: bill),
            const SizedBox(height: AppSpacing.md),
            _ChecksSection(bill: bill),
            const SizedBox(height: AppSpacing.md),
            _OriginSection(bill: bill),
            if (bill.approval != null) ...[
              const SizedBox(height: AppSpacing.md),
              _DecisionSection(bill: bill),
            ],
            const SizedBox(height: AppSpacing.md),
            _Actions(viewModel: viewModel),
          ],
        ),
      ),
    );
  }
}

class _SummarySection extends StatelessWidget {
  const _SummarySection({required this.bill});

  final BillDetail bill;

  @override
  Widget build(BuildContext context) {
    final beneficiary = bill.beneficiary;
    return SectionCard(
      title: 'Resumo',
      child: Column(
        children: [
          InfoRow(
            icon: Symbols.storefront,
            label: 'Beneficiário',
            value: beneficiary?.displayName ?? 'Não identificado',
          ),
          if (beneficiary?.taxId != null)
            InfoRow(
              icon: Symbols.badge,
              label: 'Documento',
              value: beneficiary!.taxId!,
            ),
          InfoRow(
            icon: Symbols.payments,
            label: 'Valor',
            value: formatMoney(bill.amount) +
                (bill.originalAmount != null &&
                        bill.originalAmount != bill.amount
                    ? ' (original ${formatMoney(bill.originalAmount)})'
                    : ''),
          ),
          InfoRow(
            icon: Symbols.event,
            label: 'Vencimento',
            value: formatDate(bill.dueDate),
          ),
          if (bill.bankCode != null)
            InfoRow(
              icon: Symbols.account_balance,
              label: 'Banco recebedor',
              value: bill.bankCode!,
            ),
          InfoRow(
            icon: Symbols.schedule,
            label: 'Consulta oficial',
            value: formatDateTime(bill.lastConsultedAt),
          ),
          if (bill.scheduledFor != null)
            InfoRow(
              icon: Symbols.event_available,
              label: 'Agendado para',
              value: formatDate(bill.scheduledFor),
            ),
        ],
      ),
    );
  }
}

class _ChecksSection extends StatelessWidget {
  const _ChecksSection({required this.bill});

  final BillDetail bill;

  @override
  Widget build(BuildContext context) {
    return SectionCard(
      title: 'Verificações (${bill.checks.length})',
      child: Column(
        children: [
          for (final check in bill.checks) _CheckTile(check: check),
        ],
      ),
    );
  }
}

class _CheckTile extends StatelessWidget {
  const _CheckTile({required this.check});

  final BillCheck check;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final cs = theme.colorScheme;

    final (icon, color) = switch (check.outcome) {
      CheckOutcomes.passed => (Symbols.check_circle, cs.primary),
      CheckOutcomes.failed => (Symbols.cancel, cs.error),
      CheckOutcomes.inconclusive => (Symbols.help, cs.tertiary),
      CheckOutcomes.warning => (Symbols.warning, cs.tertiary),
      _ => (Symbols.remove_circle_outline, cs.outline),
    };

    final message = check.reasonMessage;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.xs),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: color, size: 20),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        check.typeLabel,
                        style: theme.textTheme.bodyLarge,
                      ),
                    ),
                    Text(
                      CheckOutcomes.label(check.outcome),
                      style:
                          theme.textTheme.labelMedium?.copyWith(color: color),
                    ),
                    if (check.isBlockingFailure)
                      Padding(
                        padding: const EdgeInsets.only(left: AppSpacing.xs),
                        child: Text(
                          'BLOQUEIA',
                          style: theme.textTheme.labelSmall?.copyWith(
                            color: cs.error,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                  ],
                ),
                if (message != null)
                  Text(
                    message,
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: cs.onSurfaceVariant,
                    ),
                  ),
                if (check.evidence != null && message != check.evidence)
                  Text(
                    check.evidence!,
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: cs.onSurfaceVariant,
                    ),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _OriginSection extends StatelessWidget {
  const _OriginSection({required this.bill});

  final BillDetail bill;

  @override
  Widget build(BuildContext context) {
    return SectionCard(
      title: 'Origem',
      child: Column(
        children: [
          InfoRow(
            icon: Symbols.input,
            label: 'Entrada',
            value: BillSourceKinds.label(bill.origin.sourceKind),
          ),
          if (bill.origin.senderAddress != null)
            InfoRow(
              icon: Symbols.person,
              label: 'Remetente',
              value: bill.origin.senderAddress!,
            ),
          InfoRow(
            icon: Symbols.schedule,
            label: 'Recebido em',
            value: formatDateTime(bill.origin.receivedAt),
          ),
        ],
      ),
    );
  }
}

class _DecisionSection extends StatelessWidget {
  const _DecisionSection({required this.bill});

  final BillDetail bill;

  @override
  Widget build(BuildContext context) {
    final approval = bill.approval!;
    return SectionCard(
      title: 'Decisão',
      child: Column(
        children: [
          InfoRow(
            icon: Symbols.gavel,
            label: 'Decisão',
            value: approval.decision,
          ),
          InfoRow(
            icon: Symbols.schedule,
            label: 'Quando',
            value: formatDateTime(approval.decidedAt),
          ),
          if (approval.note != null)
            InfoRow(
              icon: Symbols.notes,
              label: 'Observação',
              value: approval.note!,
            ),
        ],
      ),
    );
  }
}

class _Actions extends StatelessWidget {
  const _Actions({required this.viewModel});

  final BillDetailViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    final bill = viewModel.bill!;
    if (bill.isTerminal) return const SizedBox.shrink();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (bill.acceptsDecision && viewModel.isSnapshotStale)
          Padding(
            padding: const EdgeInsets.only(bottom: AppSpacing.sm),
            child: Text(
              'Consulta desatualizada — revalide antes de aprovar.',
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: Theme.of(context).colorScheme.tertiary,
                  ),
              textAlign: TextAlign.center,
            ),
          ),
        Wrap(
          spacing: AppSpacing.sm,
          runSpacing: AppSpacing.sm,
          alignment: WrapAlignment.center,
          children: [
            if (bill.acceptsValidation)
              BillPaymentPermissionGuard(
                resource: BillPaymentResources.bill,
                scope: BillPaymentScopes.validate,
                child: OutlinedButton(
                  onPressed:
                      viewModel.isMutating ? null : viewModel.revalidate,
                  child: const Text('Revalidar'),
                ),
              ),
            if (bill.acceptsDecision)
              BillPaymentPermissionGuard(
                resource: BillPaymentResources.bill,
                scope: BillPaymentScopes.deny,
                child: OutlinedButton(
                  onPressed: viewModel.isMutating
                      ? null
                      : () => _askReason(
                            context,
                            title: 'Negar o boleto',
                            action: viewModel.deny,
                          ),
                  child: const Text('Negar'),
                ),
              ),
            if (bill.acceptsCancellation)
              BillPaymentPermissionGuard(
                resource: BillPaymentResources.bill,
                scope: BillPaymentScopes.cancel,
                child: TextButton(
                  onPressed: viewModel.isMutating
                      ? null
                      : () => _askReason(
                            context,
                            title: 'Cancelar o boleto',
                            action: viewModel.cancel,
                          ),
                  child: const Text('Cancelar boleto'),
                ),
              ),
            if (bill.acceptsDecision)
              BillPaymentPermissionGuard(
                resource: BillPaymentResources.bill,
                scope: BillPaymentScopes.approve,
                child: FilledButton(
                  onPressed: viewModel.isMutating || !viewModel.canApprove
                      ? null
                      : () => _approveSheet(context),
                  child: const Text('Aprovar…'),
                ),
              ),
          ],
        ),
      ],
    );
  }

  /// Motivo obrigatório: recusa sem motivo é buraco de auditoria.
  Future<void> _askReason(
    BuildContext context, {
    required String title,
    required Future<bool> Function(String reason) action,
  }) async {
    final controller = TextEditingController();
    final formKey = GlobalKey<FormState>();

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(title),
        content: Form(
          key: formKey,
          child: TextFormField(
            controller: controller,
            autofocus: true,
            decoration: const InputDecoration(
              labelText: 'Motivo (obrigatório)',
              border: OutlineInputBorder(),
            ),
            validator: (value) => (value == null || value.trim().isEmpty)
                ? 'Informe o motivo.'
                : null,
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Voltar'),
          ),
          FilledButton.tonal(
            onPressed: () {
              if (formKey.currentState!.validate()) {
                Navigator.of(dialogContext).pop(true);
              }
            },
            child: const Text('Confirmar'),
          ),
        ],
      ),
    );

    if (confirmed == true) await action(controller.text.trim());
    controller.dispose();
  }

  Future<void> _approveSheet(BuildContext context) async {
    final noteController = TextEditingController();
    final earliest = viewModel.earliestScheduleDate;
    DateTime scheduleFor = earliest;

    final confirmed = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      builder: (sheetContext) => Padding(
        padding: EdgeInsets.only(
          left: AppSpacing.md,
          right: AppSpacing.md,
          top: AppSpacing.md,
          bottom:
              MediaQuery.of(sheetContext).viewInsets.bottom + AppSpacing.md,
        ),
        child: StatefulBuilder(
          builder: (sheetContext, setSheetState) => Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                'Autorizar pagamento',
                style: Theme.of(sheetContext).textTheme.titleLarge,
              ),
              const SizedBox(height: AppSpacing.md),
              OutlinedButton.icon(
                icon: const Icon(Symbols.event),
                label: Text('Pagar em ${formatDate(scheduleFor)}'),
                onPressed: () async {
                  final picked = await showDatePicker(
                    context: sheetContext,
                    initialDate: scheduleFor,
                    firstDate: earliest,
                    lastDate: earliest.add(const Duration(days: 365)),
                  );
                  if (picked != null) {
                    setSheetState(() => scheduleFor = picked);
                  }
                },
              ),
              const SizedBox(height: AppSpacing.md),
              TextField(
                controller: noteController,
                decoration: const InputDecoration(
                  labelText: 'Observação (opcional)',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: AppSpacing.lg),
              FilledButton(
                onPressed: () => Navigator.of(sheetContext).pop(true),
                child: const Text('Autorizar'),
              ),
              const SizedBox(height: AppSpacing.sm),
            ],
          ),
        ),
      ),
    );

    if (confirmed == true) {
      await viewModel.approve(
        scheduleFor: scheduleFor,
        note: noteController.text.trim().isEmpty
            ? null
            : noteController.text.trim(),
      );
    }
    noteController.dispose();
  }
}
