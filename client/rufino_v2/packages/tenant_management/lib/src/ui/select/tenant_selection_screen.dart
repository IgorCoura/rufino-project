import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../domain/my_tenant.dart';
import '../../domain/tenant_enums.dart';
import '../../tenant_permissions.dart';
import 'tenant_selection_viewmodel.dart';

/// The app's front door: pick which customer you are operating as.
///
/// Serves two audiences with one screen. A customer sees the tenants they
/// belong to; a platform operator usually belongs to none, and sees instead
/// the door to the back-office and the button that registers a new customer —
/// which is the **only** place a tenant is created.
class TenantSelectionScreen extends StatefulWidget {
  /// Creates the screen.
  const TenantSelectionScreen({
    super.key,
    required this.viewModel,
    required this.onSelected,
    required this.onCreateTenant,
    required this.onOpenBackOffice,
    required this.onLogout,
  });

  /// Drives the screen.
  final TenantSelectionViewModel viewModel;

  /// Called once a tenant became the current context.
  final VoidCallback onSelected;

  /// Called to open the cadastro of a new tenant.
  final VoidCallback onCreateTenant;

  /// Called to open the back-office listing.
  final VoidCallback onOpenBackOffice;

  /// Called to sign out — authentication belongs to the shell.
  final Future<void> Function() onLogout;

  @override
  State<TenantSelectionScreen> createState() => _TenantSelectionScreenState();
}

class _TenantSelectionScreenState extends State<TenantSelectionScreen> {
  @override
  void initState() {
    super.initState();
    widget.viewModel.load();
  }

  Future<void> _select(MyTenant tenant) async {
    final moved = await widget.viewModel.select(tenant);
    if (!mounted) return;

    if (moved) {
      widget.onSelected();
      return;
    }

    final message = widget.viewModel.errorMessage;
    if (message != null) {
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(content: Text(message), behavior: SnackBarBehavior.floating),
        );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Selecionar cliente'),
        actions: [
          IconButton(
            tooltip: 'Sair',
            icon: const Icon(Icons.logout),
            onPressed: widget.onLogout,
          ),
          const SizedBox(width: AppSpacing.sm),
        ],
      ),
      floatingActionButton: TenantPermissionGuard(
        resource: TenantResources.tenant,
        scope: TenantScopes.create,
        child: FloatingActionButton.extended(
          onPressed: widget.onCreateTenant,
          icon: const Icon(Icons.add),
          label: const Text('Cadastrar cliente'),
        ),
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) => _Body(
            viewModel: widget.viewModel,
            onSelect: _select,
            onOpenBackOffice: widget.onOpenBackOffice,
            onLogout: widget.onLogout,
          ),
        ),
      ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({
    required this.viewModel,
    required this.onSelect,
    required this.onOpenBackOffice,
    required this.onLogout,
  });

  final TenantSelectionViewModel viewModel;
  final Future<void> Function(MyTenant) onSelect;
  final VoidCallback onOpenBackOffice;
  final Future<void> Function() onLogout;

  @override
  Widget build(BuildContext context) {
    if (viewModel.status == TenantSelectionStatus.loading) {
      return const Center(child: CircularProgressIndicator());
    }

    return Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: AppBreakpoints.tablet),
        child: ListView(
          padding: const EdgeInsets.fromLTRB(
            AppSpacing.md,
            AppSpacing.md,
            AppSpacing.md,
            AppSpacing.md + 72,
          ),
          children: [
            if (viewModel.status == TenantSelectionStatus.error)
              _ErrorState(
                message: viewModel.errorMessage,
                onRetry: viewModel.load,
              )
            else if (viewModel.status == TenantSelectionStatus.empty)
              _EmptyState(onLogout: onLogout)
            else
              ...viewModel.tenants.map(
                (tenant) => Padding(
                  padding: const EdgeInsets.only(bottom: AppSpacing.sm),
                  child: _TenantCard(
                    tenant: tenant,
                    isCurrent: tenant.id == viewModel.currentTenantId,
                    onTap: viewModel.isBusy ? null : () => onSelect(tenant),
                  ),
                ),
              ),
            const SizedBox(height: AppSpacing.lg),
            TenantModuleGuard(
              resource: TenantResources.tenant,
              child: _BackOfficeSection(onOpen: onOpenBackOffice),
            ),
          ],
        ),
      ),
    );
  }
}

