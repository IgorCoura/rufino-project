import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_spacing.dart';
import '../viewmodel/position_form_viewmodel.dart';

/// Form screen for creating or editing a position within a department.
///
/// When [positionId] is provided, [PositionFormViewModel.loadPosition] is
/// called on init to populate the fields from the API.
class PositionFormScreen extends StatefulWidget {
  const PositionFormScreen({
    super.key,
    required this.viewModel,
    this.positionId,
  });

  final PositionFormViewModel viewModel;

  /// The id of the position to edit. Null when creating a new position.
  final String? positionId;

  @override
  State<PositionFormScreen> createState() => _PositionFormScreenState();
}

class _PositionFormScreenState extends State<PositionFormScreen> {
  final _formKey = GlobalKey<FormState>();

  @override
  void initState() {
    super.initState();
    widget.viewModel.addListener(_onViewModelChanged);
    if (widget.positionId != null) {
      widget.viewModel.loadPosition(widget.positionId!);
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
      case PositionFormStatus.saved:
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Cargo salvo com sucesso!'),
            behavior: SnackBarBehavior.floating,
          ),
        );
        context.pop();
      case PositionFormStatus.error:
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
            widget.viewModel.isNew ? 'Criar Cargo' : 'Editar Cargo',
          ),
        ),
      ),
      body: ListenableBuilder(
        listenable: widget.viewModel,
        builder: (context, _) {
          if (widget.viewModel.isLoading) {
            return const Center(child: CircularProgressIndicator());
          }
          return _PositionFormBody(
            viewModel: widget.viewModel,
            formKey: _formKey,
            onSave: _onSave,
            onCancel: () => context.go('/department'),
          );
        },
      ),
    );
  }
}

class _PositionFormBody extends StatelessWidget {
  const _PositionFormBody({
    required this.viewModel,
    required this.formKey,
    required this.onSave,
    required this.onCancel,
  });

  final PositionFormViewModel viewModel;
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
          child: Form(
            key: formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Informações do Cargo',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: viewModel.nameController,
                  decoration: const InputDecoration(
                    labelText: 'Nome',
                    border: OutlineInputBorder(),
                  ),
                  validator: viewModel.validateName,
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: viewModel.descriptionController,
                  decoration: const InputDecoration(
                    labelText: 'Descrição',
                    border: OutlineInputBorder(),
                  ),
                  minLines: 3,
                  maxLines: null,
                  keyboardType: TextInputType.multiline,
                  validator: viewModel.validateDescription,
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: viewModel.cboController,
                  decoration: const InputDecoration(
                    labelText: 'CBO',
                    border: OutlineInputBorder(),
                    helperText:
                        'Código Brasileiro de Ocupações (máx. 6 caracteres)',
                  ),
                  maxLength: 6,
                  keyboardType: TextInputType.number,
                  validator: viewModel.validateCbo,
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
