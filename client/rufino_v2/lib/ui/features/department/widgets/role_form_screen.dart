import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_spacing.dart';
import '../../../core/widgets/error_dialog.dart';
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

  @override
  void initState() {
    super.initState();
    widget.viewModel.addListener(_onViewModelChanged);
    widget.viewModel.initialize(roleId: widget.roleId);
  }

  @override
  void didUpdateWidget(covariant RoleFormScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.viewModel != widget.viewModel) {
      oldWidget.viewModel.removeListener(_onViewModelChanged);
      widget.viewModel.addListener(_onViewModelChanged);
      widget.viewModel.initialize(roleId: widget.roleId);
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
      case RoleFormStatus.saved:
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Função salva com sucesso!'),
            behavior: SnackBarBehavior.floating,
          ),
        );
        context.pop();
      case RoleFormStatus.error:
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

class _RoleFormBody extends StatelessWidget {
  const _RoleFormBody({
    required this.viewModel,
    required this.formKey,
    required this.onSave,
    required this.onCancel,
  });

  final RoleFormViewModel viewModel;
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
                  'Informações da Função',
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
                const SizedBox(height: AppSpacing.lg),
                _RemunerationSection(
                  viewModel: viewModel,
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

class _RemunerationSection extends StatelessWidget {
  const _RemunerationSection({required this.viewModel});

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
          initialValue: viewModel.safePaymentUnitId,
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
          validator: viewModel.validatePaymentUnit,
        ),
        const SizedBox(height: AppSpacing.md),
        TextFormField(
          controller: viewModel.salaryValueController,
          decoration: const InputDecoration(
            labelText: 'Valor',
            border: OutlineInputBorder(),
            prefixText: 'R\$ ',
          ),
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          validator: viewModel.validateSalaryValue,
        ),
        const SizedBox(height: AppSpacing.md),
        DropdownButtonFormField<String>(
          initialValue: viewModel.safeSalaryTypeId,
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
          validator: viewModel.validateSalaryType,
        ),
        const SizedBox(height: AppSpacing.md),
        TextFormField(
          controller: viewModel.remunerationDescriptionController,
          decoration: const InputDecoration(
            labelText: 'Descrição da Remuneração',
            border: OutlineInputBorder(),
          ),
          minLines: 2,
          maxLines: null,
          keyboardType: TextInputType.multiline,
          validator: viewModel.validateRemunerationDescription,
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
