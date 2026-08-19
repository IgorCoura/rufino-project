import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../domain/bill_payment_enums.dart';
import '../bill_payment_back_button.dart';
import '../shared/formats.dart';
import '../shared/message_panel.dart';
import '../shared/status_badge.dart';
import 'capture_item_list_viewmodel.dart';

/// The quarantine: what the capture could not resolve on its own.
class CaptureItemListScreen extends StatefulWidget {
  /// Creates the screen.
  const CaptureItemListScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    required this.onOpenItem,
  });

  /// Drives the screen.
  final CaptureItemListViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Called with the id of the item to open.
  final void Function(String id) onOpenItem;

  @override
  State<CaptureItemListScreen> createState() => _CaptureItemListScreenState();
}

class _CaptureItemListScreenState extends State<CaptureItemListScreen> {
  final _scrollController = ScrollController();

  static const _filters = <(String label, String? status)>[
    ('Aguardando reivindicação', CaptureItemStatuses.unrouted),
    ('Não reconhecidos', CaptureItemStatuses.unrecognized),
    ('Protegidos por senha', CaptureItemStatuses.locked),
    ('Download falhou', CaptureItemStatuses.linkFailed),
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
        title: const Text('Quarentena'),
        leading: BillPaymentBackButton(fallback: widget.backFallback),
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
                      onOpenItem: widget.onOpenItem,
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
    required this.onOpenItem,
  });

  final CaptureItemListViewModel viewModel;
  final ScrollController scrollController;
  final void Function(String id) onOpenItem;

  @override
  Widget build(BuildContext context) {
    switch (viewModel.status) {
      case CaptureItemListStatus.loading:
        return const Center(child: CircularProgressIndicator());
      case CaptureItemListStatus.error:
        return MessagePanel(
          icon: Symbols.error,
          title: viewModel.errorMessage ??
              'Não foi possível carregar a quarentena.',
          action: FilledButton.tonal(
            onPressed: viewModel.load,
            child: const Text('Tentar novamente'),
          ),
        );
      case CaptureItemListStatus.empty:
        return const MessagePanel(
          icon: Symbols.inbox_customize,
          title: 'Nada aqui — a fila está limpa.',
        );
      case CaptureItemListStatus.loaded:
      case CaptureItemListStatus.loadingMore:
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
            final item = viewModel.items[index];
            final theme = Theme.of(context);
            return Padding(
              padding: const EdgeInsets.only(bottom: AppSpacing.sm),
              child: Card.outlined(
                clipBehavior: Clip.antiAlias,
                child: InkWell(
                  onTap: () => onOpenItem(item.id),
                  child: Padding(
                    padding: const EdgeInsets.all(AppSpacing.md),
                    child: Row(
                      children: [
                        Icon(
                          Symbols.mail,
                          color: theme.colorScheme.primary,
                        ),
                        const SizedBox(width: AppSpacing.md),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                item.subject ?? '(sem assunto)',
                                style: theme.textTheme.titleMedium,
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                              ),
                              Text(
                                '${item.sender ?? '—'} · '
                                '${formatDateTime(item.receivedAt)}',
                                style: theme.textTheme.bodySmall?.copyWith(
                                  color: theme.colorScheme.onSurfaceVariant,
                                ),
                              ),
                              const SizedBox(height: AppSpacing.sm),
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
