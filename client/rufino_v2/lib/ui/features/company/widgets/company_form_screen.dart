import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';

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

  late final Map<String, TextEditingController> _controllers;
  final _cnpjFormatter = MaskTextInputFormatter(
    mask: '##.###.###/####-##',
    filter: {'#': RegExp(r'[0-9]')},
  );
  final _phoneFormatter = MaskTextInputFormatter(
    mask: '(##) #####-####',
    filter: {'#': RegExp(r'[0-9]')},
  );
  final _zipCodeFormatter = MaskTextInputFormatter(
    mask: '#####-###',
    filter: {'#': RegExp(r'[0-9]')},
  );

  @override
  void initState() {
    super.initState();
    _controllers = {
      'corporateName': TextEditingController(),
      'fantasyName': TextEditingController(),
      'cnpj': TextEditingController(),
      'email': TextEditingController(),
      'phone': TextEditingController(),
      'zipCode': TextEditingController(),
      'street': TextEditingController(),
      'number': TextEditingController(),
      'complement': TextEditingController(),
      'neighborhood': TextEditingController(),
      'city': TextEditingController(),
      'state': TextEditingController(),
      'country': TextEditingController(),
    };
    widget.viewModel.addListener(_onStatusChanged);
    if (widget.companyId != null) {
      widget.viewModel.loadCompany(widget.companyId!).then((_) => _syncControllers());
    }
  }

  void _syncControllers() {
    _controllers['corporateName']!.text = widget.viewModel.corporateName;
    _controllers['fantasyName']!.text = widget.viewModel.fantasyName;
    _controllers['cnpj']!.text = widget.viewModel.cnpj;
    _controllers['email']!.text = widget.viewModel.email;
    _controllers['phone']!.text = widget.viewModel.phone;
    _controllers['zipCode']!.text = widget.viewModel.zipCode;
    _controllers['street']!.text = widget.viewModel.street;
    _controllers['number']!.text = widget.viewModel.number;
    _controllers['complement']!.text = widget.viewModel.complement;
    _controllers['neighborhood']!.text = widget.viewModel.neighborhood;
    _controllers['city']!.text = widget.viewModel.city;
    _controllers['state']!.text = widget.viewModel.state;
    _controllers['country']!.text = widget.viewModel.country;
  }

  @override
  void dispose() {
    for (final c in _controllers.values) {
      c.dispose();
    }
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
    widget.viewModel.setCorporateName(_controllers['corporateName']!.text);
    widget.viewModel.setFantasyName(_controllers['fantasyName']!.text);
    widget.viewModel.setCnpj(_cnpjFormatter.getUnmaskedText());
    widget.viewModel.setEmail(_controllers['email']!.text);
    widget.viewModel.setPhone(_phoneFormatter.getUnmaskedText());
    widget.viewModel.setZipCode(_zipCodeFormatter.getUnmaskedText());
    widget.viewModel.setStreet(_controllers['street']!.text);
    widget.viewModel.setNumber(_controllers['number']!.text);
    widget.viewModel.setComplement(_controllers['complement']!.text);
    widget.viewModel.setNeighborhood(_controllers['neighborhood']!.text);
    widget.viewModel.setCity(_controllers['city']!.text);
    widget.viewModel.setState(_controllers['state']!.text);
    widget.viewModel.setCountry(_controllers['country']!.text);
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
            formKey: _formKey,
            controllers: _controllers,
            cnpjFormatter: _cnpjFormatter,
            phoneFormatter: _phoneFormatter,
            zipCodeFormatter: _zipCodeFormatter,
            isSaving: widget.viewModel.isSaving,
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
    required this.formKey,
    required this.controllers,
    required this.cnpjFormatter,
    required this.phoneFormatter,
    required this.zipCodeFormatter,
    required this.isSaving,
    required this.onSave,
    required this.onCancel,
  });

  final GlobalKey<FormState> formKey;
  final Map<String, TextEditingController> controllers;
  final MaskTextInputFormatter cnpjFormatter;
  final MaskTextInputFormatter phoneFormatter;
  final MaskTextInputFormatter zipCodeFormatter;
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
                  'Informações da Empresa',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: controllers['corporateName']!,
                  label: 'Razão Social',
                  validator: _validateRequired,
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: controllers['fantasyName']!,
                  label: 'Nome Fantasia',
                  validator: _validateRequired,
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: controllers['cnpj']!,
                  decoration: const InputDecoration(
                    labelText: 'CNPJ',
                    border: OutlineInputBorder(),
                  ),
                  inputFormatters: [cnpjFormatter],
                  keyboardType: TextInputType.number,
                  validator: (v) {
                    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
                    final digits = cnpjFormatter.getUnmaskedText();
                    if (digits.length != 14) return 'CNPJ inválido.';
                    return null;
                  },
                ),
                const SizedBox(height: AppSpacing.lg),
                Text(
                  'Contato',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: controllers['email']!,
                  label: 'Email',
                  keyboardType: TextInputType.emailAddress,
                  validator: _validateEmail,
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: controllers['phone']!,
                  decoration: const InputDecoration(
                    labelText: 'Telefone',
                    border: OutlineInputBorder(),
                  ),
                  inputFormatters: [phoneFormatter],
                  keyboardType: TextInputType.phone,
                  validator: _validateRequired,
                ),
                const SizedBox(height: AppSpacing.lg),
                Text(
                  'Endereço',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: controllers['zipCode']!,
                  decoration: const InputDecoration(
                    labelText: 'CEP',
                    border: OutlineInputBorder(),
                  ),
                  inputFormatters: [zipCodeFormatter],
                  keyboardType: TextInputType.number,
                  validator: (v) {
                    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
                    final digits = zipCodeFormatter.getUnmaskedText();
                    if (digits.length != 8) return 'CEP inválido.';
                    return null;
                  },
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: controllers['street']!,
                  label: 'Rua',
                  validator: _validateRequired,
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: controllers['number']!,
                  label: 'Número',
                  validator: _validateRequired,
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: controllers['complement']!,
                  label: 'Complemento',
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: controllers['neighborhood']!,
                  label: 'Bairro',
                  validator: _validateRequired,
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: controllers['city']!,
                  label: 'Cidade',
                  validator: _validateRequired,
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: controllers['state']!,
                  label: 'Estado',
                  validator: _validateRequired,
                ),
                const SizedBox(height: AppSpacing.md),
                _FormField(
                  controller: controllers['country']!,
                  label: 'País',
                  validator: _validateRequired,
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

  static String? _validateRequired(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    return null;
  }

  static String? _validateEmail(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    final regex = RegExp(r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$');
    if (!regex.hasMatch(value)) return 'Email inválido.';
    return null;
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
