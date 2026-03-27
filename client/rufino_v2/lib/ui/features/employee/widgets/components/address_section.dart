import 'package:flutter/material.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';

import '../../../../../core/theme/app_spacing.dart';
import '../../../../../domain/entities/address.dart';
import '../../viewmodel/employee_profile_viewmodel.dart';
import 'profile_shared_widgets.dart';

/// Expandable card for viewing and editing employee address information.
class AddressSection extends StatefulWidget {
  const AddressSection({super.key, required this.viewModel});

  final EmployeeProfileViewModel viewModel;

  @override
  State<AddressSection> createState() => _AddressSectionState();
}

class _AddressSectionState extends State<AddressSection> {
  final _formKey = GlobalKey<FormState>();
  final _zipCodeController = TextEditingController();
  final _streetController = TextEditingController();
  final _numberController = TextEditingController();
  final _complementController = TextEditingController();
  final _neighborhoodController = TextEditingController();
  final _cityController = TextEditingController();
  final _stateController = TextEditingController();
  final _countryController = TextEditingController();

  /// CEP mask: `#####-###`.
  final _cepMask = MaskTextInputFormatter(
    mask: '#####-###',
    filter: {'#': RegExp(r'[0-9]')},
    type: MaskAutoCompletionType.lazy,
  );

  bool _isEditing = false;

  @override
  void dispose() {
    _zipCodeController.dispose();
    _streetController.dispose();
    _numberController.dispose();
    _complementController.dispose();
    _neighborhoodController.dispose();
    _cityController.dispose();
    _stateController.dispose();
    _countryController.dispose();
    super.dispose();
  }

  void _startEdit() {
    final address = widget.viewModel.address;
    if (address == null) return;

    // Apply CEP mask to raw digits stored in the entity.
    final rawZip = address.zipCode.replaceAll(RegExp(r'[^\d]'), '');
    _cepMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: rawZip),
    );
    _zipCodeController.text = _cepMask.getMaskedText();

    _streetController.text = address.street;
    _numberController.text = address.number;
    _complementController.text = address.complement;
    _neighborhoodController.text = address.neighborhood;
    _cityController.text = address.city;
    _stateController.text = address.state.toUpperCase();
    _countryController.text = address.country;
    setState(() => _isEditing = true);
  }

  Future<void> _save() async {
    if (_formKey.currentState?.validate() != true) return;

    final address = Address(
      zipCode: _zipCodeController.text.trim(),
      street: _streetController.text.trim(),
      number: _numberController.text.trim(),
      complement: _complementController.text.trim(),
      neighborhood: _neighborhoodController.text.trim(),
      city: _cityController.text.trim(),
      state: _stateController.text.trim().toUpperCase(),
      country: _countryController.text.trim(),
    );
    await widget.viewModel.saveAddress(address);
    if (mounted && widget.viewModel.addressStatus == SectionLoadStatus.loaded) {
      setState(() => _isEditing = false);
    }
  }

  void _cancel() => setState(() => _isEditing = false);

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.addressStatus;
        return ExpandableSectionCard(
          title: 'Endereço',
          onExpand: widget.viewModel.loadAddress,
          child: _buildContent(context, status),
        );
      },
    );
  }

  Widget _buildContent(BuildContext context, SectionLoadStatus status) {
    if (status == SectionLoadStatus.notLoaded ||
        status == SectionLoadStatus.loading) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: AppSpacing.md),
        child: Center(child: CircularProgressIndicator()),
      );
    }

    if (status == SectionLoadStatus.error) {
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
        child: Row(
          children: [
            Icon(
              Icons.error_outline,
              color: Theme.of(context).colorScheme.error,
              size: 20,
            ),
            const SizedBox(width: AppSpacing.sm),
            const Expanded(
              child: Text('Não foi possível carregar o endereço.'),
            ),
          ],
        ),
      );
    }

    final address = widget.viewModel.address;
    final isSaving = status == SectionLoadStatus.saving;

    if (_isEditing) {
      return _AddressEditForm(
        formKey: _formKey,
        zipCodeController: _zipCodeController,
        streetController: _streetController,
        numberController: _numberController,
        complementController: _complementController,
        neighborhoodController: _neighborhoodController,
        cityController: _cityController,
        stateController: _stateController,
        countryController: _countryController,
        cepMask: _cepMask,
        isSaving: isSaving,
        onSave: _save,
        onCancel: _cancel,
        validateCep: widget.viewModel.validateCep,
        validateRequired: widget.viewModel.validateRequired,
        validateState: widget.viewModel.validateAddressState,
      );
    }

    // ── View mode ────────────────────────────────────────────────────────────
    final hasAddress = address?.street.isNotEmpty == true;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(
              Icons.location_on_outlined,
              size: 22,
              color: Theme.of(context).colorScheme.primary,
            ),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: hasAddress
                  ? _AddressBlock(address: address!)
                  : Text(
                      'Não informado',
                      style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                            color:
                                Theme.of(context).colorScheme.onSurfaceVariant,
                          ),
                    ),
            ),
          ],
        ),
        const SizedBox(height: AppSpacing.sm),
        Align(
          alignment: Alignment.centerRight,
          child: TextButton.icon(
            onPressed: _startEdit,
            icon: const Icon(Icons.edit_outlined, size: 18),
            label: const Text('Editar'),
          ),
        ),
      ],
    );
  }
}

