import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../bill_payment_permissions.dart';
import '../bill_payment_back_button.dart';
import '../shared/formats.dart';
import '../shared/message_panel.dart';
import '../shared/status_badge.dart';
import 'capture_source_list_viewmodel.dart';

/// The capture source listing: which mailboxes feed the capture.
class CaptureSourceListScreen extends StatefulWidget {
  /// Creates the screen.
  const CaptureSourceListScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    required this.onOpenSource,
    required this.onConnectSource,
  });

  /// Drives the screen.
  final CaptureSourceListViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Called with the id of the source to open.
  final void Function(String id) onOpenSource;

  /// Opens the connect form.
  final VoidCallback onConnectSource;

  @override
  State<CaptureSourceListScreen> createState() =>
      _CaptureSourceListScreenState();
}

class _CaptureSourceListScreenState extends State<CaptureSourceListScreen> {
  final _scrollController = ScrollController();

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
        title: const Text('Fontes de captura'),
        leading: BillPaymentBackButton(fallback: widget.backFallback),
      ),
      floatingActionButton: BillPaymentPermissionGuard(
        resource: BillPaymentResources.captureSource,
        scope: BillPaymentScopes.manage,
        child: FloatingActionButton.extended(
          onPressed: widget.onConnectSource,
          icon: const Icon(Symbols.mail),
          label: const Text('Conectar caixa'),
        ),
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) {
            final viewModel = widget.viewModel;
            switch (viewModel.status) {
              case CaptureSourceListStatus.loading:
                return const Center(child: CircularProgressIndicator());
              case CaptureSourceListStatus.error:
                return MessagePanel(
                  icon: Symbols.error,
                  title: viewModel.errorMessage ??
                      'Não foi possível carregar as fontes.',
                  action: FilledButton.tonal(
                    onPressed: viewModel.load,
                    child: const Text('Tentar novamente'),
                  ),
                );
              case CaptureSourceListStatus.empty:
                return const MessagePanel(
                  icon: Symbols.mark_email_unread,
                  title: 'Nenhuma caixa conectada.\nConecte a caixa que '
                      'recebe os boletos para a captura trabalhar por você.',
                );
              case CaptureSourceListStatus.loaded:
              case CaptureSourceListStatus.loadingMore:
                return Center(
                  child: ConstrainedBox(
                    constraints: const BoxConstraints(
                      maxWidth: AppBreakpoints.desktop,
                    ),
                    child: ListView.builder(
                      controller: _scrollController,
                      padding: const EdgeInsets.fromLTRB(
                        AppSpacing.md,
                        AppSpacing.md,
                        AppSpacing.md,
                        AppSpacing.md + 72,
                      ),
                      itemCount: viewModel.items.length +
                          (viewModel.hasMore ? 1 : 0),
                      itemBuilder: (context, index) {
                        if (index >= viewModel.items.length) {
                          return const Padding(
                            padding: EdgeInsets.all(AppSpacing.md),
                            child:
                                Center(child: CircularProgressIndicator()),
                          );
                        }
                        final source = viewModel.items[index];
                        final theme = Theme.of(context);
                        return Padding(
                          padding:
                              const EdgeInsets.only(bottom: AppSpacing.sm),
                          child: Card.outlined(
                            clipBehavior: Clip.antiAlias,
                            child: InkWell(
                              onTap: () => widget.onOpenSource(source.id),
                              child: Padding(
                                padding:
                                    const EdgeInsets.all(AppSpacing.md),
                                child: Row(
                                  children: [
                                    Icon(
                                      Symbols.inbox,
                                      color: theme.colorScheme.primary,
                                    ),
                                    const SizedBox(width: AppSpacing.md),
                                    Expanded(
                                      child: Column(
                                        crossAxisAlignment:
                                            CrossAxisAlignment.start,
                                        children: [
                                          Text(
                                            source.displayName,
                                            style: theme
                                                .textTheme.titleMedium,
                                          ),
                                          Text(
                                            source.address,
                                            style: theme.textTheme.bodySmall
                                                ?.copyWith(
                                              color: theme.colorScheme
                                                  .onSurfaceVariant,
                                            ),
                                          ),
                                          const SizedBox(
                                              height: AppSpacing.sm),
                                          Wrap(
                                            spacing: AppSpacing.xs,
                                            runSpacing: AppSpacing.xs,
                                            children: [
                                              StatusBadge(
                                                label: source.isEnabled
                                                    ? 'Ativa'
                                                    : 'Desativada',
                                                tone: source.isEnabled
                                                    ? BadgeTone.positive
                                                    : BadgeTone.neutral,
                                              ),
                                              if (source.lastSyncError !=
                                                  null)
                                                const StatusBadge(
                                                  label: 'Falha na última '
                                                      'sincronização',
                                                  tone: BadgeTone.problem,
                                                )
                                              else if (source.lastSyncAt !=
                                                  null)
                                                StatusBadge(
                                                  label: 'Sincronizada em '
                                                      '${formatDateTime(source.lastSyncAt)}',
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
                    ),
                  ),
                );
            }
          },
        ),
      ),
    );
  }
}
