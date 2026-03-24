import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_spacing.dart';
import '../../../../domain/entities/document_group.dart';
import '../viewmodel/document_group_list_viewmodel.dart';

/// Displays the list of document groups for the currently selected company.
///
/// Uses [ListenableBuilder] to react to [DocumentGroupListViewModel] state changes.
/// Navigation to create/edit screens is delegated to [GoRouter].
class DocumentGroupListScreen extends StatefulWidget {
  const DocumentGroupListScreen({super.key, required this.viewModel});

  /// The view model that drives this screen.
  final DocumentGroupListViewModel viewModel;

  @override
  State<DocumentGroupListScreen> createState() =>
      _DocumentGroupListScreenState();
}

class _DocumentGroupListScreenState extends State<DocumentGroupListScreen> {
  @override
  void initState() {
    super.initState();
    widget.viewModel.loadGroups();
  }

  @override
  void didUpdateWidget(DocumentGroupListScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.viewModel != widget.viewModel) {
      widget.viewModel.loadGroups();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          tooltip: 'Voltar',
          onPressed: () => context.go('/home'),
        ),
        title: const Text('Grupos de Documentos'),
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
                  'Erro ao carregar grupos.',
              onRetry: widget.viewModel.loadGroups,
            );
          }

          if (widget.viewModel.groups.isEmpty) {
            return const _EmptyState();
          }

          return RefreshIndicator(
            onRefresh: widget.viewModel.loadGroups,
            child: ListView.separated(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.md,
                AppSpacing.md,
                AppSpacing.md,
                AppSpacing.md + 80,
              ),
              itemCount: widget.viewModel.groups.length,
              separatorBuilder: (_, __) =>
                  const SizedBox(height: AppSpacing.sm),
              itemBuilder: (context, index) {
                final group = widget.viewModel.groups[index];
                return _DocumentGroupTile(
                  group: group,
                  onTap: () => context
                      .push('/document-group/edit/${group.id}')
                      .then((_) => widget.viewModel.loadGroups()),
                );
              },
            ),
          );
        },
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => context
            .push('/document-group/create')
            .then((_) => widget.viewModel.loadGroups()),
        tooltip: 'Adicionar grupo',
        child: const Icon(Icons.add),
      ),
    );
  }
}

/// A single document group card in the list.
class _DocumentGroupTile extends StatelessWidget {
  const _DocumentGroupTile({
    required this.group,
    required this.onTap,
  });

  final DocumentGroup group;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    return Card.outlined(
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.md),
          child: Row(
            children: [
              CircleAvatar(
                backgroundColor: cs.primaryContainer,
                child: Icon(
                  Icons.folder_outlined,
                  color: cs.onPrimaryContainer,
                ),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      group.name,
                      style: Theme.of(context).textTheme.titleMedium,
                    ),
                    if (group.description.isNotEmpty) ...[
                      const SizedBox(height: AppSpacing.xs),
                      Text(
                        group.description,
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(
                              color: cs.onSurfaceVariant,
                            ),
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ],
                  ],
                ),
              ),
              Icon(
                Icons.chevron_right,
                color: cs.onSurfaceVariant,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Empty state shown when no document groups exist.
class _EmptyState extends StatelessWidget {
  const _EmptyState();

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.xl),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              Icons.folder_off_outlined,
              size: 64,
              color: cs.onSurfaceVariant.withOpacity(0.5),
            ),
            const SizedBox(height: AppSpacing.md),
            Text(
              'Nenhum grupo cadastrado',
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    color: cs.onSurfaceVariant,
                  ),
            ),
            const SizedBox(height: AppSpacing.sm),
            Text(
              'Toque no botão + para criar um novo grupo de documentos.',
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: cs.onSurfaceVariant.withOpacity(0.7),
                  ),
              textAlign: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }
}

/// Error state with a retry button.
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
