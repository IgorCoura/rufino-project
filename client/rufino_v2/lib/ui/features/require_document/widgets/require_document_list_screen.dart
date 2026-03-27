import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_spacing.dart';
import '../../../../domain/entities/require_document.dart';
import '../../../core/widgets/permission_guard.dart';
import '../viewmodel/require_document_list_viewmodel.dart';

/// Displays the list of require documents for the currently selected company.
///
/// Uses [ListenableBuilder] to react to [RequireDocumentListViewModel] state
/// changes. Navigation to create/edit screens is delegated to [GoRouter].
class RequireDocumentListScreen extends StatefulWidget {
  const RequireDocumentListScreen({super.key, required this.viewModel});

  /// The view model that drives this screen.
  final RequireDocumentListViewModel viewModel;

  @override
  State<RequireDocumentListScreen> createState() =>
      _RequireDocumentListScreenState();
}

class _RequireDocumentListScreenState extends State<RequireDocumentListScreen> {
  @override
  void initState() {
    super.initState();
    widget.viewModel.loadRequireDocuments();
  }

  @override
  void didUpdateWidget(RequireDocumentListScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.viewModel != widget.viewModel) {
      widget.viewModel.loadRequireDocuments();
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
        title: const Text('Requerimentos de Documentos'),
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
                  'Erro ao carregar requerimentos.',
              onRetry: widget.viewModel.loadRequireDocuments,
            );
          }

          if (widget.viewModel.requireDocuments.isEmpty) {
            return const Center(
              child: Text('Nenhum requerimento de documento cadastrado.'),
            );
          }

          return RefreshIndicator(
            onRefresh: widget.viewModel.loadRequireDocuments,
            child: ListView.builder(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.md,
                AppSpacing.md,
                AppSpacing.md,
                AppSpacing.md + 80,
              ),
              itemCount: widget.viewModel.requireDocuments.length,
              itemBuilder: (context, index) {
                final item = widget.viewModel.requireDocuments[index];
                return _RequireDocumentTile(
                  requireDocument: item,
                  onTap: () => context
                      .push('/require-document/edit/${item.id}')
                      .then((_) => widget.viewModel.loadRequireDocuments()),
                );
              },
            ),
          );
        },
      ),
      floatingActionButton: PermissionGuard(
        resource: 'require-documents',
        scope: 'create',
        child: FloatingActionButton(
          onPressed: () => context
              .push('/require-document/create')
              .then((_) => widget.viewModel.loadRequireDocuments()),
          tooltip: 'Adicionar requerimento',
          child: const Icon(Icons.add),
        ),
      ),
    );
  }
}

/// A single require document card in the list.
class _RequireDocumentTile extends StatelessWidget {
  const _RequireDocumentTile({
    required this.requireDocument,
    required this.onTap,
  });

  final RequireDocument requireDocument;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    return Card.outlined(
      margin: const EdgeInsets.only(bottom: AppSpacing.md),
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
                  Icons.description_outlined,
                  color: cs.onPrimaryContainer,
                ),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      requireDocument.name,
                      style: Theme.of(context).textTheme.titleMedium,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    if (requireDocument.description.isNotEmpty) ...[
                      const SizedBox(height: AppSpacing.xs),
                      Text(
                        requireDocument.description,
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
