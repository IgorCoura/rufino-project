import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../bill_payment_permissions.dart';
import '../../domain/bill_payment_enums.dart';
import '../bill_payment_back_button.dart';
import '../shared/message_panel.dart';
import '../shared/status_badge.dart';
import 'expectation_list_viewmodel.dart';

/// The expectation listing: what this tenant expects to receive.
class ExpectationListScreen extends StatefulWidget {
  /// Creates the screen.
  const ExpectationListScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    required this.onOpenExpectation,
    required this.onCreateExpectation,
  });

  /// Drives the screen.
  final ExpectationListViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Called with the id of the expectation to open.
  final void Function(String id) onOpenExpectation;

  /// Opens the register form.
  final VoidCallback onCreateExpectation;

  @override
  State<ExpectationListScreen> createState() => _ExpectationListScreenState();
}

class _ExpectationListScreenState extends State<ExpectationListScreen> {
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
        title: const Text('Expectativas'),
        leading: BillPaymentBackButton(fallback: widget.backFallback),
      ),
      floatingActionButton: BillPaymentPermissionGuard(
        resource: BillPaymentResources.expectation,
        scope: BillPaymentScopes.manage,
        child: FloatingActionButton.extended(
          onPressed: widget.onCreateExpectation,
          icon: const Icon(Symbols.add_alert),
          label: const Text('Cadastrar'),
        ),
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) {
            final viewModel = widget.viewModel;
            switch (viewModel.status) {
              case ExpectationListStatus.loading:
                return const Center(child: CircularProgressIndicator());
              case ExpectationListStatus.error:
                return MessagePanel(
                  icon: Symbols.error,
                  title: viewModel.errorMessage ??
                      'Não foi possível carregar as expectativas.',
                  action: FilledButton.tonal(
                    onPressed: viewModel.load,
                    child: const Text('Tentar novamente'),
                  ),
                );
              case ExpectationListStatus.empty:
                return const MessagePanel(
                  icon: Symbols.notifications,
                  title: 'Nenhuma expectativa.\nCadastre o que você espera '
                      'receber e o sistema avisa quando não chegar.',
                );
              case ExpectationListStatus.loaded:
              case ExpectationListStatus.loadingMore:
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
                        final expectation = viewModel.items[index];
                        final theme = Theme.of(context);
                        return Padding(
                          padding:
                              const EdgeInsets.only(bottom: AppSpacing.sm),
                          child: Card.outlined(
                            clipBehavior: Clip.antiAlias,
                            child: InkWell(
                              onTap: () =>
                                  widget.onOpenExpectation(expectation.id),
                              child: Padding(
                                padding:
                                    const EdgeInsets.all(AppSpacing.md),
                                child: Row(
                                  children: [
                                    Icon(
                                      Symbols.notifications_active,
                                      color: theme.colorScheme.primary,
                                    ),
                                    const SizedBox(width: AppSpacing.md),
                                    Expanded(
                                      child: Column(
                                        crossAxisAlignment:
                                            CrossAxisAlignment.start,
                                        children: [
                                          Text(
                                            expectation.label,
                                            style: theme
                                                .textTheme.titleMedium,
                                          ),
                                          Text(
                                            '${Recurrences.label(expectation.recurrence)} '
                                            '· dia ${expectation.expectedDueDay}'
                                            '${expectation.accountReference == null ? '' : ' · conta ${expectation.accountReference}'}',
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
                                                label: expectation.isActive
                                                    ? 'Ativa'
                                                    : 'Desativada',
                                                tone: expectation.isActive
                                                    ? BadgeTone.positive
                                                    : BadgeTone.neutral,
                                              ),
                                              StatusBadge(
                                                label: ExpectationOrigins
                                                    .label(
                                                  expectation.origin,
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
