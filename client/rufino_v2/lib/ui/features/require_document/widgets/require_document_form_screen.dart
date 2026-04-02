import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_spacing.dart';
import '../../../core/widgets/error_dialog.dart';
import '../viewmodel/require_document_form_viewmodel.dart';

/// Form screen for creating or editing a require document.
///
/// When [requireDocumentId] is provided,
/// [RequireDocumentFormViewModel.loadRequireDocument] is called on init to
/// populate the fields from the API.
class RequireDocumentFormScreen extends StatefulWidget {
  const RequireDocumentFormScreen({
    super.key,
    required this.viewModel,
    this.requireDocumentId,
  });

  /// The view model that manages form state and validation.
  final RequireDocumentFormViewModel viewModel;

  /// The id of the require document to edit. Null when creating a new one.
  final String? requireDocumentId;

  @override
  State<RequireDocumentFormScreen> createState() =>
      _RequireDocumentFormScreenState();
}

class _RequireDocumentFormScreenState extends State<RequireDocumentFormScreen> {
  final _formKey = GlobalKey<FormState>();

  @override
  void initState() {
    super.initState();
    widget.viewModel.addListener(_onViewModelChanged);
    if (widget.requireDocumentId != null) {
      widget.viewModel.loadRequireDocument(widget.requireDocumentId!);
    } else {
      widget.viewModel.loadOptions();
    }
  }

  @override
  void didUpdateWidget(covariant RequireDocumentFormScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.viewModel != widget.viewModel) {
      oldWidget.viewModel.removeListener(_onViewModelChanged);
      widget.viewModel.addListener(_onViewModelChanged);
      if (widget.requireDocumentId != null) {
        widget.viewModel.loadRequireDocument(widget.requireDocumentId!);
      } else {
        widget.viewModel.loadOptions();
      }
    }
  }

  @override
  void dispose() {
    widget.viewModel.removeListener(_onViewModelChanged);
    super.dispose();
  }

  void _onViewModelChanged() {
    if (!mounted) return;
    switch (widget.viewModel.status) {
      case RequireDocumentFormStatus.saved:
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Requerimento salvo com sucesso!'),
            behavior: SnackBarBehavior.floating,
          ),
        );
        context.pop();
      case RequireDocumentFormStatus.error:
        showErrorSnackBar(
          context,
          messages: widget.viewModel.serverErrors,
          fallbackMessage: widget.viewModel.errorMessage ?? 'Erro ao salvar.',
        );
      default:
        break;
    }
  }

  void _onSave() {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    widget.viewModel.save();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) => Text(
            widget.viewModel.isNew
                ? 'Criar Requerimento'
                : 'Editar Requerimento',
          ),
        ),
      ),
      body: ListenableBuilder(
        listenable: widget.viewModel,
        builder: (context, _) {
          if (widget.viewModel.isLoading) {
            return const Center(child: CircularProgressIndicator());
          }
          return _RequireDocumentFormBody(
            viewModel: widget.viewModel,
            formKey: _formKey,
            onSave: _onSave,
            onCancel: () => context.pop(),
          );
        },
      ),
    );
  }
}

/// The scrollable form body containing all require document fields.
class _RequireDocumentFormBody extends StatelessWidget {
  const _RequireDocumentFormBody({
    required this.viewModel,
    required this.formKey,
    required this.onSave,
    required this.onCancel,
  });

