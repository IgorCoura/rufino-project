import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../domain/tenant.dart';
import '../../domain/tenant_enums.dart';
import '../../tenant_permissions.dart';
import 'components/access_tab.dart';
import 'components/address_section.dart';
import 'components/contact_section.dart';
import 'components/identification_section.dart';
import '../tenant_back_button.dart';
import 'components/products_tab.dart';
import 'tenant_detail_viewmodel.dart';

/// The full cadastro of one tenant: identity, access and products.
class TenantDetailScreen extends StatefulWidget {
  /// Creates the screen.
  const TenantDetailScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
  });

  /// Drives the screen.
  final TenantDetailViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  @override
  State<TenantDetailScreen> createState() => _TenantDetailScreenState();
}

class _TenantDetailScreenState extends State<TenantDetailScreen> {
  @override
  void initState() {
    super.initState();
    widget.viewModel.addListener(_onChanged);
    widget.viewModel.load();
  }

  @override
  void dispose() {
    widget.viewModel.removeListener(_onChanged);
    super.dispose();
  }

  void _onChanged() {
    if (!mounted) return;
    final message = widget.viewModel.consumeSnackMessage();
    if (message == null) return;

    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(content: Text(message), behavior: SnackBarBehavior.floating),
      );
  }

  Future<void> _suspend(Tenant tenant) async {
    final controller = TextEditingController();
    final formKey = GlobalKey<FormState>();

    final reason = await showDialog<String>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Suspender cliente'),
        content: Form(
          key: formKey,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Text(
                'O cadastro é preservado e as alterações ficam bloqueadas.',
              ),
              const SizedBox(height: AppSpacing.md),
              TextFormField(
                controller: controller,
                decoration: const InputDecoration(
                  labelText: 'Motivo',
                  border: OutlineInputBorder(),
                ),
                maxLines: 2,
                validator: Tenant.validateSuspensionReason,
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () {
              if (formKey.currentState?.validate() != true) return;
              Navigator.of(dialogContext).pop(controller.text.trim());
            },
            child: const Text('Suspender'),
          ),
        ],
      ),
    );

    controller.dispose();
    if (reason != null) await widget.viewModel.suspend(reason);
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final tenant = widget.viewModel.tenant;

        if (widget.viewModel.status == TenantDetailStatus.loading) {
          // Com AppBar mesmo carregando: sem ela, uma carga lenta ou falha de
          // rede deixaria a tela sem nenhuma saída.
          return Scaffold(
            appBar: AppBar(
              title: const Text('Cliente'),
              leading: TenantBackButton(fallback: widget.backFallback),
            ),
            body: const Center(child: CircularProgressIndicator()),
          );
        }

        if (widget.viewModel.status == TenantDetailStatus.error ||
            tenant == null) {
          return Scaffold(
            appBar: AppBar(
              title: const Text('Cliente'),
              leading: TenantBackButton(fallback: widget.backFallback),
            ),
            body: Center(
              child: Padding(
                padding: const EdgeInsets.all(AppSpacing.lg),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      widget.viewModel.errorMessage ??
                          'Não foi possível carregar o cliente.',
                      style: Theme.of(context).textTheme.titleMedium,
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: AppSpacing.md),
                    FilledButton.tonal(
                      onPressed: widget.viewModel.load,
                      child: const Text('Tentar novamente'),
                    ),
                  ],
                ),
              ),
            ),
          );
        }

        return DefaultTabController(
          length: 3,
          child: Scaffold(
            appBar: AppBar(
              title: Text(tenant.displayName),
              leading: TenantBackButton(fallback: widget.backFallback),
              actions: [
                TenantPermissionGuard(
                  resource: TenantResources.tenant,
                  scope: TenantScopes.suspend,
                  child: PopupMenuButton<String>(
                    onSelected: (value) {
                      if (value == 'suspend') {
                        _suspend(tenant);
                      } else {
                        widget.viewModel.reactivate();
                      }
                    },
                    itemBuilder: (_) => [
                      if (tenant.isSuspended)
                        const PopupMenuItem(
                          value: 'reactivate',
                          child: Text('Reativar cliente'),
                        )
                      else
                        const PopupMenuItem(
                          value: 'suspend',
                          child: Text('Suspender cliente'),
                        ),
                    ],
                  ),
                ),
              ],
              bottom: const TabBar(
                tabs: [
                  Tab(text: 'Cadastro'),
                  Tab(text: 'Acessos'),
                  Tab(text: 'Produtos'),
                ],
              ),
            ),
            body: SafeArea(
              child: Center(
                child: ConstrainedBox(
                  constraints: const BoxConstraints(
                    maxWidth: AppBreakpoints.desktop,
                  ),
                  child: Column(
                    children: [
                      _Header(tenant: tenant),
                      Expanded(
                        child: TabBarView(
                          children: [
                            ListView(
                              padding: const EdgeInsets.all(AppSpacing.md),
                              children: [
                                IdentificationSection(
                                  viewModel: widget.viewModel,
                                ),
                                const SizedBox(height: AppSpacing.md),
                                ContactSection(viewModel: widget.viewModel),
                                const SizedBox(height: AppSpacing.md),
                                AddressSection(viewModel: widget.viewModel),
                              ],
                            ),
                            AccessTab(viewModel: widget.viewModel),
                            ProductsTab(viewModel: widget.viewModel),
                          ],
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

class _Header extends StatelessWidget {
  const _Header({required this.tenant});

  final Tenant tenant;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final cs = theme.colorScheme;

    return Padding(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.md,
        AppSpacing.md,
        AppSpacing.md,
        0,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Wrap(
            spacing: AppSpacing.sm,
            runSpacing: AppSpacing.xs,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              Text(
                tenant.formattedTaxId,
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: cs.onSurfaceVariant,
                ),
              ),
              _Badge(label: TenantKinds.label(tenant.kind)),
              _Badge(
                label: TenantStatuses.label(tenant.status),
                background: tenant.isSuspended ? cs.errorContainer : null,
                foreground: tenant.isSuspended ? cs.onErrorContainer : null,
              ),
              _Badge(
                label: ProvisioningStatuses.label(tenant.accessProvisioning),
                background:
                    tenant.hasFailedProvisioning ? cs.errorContainer : null,
                foreground:
                    tenant.hasFailedProvisioning ? cs.onErrorContainer : null,
              ),
            ],
          ),
          if (tenant.isSuspended)
            Padding(
              padding: const EdgeInsets.only(top: AppSpacing.sm),
              child: Card.filled(
                color: cs.errorContainer,
                child: Padding(
                  padding: const EdgeInsets.all(AppSpacing.md),
                  child: Row(
                    children: [
                      Icon(Symbols.lock, color: cs.onErrorContainer),
                      const SizedBox(width: AppSpacing.md),
                      Expanded(
                        child: Text(
                          tenant.suspensionReason.isEmpty
                              ? 'Cliente suspenso — alterações bloqueadas.'
                              : 'Cliente suspenso — alterações bloqueadas. '
                                  'Motivo: ${tenant.suspensionReason}',
                          style: theme.textTheme.bodyMedium?.copyWith(
                            color: cs.onErrorContainer,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
        ],
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
