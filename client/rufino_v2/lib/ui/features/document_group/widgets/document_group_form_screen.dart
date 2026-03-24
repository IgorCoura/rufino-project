import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_spacing.dart';
import '../viewmodel/document_group_form_viewmodel.dart';

/// Form screen for creating or editing a document group.
///
/// When [groupId] is provided, [DocumentGroupFormViewModel.loadGroup]
/// is called on init to populate the fields from the API.
class DocumentGroupFormScreen extends StatefulWidget {
  const DocumentGroupFormScreen({
    super.key,
    required this.viewModel,
    this.groupId,
  });

  /// The view model that manages form state and validation.
  final DocumentGroupFormViewModel viewModel;

  /// The id of the group to edit. Null when creating a new group.
  final String? groupId;

  @override
  State<DocumentGroupFormScreen> createState() =>
      _DocumentGroupFormScreenState();
}

class _DocumentGroupFormScreenState extends State<DocumentGroupFormScreen> {
  final _formKey = GlobalKey<FormState>();

  @override
  void initState() {
    super.initState();
    widget.viewModel.addListener(_onViewModelChanged);
    if (widget.groupId != null) {
      widget.viewModel.loadGroup(widget.groupId!);
    }
  }

  @override
  void didUpdateWidget(covariant DocumentGroupFormScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.viewModel != widget.viewModel) {
      oldWidget.viewModel.removeListener(_onViewModelChanged);
      widget.viewModel.addListener(_onViewModelChanged);
      if (widget.groupId != null) {
        widget.viewModel.loadGroup(widget.groupId!);
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
      case DocumentGroupFormStatus.saved:
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Grupo salvo com sucesso!'),
            behavior: SnackBarBehavior.floating,
          ),
        );
        context.pop();
      case DocumentGroupFormStatus.error:
        ScaffoldMessenger.of(context)
          ..hideCurrentSnackBar()
          ..showSnackBar(
            SnackBar(
              content:
                  Text(widget.viewModel.errorMessage ?? 'Erro ao salvar.'),
              behavior: SnackBarBehavior.floating,
            ),
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
            widget.viewModel.isNew ? 'Criar Grupo' : 'Editar Grupo',
          ),
        ),
      ),
      body: ListenableBuilder(
        listenable: widget.viewModel,
        builder: (context, _) {
          if (widget.viewModel.isLoading) {
            return const Center(child: CircularProgressIndicator());
          }
          return _DocumentGroupFormBody(
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

/// The scrollable form body containing all group fields.
class _DocumentGroupFormBody extends StatelessWidget {
  const _DocumentGroupFormBody({
    required this.viewModel,
    required this.formKey,
    required this.onSave,
    required this.onCancel,
  });

  final DocumentGroupFormViewModel viewModel;
  final GlobalKey<FormState> formKey;
  final VoidCallback onSave;
  final VoidCallback onCancel;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 600),
          child: Form(
            key: formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Informações do Grupo',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                const SizedBox(height: AppSpacing.lg),
                TextFormField(
                  controller: viewModel.nameController,
                  decoration: const InputDecoration(
                    labelText: 'Nome',
                    border: OutlineInputBorder(),
                    prefixIcon: Icon(Icons.label_outlined),
                    helperText: 'Máximo de 100 caracteres',
                  ),
                  textCapitalization: TextCapitalization.sentences,
                  maxLength: 100,
                  inputFormatters: [
                    LengthLimitingTextInputFormatter(100),
                  ],
                  validator: viewModel.validateName,
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: viewModel.descriptionController,
                  decoration: const InputDecoration(
                    labelText: 'Descrição',
                    border: OutlineInputBorder(),
                    prefixIcon: Icon(Icons.description_outlined),
                    alignLabelWithHint: true,
                    helperText: 'Máximo de 1000 caracteres',
                  ),
                  textCapitalization: TextCapitalization.sentences,
                  maxLines: 4,
                  maxLength: 1000,
                  inputFormatters: [
                    LengthLimitingTextInputFormatter(1000),
                  ],
                  validator: viewModel.validateDescription,
                ),
                const SizedBox(height: AppSpacing.xl),
                _FormActions(
                  isSaving: viewModel.isSaving,
                  onSave: onSave,
                  onCancel: onCancel,
                ),
              ],
            ),
          ),
        ),
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
