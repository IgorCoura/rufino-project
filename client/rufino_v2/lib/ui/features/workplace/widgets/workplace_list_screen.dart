import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_spacing.dart';
import '../../../../domain/entities/workplace.dart';
import '../../../core/widgets/permission_guard.dart';
import '../viewmodel/workplace_list_viewmodel.dart';

/// Displays the list of workplaces for the currently selected company.
///
/// Uses [ListenableBuilder] to react to [WorkplaceListViewModel] state changes.
/// Navigation to create/edit screens is delegated to [GoRouter].
class WorkplaceListScreen extends StatefulWidget {
  const WorkplaceListScreen({super.key, required this.viewModel});

  final WorkplaceListViewModel viewModel;

  @override
  State<WorkplaceListScreen> createState() => _WorkplaceListScreenState();
}

class _WorkplaceListScreenState extends State<WorkplaceListScreen> {
  @override
  void initState() {
    super.initState();
    widget.viewModel.loadWorkplaces();
  }

  @override
  void didUpdateWidget(WorkplaceListScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.viewModel != widget.viewModel) {
      widget.viewModel.loadWorkplaces();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          tooltip: 'Voltar para início',
          onPressed: () => context.go('/home'),
        ),
        title: const Text('Locais de Trabalho'),
      ),
      body: ListenableBuilder(
        listenable: widget.viewModel,
        builder: (context, _) {
          if (widget.viewModel.isLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          if (widget.viewModel.hasError) {
            return _ErrorState(
              message: widget.viewModel.errorMessage ??
                  'Erro ao carregar locais de trabalho.',
              onRetry: widget.viewModel.loadWorkplaces,
            );
          }

          if (widget.viewModel.workplaces.isEmpty) {
            return const Center(
              child: Text('Nenhum local de trabalho cadastrado.'),
            );
          }

          return RefreshIndicator(
            onRefresh: widget.viewModel.loadWorkplaces,
            child: ListView.separated(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.md, AppSpacing.md, AppSpacing.md, AppSpacing.md + 80,
              ),
              itemCount: widget.viewModel.workplaces.length,
              separatorBuilder: (_, __) =>
                  const SizedBox(height: AppSpacing.sm),
              itemBuilder: (context, index) {
                final workplace = widget.viewModel.workplaces[index];
                return _WorkplaceTile(
                  workplace: workplace,
                  onEdit: () => context
                      .push('/workplace/edit/${workplace.id}')
                      .then((_) => widget.viewModel.loadWorkplaces()),
                );
              },
            ),
          );
        },
      ),
      floatingActionButton: PermissionGuard(
        resource: 'workplace',
        scope: 'create',
        child: FloatingActionButton(
          onPressed: () => context
              .push('/workplace/create')
              .then((_) => widget.viewModel.loadWorkplaces()),
          tooltip: 'Adicionar local de trabalho',
          child: const Icon(Icons.add),
        ),
      ),
    );
  }
}

class _WorkplaceTile extends StatelessWidget {
  const _WorkplaceTile({
    required this.workplace,
    required this.onEdit,
  });

  final Workplace workplace;
  final VoidCallback onEdit;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        leading: const Icon(Icons.business_outlined),
        title: Text(
          workplace.name,
          style: Theme.of(context).textTheme.titleSmall,
        ),
        subtitle: Text(
          workplace.address.minimal,
          style: Theme.of(context).textTheme.bodySmall,
        ),
        trailing: Semantics(
          label: 'Editar local de trabalho ${workplace.name}',
          button: true,
          child: IconButton(
            icon: const Icon(Icons.edit_outlined),
            onPressed: onEdit,
          ),
        ),
      ),
    );
  }
}

class _ErrorState extends StatelessWidget {
  const _ErrorState({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(message, style: Theme.of(context).textTheme.bodyLarge),
          const SizedBox(height: AppSpacing.md),
          FilledButton.tonal(
            onPressed: onRetry,
            child: const Text('Tentar novamente'),
          ),
        ],
      ),
    );
  }
}
