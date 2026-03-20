import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_spacing.dart';
import '../viewmodel/role_form_viewmodel.dart';

/// Form screen for creating or editing a role within a position.
///
/// On init this screen triggers [RoleFormViewModel.initialize], which fetches
/// the payment-unit and salary-type lookup data. When [roleId] is provided,
/// the existing role data is also loaded from the API.
class RoleFormScreen extends StatefulWidget {
  const RoleFormScreen({
    super.key,
    required this.viewModel,
    this.roleId,
  });

  final RoleFormViewModel viewModel;

  /// The id of the role to edit. Null when creating a new role.
  final String? roleId;

  @override
  State<RoleFormScreen> createState() => _RoleFormScreenState();
}

class _RoleFormScreenState extends State<RoleFormScreen> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _nameController;
  late final TextEditingController _descriptionController;
  late final TextEditingController _cboController;
  late final TextEditingController _salaryValueController;
  late final TextEditingController _remunerationDescriptionController;

  @override
  void initState() {
    super.initState();
    _nameController = TextEditingController();
    _descriptionController = TextEditingController();
    _cboController = TextEditingController();
    _salaryValueController = TextEditingController();
    _remunerationDescriptionController = TextEditingController();

    widget.viewModel.addListener(_onStatusChanged);
    widget.viewModel
        .initialize(roleId: widget.roleId)
        .then((_) => _syncControllers());
  }

  void _syncControllers() {
    _nameController.text = widget.viewModel.name;
    _descriptionController.text = widget.viewModel.description;
    _cboController.text = widget.viewModel.cbo;
    _salaryValueController.text = widget.viewModel.baseSalaryValue;
    _remunerationDescriptionController.text =
        widget.viewModel.remunerationDescription;
  }

  @override
  void dispose() {
    _nameController.dispose();
    _descriptionController.dispose();
    _cboController.dispose();
    _salaryValueController.dispose();
    _remunerationDescriptionController.dispose();
    widget.viewModel.removeListener(_onStatusChanged);
    super.dispose();
  }

  void _onStatusChanged() {
    if (!mounted) return;
    switch (widget.viewModel.status) {
      case RoleFormStatus.saved:
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Função salva com sucesso!'),
            behavior: SnackBarBehavior.floating,
          ),
        );
        context.pop();
      case RoleFormStatus.error:
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
    widget.viewModel.setBaseSalaryValue(_salaryValueController.text);
    widget.viewModel.setRemunerationDescription(
        _remunerationDescriptionController.text);
    widget.viewModel.save();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) => Text(
            widget.viewModel.isNew ? 'Criar Função' : 'Editar Função',
          ),
        ),
      ),
      body: ListenableBuilder(
        listenable: widget.viewModel,
        builder: (context, _) {
          if (widget.viewModel.isLoading) {
            return const Center(child: CircularProgressIndicator());
          }
          return _RoleFormBody(
            formKey: _formKey,
            nameController: _nameController,
            descriptionController: _descriptionController,
            cboController: _cboController,
            salaryValueController: _salaryValueController,
            remunerationDescriptionController:
                _remunerationDescriptionController,
            viewModel: widget.viewModel,
            isSaving: widget.viewModel.isSaving,
            onSave: _onSave,
            onCancel: () => context.go('/department'),
          );
        },
      ),
    );
  }
}

class _RoleFormBody extends StatelessWidget {
  const _RoleFormBody({
    required this.formKey,
    required this.nameController,
    required this.descriptionController,
    required this.cboController,
    required this.salaryValueController,
    required this.remunerationDescriptionController,
    required this.viewModel,
    required this.isSaving,
    required this.onSave,
    required this.onCancel,
  });

  final GlobalKey<FormState> formKey;
  final TextEditingController nameController;
  final TextEditingController descriptionController;
  final TextEditingController cboController;
  final TextEditingController salaryValueController;
  final TextEditingController remunerationDescriptionController;
  final RoleFormViewModel viewModel;
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
                  'Informações da Função',
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
                    helperText:
                        'Código Brasileiro de Ocupações (máx. 6 caracteres)',
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
                const SizedBox(height: AppSpacing.lg),
                _RemunerationSection(
                  salaryValueController: salaryValueController,
                  remunerationDescriptionController:
                      remunerationDescriptionController,
                  viewModel: viewModel,
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

class _RemunerationSection extends StatelessWidget {
  const _RemunerationSection({
    required this.salaryValueController,
    required this.remunerationDescriptionController,
    required this.viewModel,
  });

  final TextEditingController salaryValueController;
  final TextEditingController remunerationDescriptionController;
  final RoleFormViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'Remuneração',
          style: Theme.of(context).textTheme.titleMedium,
        ),
        const SizedBox(height: AppSpacing.md),
        DropdownButtonFormField<String>(
          value: viewModel.paymentUnits.any((u) => u.id == viewModel.paymentUnitId)
              ? viewModel.paymentUnitId
              : null,
          decoration: const InputDecoration(
            labelText: 'Unidade de Pagamento',
            border: OutlineInputBorder(),
          ),
          items: viewModel.paymentUnits
              .map((u) => DropdownMenuItem(value: u.id, child: Text(u.name)))
              .toList(),
          onChanged: (v) {
            if (v != null) viewModel.setPaymentUnitId(v);
          },
          validator: (v) =>
              (v == null || v.isEmpty) ? 'Selecione uma opção.' : null,
        ),
        const SizedBox(height: AppSpacing.md),
        TextFormField(
          controller: salaryValueController,
          decoration: const InputDecoration(
            labelText: 'Valor',
            border: OutlineInputBorder(),
            prefixText: 'R\$ ',
          ),
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          validator: (v) {
            if (v == null || v.isEmpty) return 'Não pode ser vazio.';
            final normalized = v.replaceAll(',', '.');
            final regex = RegExp(r'^\d+(\.\d{1,2})?$');
            if (!regex.hasMatch(normalized)) return 'Valor inválido.';
            return null;
          },
        ),
        const SizedBox(height: AppSpacing.md),
        DropdownButtonFormField<String>(
          value: viewModel.salaryTypes.any((t) => t.id == viewModel.salaryTypeId)
              ? viewModel.salaryTypeId
              : null,
          decoration: const InputDecoration(
            labelText: 'Tipo Monetário',
            border: OutlineInputBorder(),
          ),
          items: viewModel.salaryTypes
              .map((t) => DropdownMenuItem(value: t.id, child: Text(t.name)))
              .toList(),
          onChanged: (v) {
            if (v != null) viewModel.setSalaryTypeId(v);
          },
          validator: (v) =>
              (v == null || v.isEmpty) ? 'Selecione uma opção.' : null,
        ),
        const SizedBox(height: AppSpacing.md),
        TextFormField(
          controller: remunerationDescriptionController,
          decoration: const InputDecoration(
            labelText: 'Descrição da Remuneração',
            border: OutlineInputBorder(),
          ),
          minLines: 2,
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
      ],
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
