import 'package:flutter/material.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../../domain/tenant.dart';
import '../../../tenant_permissions.dart';
import '../tenant_detail_viewmodel.dart';
import 'section_edit_actions.dart';

/// The address, read then edited in place, with CEP autofill.
class AddressSection extends StatefulWidget {
  /// Creates the section.
  const AddressSection({super.key, required this.viewModel});

  /// Drives the section.
  final TenantDetailViewModel viewModel;

  @override
  State<AddressSection> createState() => _AddressSectionState();
}

class _AddressSectionState extends State<AddressSection> {
  final _formKey = GlobalKey<FormState>();
  final _zipController = TextEditingController();
  final _streetController = TextEditingController();
  final _numberController = TextEditingController();
  final _complementController = TextEditingController();
  final _neighborhoodController = TextEditingController();
  final _cityController = TextEditingController();
  final _stateController = TextEditingController();

  final _zipMask = MaskTextInputFormatter(
    mask: '#####-###',
    filter: {'#': RegExp(r'[0-9]')},
    type: MaskAutoCompletionType.lazy,
  );

  bool _isEditing = false;
  bool _isLookingUpCep = false;

  @override
  void dispose() {
    _zipController.dispose();
    _streetController.dispose();
    _numberController.dispose();
    _complementController.dispose();
    _neighborhoodController.dispose();
    _cityController.dispose();
    _stateController.dispose();
    super.dispose();
  }

  void _startEdit(TenantAddress address) {
    final digits = address.zipCode.replaceAll(RegExp(r'[^\d]'), '');
    _zipMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: digits),
    );
    _zipController.text = digits.isEmpty ? '' : _zipMask.getMaskedText();
    _streetController.text = address.street;
    _numberController.text = address.number;
    _complementController.text = address.complement;
    _neighborhoodController.text = address.neighborhood;
    _cityController.text = address.city;
    _stateController.text = address.state;
    setState(() => _isEditing = true);
  }

  void _cancel() => setState(() => _isEditing = false);

  Future<void> _lookupCep() async {
    final digits = _zipController.text.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 8) return;

    setState(() => _isLookingUpCep = true);
    final lookup = await widget.viewModel.lookupCep(digits);
    if (!mounted) return;

    if (lookup != null) {
      // Número e complemento não são tocados: o que a pessoa digitou vale
      // mais do que o que o CEP sabe.
      _streetController.text = lookup.street;
      _neighborhoodController.text = lookup.neighborhood;
      _cityController.text = lookup.city;
      _stateController.text = lookup.state;
    }
    setState(() => _isLookingUpCep = false);
  }

  Future<void> _save() async {
    if (_formKey.currentState?.validate() != true) return;

    final saved = await widget.viewModel.saveAddress(
      TenantAddress(
        zipCode: _zipController.text.replaceAll(RegExp(r'[^\d]'), ''),
        street: _streetController.text,
        number: _numberController.text,
        complement: _complementController.text,
        neighborhood: _neighborhoodController.text,
        city: _cityController.text,
        state: _stateController.text.toUpperCase(),
      ),
    );

    if (mounted && saved) setState(() => _isEditing = false);
  }

  @override
  Widget build(BuildContext context) {
    final tenant = widget.viewModel.tenant;
    if (tenant == null) return const SizedBox.shrink();

    final isSaving =
        widget.viewModel.addressStatus == TenantSectionStatus.saving;

    return SectionCard(
      title: 'Endereço',
      child: _isEditing
          ? _EditMode(
              formKey: _formKey,
              isSaving: isSaving,
              isLookingUpCep: _isLookingUpCep,
              zipController: _zipController,
              zipMask: _zipMask,
              streetController: _streetController,
              numberController: _numberController,
              complementController: _complementController,
              neighborhoodController: _neighborhoodController,
              cityController: _cityController,
              stateController: _stateController,
              error: widget.viewModel.addressError,
              onLookupCep: _lookupCep,
              onCancel: _cancel,
              onSave: _save,
            )
          : Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                InfoRow(
                  icon: Symbols.home_pin,
                  label: 'Endereço',
                  value: tenant.address.singleLine,
                ),
                const SizedBox(height: AppSpacing.sm),
                Align(
                  alignment: Alignment.centerRight,
                  child: TenantPermissionGuard(
                    resource: TenantResources.tenant,
                    scope: TenantScopes.edit,
                    child: TextButton.icon(
                      onPressed: widget.viewModel.isFrozen
                          ? null
                          : () => _startEdit(tenant.address),
                      icon: const Icon(Icons.edit_outlined, size: 18),
                      label: const Text('Editar'),
                    ),
                  ),
                ),
              ],
            ),
    );
  }
}

