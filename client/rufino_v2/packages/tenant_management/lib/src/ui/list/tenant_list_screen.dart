import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../domain/tenant_enums.dart';
import '../../domain/tenant_summary.dart';
import '../tenant_back_button.dart';
import 'tenant_list_viewmodel.dart';

/// The back-office listing: find a customer, and see what went wrong.
///
/// Two badges carry the weight here — the cadastro's status and the state of
/// the access grant. The second is the reason this screen exists: a customer
/// can be perfectly registered and still locked out.
class TenantListScreen extends StatefulWidget {
  /// Creates the screen.
  const TenantListScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    required this.onOpenTenant,
  });

  /// Drives the screen.
  final TenantListViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha — o Home do app, já que o
  /// menu chega aqui por substituição.
  final String backFallback;

  /// Called with the id of the tenant to open.
  final void Function(String id) onOpenTenant;

  @override
  State<TenantListScreen> createState() => _TenantListScreenState();
}

class _TenantListScreenState extends State<TenantListScreen> {
  final _searchController = TextEditingController();
  final _scrollController = ScrollController();

  @override
  void initState() {
    super.initState();
    _searchController.text = widget.viewModel.filter.search ?? '';
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
        title: const Text('Clientes da plataforma'),
        leading: TenantBackButton(fallback: widget.backFallback),
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) => Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(
                maxWidth: AppBreakpoints.desktop,
              ),
              child: Column(
                children: [
                  _Filters(
                    viewModel: widget.viewModel,
                    controller: _searchController,
                  ),
                  Expanded(
                    child: _Results(
                      viewModel: widget.viewModel,
                      scrollController: _scrollController,
                      onOpenTenant: widget.onOpenTenant,
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

class _Filters extends StatelessWidget {
  const _Filters({required this.viewModel, required this.controller});

  final TenantListViewModel viewModel;
  final TextEditingController controller;

  @override
  Widget build(BuildContext context) {
    final filter = viewModel.filter;

    return Padding(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.md,
        AppSpacing.md,
        AppSpacing.md,
        AppSpacing.sm,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          TextField(
            controller: controller,
            decoration: InputDecoration(
              hintText: 'Nome, nome fantasia ou documento',
              prefixIcon: const Icon(Icons.search),
              border: const OutlineInputBorder(),
              suffixIcon: controller.text.isEmpty
                  ? null
                  : IconButton(
                      icon: const Icon(Icons.close),
                      onPressed: () {
                        controller.clear();
                        viewModel.search('');
                      },
                    ),
            ),
            textInputAction: TextInputAction.search,
            onSubmitted: viewModel.search,
          ),
          const SizedBox(height: AppSpacing.sm),
          Wrap(
            spacing: AppSpacing.sm,
            runSpacing: AppSpacing.xs,
            children: [
              FilterChip(
                label: const Text('Pessoa física'),
                selected: filter.kind == TenantKinds.individual,
                onSelected: (_) =>
                    viewModel.toggleKind(TenantKinds.individual),
              ),
              FilterChip(
                label: const Text('Pessoa jurídica'),
                selected: filter.kind == TenantKinds.company,
                onSelected: (_) => viewModel.toggleKind(TenantKinds.company),
              ),
              FilterChip(
                label: const Text('Ativos'),
                selected: filter.status == TenantStatuses.active,
                onSelected: (_) =>
                    viewModel.toggleStatus(TenantStatuses.active),
              ),
              FilterChip(
                label: const Text('Suspensos'),
                selected: filter.status == TenantStatuses.suspended,
                onSelected: (_) =>
                    viewModel.toggleStatus(TenantStatuses.suspended),
              ),
              FilterChip(
                label: const Text('Gestão de Pessoas'),
                selected: filter.product == TenantProducts.peopleManagement,
                onSelected: (_) =>
                    viewModel.toggleProduct(TenantProducts.peopleManagement),
              ),
              FilterChip(
                label: const Text('Contas a Pagar'),
                selected: filter.product == TenantProducts.billPayment,
                onSelected: (_) =>
                    viewModel.toggleProduct(TenantProducts.billPayment),
              ),
              if (viewModel.hasFilters)
                ActionChip(
                  avatar: const Icon(Icons.filter_alt_off_outlined, size: 18),
                  label: const Text('Limpar filtros'),
                  onPressed: () {
                    controller.clear();
                    viewModel.clearFilters();
                  },
                ),
            ],
          ),
        ],
      ),
    );
  }
}

class _Results extends StatelessWidget {
  const _Results({
    required this.viewModel,
    required this.scrollController,
    required this.onOpenTenant,
  });

  final TenantListViewModel viewModel;
  final ScrollController scrollController;
  final void Function(String id) onOpenTenant;

  @override
  Widget build(BuildContext context) {
    switch (viewModel.status) {
      case TenantListStatus.loading:
        return const Center(child: CircularProgressIndicator());
      case TenantListStatus.error:
        return _Message(
          icon: Symbols.error,
          title: viewModel.errorMessage ??
              'Não foi possível carregar os clientes.',
          action: FilledButton.tonal(
            onPressed: viewModel.load,
            child: const Text('Tentar novamente'),
          ),
        );
      case TenantListStatus.empty:
        return _Message(
          icon: Symbols.search_off,
          title: 'Nenhum cliente encontrado.',
          action: viewModel.hasFilters
              ? TextButton(
                  onPressed: viewModel.clearFilters,
                  child: const Text('Limpar filtros'),
                )
              : null,
        );
      case TenantListStatus.loaded:
      case TenantListStatus.loadingMore:
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
            final tenant = viewModel.items[index];
            return Padding(
              padding: const EdgeInsets.only(bottom: AppSpacing.sm),
              child: _TenantRow(
                tenant: tenant,
                onTap: () => onOpenTenant(tenant.id),
              ),
            );
          },
        );
    }
  }
}

class _TenantRow extends StatelessWidget {
  const _TenantRow({required this.tenant, required this.onTap});

