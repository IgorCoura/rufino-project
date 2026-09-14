import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../bill_payment_permissions.dart';
import '../../domain/bill.dart';
import '../../domain/bill_payment_enums.dart';
import '../bill_payment_back_button.dart';
import '../shared/formats.dart';
import '../shared/message_panel.dart';
import '../shared/status_badge.dart';
import 'bill_export_sheet.dart';
import 'bill_list_viewmodel.dart';

/// The bill listing — the approver's work queue when filtered by
/// "aguardando aprovação".
///
/// Also where several bills are selected to download their documents at once:
/// the "Baixar documentos" button (or a long press on a card) turns the
/// selection on, and a finished download turns it off.
class BillListScreen extends StatefulWidget {
  /// Creates the screen.
  const BillListScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    required this.onOpenBill,
    required this.onScheduleBill,
    required this.onImportBill,
  });

  /// Drives the screen.
  final BillListViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Called with the id of the bill to open.
  final void Function(String id) onOpenBill;

  /// Called with the id of the approved bill to schedule.
  ///
  /// Opens the same detail screen — the date sheet lives there, and the
  /// approver needs the checks in front of them before releasing money.
  final void Function(String id) onScheduleBill;

  /// Opens the manual import form.
  final VoidCallback onImportBill;

  @override
  State<BillListScreen> createState() => _BillListScreenState();
}

class _BillListScreenState extends State<BillListScreen> {
  final _scrollController = ScrollController();