/// Renders a full address as a compact multi-line text block.
class _AddressBlock extends StatelessWidget {
  const _AddressBlock({required this.address});

  final Address address;

  @override
  Widget build(BuildContext context) {
    final tt = Theme.of(context).textTheme;
    final cs = Theme.of(context).colorScheme;
    final secondary = tt.bodySmall?.copyWith(color: cs.onSurfaceVariant);
    final lines = address.formattedDisplay.split('\n');

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        for (int i = 0; i < lines.length; i++) ...[
          if (i > 0) const SizedBox(height: 2),
          Text(
            lines[i],
            style: i == 0
                ? tt.bodyMedium?.copyWith(fontWeight: FontWeight.w500)
                : secondary,
          ),
        ],
      ],
    );
  }
}

/// The address edit form, extracted to a [StatelessWidget] to keep
/// [_AddressSectionState] readable.
class _AddressEditForm extends StatelessWidget {
  const _AddressEditForm({
    required this.formKey,
    required this.zipCodeController,
    required this.streetController,
    required this.numberController,
    required this.complementController,
    required this.neighborhoodController,
    required this.cityController,
    required this.stateController,
    required this.countryController,
    required this.cepMask,
    required this.isSaving,
    required this.onSave,
    required this.onCancel,
    required this.validateCep,
    required this.validateRequired,
    required this.validateState,
  });

  final GlobalKey<FormState> formKey;
  final TextEditingController zipCodeController;
  final TextEditingController streetController;
  final TextEditingController numberController;
  final TextEditingController complementController;
  final TextEditingController neighborhoodController;
  final TextEditingController cityController;
  final TextEditingController stateController;
  final TextEditingController countryController;
  final MaskTextInputFormatter cepMask;
  final bool isSaving;
  final VoidCallback onSave;
  final VoidCallback onCancel;
  final String? Function(String?) validateCep;
  final String? Function(String?, String) validateRequired;
  final String? Function(String?) validateState;

  @override
  Widget build(BuildContext context) {
    return Form(
      key: formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // ── CEP ──────────────────────────────────────────────────────────
          TextFormField(
            controller: zipCodeController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'CEP *',
              prefixIcon: Icon(Icons.markunread_mailbox_outlined),
              border: OutlineInputBorder(),
              helperText: 'Ex: 01310-100',
            ),
            keyboardType: TextInputType.number,
            inputFormatters: [cepMask],
            validator: validateCep,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Logradouro ───────────────────────────────────────────────────
          TextFormField(
            controller: streetController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Logradouro *',
              prefixIcon: Icon(Icons.signpost_outlined),
              border: OutlineInputBorder(),
              hintText: 'Rua, Avenida, Travessa…',
            ),
            textCapitalization: TextCapitalization.words,
            validator: (v) => validateRequired(v, 'Logradouro'),
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Número + Complemento ─────────────────────────────────────────
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                flex: 2,
                child: TextFormField(
                  controller: numberController,
                  enabled: !isSaving,
                  decoration: const InputDecoration(
                    labelText: 'Número *',
                    border: OutlineInputBorder(),
                  ),
                  keyboardType: TextInputType.text,
                  validator: (v) => validateRequired(v, 'Número'),
                ),
              ),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                flex: 3,
                child: TextFormField(
                  controller: complementController,
                  enabled: !isSaving,
                  decoration: const InputDecoration(
                    labelText: 'Complemento',
                    border: OutlineInputBorder(),
                    hintText: 'Apto, Sala…',
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Bairro ───────────────────────────────────────────────────────
          TextFormField(
            controller: neighborhoodController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Bairro',
              prefixIcon: Icon(Icons.holiday_village_outlined),
              border: OutlineInputBorder(),
            ),
            textCapitalization: TextCapitalization.words,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Cidade + Estado ──────────────────────────────────────────────
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                flex: 3,
                child: TextFormField(
                  controller: cityController,
                  enabled: !isSaving,
                  decoration: const InputDecoration(
                    labelText: 'Cidade *',
                    border: OutlineInputBorder(),
                  ),
                  textCapitalization: TextCapitalization.words,
                  validator: (v) => validateRequired(v, 'Cidade'),
                ),
              ),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                flex: 2,
                child: TextFormField(
                  controller: stateController,
                  enabled: !isSaving,
                  decoration: const InputDecoration(
                    labelText: 'UF',
                    border: OutlineInputBorder(),
                    hintText: 'SP',
                    counterText: '',
                  ),
                  textCapitalization: TextCapitalization.characters,
                  maxLength: 2,
                  validator: validateState,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.md),

          // ── País ─────────────────────────────────────────────────────────
          TextFormField(
            controller: countryController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'País',
              prefixIcon: Icon(Icons.public_outlined),
              border: OutlineInputBorder(),
              hintText: 'Brasil',
            ),
            textCapitalization: TextCapitalization.words,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Actions ──────────────────────────────────────────────────────
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              TextButton(
                onPressed: isSaving ? null : onCancel,
                child: const Text('Cancelar'),
              ),
              const SizedBox(width: AppSpacing.sm),
              isSaving
                  ? const SizedBox(
                      width: 24,
                      height: 24,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : FilledButton(
                      onPressed: onSave,
                      child: const Text('Salvar'),
                    ),
            ],
          ),
        ],
      ),
    );
  }
}
