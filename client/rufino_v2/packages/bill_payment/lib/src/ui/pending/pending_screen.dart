import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../domain/bill_payment_enums.dart';
import '../../domain/expectation.dart';
import '../bill_payment_back_button.dart';
import '../shared/formats.dart';
import '../shared/message_panel.dart';
import '../shared/status_badge.dart';
import 'pending_viewmodel.dart';

/// The daily panel: what needs someone's eyes today.
///
/// Three lists with three different calls to action — "go fetch it", "fix
/// the item", plain anticipation — never collapsed into one.
class PendingScreen extends StatefulWidget {
  /// Creates the screen.
  const PendingScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    required this.onOpenApprovalQueue,
    required this.onOpenExpectation,
    required this.onOpenCaptureItem,
    required this.onOpenPayerProfile,
  });

  /// Drives the screen.
  final PendingViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Opens the bill list filtered by awaiting approval.
  final VoidCallback onOpenApprovalQueue;

  /// Opens one expectation's detail.
  final void Function(String id) onOpenExpectation;

  /// Opens one quarantine item.
  final void Function(String id) onOpenCaptureItem;

  /// Opens the payer profile onboarding.
  final VoidCallback onOpenPayerProfile;

  @override
  State<PendingScreen> createState() => _PendingScreenState();
}

class _PendingScreenState extends State<PendingScreen> {
  @override
  void initState() {
    super.initState();
    widget.viewModel.load();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Painel de contas'),
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
          builder: (context, _) {
            final viewModel = widget.viewModel;
            switch (viewModel.status) {
              case PendingStatus.loading:
                return const Center(child: CircularProgressIndicator());
              case PendingStatus.error:
                return MessagePanel(
                  icon: Symbols.error,
                  title: viewModel.errorMessage ??
                      'Não foi possível carregar as pendências.',
                  action: FilledButton.tonal(
                    onPressed: viewModel.load,
                    child: const Text('Tentar novamente'),
                  ),
                );
              case PendingStatus.loaded:
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

  final PendingViewModel viewModel;
  final PendingScreen widget;

  @override
  Widget build(BuildContext context) {
    final view = viewModel.view;
    return Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: AppBreakpoints.tablet),
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.md),
          children: [
            if (viewModel.missingPayerProfile) ...[
              Card(
                color: Theme.of(context).colorScheme.tertiaryContainer,
                child: Padding(
                  padding: const EdgeInsets.all(AppSpacing.md),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Configure o perfil do pagador',
                        style: Theme.of(context)
                            .textTheme
                            .titleMedium
                            ?.copyWith(
                              color: Theme.of(context)
                                  .colorScheme
                                  .onTertiaryContainer,
                            ),
                      ),
                      const SizedBox(height: AppSpacing.xs),
                      Text(
                        'Sem ele, o que a captura não reconhecer é '
                        'descartado — configure antes de conectar uma '
                        'caixa.',
                        style: Theme.of(context)
                            .textTheme
                            .bodyMedium
                            ?.copyWith(
                              color: Theme.of(context)
                                  .colorScheme
                                  .onTertiaryContainer,
                            ),
                      ),
                      const SizedBox(height: AppSpacing.sm),
                      FilledButton.tonal(
                        onPressed: widget.onOpenPayerProfile,
                        child: const Text('Configurar agora'),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: AppSpacing.md),
            ],
            SectionCard(
              title: 'Aguardando aprovação',
              trailing: TextButton(
                onPressed: widget.onOpenApprovalQueue,
                child: const Text('Ver fila'),
              ),
              child: Text(
                viewModel.awaitingApprovalCount == 0
                    ? 'Nenhum boleto esperando decisão.'
                    : '${viewModel.awaitingApprovalCount}'
                        '${viewModel.awaitingApprovalTruncated ? '+' : ''} '
                        'boleto(s) esperando decisão.',
                style: Theme.of(context).textTheme.bodyLarge,
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            // Vencidas primeiro: aqui há encargos correndo, e é a lista que
            // não pode se perder no meio das outras.
            _PendingList(
              title: 'Vencidas e não chegaram',
              subtitle: 'Passou do vencimento e a conta nunca apareceu — '
                  'há encargos correndo.',
              items: view.overdue,
              onTap: (pending) =>
                  widget.onOpenExpectation(pending.expectationId),
            ),
            const SizedBox(height: AppSpacing.md),
            _PendingList(
              title: 'Não chegaram',
              subtitle: 'A conta era esperada e não apareceu — vá buscá-la '
                  'no portal ou importe à mão. Ainda dá tempo.',
              items: view.missing,
              onTap: (pending) =>
                  widget.onOpenExpectation(pending.expectationId),
            ),
            const SizedBox(height: AppSpacing.md),
            _PendingList(
              title: 'Chegaram com problema',
              subtitle: 'Algo chegou e não pôde ser lido — resolva o item '
                  'na quarentena.',
              items: view.captureFailed,
              onTap: (pending) => pending.blockedByCaptureItemId != null
                  ? widget.onOpenCaptureItem(pending.blockedByCaptureItemId!)
                  : widget.onOpenExpectation(pending.expectationId),
            ),
            const SizedBox(height: AppSpacing.md),
            _PendingList(
              title: 'Vencem em breve',
              subtitle: 'Antecedência — nada a fazer ainda.',
              items: view.dueSoon,
              onTap: (pending) =>
                  widget.onOpenExpectation(pending.expectationId),
            ),
            if (view.isEmpty && viewModel.awaitingApprovalCount == 0)
              const Padding(
                padding: EdgeInsets.only(top: AppSpacing.xl),
                child: MessagePanel(
                  icon: Symbols.task_alt,
                  title: 'Nenhuma pendência — tudo em dia.',
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _PendingList extends StatelessWidget {
  const _PendingList({
    required this.title,
    required this.subtitle,
    required this.items,
    required this.onTap,
  });

  final String title;
  final String subtitle;
  final List<PendingExpectation> items;
  final void Function(PendingExpectation pending) onTap;

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) return const SizedBox.shrink();

    final theme = Theme.of(context);
    return SectionCard(
      title: '$title (${items.length})',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            subtitle,
            style: theme.textTheme.bodySmall?.copyWith(
              color: theme.colorScheme.onSurfaceVariant,
            ),
          ),
          const SizedBox(height: AppSpacing.sm),
          for (final pending in items)
            ListTile(
              contentPadding: EdgeInsets.zero,
              title: Text(pending.label),
              subtitle: Text(
                'Competência ${pending.competence} · venc. previsto '
                '${formatDate(pending.expectedDueDate)}'
                '${pending.missReason == null ? '' : ' · ${MissReasons.label(pending.missReason!)}'}',
              ),
              trailing: pending.lastAlertLevel == null
                  ? null
                  : StatusBadge.alertLevel(pending.lastAlertLevel!),
              onTap: () => onTap(pending),
            ),
        ],
      ),
    );
  }
}
