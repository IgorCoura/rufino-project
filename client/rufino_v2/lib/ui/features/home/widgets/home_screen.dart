import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../../core/theme/app_breakpoints.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/theme/theme_notifier.dart';
import '../../../core/widgets/permission_guard.dart';
import '../../auth/viewmodel/permission_notifier.dart';
import '../viewmodel/home_viewmodel.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key, required this.viewModel});

  final HomeViewModel viewModel;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  @override
  void initState() {
    super.initState();
    widget.viewModel.addListener(_onStatusChanged);
    widget.viewModel.loadCompany();
  }

  @override
  void didUpdateWidget(covariant HomeScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.viewModel != widget.viewModel) {
      oldWidget.viewModel.removeListener(_onStatusChanged);
      widget.viewModel.addListener(_onStatusChanged);
      widget.viewModel.loadCompany();
    }
  }

  @override
  void dispose() {
    widget.viewModel.removeListener(_onStatusChanged);
    super.dispose();
  }

  void _onStatusChanged() {
    if (!mounted) return;
    if (widget.viewModel.status == HomeStatus.error) {
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            content: Text(widget.viewModel.errorMessage ?? 'Erro ao carregar.'),
            behavior: SnackBarBehavior.floating,
          ),
        );
    }
  }

  Future<void> _onLogout() async {
    await widget.viewModel.logout();
    if (mounted) context.go('/login');
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
                onChangeCompany: () => context.go('/company'),
                onEditCompany: () {
                  final id = widget.viewModel.company?.id;
                  if (id != null) context.go('/company/edit/$id');
                },
                onToggleTheme: () => context.read<ThemeNotifier>().toggle(),
                isDark: context.watch<ThemeNotifier>().isDark,
                onRefreshPermissions: () =>
                    context.read<PermissionNotifier>().loadPermissions(),
                onLogout: _onLogout,
              ),
            ],
          ),
          body: widget.viewModel.isLoading
              ? const Center(child: CircularProgressIndicator())
              : const _HomeBody(),
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
    return Row(
      children: [
        CircleAvatar(
          backgroundColor: Theme.of(context).colorScheme.primaryContainer,
          child: Icon(
            Icons.business,
            color: Theme.of(context).colorScheme.onPrimaryContainer,
          ),
        ),
        const SizedBox(width: AppSpacing.sm),
        Expanded(
          child: Text(
            viewModel.companyDisplayName,
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ],
    );
  }
}

class _UserMenu extends StatelessWidget {
  const _UserMenu({
    required this.onChangeCompany,
    required this.onEditCompany,
    required this.onToggleTheme,
    required this.isDark,
    required this.onRefreshPermissions,
    required this.onLogout,
  });

  final VoidCallback onChangeCompany;
  final VoidCallback onEditCompany;
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
          case 'change-company':
            onChangeCompany();
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
            value: 'change-company', child: Text('Alterar Empresa')),
        if (ctx.read<PermissionNotifier>().hasPermission('company', 'edit'))
          const PopupMenuItem(
              value: 'edit-company', child: Text('Editar Empresa')),
        const PopupMenuDivider(),
        PopupMenuItem(
          value: 'toggle-theme',
          child: Row(
            children: [
              Icon(isDark
                  ? Icons.light_mode_outlined
                  : Icons.dark_mode_outlined),
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

class _HomeBody extends StatelessWidget {
  const _HomeBody();

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final isWide = constraints.maxWidth >= AppBreakpoints.tablet;
        return Padding(
          padding: EdgeInsets.all(isWide ? AppSpacing.lg : AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Menu',
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              const SizedBox(height: AppSpacing.md),
              Wrap(
                spacing: AppSpacing.md,
                runSpacing: AppSpacing.md,
                children: [
                  ModuleGuard(
                    resource: 'employee',
                    child: _MenuCard(
                      icon: Icons.people_outline,
                      label: 'Funcionários',
                      onTap: () => context.go('/employee'),
                    ),
                  ),
                  ModuleGuard(
                    resource: 'workplace',
                    child: _MenuCard(
                      icon: Icons.location_on_outlined,
                      label: 'Locais de Trabalho',
                      onTap: () => context.go('/workplace'),
                    ),
                  ),
                  ModuleGuard(
                    resource: 'department',
                    child: _MenuCard(
                      icon: Icons.apartment_outlined,
                      label: 'Setores',
                      onTap: () => context.go('/department'),
                    ),
                  ),
                  ModuleGuard(
                    resource: 'document-group',
                    child: _MenuCard(
                      icon: Icons.folder_outlined,
                      label: 'Grupos de Template de Documentos',
                      onTap: () => context.go('/document-group'),
                    ),
                  ),
                  ModuleGuard(
                    resource: 'require-documents',
                    child: _MenuCard(
                      icon: Icons.description_outlined,
                      label: 'Requerimentos de Documentos',
                      onTap: () => context.go('/require-document'),
                    ),
                  ),
                ],
              ),
            ],
          ),
        );
      },
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
