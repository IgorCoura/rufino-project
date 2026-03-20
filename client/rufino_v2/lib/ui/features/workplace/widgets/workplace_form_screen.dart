import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_spacing.dart';
import '../viewmodel/workplace_form_viewmodel.dart';

/// Form screen for creating or editing a workplace.
///
/// When [workplaceId] is provided, [WorkplaceFormViewModel.loadWorkplace]
/// is called on init to populate the fields from the API.
class WorkplaceFormScreen extends StatefulWidget {
  const WorkplaceFormScreen({
    super.key,
    required this.viewModel,
    this.workplaceId,
  });

  final WorkplaceFormViewModel viewModel;

  /// The id of the workplace to edit. Null when creating a new workplace.
  final String? workplaceId;

  @override
  State<WorkplaceFormScreen> createState() => _WorkplaceFormScreenState();
}

class _WorkplaceFormScreenState extends State<WorkplaceFormScreen> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _nameController;
  late final TextEditingController _zipCodeController;
  late final TextEditingController _streetController;
  late final TextEditingController _numberController;
  late final TextEditingController _complementController;
  late final TextEditingController _neighborhoodController;
  late final TextEditingController _cityController;
  late final TextEditingController _stateController;
  late final TextEditingController _countryController;

  @override
  void initState() {
    super.initState();
    _nameController = TextEditingController();
    _zipCodeController = TextEditingController();
    _streetController = TextEditingController();
    _numberController = TextEditingController();
    _complementController = TextEditingController();
    _neighborhoodController = TextEditingController();
    _cityController = TextEditingController();
    _stateController = TextEditingController();
    _countryController = TextEditingController();

    widget.viewModel.addListener(_onStatusChanged);

    if (widget.workplaceId != null) {
      widget.viewModel
          .loadWorkplace(widget.workplaceId!)
          .then((_) => _syncControllers());
    }
  }

  void _syncControllers() {
    _nameController.text = widget.viewModel.name;
    _zipCodeController.text = widget.viewModel.zipCode;
    _streetController.text = widget.viewModel.street;
    _numberController.text = widget.viewModel.number;
    _complementController.text = widget.viewModel.complement;
    _neighborhoodController.text = widget.viewModel.neighborhood;
    _cityController.text = widget.viewModel.city;
    _stateController.text = widget.viewModel.state;
    _countryController.text = widget.viewModel.country;
  }

  @override
  void dispose() {
    _nameController.dispose();
    _zipCodeController.dispose();
    _streetController.dispose();
    _numberController.dispose();
    _complementController.dispose();
    _neighborhoodController.dispose();
    _cityController.dispose();
    _stateController.dispose();
    _countryController.dispose();
    widget.viewModel.removeListener(_onStatusChanged);
    super.dispose();
  }

  void _onStatusChanged() {
    if (!mounted) return;
    switch (widget.viewModel.status) {
      case WorkplaceFormStatus.saved:
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Local de trabalho salvo com sucesso!'),
            behavior: SnackBarBehavior.floating,
          ),
        );
        context.pop();
      case WorkplaceFormStatus.error:
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
    widget.viewModel.setZipCode(_zipCodeController.text);
    widget.viewModel.setStreet(_streetController.text);
    widget.viewModel.setNumber(_numberController.text);
    widget.viewModel.setComplement(_complementController.text);
    widget.viewModel.setNeighborhood(_neighborhoodController.text);
    widget.viewModel.setCity(_cityController.text);
    widget.viewModel.setState(_stateController.text);
    widget.viewModel.setCountry(_countryController.text);
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
                ? 'Criar Local de Trabalho'
                : 'Editar Local de Trabalho',
          ),
        ),
      ),
      body: ListenableBuilder(
        listenable: widget.viewModel,
        builder: (context, _) {
          if (widget.viewModel.isLoading) {
            return const Center(child: CircularProgressIndicator());
          }
          return _WorkplaceFormBody(
            formKey: _formKey,
            nameController: _nameController,
            zipCodeController: _zipCodeController,
            streetController: _streetController,
            numberController: _numberController,
            complementController: _complementController,
            neighborhoodController: _neighborhoodController,
            cityController: _cityController,
            stateController: _stateController,
            countryController: _countryController,
            isSaving: widget.viewModel.isSaving,
            onSave: _onSave,
            onCancel: () => context.pop(),
          );
        },
      ),
    );
  }
}