class _TenantCard extends StatelessWidget {
  const _TenantCard({
    required this.tenant,
    required this.isCurrent,
    required this.onTap,
  });

  final MyTenant tenant;
  final bool isCurrent;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final cs = theme.colorScheme;
    final enabled = tenant.isSelectable && onTap != null;

    return Card.outlined(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: enabled ? onTap : null,
        child: Opacity(
          opacity: tenant.isSelectable ? 1 : 0.6,
          child: Padding(
            padding: const EdgeInsets.all(AppSpacing.md),
            child: Row(
              children: [
                Icon(
                  tenant.isIndividual ? Symbols.person : Symbols.apartment,
                  size: 28,
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
                      const SizedBox(height: AppSpacing.sm),
                      Wrap(
                        spacing: AppSpacing.xs,
                        runSpacing: AppSpacing.xs,
                        children: [
                          for (final product in tenant.activeProducts)
                            _Chip(label: TenantProductLabels.label(product)),
                          if (tenant.isSuspended)
                            _Chip(
                              label: 'Suspenso',
                              background: cs.errorContainer,
                              foreground: cs.onErrorContainer,
                            ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: AppSpacing.sm),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(
                      tenant.roleLabel,
                      style: theme.textTheme.labelMedium?.copyWith(
                        color: cs.onSurfaceVariant,
                      ),
                    ),
                    if (isCurrent)
                      Padding(
                        padding: const EdgeInsets.only(top: AppSpacing.xs),
                        child: Icon(
                          Symbols.check_circle,
                          size: 18,
                          color: cs.primary,
                        ),
                      ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _Chip extends StatelessWidget {
  const _Chip({required this.label, this.background, this.foreground});

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

class _EmptyState extends StatelessWidget {
  const _EmptyState({required this.onLogout});

  final Future<void> Function() onLogout;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      children: [
        const SizedBox(height: AppSpacing.xl),
        Icon(
          Symbols.apartment,
          size: 64,
          color: theme.colorScheme.outline,
        ),
        const SizedBox(height: AppSpacing.md),
        Text(
          'Você ainda não faz parte de nenhum cliente.',
          style: theme.textTheme.titleMedium,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: AppSpacing.sm),
        Text(
          'Fale com o responsável pela sua conta para receber acesso.',
          style: theme.textTheme.bodyMedium?.copyWith(
            color: theme.colorScheme.onSurfaceVariant,
          ),
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: AppSpacing.lg),
        OutlinedButton.icon(
          onPressed: onLogout,
          icon: const Icon(Icons.logout),
          label: const Text('Sair'),
        ),
      ],
    );
  }
}

class _ErrorState extends StatelessWidget {
  const _ErrorState({required this.message, required this.onRetry});

  final String? message;
  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      children: [
        const SizedBox(height: AppSpacing.xl),
        Icon(
          Symbols.error,
          size: 56,
          color: theme.colorScheme.error,
        ),
        const SizedBox(height: AppSpacing.md),
        Text(
          message ?? 'Não foi possível carregar seus clientes.',
          style: theme.textTheme.bodyLarge,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: AppSpacing.md),
        FilledButton.tonal(
          onPressed: onRetry,
          child: const Text('Tentar novamente'),
        ),
      ],
    );
  }
}

class _BackOfficeSection extends StatelessWidget {
  const _BackOfficeSection({required this.onOpen});

  final VoidCallback onOpen;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'ADMINISTRAÇÃO DA PLATAFORMA',
          style: theme.textTheme.labelSmall?.copyWith(
            color: theme.colorScheme.onSurfaceVariant,
            letterSpacing: 0.8,
          ),
        ),
        const SizedBox(height: AppSpacing.sm),
        Card.outlined(
          clipBehavior: Clip.antiAlias,
          child: ListTile(
            leading: const Icon(Symbols.manage_accounts),
            title: const Text('Clientes'),
            subtitle: const Text('Cadastro, acessos e produtos'),
            trailing: const Icon(Icons.chevron_right),
            onTap: onOpen,
          ),
        ),
      ],
    );
  }
}