  final RequireDocumentFormViewModel viewModel;
  final GlobalKey<FormState> formKey;
  final VoidCallback onSave;
  final VoidCallback onCancel;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 800),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // ─── Form with validated fields ──────────────────────────
              Form(
                key: formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text(
                      'Informações do Requerimento',
                      style: Theme.of(context).textTheme.titleLarge,
                    ),
                    const SizedBox(height: AppSpacing.md),
                    TextFormField(
                      controller: viewModel.nameController,
                      decoration: const InputDecoration(
                        labelText: 'Nome',
                        border: OutlineInputBorder(),
                        prefixIcon: Icon(Icons.text_fields),
                        helperText: 'Máx. 100 caracteres',
                      ),
                      maxLength: 100,
                      validator: viewModel.validateName,
                    ),
                    const SizedBox(height: AppSpacing.md),
                    TextFormField(
                      controller: viewModel.descriptionController,
                      decoration: const InputDecoration(
                        labelText: 'Descrição',
                        border: OutlineInputBorder(),
                        prefixIcon: Icon(Icons.notes),
                        helperText: 'Máx. 500 caracteres',
                      ),
                      maxLines: 3,
                      maxLength: 500,
                      validator: viewModel.validateDescription,
                    ),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.lg),

              // ─── Association ─────────────────────────────────────────
              Text(
                'Associação',
                style: Theme.of(context).textTheme.titleLarge,
              ),
              const SizedBox(height: AppSpacing.md),
              DropdownButtonFormField<String>(
                initialValue: viewModel.selectedAssociationTypeId.isNotEmpty
                    ? viewModel.selectedAssociationTypeId
                    : null,
                decoration: const InputDecoration(
                  labelText: 'Tipo de Associação',
                  border: OutlineInputBorder(),
                  prefixIcon: Icon(Icons.category_outlined),
                ),
                items: viewModel.associationTypes
                    .map<DropdownMenuItem<String>>((t) =>
                        DropdownMenuItem<String>(
                            value: t.id, child: Text(t.name)))
                    .toList(),
                onChanged: viewModel.onAssociationTypeChanged,
              ),
              const SizedBox(height: AppSpacing.md),
              if (viewModel.unselectedAssociations.isNotEmpty)
                DropdownButtonFormField<String>(
                  key: ValueKey(viewModel.selectedAssociationIds.length),
                  decoration: const InputDecoration(
                    labelText: 'Adicionar Associação',
                    border: OutlineInputBorder(),
                    prefixIcon: Icon(Icons.add_link),
                  ),
                  items: viewModel.unselectedAssociations
                      .map<DropdownMenuItem<String>>((a) =>
                          DropdownMenuItem<String>(
                              value: a.id, child: Text(a.name)))
                      .toList(),
                  onChanged: (id) {
                    if (id != null) viewModel.toggleAssociation(id);
                  },
                ),
              if (viewModel.selectedAssociations.isNotEmpty) ...[
                const SizedBox(height: AppSpacing.sm),
                Wrap(
                  spacing: AppSpacing.sm,
                  runSpacing: AppSpacing.sm,
                  children: viewModel.selectedAssociations
                      .map((a) => InputChip(
                            label: Text(a.name),
                            onDeleted: () =>
                                viewModel.removeAssociation(a.id),
                          ))
                      .toList(),
                ),
              ],
              const SizedBox(height: AppSpacing.lg),

              // ─── Document Templates ──────────────────────────────────
              _DocumentTemplatesSection(viewModel: viewModel),
              const SizedBox(height: AppSpacing.lg),

              // ─── Listen Events ───────────────────────────────────────
              _ListenEventsSection(viewModel: viewModel),
              const SizedBox(height: AppSpacing.xl),

              // ─── Actions ─────────────────────────────────────────────
              _FormActions(
                isSaving: viewModel.isSaving,
                onSave: onSave,
                onCancel: onCancel,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Section for managing document templates attached to the require document.
class _DocumentTemplatesSection extends StatelessWidget {
  const _DocumentTemplatesSection({required this.viewModel});

  final RequireDocumentFormViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    return InputDecorator(
      decoration: InputDecoration(
        labelText: 'Templates de Documento',
        border: OutlineInputBorder(borderRadius: BorderRadius.circular(4)),
        contentPadding:
            const EdgeInsets.fromLTRB(12, AppSpacing.md, 12, AppSpacing.sm),
      ),
      child: Padding(
        padding: const EdgeInsets.only(top: AppSpacing.sm),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (viewModel.selectedDocumentTemplates.isEmpty)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: AppSpacing.md),
                child: Text(
                  'Nenhum template adicionado.',
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: cs.onSurfaceVariant,
                      ),
                  textAlign: TextAlign.center,
                ),
              )
            else
              ...viewModel.selectedDocumentTemplates.map(
                (template) => Card.filled(
                  margin: const EdgeInsets.only(bottom: AppSpacing.sm),
                  child: ListTile(
                    leading: Icon(
                      Icons.description_outlined,
                      color: cs.primary,
                    ),
                    title: Text(
                      template.name,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    trailing: IconButton(
                      icon: Icon(Icons.close, color: cs.error),
                      tooltip: 'Remover template',
                      onPressed: () =>
                          viewModel.removeDocumentTemplate(template.id),
                    ),
                  ),
                ),
              ),
            const SizedBox(height: AppSpacing.sm),
            Align(
              alignment: Alignment.centerLeft,
              child: TextButton.icon(
                onPressed: () =>
                    _showAddDocumentTemplateDialog(context, viewModel),
                icon: const Icon(Icons.add, size: 18),
                label: const Text('Adicionar Template'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _showAddDocumentTemplateDialog(
      BuildContext context, RequireDocumentFormViewModel viewModel) async {
    String? selectedId;
    await showDialog<void>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Adicionar Template de Documento'),
        content: StatefulBuilder(
          builder: (context, setDialogState) {
            final available = viewModel.unselectedDocumentTemplates;
            return DropdownButtonFormField<String>(
              initialValue: selectedId,
              decoration: const InputDecoration(
                labelText: 'Selecione um template',
                border: OutlineInputBorder(),
              ),
              items: available
                  .map((t) => DropdownMenuItem<String>(
                      value: t.id, child: Text(t.name)))
                  .toList(),
              onChanged: (v) => setDialogState(() => selectedId = v),
            );
          },
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () {
              if (selectedId != null) {
                viewModel.addDocumentTemplate(selectedId!);
              }
              Navigator.of(dialogContext).pop();
            },
            child: const Text('Adicionar'),
          ),
        ],
      ),
    );
  }
}

/// Section for managing listen events and their statuses.
class _ListenEventsSection extends StatelessWidget {
  const _ListenEventsSection({required this.viewModel});

  final RequireDocumentFormViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    return InputDecorator(
      decoration: InputDecoration(
        labelText: 'Eventos Observados',
        border: OutlineInputBorder(borderRadius: BorderRadius.circular(4)),
        contentPadding:
            const EdgeInsets.fromLTRB(12, AppSpacing.md, 12, AppSpacing.sm),
      ),
      child: Padding(
        padding: const EdgeInsets.only(top: AppSpacing.sm),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (viewModel.listenEvents.isEmpty)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: AppSpacing.md),
                child: Text(
                  'Nenhum evento adicionado.',
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: cs.onSurfaceVariant,
                      ),
                  textAlign: TextAlign.center,
                ),
              )
            else
              ...viewModel.listenEvents.map(
                (event) => _ListenEventCard(
                  event: event,
                  viewModel: viewModel,
                ),
              ),
            const SizedBox(height: AppSpacing.sm),
            Align(
              alignment: Alignment.centerLeft,
              child: TextButton.icon(
                onPressed: () => _showAddEventDialog(context, viewModel),
                icon: const Icon(Icons.add, size: 18),
                label: const Text('Adicionar Evento'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _showAddEventDialog(
      BuildContext context, RequireDocumentFormViewModel viewModel) async {
    String? selectedId;
    await showDialog<void>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Adicionar Evento Observado'),
        content: StatefulBuilder(
          builder: (context, setDialogState) {
            final available = viewModel.unselectedEvents;
            final effectiveValue = selectedId != null &&
                    available.any((e) => e.id == selectedId)
                ? selectedId
                : null;
            return DropdownButtonFormField<String>(
              key: ValueKey(effectiveValue),
              initialValue: effectiveValue,
              decoration: const InputDecoration(
                labelText: 'Selecione um evento',
                border: OutlineInputBorder(),
              ),
              items: available
                  .map((e) => DropdownMenuItem<String>(
                      value: e.id, child: Text(e.name)))
                  .toList(),
              onChanged: (v) => setDialogState(() => selectedId = v),
            );
          },
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () {
              if (selectedId != null) {
                viewModel.addListenEvent(selectedId!);
              }
              Navigator.of(dialogContext).pop();
            },
            child: const Text('Adicionar'),
          ),
        ],
      ),
    );
  }
}

