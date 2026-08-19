import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../bill_payment_permissions.dart';
import '../bill_payment_back_button.dart';
import '../shared/message_panel.dart';
import '../shared/status_badge.dart';
import 'payee_list_viewmodel.dart';

/// The payee listing: who this tenant expects to pay.
class PayeeListScreen extends StatefulWidget {
  /// Creates the screen.
  const PayeeListScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    required this.onOpenPayee,
    required this.onCreatePayee,
  });

  /// Drives the screen.
  final PayeeListViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Called with the id of the payee to open.
  final void Function(String id) onOpenPayee;

  /// Opens the register form.
  final VoidCallback onCreatePayee;

  @override
  State<PayeeListScreen> createState() => _PayeeListScreenState();
}

class _PayeeListScreenState extends State<PayeeListScreen> {
  final _searchController = TextEditingController();
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
    _searchController.dispose();
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
        title: const Text('Beneficiários'),
        leading: BillPaymentBackButton(fallback: widget.backFallback),
      ),
      floatingActionButton: BillPaymentPermissionGuard(
        resource: BillPaymentResources.payee,
        scope: BillPaymentScopes.manage,
        child: FloatingActionButton.extended(
          onPressed: widget.onCreatePayee,
          icon: const Icon(Symbols.add),
          label: const Text('Cadastrar'),
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
                    child: TextField(
                      controller: _searchController,
                      decoration: InputDecoration(
                        hintText: 'Buscar por CPF/CNPJ exato',
                        prefixIcon: const Icon(Icons.search),
                        border: const OutlineInputBorder(),
                        suffixIcon: _searchController.text.isEmpty
                            ? null
                            : IconButton(
                                icon: const Icon(Icons.close),
                                onPressed: () {
                                  _searchController.clear();
                                  widget.viewModel.searchByTaxId('');
                                },
                              ),
                      ),
                      textInputAction: TextInputAction.search,
                      onSubmitted: widget.viewModel.searchByTaxId,
                    ),
                  ),
                  Expanded(
                    child: _Results(
                      viewModel: widget.viewModel,
                      scrollController: _scrollController,
                      onOpenPayee: widget.onOpenPayee,
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
    required this.onOpenPayee,
  });

  final PayeeListViewModel viewModel;
  final ScrollController scrollController;
  final void Function(String id) onOpenPayee;

  @override
  Widget build(BuildContext context) {
    switch (viewModel.status) {
      case PayeeListStatus.loading:
        return const Center(child: CircularProgressIndicator());
      case PayeeListStatus.error:
        return MessagePanel(
          icon: Symbols.error,
          title: viewModel.errorMessage ??
              'Não foi possível carregar os beneficiários.',
          action: FilledButton.tonal(
            onPressed: viewModel.load,
            child: const Text('Tentar novamente'),
          ),
        );
      case PayeeListStatus.empty:
        return MessagePanel(
          icon: Symbols.search_off,
          title: viewModel.isSearching
              ? 'Nenhum beneficiário com este documento.'
              : 'Nenhum beneficiário cadastrado.\nSem cadastro, o que a '
                  'captura não reconhecer é descartado.',
          action: viewModel.isSearching
              ? TextButton(
                  onPressed: () => viewModel.searchByTaxId(''),
                  child: const Text('Limpar busca'),
                )
              : null,
        );
      case PayeeListStatus.loaded:
      case PayeeListStatus.loadingMore:
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
            final payee = viewModel.items[index];
            return Padding(
              padding: const EdgeInsets.only(bottom: AppSpacing.sm),
              child: Card.outlined(
                clipBehavior: Clip.antiAlias,
                child: InkWell(
                  onTap: () => onOpenPayee(payee.id),
                  child: Padding(
                    padding: const EdgeInsets.all(AppSpacing.md),
                    child: Row(
                      children: [
                        Icon(
                          Symbols.storefront,
                          color: Theme.of(context).colorScheme.primary,
                        ),
                        const SizedBox(width: AppSpacing.md),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                payee.legalName,
                                style:
                                    Theme.of(context).textTheme.titleMedium,
                              ),
                              Text(
                                payee.taxId,
                                style: Theme.of(context)
                                    .textTheme
                                    .bodySmall
                                    ?.copyWith(
                                      color: Theme.of(context)
                                          .colorScheme
                                          .onSurfaceVariant,
                                    ),
                              ),
                              const SizedBox(height: AppSpacing.sm),
                              Wrap(
                                spacing: AppSpacing.xs,
                                runSpacing: AppSpacing.xs,
                                children: [
                                  StatusBadge(
                                    label: payee.amountPolicy.summary,
                                  ),
                                  if (!payee.isActive)
                                    const StatusBadge(
                                      label: 'Desativado',
                                      tone: BadgeTone.problem,
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