class _WorkplaceFormBody extends StatelessWidget {
  const _WorkplaceFormBody({
    required this.formKey,
    required this.nameController,
    required this.zipCodeController,
    required this.streetController,
    required this.numberController,
    required this.complementController,
    required this.neighborhoodController,
    required this.cityController,
    required this.stateController,
    required this.countryController,
    required this.isSaving,
    required this.onSave,
    required this.onCancel,
  });

  final GlobalKey<FormState> formKey;
  final TextEditingController nameController;
  final TextEditingController zipCodeController;
  final TextEditingController streetController;
  final TextEditingController numberController;
  final TextEditingController complementController;
  final TextEditingController neighborhoodController;
  final TextEditingController cityController;
  final TextEditingController stateController;
  final TextEditingController countryController;
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
                  'Informações do Local',
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
                const SizedBox(height: AppSpacing.lg),
                Text(
                  'Endereço',
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: zipCodeController,
                  decoration: const InputDecoration(
                    labelText: 'CEP',
                    border: OutlineInputBorder(),
                    helperText: '8 dígitos',
                  ),
                  keyboardType: TextInputType.number,
                  maxLength: 8,
                  validator: (v) {
                    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
                    final digits = v.replaceAll(RegExp(r'\D'), '');
                    if (digits.length != 8) return 'CEP inválido.';
                    return null;
                  },
                ),
                const SizedBox(height: AppSpacing.md),
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      flex: 3,
                      child: TextFormField(
                        controller: streetController,
                        decoration: const InputDecoration(
                          labelText: 'Rua',
                          border: OutlineInputBorder(),
                        ),
                        validator: (v) {
                          if (v == null || v.isEmpty) {
                            return 'Não pode ser vazio.';
                          }
                          if (v.length > 100) {
                            return 'Máx. 100 caracteres.';
                          }
                          return null;
                        },
                      ),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    Expanded(
                      child: TextFormField(
                        controller: numberController,
                        decoration: const InputDecoration(
                          labelText: 'Número',
                          border: OutlineInputBorder(),
                        ),
                        keyboardType: TextInputType.number,
                        validator: (v) {
                          if (v == null || v.isEmpty) {
                            return 'Não pode ser vazio.';
                          }
                          return null;
                        },
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: complementController,
                  decoration: const InputDecoration(
                    labelText: 'Complemento',
                    border: OutlineInputBorder(),
                    helperText: 'Opcional',
                  ),
                  validator: (v) {
                    if (v != null && v.length > 50) {
                      return 'Máx. 50 caracteres.';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: neighborhoodController,
                  decoration: const InputDecoration(
                    labelText: 'Bairro',
                    border: OutlineInputBorder(),
                  ),
                  validator: (v) {
                    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
                    if (v.length > 50) return 'Máx. 50 caracteres.';
                    return null;
                  },
                ),
                const SizedBox(height: AppSpacing.md),
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      flex: 2,
                      child: TextFormField(
                        controller: cityController,
                        decoration: const InputDecoration(
                          labelText: 'Cidade',
                          border: OutlineInputBorder(),
                        ),
                        validator: (v) {
                          if (v == null || v.isEmpty) {
                            return 'Não pode ser vazio.';
                          }
                          if (v.length > 50) return 'Máx. 50 caracteres.';
                          return null;
                        },
                      ),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    Expanded(
                      child: TextFormField(
                        controller: stateController,
                        decoration: const InputDecoration(
                          labelText: 'Estado',
                          border: OutlineInputBorder(),
                        ),
                        validator: (v) {
                          if (v == null || v.isEmpty) {
                            return 'Não pode ser vazio.';
                          }
                          if (v.length > 50) return 'Máx. 50 caracteres.';
                          return null;
                        },
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: countryController,
                  decoration: const InputDecoration(
                    labelText: 'País',
                    border: OutlineInputBorder(),
                  ),
                  validator: (v) {
                    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
                    if (v.length > 50) return 'Máx. 50 caracteres.';
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
