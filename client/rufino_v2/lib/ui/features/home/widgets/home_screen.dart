import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:tenant_management/tenant_management.dart';

import 'package:rufino_core/rufino_core.dart';
import '../viewmodel/home_viewmodel.dart';
import 'package:people_management/people_management.dart';

/// The hub the app opens on: the features of the products the selected
/// customer has enabled, and that this person is allowed to use.
///
/// Every entry passes **two** gates. `ProductGuard` answers "is this product
/// on for this customer?", `ModuleGuard` answers "is this person allowed?".
/// A group whose entries are all hidden does not render its header — a title
/// with nothing under it is worse than no title.
class HomeScreen extends StatefulWidget {
  /// Creates the screen.
  const HomeScreen({super.key, required this.viewModel});

  /// Drives the screen.
  final HomeViewModel viewModel;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  Future<void> _onLogout() async {
    final router = GoRouter.of(context);
    await widget.viewModel.logout();
    router.go('/login');
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        return Scaffold(
          appBar: AppBar(
            title: _AppBarTitle(viewModel: widget.viewModel),
            actions: [
              _UserMenu(
                onChangeTenant: () => context.go(TenantRoutes.select),
                onEditCompany: () {
                  final id = widget.viewModel.tenant?.id;
                  if (id != null) context.go('/company/edit/$id');
                },
                canEditCompany: widget.viewModel.isPeopleManagementReady,
                onToggleTheme: () => context.read<ThemeNotifier>().toggle(),
                isDark: context.watch<ThemeNotifier>().isDark,
                onRefreshPermissions: widget.viewModel.refreshPermissions,
                onLogout: _onLogout,
              ),
            ],
          ),
          body: _HomeBody(viewModel: widget.viewModel),
        );
      },
    );
  }
}

class _AppBarTitle extends StatelessWidget {
  const _AppBarTitle({required this.viewModel});

  final HomeViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final tenant = viewModel.tenant;

    return Row(
      children: [
        CircleAvatar(
          backgroundColor: theme.colorScheme.primaryContainer,
          child: Icon(
            tenant?.isIndividual == true ? Icons.person : Icons.business,
            color: theme.colorScheme.onPrimaryContainer,
          ),
        ),
        const SizedBox(width: AppSpacing.sm),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                viewModel.tenantDisplayName,
                overflow: TextOverflow.ellipsis,
              ),
              if (viewModel.tenantSubtitle.isNotEmpty)
                Text(
                  viewModel.tenantSubtitle,
                  style: theme.textTheme.labelSmall?.copyWith(
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                  overflow: TextOverflow.ellipsis,
                ),
            ],
          ),
        ),
      ],
    );
  }
}

class _UserMenu extends StatelessWidget {
  const _UserMenu({
    required this.onChangeTenant,
    required this.onEditCompany,
    required this.canEditCompany,
    required this.onToggleTheme,
    required this.isDark,
    required this.onRefreshPermissions,
    required this.onLogout,
  });

  final VoidCallback onChangeTenant;
  final VoidCallback onEditCompany;
  final bool canEditCompany;
  final VoidCallback onToggleTheme;
  final bool isDark;
  final VoidCallback onRefreshPermissions;
  final VoidCallback onLogout;

  @override
  Widget build(BuildContext context) {
    return PopupMenuButton<String>(
      icon: Row(
        children: [
          CircleAvatar(
            backgroundColor: Theme.of(context).colorScheme.secondaryContainer,
            child: Icon(
              Icons.person,
              color: Theme.of(context).colorScheme.onSecondaryContainer,
            ),
          ),
          const Icon(Icons.arrow_drop_down),
        ],
      ),
      onSelected: (value) {
        switch (value) {
          case 'change-tenant':
            onChangeTenant();
          case 'edit-company':
            onEditCompany();
          case 'toggle-theme':
            onToggleTheme();
          case 'refresh-permissions':
            onRefreshPermissions();
          case 'logout':
            onLogout();
        }
      },
      itemBuilder: (ctx) => [
        const PopupMenuItem(
          value: 'change-tenant',
          child: Text('Trocar cliente'),
        ),
        if (canEditCompany &&
            ctx.read<PermissionNotifier>().hasPermission('company', 'edit'))
          const PopupMenuItem(
            value: 'edit-company',
            child: Text('Editar dados da empresa'),
          ),
        const PopupMenuDivider(),
        PopupMenuItem(
          value: 'toggle-theme',
          child: Row(
            children: [
              Icon(
                isDark ? Icons.light_mode_outlined : Icons.dark_mode_outlined,
              ),
              const SizedBox(width: AppSpacing.sm),
              Text(isDark ? 'Modo Claro' : 'Modo Escuro'),
            ],
          ),
        ),
        const PopupMenuDivider(),
        const PopupMenuItem(
          value: 'refresh-permissions',
          child: Row(
            children: [
              Icon(Icons.sync, size: 20),
              SizedBox(width: AppSpacing.sm),
              Text('Atualizar Permissões'),
            ],
          ),
        ),
        const PopupMenuDivider(),
        const PopupMenuItem(value: 'logout', child: Text('Sair')),
      ],
    );
  }
}