  static const _filters = <(String label, String? status)>[
    ('Aguardando aprovação', BillStatuses.awaitingApproval),
    ('Rejeitados', BillStatuses.rejected),
    ('Aprovados', BillStatuses.approved),
    // Fase 3: sem estes três, um boleto que virasse Agendado SUMIA da vista
    // — só aparecia em "Todos". "Falhou" é a fila operacional do pagamento.
    ('Agendados', BillStatuses.scheduled),
    ('Pagos', BillStatuses.paid),
    ('Falhou', BillStatuses.failed),
    ('Negados', BillStatuses.denied),
    ('Cancelados', BillStatuses.cancelled),
    ('Todos', null),
  ];

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_onScroll);
    widget.viewModel.addListener(_onViewModelChanged);
    widget.viewModel.load();
  }

  @override
  void dispose() {
    widget.viewModel.removeListener(_onViewModelChanged);
    _scrollController
      ..removeListener(_onScroll)
      ..dispose();
    super.dispose();
  }

  void _onScroll() {
    if (!_scrollController.hasClients) return;
    final position = _scrollController.position;
    if (position.pixels >= position.maxScrollExtent - 240) {
      widget.viewModel.loadMore();
    }
  }

  /// Mostra o desfecho do download uma vez só e o esquece — duas vezes
  /// "Arquivo salvo." seguidas precisam aparecer duas vezes.
  void _onViewModelChanged() {
    final message = widget.viewModel.exportMessage;
    if (message == null || !mounted) return;

    widget.viewModel.clearExportMessage();
    ScaffoldMessenger.of(context)
        .showSnackBar(SnackBar(content: Text(message)));
  }

  Future<void> _export() async {
    final options =
        await showBillExportSheet(context, widget.viewModel.selectedBills);
    if (options == null || !mounted) return;
    await widget.viewModel.exportSelected(options);
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final viewModel = widget.viewModel;
        final selecting = viewModel.isSelecting;

        // Voltar com a seleção ativa sai da seleção, e não da tela.
        return PopScope(
          canPop: !selecting,
          onPopInvokedWithResult: (didPop, _) {
            if (!didPop) viewModel.clearSelection();
          },
          child: Scaffold(
            appBar: selecting
                ? _SelectionAppBar(viewModel: viewModel)
                : AppBar(
                    title: const Text('Boletos'),
                    leading:
                        BillPaymentBackButton(fallback: widget.backFallback),
                  ),
            floatingActionButton: _FloatingActions(
              viewModel: viewModel,
              onImportBill: widget.onImportBill,
              onExport: _export,
            ),
            body: SafeArea(
              child: Center(
                child: ConstrainedBox(
                  constraints:
                      const BoxConstraints(maxWidth: AppBreakpoints.desktop),
                  child: Column(
                    children: [
                      if (viewModel.isExporting)
                        const LinearProgressIndicator(),
                      Padding(
                        padding: const EdgeInsets.fromLTRB(
                          AppSpacing.md,
                          AppSpacing.md,
                          AppSpacing.md,
                          AppSpacing.sm,
                        ),
                        child: Align(
                          alignment: Alignment.centerLeft,
                          child: Wrap(
                            spacing: AppSpacing.sm,
                            runSpacing: AppSpacing.xs,
                            children: [
                              for (final (label, status) in _filters)
                                FilterChip(
                                  label: Text(label),
                                  selected: viewModel.statusFilter == status,
                                  onSelected: (_) =>
                                      viewModel.selectStatus(status),
                                ),
                            ],
                          ),
                        ),
                      ),
                      Expanded(
                        child: _Results(
                          viewModel: viewModel,
                          scrollController: _scrollController,
                          onOpenBill: widget.onOpenBill,
                          onScheduleBill: widget.onScheduleBill,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        );
      },
    );
  }
}

/// Os botões flutuantes: fora da seleção, "Baixar documentos" (liga a
/// seleção) acima do "Importar"; dentro dela, só "Baixar (N)", que abre a
/// folha de opções.
class _FloatingActions extends StatelessWidget {
  const _FloatingActions({
    required this.viewModel,
    required this.onImportBill,
    required this.onExport,
  });

  final BillListViewModel viewModel;
  final VoidCallback onImportBill;
  final VoidCallback onExport;

  @override
  Widget build(BuildContext context) {
    // Dois FloatingActionButton na mesma rota precisam de heroTag distinto,
    // senão a transição de página lança por tag duplicada.
    if (viewModel.isSelecting) {
      final count = viewModel.selectedBills.length;
      final enabled = count > 0 && !viewModel.isExporting;

      return FloatingActionButton.extended(
        heroTag: 'bills-export',
        onPressed: enabled ? onExport : null,
        backgroundColor:
            enabled ? null : Theme.of(context).colorScheme.surfaceContainerHighest,
        icon: const Icon(Symbols.download),
        label: Text(count == 0 ? 'Baixar' : 'Baixar ($count)'),
      );
    }

    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.end,
      children: [
        if (viewModel.canExport)
          FloatingActionButton.extended(
            heroTag: 'bills-start-selection',
            onPressed: viewModel.startSelection,
            icon: const Icon(Symbols.download),
            label: const Text('Baixar documentos'),
          ),
        BillPaymentPermissionGuard(
          resource: BillPaymentResources.bill,
          scope: BillPaymentScopes.import,
          child: Padding(
            padding: EdgeInsets.only(
              top: viewModel.canExport ? AppSpacing.sm : 0,
            ),
            child: FloatingActionButton.extended(
              heroTag: 'bills-import',
              onPressed: onImportBill,
              icon: const Icon(Symbols.upload_file),
              label: const Text('Importar'),
            ),
          ),
        ),
      ],
    );
  }
}

/// A barra do modo seleção: quantos, marcar os carregados e sair.
class _SelectionAppBar extends StatelessWidget implements PreferredSizeWidget {
  const _SelectionAppBar({required this.viewModel});

  final BillListViewModel viewModel;

  @override
  Size get preferredSize => const Size.fromHeight(kToolbarHeight);

  @override
  Widget build(BuildContext context) {
    final count = viewModel.selectedBills.length;

    return AppBar(
      leading: IconButton(
        icon: const Icon(Icons.close),
        tooltip: 'Sair da seleção',
        onPressed: viewModel.clearSelection,
      ),
      title: Text(switch (count) {
        0 => 'Selecione os boletos',
        1 => '1 selecionado',
        _ => '$count selecionados',
      }),
      actions: [
        IconButton(
          icon: const Icon(Symbols.select_all),
          // A lista é paginada: "todos" são os que já carregaram, e o
          // tooltip diz isso em vez de prometer o que não vai acontecer.
          tooltip: 'Selecionar todos os carregados',
          onPressed: viewModel.selectAllLoaded,
        ),
      ],
    );
  }
}

class _Results extends StatelessWidget {
  const _Results({
    required this.viewModel,
    required this.scrollController,
    required this.onOpenBill,
    required this.onScheduleBill,
  });

  final BillListViewModel viewModel;
  final ScrollController scrollController;
  final void Function(String id) onOpenBill;
  final void Function(String id) onScheduleBill;

  @override
  Widget build(BuildContext context) {
    switch (viewModel.status) {
      case BillListStatus.loading:
        return const Center(child: CircularProgressIndicator());
      case BillListStatus.error:
        return MessagePanel(
          icon: Symbols.error,
          title: viewModel.errorMessage ??
              'Não foi possível carregar os boletos.',
          action: FilledButton.tonal(
            onPressed: viewModel.load,
            child: const Text('Tentar novamente'),
          ),
        );
      case BillListStatus.empty:
        return const MessagePanel(
          icon: Symbols.receipt_long,
          title: 'Nenhum boleto neste estado.',
        );
      case BillListStatus.loaded:
      case BillListStatus.loadingMore:
        // A caixa de seleção só aparece com a seleção ligada — pelo botão
        // "Baixar documentos" ou pelo toque longo.
        final selecting = viewModel.isSelecting;

        return ListView.builder(
          controller: scrollController,
          // Folga no fim para a última linha não ficar embaixo dos botões
          // flutuantes — dois fora da seleção, um dentro dela.
          padding: EdgeInsets.fromLTRB(
            AppSpacing.md,
            0,
            AppSpacing.md,
            AppSpacing.md + (selecting || !viewModel.canExport ? 72 : 136),
          ),
          itemCount: viewModel.items.length + (viewModel.hasMore ? 1 : 0),
          itemBuilder: (context, index) {
            if (index >= viewModel.items.length) {
              return const Padding(
                padding: EdgeInsets.all(AppSpacing.md),
                child: Center(child: CircularProgressIndicator()),
              );
            }
            final bill = viewModel.items[index];
            return _BillCard(
              bill: bill,
              selected: viewModel.isSelected(bill.id),
              showCheckbox: selecting,
              selecting: selecting,
              onToggle: () => viewModel.toggleSelection(bill),
              onOpen: () => onOpenBill(bill.id),
              onSchedule: () => onScheduleBill(bill.id),
            );
          },
        );
    }
  }
}

class _BillCard extends StatelessWidget {
  const _BillCard({
    required this.bill,
    required this.selected,
    required this.showCheckbox,
    required this.selecting,
    required this.onToggle,
    required this.onOpen,
    required this.onSchedule,
  });

  final Bill bill;
  final bool selected;
  final bool showCheckbox;
  final bool selecting;
  final VoidCallback onToggle;
  final VoidCallback onOpen;
  final VoidCallback onSchedule;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: Card.outlined(
        clipBehavior: Clip.antiAlias,
        color: selected ? theme.colorScheme.secondaryContainer : null,
        child: InkWell(
          // Com a seleção ativa o toque marca em vez de abrir: abrir o
          // detalhe no meio da seleção a perderia.
          onTap: selecting ? onToggle : onOpen,
          onLongPress: onToggle,
          child: Padding(
            padding: const EdgeInsets.all(AppSpacing.md),
            child: Row(
              children: [
                if (showCheckbox)
                  Padding(
                    padding: const EdgeInsets.only(right: AppSpacing.sm),
                    child: Checkbox(
                      value: selected,
                      onChanged: (_) => onToggle(),
                    ),
                  ),
                Icon(
                  bill.rail == PaymentRails.pix
                      ? Symbols.qr_code_2
                      : Symbols.receipt_long,
                  color: theme.colorScheme.primary,
                ),
                const SizedBox(width: AppSpacing.md),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      if (bill.beneficiary?.displayName != null)
                        Text(
                          bill.beneficiary!.displayName!,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: theme.textTheme.titleSmall,
                        ),
                      Text(
                        formatMoney(bill.amount),
                        style: theme.textTheme.titleMedium,
                      ),
                      Text(
                        'Vence em ${formatDate(bill.dueDate)}'
                        '${bill.bankCode == null ? '' : ' · banco ${bill.bankCode}'}'
                        // A data de pagamento na linha: um Agendado
                        // sem ela obrigaria a abrir o detalhe.
                        '${bill.scheduledFor == null ? '' : ' · pagar em ${formatDate(bill.scheduledFor)}'}',
                        style: theme.textTheme.bodySmall?.copyWith(
                          color: theme.colorScheme.onSurfaceVariant,
                        ),
                      ),
                      const SizedBox(height: AppSpacing.sm),
                      Wrap(
                        spacing: AppSpacing.xs,
                        runSpacing: AppSpacing.xs,
                        children: [
                          StatusBadge.billStatus(bill.status),
                          // Perigo e Extremo Perigo pedem o olho já
                          // na fila — os níveis leves não poluem.
                          if (RiskLevels.tier(bill.riskLevel) >=
                              RiskLevels.tier(RiskLevels.danger))
                            StatusBadge(
                              label: RiskLevels.label(bill.riskLevel),
                              tone: BadgeTone.problem,
                            ),
                          StatusBadge(label: bill.rail),
                          StatusBadge(
                            label: BillKinds.label(bill.kind),
                          ),
                          // A análise não bloqueia o boleto, mas quem
                          // vê a fila precisa saber que a competência
                          // e a descrição ainda estão por vir.
                          if (ReadingStatuses.speaks(bill.readingStatus))
                            StatusBadge(
                              label: ReadingStatuses.label(bill.readingStatus),
                            ),
                          // Aprovado passou a ter dois significados
                          // (ADR-018): sem data espera alguém agendar,
                          // com data já tem ordem a caminho. Sem este
                          // selo a aba de Aprovados mistura os dois.
                          if (BillStatuses.isAwaitingSubmission(
                            bill.status,
                            bill.scheduledFor,
                          ))
                            const StatusBadge(label: 'Na fila de envio'),
                        ],
                      ),
                    ],
                  ),
                ),
                // O agendamento direto no card: era isso ou abrir o
                // detalhe de cada boleto para mandar pagar. Some durante a
                // seleção, que é outro trabalho.
                if (!selecting &&
                    BillStatuses.acceptsScheduling(
                      bill.status,
                      bill.scheduledFor,
                    ))
                  BillPaymentPermissionGuard(
                    resource: BillPaymentResources.bill,
                    scope: BillPaymentScopes.schedule,
                    child: Padding(
                      padding: const EdgeInsets.only(right: AppSpacing.sm),
                      child: FilledButton.tonal(
                        onPressed: onSchedule,
                        child: const Text('Agendar'),
                      ),
                    ),
                  ),
                if (!selecting) const Icon(Icons.chevron_right),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
