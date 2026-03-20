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
  late final TextEditingController _nameController;
  late final TextEditingController _descriptionController;
  late final TextEditingController _cboController;

  @override
  void initState() {
    super.initState();
    _nameController = TextEditingController();
    _descriptionController = TextEditingController();
    _cboController = TextEditingController();
    widget.viewModel.addListener(_onStatusChanged);

    if (widget.positionId != null) {
      widget.viewModel
          .loadPosition(widget.positionId!)
          .then((_) => _syncControllers());
    }
  }

  void _syncControllers() {
    _nameController.text = widget.viewModel.name;
    _descriptionController.text = widget.viewModel.description;
    _cboController.text = widget.viewModel.cbo;
  }

  @override
  void dispose() {
    _nameController.dispose();
    _descriptionController.dispose();
    _cboController.dispose();
    widget.viewModel.removeListener(_onStatusChanged);
    super.dispose();
  }

  void _onStatusChanged() {
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
    widget.viewModel.setName(_nameController.text);
    widget.viewModel.setDescription(_descriptionController.text);
    widget.viewModel.setCbo(_cboController.text);
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
            formKey: _formKey,
            nameController: _nameController,
            descriptionController: _descriptionController,
            cboController: _cboController,
            isSaving: widget.viewModel.isSaving,
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
    required this.formKey,
    required this.nameController,
    required this.descriptionController,
    required this.cboController,
    required this.isSaving,
    required this.onSave,
    required this.onCancel,
  });

  final GlobalKey<FormState> formKey;
  final TextEditingController nameController;
  final TextEditingController descriptionController;
  final TextEditingController cboController;
  final bool isSaving;
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
                  controller: nameController,
                  decoration: const InputDecoration(
                    labelText: 'Nome',
                    border: OutlineInputBorder(),
                  ),
                  validator: (v) {
                    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
                    if (v.length > 100) {
                      return 'Não pode ser maior que 100 caracteres.';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: descriptionController,
                  decoration: const InputDecoration(
                    labelText: 'Descrição',
                    border: OutlineInputBorder(),
                  ),
                  minLines: 3,
                  maxLines: null,
                  keyboardType: TextInputType.multiline,
                  validator: (v) {
                    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
                    if (v.length > 2000) {
                      return 'Não pode ser maior que 2000 caracteres.';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: cboController,
                  decoration: const InputDecoration(
                    labelText: 'CBO',
                    border: OutlineInputBorder(),
                    helperText: 'Código Brasileiro de Ocupações (máx. 6 caracteres)',
                  ),
                  maxLength: 6,
                  keyboardType: TextInputType.number,
                  validator: (v) {
                    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
                    if (v.length > 6) {
                      return 'Não pode ser maior que 6 caracteres.';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: AppSpacing.xl),
                _FormActions(
                  isSaving: isSaving,
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