const _toolEntries = <HomeEntry>[
  HomeEntry(
    icon: Icons.bug_report_outlined,
    label: 'Debug',
    route: '/debug',
    resource: PeopleManagementResources.debug,
  ),
];

class _HomeBody extends StatelessWidget {
  const _HomeBody({required this.viewModel});

  final HomeViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    final permissions = context.watch<PermissionNotifier>();

    if (permissions.status == PermissionStatus.loading) {
      return const Center(child: CircularProgressIndicator());
    }

    // Cada módulo responde pelas suas duas porteiras — produto habilitado no
    // tenant e permissão da pessoa —, porque só ele sabe qual produto e qual
    // notifier são os seus. A casca só pergunta (D6).
    final modules = context.read<List<AppModule>>();
    final groups = [
      for (final module in modules)
        (title: module.menuTitle, entries: module.visibleEntries(context)),
    ];

    // A entrada de diagnóstico é da casca: ela fala do app, não de um produto.
    final tools = _toolEntries
        .where((e) => permissions.hasAnyScope(e.resource))
        .toList();

    return LayoutBuilder(
      builder: (context, constraints) {
        final isWide = constraints.maxWidth >= AppBreakpoints.tablet;
        return SingleChildScrollView(
          padding: EdgeInsets.all(isWide ? AppSpacing.lg : AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (viewModel.isPeopleManagementPending)
                const Padding(
                  padding: EdgeInsets.only(bottom: AppSpacing.md),
                  child: _PendingProductNotice(),
                ),
              for (final group in groups)
                _Group(title: group.title, entries: group.entries),
              _Group(title: 'FERRAMENTAS', entries: tools),
              if (groups.every((g) => g.entries.isEmpty) && tools.isEmpty)
                const _NothingAvailable(),
            ],
          ),
        );
      },
    );
  }
}

class _Group extends StatelessWidget {
  const _Group({required this.title, required this.entries});

  final String title;
  final Iterable<HomeEntry> entries;

  @override
  Widget build(BuildContext context) {
    if (entries.isEmpty) return const SizedBox.shrink();

    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: Theme.of(context).textTheme.labelSmall?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                  letterSpacing: 0.8,
                ),
          ),
          const SizedBox(height: AppSpacing.sm),
          Wrap(
            spacing: AppSpacing.md,
            runSpacing: AppSpacing.md,
            children: [
              for (final entry in entries)
                _MenuCard(
                  icon: entry.icon,
                  label: entry.label,
                  onTap: () => context.go(entry.route),
                ),
            ],
          ),
        ],
      ),
    );
  }
}

/// Shown when the tenant has People Management enabled but no company record
/// answers for it — the migration made visible, instead of a 403 later on.
class _PendingProductNotice extends StatelessWidget {
  const _PendingProductNotice();

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Card.filled(
      color: cs.secondaryContainer,
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Row(
          children: [
            Icon(Icons.info_outline, color: cs.onSecondaryContainer),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: Text(
                'Gestão de Pessoas ainda não está liberada para este cliente.',
                style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                      color: cs.onSecondaryContainer,
                    ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _NothingAvailable extends StatelessWidget {
  const _NothingAvailable();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.xl),
      child: Column(
        children: [
          Icon(
            Icons.inbox_outlined,
            size: 56,
            color: theme.colorScheme.outline,
          ),
          const SizedBox(height: AppSpacing.md),
          Text(
            'Nenhuma funcionalidade disponível para este cliente.',
            style: theme.textTheme.titleMedium,
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: AppSpacing.sm),
          Text(
            'Fale com o responsável pela conta para liberar um produto ou '
            'conceder permissões.',
            style: theme.textTheme.bodyMedium?.copyWith(
              color: theme.colorScheme.onSurfaceVariant,
            ),
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}

class _MenuCard extends StatelessWidget {
  const _MenuCard({
    required this.icon,
    required this.label,
    required this.onTap,
  });

  final IconData icon;
  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: SizedBox(
          width: 160,
          height: 120,
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(
                icon,
                size: 40,
                color: Theme.of(context).colorScheme.primary,
              ),
              const SizedBox(height: AppSpacing.sm),
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: AppSpacing.sm),
                child: Text(
                  label,
                  style: Theme.of(context).textTheme.labelLarge,
                  textAlign: TextAlign.center,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
