import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../bill_payment_permissions.dart';
import '../../domain/bill_payment_enums.dart';
import '../bill_payment_back_button.dart';
import '../shared/formats.dart';
import '../shared/message_panel.dart';
import '../shared/status_badge.dart';
import 'bill_list_viewmodel.dart';

/// The bill listing — the approver's work queue when filtered by
/// "aguardando aprovação".
class BillListScreen extends StatefulWidget {
  /// Creates the screen.
  const BillListScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    required this.onOpenBill,
    required this.onImportBill,
  });

  /// Drives the screen.
  final BillListViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Called with the id of the bill to open.
  final void Function(String id) onOpenBill;

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
    widget.viewModel.load();
  }

  @override
  void dispose() {
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Boletos'),
        leading: BillPaymentBackButton(fallback: widget.backFallback),
      ),
      floatingActionButton: BillPaymentPermissionGuard(
        resource: BillPaymentResources.bill,
        scope: BillPaymentScopes.import,
        child: FloatingActionButton.extended(
          onPressed: widget.onImportBill,
          icon: const Icon(Symbols.upload_file),
          label: const Text('Importar'),
        ),
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
                              selected:
                                  widget.viewModel.statusFilter == status,
                              onSelected: (_) =>
                                  widget.viewModel.selectStatus(status),
                            ),
                        ],
                      ),
                    ),
                  ),
                  Expanded(
                    child: _Results(
                      viewModel: widget.viewModel,
                      scrollController: _scrollController,
                      onOpenBill: widget.onOpenBill,
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
  });

  final BillListViewModel viewModel;
  final ScrollController scrollController;
  final void Function(String id) onOpenBill;

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
        return ListView.builder(
          controller: scrollController,
          padding: const EdgeInsets.fromLTRB(
            AppSpacing.md,
            0,
            AppSpacing.md,
            AppSpacing.md + 72,
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
            final theme = Theme.of(context);
            return Padding(
              padding: const EdgeInsets.only(bottom: AppSpacing.sm),
              child: Card.outlined(
                clipBehavior: Clip.antiAlias,
                child: InkWell(
                  onTap: () => onOpenBill(bill.id),
                  child: Padding(
                    padding: const EdgeInsets.all(AppSpacing.md),
                    child: Row(
                      children: [
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
                                      label:
                                          RiskLevels.label(bill.riskLevel),
                                      tone: BadgeTone.problem,
                                    ),
                                  StatusBadge(label: bill.rail),
                                  StatusBadge(
                                    label: BillKinds.label(bill.kind),
                                  ),
                                  // A análise não bloqueia o boleto, mas quem
                                  // vê a fila precisa saber que a competência
                                  // e a descrição ainda estão por vir.
                                  if (ReadingStatuses.speaks(
                                    bill.readingStatus,
                                  ))
                                    StatusBadge(
                                      label: ReadingStatuses.label(
                                        bill.readingStatus,
                                      ),
                                    ),
                                ],
                              ),
                            ],
                          ),
                        ),
                        const Icon(Icons.chevron_right),
                      ],
                    ),
                  ),
                ),
              ),
            );
          },
        );
    }
  }
}