/// A card displaying a single listen event with its statuses.
class _ListenEventCard extends StatelessWidget {
  const _ListenEventCard({
    required this.event,
    required this.viewModel,
  });

  final ListenEventDisplay event;
  final RequireDocumentFormViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    return Card.outlined(
      margin: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: ExpansionTile(
        controlAffinity: ListTileControlAffinity.leading,
        title: Text(
          event.eventName,
          style: Theme.of(context).textTheme.titleSmall,
        ),
        trailing: IconButton(
          icon: Icon(Icons.close, color: cs.error),
          tooltip: 'Remover evento',
          onPressed: () => viewModel.removeListenEvent(event.eventId),
        ),
        shape: const Border(),
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.md,
              0,
              AppSpacing.md,
              AppSpacing.md,
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Status',
                  style: Theme.of(context).textTheme.labelMedium?.copyWith(
                        color: cs.onSurfaceVariant,
                      ),
                ),
                const SizedBox(height: AppSpacing.sm),
                Wrap(
                  spacing: AppSpacing.sm,
                  runSpacing: AppSpacing.xs,
                  children: viewModel.availableStatuses.map((status) {
                    final statusId = int.tryParse(status.id) ?? 0;
                    return FilterChip(
                      label: Text(status.name),
                      selected: event.statuses.any((s) => s.id == statusId),
                      onSelected: (_) => viewModel.toggleStatusOnEvent(
                        event.eventId,
                        status.id,
                      ),
                    );
                  }).toList(),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

}

/// Save and cancel action buttons for the form.
class _FormActions extends StatelessWidget {
  const _FormActions({
    required this.isSaving,
    required this.onSave,
    required this.onCancel,
  });

  final bool isSaving;
  final VoidCallback onSave;
  final VoidCallback onCancel;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.end,
      children: [
        TextButton(
          onPressed: onCancel,
          child: const Text('Cancelar'),
        ),
        const SizedBox(width: AppSpacing.md),
        if (isSaving)
          const SizedBox(
            width: 24,
            height: 24,
            child: CircularProgressIndicator(strokeWidth: 2),
          )
        else
          FilledButton(
            onPressed: onSave,
            child: const Text('Salvar'),
          ),
      ],
    );
  }
}