class _EditMode extends StatelessWidget {
  const _EditMode({
    required this.formKey,
    required this.isSaving,
    required this.isLookingUpCep,
    required this.zipController,
    required this.zipMask,
    required this.streetController,
    required this.numberController,
    required this.complementController,
    required this.neighborhoodController,
    required this.cityController,
    required this.stateController,
    required this.error,
    required this.onLookupCep,
    required this.onCancel,
    required this.onSave,
  });

  final GlobalKey<FormState> formKey;
  final bool isSaving;
  final bool isLookingUpCep;
  final TextEditingController zipController;
  final MaskTextInputFormatter zipMask;
  final TextEditingController streetController;
  final TextEditingController numberController;
  final TextEditingController complementController;
  final TextEditingController neighborhoodController;
  final TextEditingController cityController;
  final TextEditingController stateController;
  final String? error;
  final Future<void> Function() onLookupCep;
  final VoidCallback onCancel;
  final Future<void> Function() onSave;

  @override
  Widget build(BuildContext context) {
    return Form(
      key: formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          TextFormField(
            controller: zipController,
            enabled: !isSaving,
            decoration: InputDecoration(
              labelText: 'CEP',
              border: const OutlineInputBorder(),
              suffixIcon: isLookingUpCep
                  ? const Padding(
                      padding: EdgeInsets.all(AppSpacing.sm),
                      child: SizedBox(
                        width: 20,
                        height: 20,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      ),
                    )
                  : IconButton(
                      tooltip: 'Buscar endereço pelo CEP',
                      icon: const Icon(Icons.search),
                      onPressed: onLookupCep,
                    ),
            ),
            keyboardType: TextInputType.number,
            inputFormatters: [zipMask],
            validator: Tenant.validateZipCode,
            onEditingComplete: onLookupCep,
          ),
          const SizedBox(height: AppSpacing.md),
          TextFormField(
            controller: streetController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Logradouro',
              border: OutlineInputBorder(),
            ),
            validator: Tenant.validateRequired,
          ),
          const SizedBox(height: AppSpacing.md),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: TextFormField(
                  controller: numberController,
                  enabled: !isSaving,
                  decoration: const InputDecoration(
                    labelText: 'Número',
                    border: OutlineInputBorder(),
                  ),
                  validator: Tenant.validateRequired,
                ),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                flex: 2,
                child: TextFormField(
                  controller: complementController,
                  enabled: !isSaving,
                  decoration: const InputDecoration(
                    labelText: 'Complemento (opcional)',
                    border: OutlineInputBorder(),
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.md),
          TextFormField(
            controller: neighborhoodController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Bairro',
              border: OutlineInputBorder(),
            ),
            validator: Tenant.validateRequired,
          ),
          const SizedBox(height: AppSpacing.md),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                flex: 3,
                child: TextFormField(
                  controller: cityController,
                  enabled: !isSaving,
                  decoration: const InputDecoration(
                    labelText: 'Cidade',
                    border: OutlineInputBorder(),
                  ),
                  validator: Tenant.validateRequired,
                ),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: TextFormField(
                  controller: stateController,
                  enabled: !isSaving,
                  textCapitalization: TextCapitalization.characters,
                  maxLength: 2,
                  decoration: const InputDecoration(
                    labelText: 'UF',
                    border: OutlineInputBorder(),
                    counterText: '',
                  ),
                  validator: Tenant.validateState,
                ),
              ),
            ],
          ),
          SectionEditActions(
            isSaving: isSaving,
            error: error,
            onCancel: onCancel,
            onSave: onSave,
          ),
        ],
      ),
    );
  }
}
