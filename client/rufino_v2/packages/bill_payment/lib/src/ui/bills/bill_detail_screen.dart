import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:provider/provider.dart';
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
    required this.onOpenArtifact,
    required this.onOpenEmail,
  });

  /// Drives the screen.
  final BillDetailViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Opens the original document the bill came from.
  final VoidCallback onOpenArtifact;

  /// Abre o e-mail que trouxe o boleto.
  final VoidCallback onOpenEmail;

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
                return _Body(
                  viewModel: viewModel,
                  onOpenArtifact: widget.onOpenArtifact,
                  onOpenEmail: widget.onOpenEmail,
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
    required this.onOpenArtifact,
    required this.onOpenEmail,
  });

  final BillDetailViewModel viewModel;
  final VoidCallback onOpenArtifact;

  /// Abre o e-mail que trouxe o boleto.
  final VoidCallback onOpenEmail;

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
            // O veredito colorido abre as verificações (ADR-015): o sistema
            // classifica e destaca; quem decide é sempre o usuário.
            if (bill.riskLevel != null) ...[
              _RiskBanner(bill: bill),
              const SizedBox(height: AppSpacing.md),
            ],
            _ChecksSection(bill: bill),
            const SizedBox(height: AppSpacing.md),
            if (bill.bankSlipLookup != null || bill.pixLookup != null) ...[
              _LookupSection(bill: bill),
              const SizedBox(height: AppSpacing.md),
            ],
            _OriginSection(
              bill: bill,
              onOpenArtifact: onOpenArtifact,
              onOpenEmail: onOpenEmail,
            ),
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
          // O que a IA leu do documento e do e-mail: a competência e a
          // descrição só ocupam espaço quando existem.
          if (bill.reading?.competenceLabel != null)
            InfoRow(
              icon: Symbols.calendar_month,
              label: 'Referente a',
              value: bill.reading!.competenceLabel!,
            ),
          if (bill.reading?.description != null)
            InfoRow(
              icon: Symbols.notes,
              label: 'Descrição',
              value: bill.reading!.description!,
            ),
          if (bill.reading?.accountReference != null)
            InfoRow(
              icon: Symbols.tag,
              label: 'Referência',
              value: bill.reading!.accountReference!,
            ),
          // Sem isto, os três campos acima simplesmente não aparecem — e some
          // com eles a explicação de por quê. "Ainda não leu" e "não há o que
          // ler" ficavam idênticos na tela, que é o que este aviso desfaz.
          if (ReadingStatuses.speaks(bill.readingStatus))
            _ReadingNotice(status: bill.readingStatus),
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

class _RiskBanner extends StatelessWidget {
  const _RiskBanner({required this.bill});

  final BillDetail bill;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final (color, onColor, icon, title, message) = switch (bill.riskLevel) {
      // O visual mais duro da escala: fundo cheio na cor de erro, não o container.
      RiskLevels.extremeDanger => (
          theme.colorScheme.error,
          theme.colorScheme.onError,
          Symbols.gpp_bad,
          'Extremo Perigo',
          'O beneficiário ou a origem deste boleto está na sua lista de '
              'bloqueio. Aprovar exige a alçada máxima e assumir o risco '
              'explicitamente.',
        ),
      RiskLevels.danger => (
          theme.colorScheme.errorContainer,
          theme.colorScheme.onErrorContainer,
          Symbols.gpp_bad,
          'Perigo',
          'As verificações encontraram contradição entre as fontes, ou a '
              'consulta oficial não pôde conferir o documento. Confira as '
              'evidências abaixo — aprovar exige assumir o risco '
              'explicitamente.',
        ),
      RiskLevels.attention => (
          const Color(0xFFFFE9B8),
          const Color(0xFF5C4400),
          Symbols.warning,
          'Atenção',
          'Nada contradiz, mas algo não pôde ser confirmado. Confira os '
              'pontos destacados antes de autorizar.',
        ),
      RiskLevels.safe => (
          const Color(0xFFCDE8CF),
          const Color(0xFF10401A),
          Symbols.verified_user,
          'Seguro',
          'Todas as verificações passaram. Nenhuma divergência encontrada.',
        ),
      // Nível que este app não conhece NUNCA pode ler como "Seguro" — um
      // servidor mais novo estaria classificando pior, não melhor.
      _ => (
          theme.colorScheme.surfaceContainerHighest,
          theme.colorScheme.onSurface,
          Symbols.help,
          'Nível de risco desconhecido',
          'O servidor classificou este boleto num nível que esta versão do '
              'aplicativo não conhece. Atualize o aplicativo antes de decidir.',
        ),
    };

