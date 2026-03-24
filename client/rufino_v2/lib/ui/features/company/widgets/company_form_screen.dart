import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_spacing.dart';
import '../viewmodel/company_form_viewmodel.dart';

class CompanyFormScreen extends StatefulWidget {
  const CompanyFormScreen({
    super.key,
    required this.viewModel,
    this.companyId,
  });

  final CompanyFormViewModel viewModel;
  final String? companyId;

  @override
  State<CompanyFormScreen> createState() => _CompanyFormScreenState();
}

class _CompanyFormScreenState extends State<CompanyFormScreen> {
  final _formKey = GlobalKey<FormState>();

  @override
  void initState() {
    super.initState();
    widget.viewModel.addListener(_onStatusChanged);
    if (widget.companyId != null) {
      widget.viewModel.loadCompany(widget.companyId!);
    }
  }

  @override
  void didUpdateWidget(covariant CompanyFormScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.viewModel != widget.viewModel) {
      oldWidget.viewModel.removeListener(_onStatusChanged);
      widget.viewModel.addListener(_onStatusChanged);
      if (widget.companyId != null) {
        widget.viewModel.loadCompany(widget.companyId!);
      }
    }
  }

  @override
  void dispose() {
    widget.viewModel.removeListener(_onStatusChanged);
    super.dispose();
  }

  void _onStatusChanged() {
    if (!mounted) return;
    switch (widget.viewModel.status) {
      case CompanyFormStatus.saved:
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Empresa salva com sucesso!'),
            behavior: SnackBarBehavior.floating,
          ),
        );
        context.go('/company');
      case CompanyFormStatus.error:
        ScaffoldMessenger.of(context)
          ..hideCurrentSnackBar()
          ..showSnackBar(
            SnackBar(
              content: Text(widget.viewModel.errorMessage ?? 'Erro ao salvar.'),
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
            widget.viewModel.isNew ? 'Criar Empresa' : 'Editar Empresa',
          ),
        ),
      ),
      body: ListenableBuilder(
        listenable: widget.viewModel,
        builder: (context, _) {
          if (widget.viewModel.isLoading) {
            return const Center(child: CircularProgressIndicator());
          }
          return _CompanyFormBody(
            viewModel: widget.viewModel,
            formKey: _formKey,
            onSave: _onSave,
            onCancel: () => context.go('/company'),
          );
        },
      ),
    );
  }
}

class _CompanyFormBody extends StatelessWidget {
  const _CompanyFormBody({
    required this.viewModel,
    required this.formKey,
    required this.onSave,
    required this.onCancel,
  });

  final CompanyFormViewModel viewModel;
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
                  'Informações da Empresa',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: viewModel.corporateNameController,
                  label: 'Razão Social',
                  validator: viewModel.validateRequired,
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: viewModel.fantasyNameController,
                  label: 'Nome Fantasia',
                  validator: viewModel.validateRequired,
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: viewModel.cnpjController,
                  decoration: const InputDecoration(
                    labelText: 'CNPJ',
                    border: OutlineInputBorder(),
                  ),
                  inputFormatters: [viewModel.cnpjFormatter],
                  keyboardType: TextInputType.number,
                  validator: viewModel.validateCnpj,
                ),
                const SizedBox(height: AppSpacing.lg),
                Text(
                  'Contato',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: viewModel.emailController,
                  label: 'Email',
                  keyboardType: TextInputType.emailAddress,
                  validator: viewModel.validateEmail,
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: viewModel.phoneController,
                  decoration: const InputDecoration(
                    labelText: 'Telefone',
                    border: OutlineInputBorder(),
                  ),
                  inputFormatters: [viewModel.phoneFormatter],
                  keyboardType: TextInputType.phone,
                  validator: viewModel.validateRequired,
                ),
                const SizedBox(height: AppSpacing.lg),
                Text(
                  'Endereço',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: viewModel.zipCodeController,
                  decoration: const InputDecoration(
                    labelText: 'CEP',
                    border: OutlineInputBorder(),
                  ),
                  inputFormatters: [viewModel.zipCodeFormatter],
                  keyboardType: TextInputType.number,
                  validator: viewModel.validateZipCode,
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: viewModel.streetController,
                  label: 'Rua',
                  validator: viewModel.validateRequired,
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: viewModel.numberController,
                  label: 'Número',
                  validator: viewModel.validateRequired,
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: viewModel.complementController,
                  label: 'Complemento',
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: viewModel.neighborhoodController,
                  label: 'Bairro',
                  validator: viewModel.validateRequired,
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: viewModel.cityController,
                  label: 'Cidade',
                  validator: viewModel.validateRequired,
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: viewModel.stateController,
                  label: 'Estado',
                  validator: viewModel.validateRequired,
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: viewModel.countryController,
                  label: 'País',
                  validator: viewModel.validateRequired,
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

class _FormField extends StatelessWidget {
  const _FormField({
    required this.controller,
    required this.label,
    this.keyboardType,
    this.validator,
  });

  final TextEditingController controller;
  final String label;
  final TextInputType? keyboardType;
  final FormFieldValidator<String>? validator;

  @override
  Widget build(BuildContext context) {
    return TextFormField(
      controller: controller,
      decoration: InputDecoration(
        labelText: label,
        border: const OutlineInputBorder(),
      ),
      keyboardType: keyboardType,
      validator: validator,
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
