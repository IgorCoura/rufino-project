import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_spacing.dart';
import '../../../../domain/entities/document_group_with_templates.dart';
import '../viewmodel/document_group_with_templates_viewmodel.dart';

/// Displays document groups as expandable cards that reveal their templates.
///
/// Unifies the former separate document group and document template list
/// screens into a single view. Uses [ListenableBuilder] to react to
/// [DocumentGroupWithTemplatesViewModel] state changes.
class DocumentGroupWithTemplatesScreen extends StatefulWidget {
  const DocumentGroupWithTemplatesScreen({
    super.key,
    required this.viewModel,
  });

  /// The view model that drives this screen.
  final DocumentGroupWithTemplatesViewModel viewModel;

  @override
  State<DocumentGroupWithTemplatesScreen> createState() =>
      _DocumentGroupWithTemplatesScreenState();
}

class _DocumentGroupWithTemplatesScreenState
    extends State<DocumentGroupWithTemplatesScreen> {
  @override
  void initState() {
    super.initState();
    widget.viewModel.loadGroups();
  }

  @override
  void didUpdateWidget(DocumentGroupWithTemplatesScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.viewModel != widget.viewModel) {
      widget.viewModel.loadGroups();
    }
  }

  void _reload() => widget.viewModel.loadGroups();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          tooltip: 'Voltar',
          onPressed: () => context.go('/home'),
        ),
        title: const Text('Documentos'),
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
                  'Erro ao carregar documentos.',
              onRetry: _reload,
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
                return _DocumentGroupCard(
                  group: group,
                  onEditGroup: () => context
                      .push('/document-group/edit/${group.id}')
                      .then((_) => _reload()),
                  onTapTemplate: (templateId) => context
                      .push('/document-template/edit/$templateId')
                      .then((_) => _reload()),
                  onCreateTemplate: () => context
                      .push('/document-template/create')
                      .then((_) => _reload()),
                );
              },
            ),
          );
        },
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () =>
            context.push('/document-group/create').then((_) => _reload()),
        tooltip: 'Adicionar grupo',
        child: const Icon(Icons.add),
      ),
    );
  }
}

/// An expandable card showing a document group with its templates.
class _DocumentGroupCard extends StatelessWidget {
  const _DocumentGroupCard({
    required this.group,
    required this.onEditGroup,
    required this.onTapTemplate,
    required this.onCreateTemplate,
  });

  final DocumentGroupWithTemplates group;
  final VoidCallback onEditGroup;
  final void Function(String templateId) onTapTemplate;
  final VoidCallback onCreateTemplate;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return Card.outlined(
      clipBehavior: Clip.antiAlias,
      child: ExpansionTile(
        leading: CircleAvatar(
          backgroundColor: cs.primaryContainer,
          child: Icon(
            Icons.folder_outlined,
            color: cs.onPrimaryContainer,
          ),
        ),
        title: Text(group.name, style: textTheme.titleMedium),
        subtitle: group.description.isNotEmpty
            ? Text(
                group.description,
                style: textTheme.bodySmall?.copyWith(
                  color: cs.onSurfaceVariant,
                ),
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              )
            : null,
        trailing: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              '${group.templates.length}',
              style: textTheme.labelMedium?.copyWith(
                color: cs.onSurfaceVariant,
              ),
            ),
            const SizedBox(width: AppSpacing.xs),
            Icon(
              Icons.description_outlined,
              size: 16,
              color: cs.onSurfaceVariant,
            ),
          ],
        ),
        children: [
          const Divider(height: 1),
          _GroupActions(
            onEdit: onEditGroup,
            onCreateTemplate: onCreateTemplate,
          ),
          if (group.templates.isEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(
                horizontal: AppSpacing.md,
                vertical: AppSpacing.md,
              ),
              child: Text(
                'Nenhum template neste grupo.',
                style: textTheme.bodySmall?.copyWith(
                  color: cs.onSurfaceVariant,
                ),
              ),
            )
          else
            ...group.templates.map(
              (template) => _TemplateTile(
                template: template,
                onTap: () => onTapTemplate(template.id),
              ),
            ),
        ],
      ),
    );
  }
}

/// Action buttons below the group header inside the expansion.
class _GroupActions extends StatelessWidget {
  const _GroupActions({
    required this.onEdit,
    required this.onCreateTemplate,
  });

  final VoidCallback onEdit;
  final VoidCallback onCreateTemplate;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: AppSpacing.xs,
      ),
      child: Row(
        children: [
          TextButton.icon(
            onPressed: onEdit,
            icon: const Icon(Icons.edit_outlined, size: 18),
            label: const Text('Editar Grupo'),
          ),
          const SizedBox(width: AppSpacing.sm),
          TextButton.icon(
            onPressed: onCreateTemplate,
            icon: const Icon(Icons.add, size: 18),
            label: const Text('Novo Template'),
          ),
        ],
      ),
    );
  }
}

/// A single template row inside an expanded group.
class _TemplateTile extends StatelessWidget {
  const _TemplateTile({
    required this.template,
    required this.onTap,
  });

  final DocumentTemplateSummary template;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.md,
          vertical: AppSpacing.sm,
        ),
        child: Row(
          children: [
            Container(
              width: 32,
              height: 32,
              decoration: BoxDecoration(
                color: cs.secondaryContainer,
                borderRadius: BorderRadius.circular(6),
              ),
              child: Icon(
                Icons.description_outlined,
                size: 18,
                color: cs.onSecondaryContainer,
              ),
            ),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(template.name, style: textTheme.bodyMedium),
                  if (template.description.isNotEmpty)
                    Text(
                      template.description,
                      style: textTheme.bodySmall?.copyWith(
                        color: cs.onSurfaceVariant,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                ],
              ),
            ),
            Icon(
              Icons.chevron_right,
              size: 20,
              color: cs.onSurfaceVariant,
            ),
          ],
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
              color: cs.onSurfaceVariant.withValues(alpha: 0.5),
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
                    color: cs.onSurfaceVariant.withValues(alpha: 0.7),
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
    final theme = Theme.of(context);

    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            Icons.error_outline,
            size: 48,
            color: theme.colorScheme.error,
          ),
          const SizedBox(height: AppSpacing.md),
          Text(
            message,
            style: theme.textTheme.bodyLarge,
            textAlign: TextAlign.center,
          ),
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