    return Container(
      decoration: BoxDecoration(
        color: color,
        borderRadius: BorderRadius.circular(12),
      ),
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: onColor, size: 32),
          const SizedBox(width: AppSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: theme.textTheme.titleMedium?.copyWith(
                    color: onColor,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: AppSpacing.xs),
                Text(
                  message,
                  style: theme.textTheme.bodyMedium?.copyWith(color: onColor),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// Says where the AI reading stands, so an empty summary stops looking final.
///
/// Deliberately quieter than [_RiskBanner]: this is not a signal about the
/// bill, it is a signal about the system. The bill never waits for the queue —
/// it can be approved with what the deterministic funnel proved.
class _ReadingNotice extends StatelessWidget {
  const _ReadingNotice({required this.status});

  final String status;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final queued = status == ReadingStatuses.queued;

    final onColor = queued
        ? theme.colorScheme.onSurfaceVariant
        : theme.colorScheme.onErrorContainer;

    return Container(
      margin: const EdgeInsets.only(top: AppSpacing.sm),
      padding: const EdgeInsets.all(AppSpacing.sm),
      decoration: BoxDecoration(
        color: queued
            ? theme.colorScheme.surfaceContainerHighest
            : theme.colorScheme.errorContainer,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(
            queued ? Symbols.hourglass_top : Symbols.cloud_off,
            color: onColor,
            size: 20,
          ),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  ReadingStatuses.label(status),
                  style: theme.textTheme.labelLarge?.copyWith(
                    color: onColor,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: AppSpacing.xs),
                Text(
                  ReadingStatuses.detail(status),
                  style: theme.textTheme.bodySmall?.copyWith(color: onColor),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _LookupSection extends StatelessWidget {
  const _LookupSection({required this.bill});

  final BillDetail bill;

  @override
  Widget build(BuildContext context) {
    final bankSlip = bill.bankSlipLookup;
    final pix = bill.pixLookup;
    return SectionCard(
      title: 'Consulta oficial',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (pix != null) ...[
            Text(
              'Decode do QR Pix',
              style: Theme.of(context).textTheme.titleSmall,
            ),
            InfoRow(
              icon: Symbols.storefront,
              label: 'Recebedor',
              value: pix.receiver?.displayName ?? 'Não informado',
            ),
            if (pix.receiver?.taxId != null)
              InfoRow(
                icon: Symbols.badge,
                label: 'Documento',
                value: pix.receiver!.taxId!,
              ),
            if (pix.receiverIspbName != null)
              InfoRow(
                icon: Symbols.account_balance,
                label: 'Instituição',
                value: pix.receiverIspbName!,
              ),
            if (pix.totalAmount != null)
              InfoRow(
                icon: Symbols.payments,
                label: 'Valor total',
                value: formatMoney(pix.totalAmount) +
                    (pix.interest != null || pix.fine != null
                        ? ' (com encargos)'
                        : ''),
              ),
            if (pix.dueDate != null)
              InfoRow(
                icon: Symbols.event,
                label: 'Vencimento',
                value: formatDate(pix.dueDate),
              ),
            InfoRow(
              icon: Symbols.schedule,
              label: 'Consultado em',
              value: formatDateTime(pix.consultedAt),
            ),
          ],
          if (bankSlip != null && pix != null)
            const SizedBox(height: AppSpacing.sm),
          if (bankSlip != null) ...[
            Text(
              'Registro do boleto',
              style: Theme.of(context).textTheme.titleSmall,
            ),
            InfoRow(
              icon: Symbols.storefront,
              label: 'Beneficiário',
              value: bankSlip.beneficiary?.displayName ?? 'Não informado',
            ),
            if (bankSlip.beneficiary?.taxId != null)
              InfoRow(
                icon: Symbols.badge,
                label: 'Documento',
                value: bankSlip.beneficiary!.taxId!,
              ),
            if (bankSlip.bankCode != null)
              InfoRow(
                icon: Symbols.account_balance,
                label: 'Banco',
                value: bankSlip.bankCode!,
              ),
            if (bankSlip.amount != null)
              InfoRow(
                icon: Symbols.payments,
                label: 'Valor hoje',
                value: formatMoney(bankSlip.amount) +
                    (bankSlip.originalAmount != null &&
                            bankSlip.originalAmount != bankSlip.amount
                        ? ' (original ${formatMoney(bankSlip.originalAmount)})'
                        : ''),
              ),
            if (bankSlip.dueDate != null)
              InfoRow(
                icon: Symbols.event,
                label: 'Vencimento',
                value: formatDate(bankSlip.dueDate),
              ),
            InfoRow(
              icon: Symbols.schedule,
              label: 'Consultado em',
              value: formatDateTime(bankSlip.consultedAt),
            ),
          ],
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
          for (final check in bill.checks)
            _CheckTile(check: check, readingStatus: bill.readingStatus),
        ],
      ),
    );
  }
}

class _CheckTile extends StatelessWidget {
  const _CheckTile({required this.check, required this.readingStatus});

  final BillCheck check;

  /// One of [ReadingStatuses]. Only the check that depends on the AI reading
  /// looks at it.
  final String readingStatus;

  /// The check the server skips for lack of an AI reading.
  static const String _readingNotAvailable = 'reading_not_available';

  /// Whether this row is a check still WAITING on the AI queue.
  ///
  /// The server is right to skip the check — there is nothing to compare yet —
  /// but "Não se aplica" reads as a verdict, and the user acted on it as one.
  /// Pending and inapplicable are different facts and must not share a label.
  bool get _awaitsReading =>
      check.reasonCode == _readingNotAvailable &&
      readingStatus == ReadingStatuses.queued;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final cs = theme.colorScheme;

    final (icon, color) = _awaitsReading
        ? (Symbols.hourglass_top, cs.tertiary)
        : switch (check.outcome) {
            CheckOutcomes.passed => (Symbols.check_circle, cs.primary),
            CheckOutcomes.failed => (Symbols.cancel, cs.error),
            CheckOutcomes.inconclusive => (Symbols.help, cs.tertiary),
            CheckOutcomes.warning => (Symbols.warning, cs.tertiary),
            _ => (Symbols.remove_circle_outline, cs.outline),
          };

    final message = _awaitsReading
        ? 'A leitura por IA ainda está na fila; a comparação é feita quando '
            'ela chegar.'
        : check.reasonMessage;

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
                      _awaitsReading
                          ? 'Aguardando'
                          : CheckOutcomes.label(check.outcome),
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
                if (!_awaitsReading &&
                    check.evidence != null &&
                    message != check.evidence)
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
  const _OriginSection({
    required this.bill,
    required this.onOpenArtifact,
    required this.onOpenEmail,
  });

  final BillDetail bill;
  final VoidCallback onOpenArtifact;
  final VoidCallback onOpenEmail;

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

          // Importação manual nasce só com os dígitos: não há papel para
          // mostrar, e o botão simplesmente não existe — desabilitar seria
          // prometer um documento que nunca vai chegar.
          if (bill.origin.hasArtifact ||
              bill.origin.sourceKind == BillSourceKinds.mailbox) ...[
            const SizedBox(height: AppSpacing.sm),
            Align(
              alignment: Alignment.centerLeft,
              child: Wrap(
                spacing: AppSpacing.sm,
                runSpacing: AppSpacing.xs,
                children: [
                  if (bill.origin.hasArtifact)
                    OutlinedButton.icon(
                      onPressed: onOpenArtifact,
                      icon: const Icon(Symbols.description),
                      label: const Text('Ver documento'),
                    ),
                  // Só boleto vindo de caixa tem e-mail por trás — para os
                  // demais o botão não existe, como o de documento.
                  if (bill.origin.sourceKind == BillSourceKinds.mailbox)
                    OutlinedButton.icon(
                      onPressed: onOpenEmail,
                      icon: const Icon(Symbols.mail),
                      label: const Text('Ver e-mail'),
                    ),
                ],
              ),
            ),
          ],
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
                child: Builder(builder: (context) {
                  // Alçada por risco (espelho do BLP.BIL32): sem o escopo do
                  // nível, o botão desabilita com o motivo à vista — o
                  // servidor recusaria com 403 de qualquer jeito.
                  final hasClearance = context
                      .watch<BillPaymentPermissionNotifier>()
                      .canApproveAtRisk(viewModel.bill?.riskLevel);
                  return Tooltip(
                    message: hasClearance
                        ? ''
                        : 'Boleto em ${RiskLevels.label(viewModel.bill?.riskLevel)} '
                            '— acima da sua alçada de aprovação.',
                    child: FilledButton(
                      onPressed: viewModel.isMutating ||
                              !viewModel.canApprove ||
                              !hasClearance
                          ? null
                          : () => _approveSheet(context),
                      child: const Text('Aprovar…'),
                    ),
                  );
                }),
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
    final needsAcknowledgement =
        viewModel.bill?.requiresRiskAcknowledgement ?? false;
    final riskLabel = RiskLevels.label(viewModel.bill?.riskLevel);
    DateTime scheduleFor = earliest;
    var riskAcknowledged = false;

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
              // ADR-015: boleto em Perigo ou Extremo Perigo só autoriza com o
              // aceite marcado — e o servidor recusa sem ele, então o botão
              // nem habilita.
              if (needsAcknowledgement) ...[
                const SizedBox(height: AppSpacing.md),
                CheckboxListTile(
                  value: riskAcknowledged,
                  onChanged: (value) =>
                      setSheetState(() => riskAcknowledged = value ?? false),
                  controlAffinity: ListTileControlAffinity.leading,
                  contentPadding: EdgeInsets.zero,
                  title: Text(
                    'Vi o alerta de $riskLabel e assumo o risco de autorizar '
                    'este pagamento.',
                    style: Theme.of(sheetContext).textTheme.bodyMedium,
                  ),
                ),
              ],
              const SizedBox(height: AppSpacing.lg),
              FilledButton(
                onPressed: needsAcknowledgement && !riskAcknowledged
                    ? null
                    : () => Navigator.of(sheetContext).pop(true),
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
        acknowledgeRisk: riskAcknowledged,
      );
    }
    noteController.dispose();
  }
}