  final TenantSummary tenant;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final cs = theme.colorScheme;

    return Card.outlined(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.md),
          child: Row(
            children: [
              Icon(
                tenant.isIndividual ? Symbols.person : Symbols.apartment,
                color: cs.primary,
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      tenant.displayName,
                      style: theme.textTheme.titleMedium,
                    ),
                    if (tenant.tradeName.isNotEmpty)
                      Text(
                        tenant.legalName,
                        style: theme.textTheme.bodySmall?.copyWith(
                          color: cs.onSurfaceVariant,
                        ),
                      ),
                    Text(
                      tenant.formattedTaxId,
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: cs.onSurfaceVariant,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.sm),
                    Wrap(
                      spacing: AppSpacing.xs,
                      runSpacing: AppSpacing.xs,
                      children: [
                        for (final product in tenant.activeProducts)
                          _Badge(label: TenantProductLabels.label(product)),
                        if (tenant.isSuspended)
                          _Badge(
                            label: 'Suspenso',
                            background: cs.errorContainer,
                            foreground: cs.onErrorContainer,
                          ),
                        if (tenant.hasFailedProvisioning)
                          _Badge(
                            label: 'Acesso falhou',
                            background: cs.errorContainer,
                            foreground: cs.onErrorContainer,
                          )
                        else if (tenant.hasPendingProvisioning)
                          _Badge(
                            label: 'Acesso pendente',
                            background: cs.tertiaryContainer,
                            foreground: cs.onTertiaryContainer,
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
    );
  }
}

class _Badge extends StatelessWidget {
  const _Badge({required this.label, this.background, this.foreground});

  final String label;
  final Color? background;
  final Color? foreground;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: 2,
      ),
      decoration: BoxDecoration(
        color: background ?? cs.secondaryContainer,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        label,
        style: Theme.of(context).textTheme.labelSmall?.copyWith(
              color: foreground ?? cs.onSecondaryContainer,
            ),
      ),
    );
  }
}

class _Message extends StatelessWidget {
  const _Message({required this.icon, required this.title, this.action});

  final IconData icon;
  final String title;
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 56, color: theme.colorScheme.outline),
            const SizedBox(height: AppSpacing.md),
            Text(
              title,
              style: theme.textTheme.titleMedium,
              textAlign: TextAlign.center,
            ),
            if (action != null) ...[
              const SizedBox(height: AppSpacing.md),
              action!,
            ],
          ],
        ),
      ),
    );
  }
}
