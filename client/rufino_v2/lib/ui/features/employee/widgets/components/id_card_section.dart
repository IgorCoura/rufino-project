import 'package:flutter/material.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';

import '../../../../../core/theme/app_spacing.dart';
import '../../../../../domain/entities/employee_id_card.dart';
import '../../viewmodel/employee_profile_viewmodel.dart';
import 'profile_shared_widgets.dart';

/// Expandable card for viewing and editing employee ID card (Identidade) data.
class IdCardSection extends StatefulWidget {
  const IdCardSection({super.key, required this.viewModel});

  final EmployeeProfileViewModel viewModel;

  @override
  State<IdCardSection> createState() => _IdCardSectionState();
}

class _IdCardSectionState extends State<IdCardSection> {
  final _formKey = GlobalKey<FormState>();
  final _cpfController = TextEditingController();
  final _motherNameController = TextEditingController();
  final _fatherNameController = TextEditingController();
  final _dateOfBirthController = TextEditingController();
  final _birthCityController = TextEditingController();
  final _birthStateController = TextEditingController();
  final _nationalityController = TextEditingController();

  /// CPF mask: `###.###.###-##`.
  final _cpfMask = MaskTextInputFormatter(
    mask: '###.###.###-##',
    filter: {'#': RegExp(r'[0-9]')},
    type: MaskAutoCompletionType.lazy,
  );

  /// Date of birth mask: `##/##/####`.
  final _dobMask = MaskTextInputFormatter(
    mask: '##/##/####',
    filter: {'#': RegExp(r'[0-9]')},
    type: MaskAutoCompletionType.lazy,
  );

  bool _isEditing = false;

  @override
  void dispose() {
    _cpfController.dispose();
    _motherNameController.dispose();
    _fatherNameController.dispose();
    _dateOfBirthController.dispose();
    _birthCityController.dispose();
    _birthStateController.dispose();
    _nationalityController.dispose();
    super.dispose();
  }

  void _startEdit() {
    final idCard = widget.viewModel.idCard;
    if (idCard == null) return;

    // Apply CPF mask to the stored raw digits.
    final rawCpf = idCard.cpf.replaceAll(RegExp(r'[^\d]'), '');
    _cpfMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: rawCpf),
    );
    _cpfController.text = _cpfMask.getMaskedText();

    // Apply date-of-birth mask to the stored raw digits.
    final rawDob = idCard.dateOfBirth.replaceAll(RegExp(r'[^\d]'), '');
    _dobMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: rawDob),
    );
    _dateOfBirthController.text = _dobMask.getMaskedText();

    _motherNameController.text = idCard.motherName;
    _fatherNameController.text = idCard.fatherName;
    _birthCityController.text = idCard.birthCity;
    _birthStateController.text = idCard.birthState.toUpperCase();
    _nationalityController.text = idCard.nationality;
    setState(() => _isEditing = true);
  }

  Future<void> _save() async {
    if (_formKey.currentState?.validate() != true) return;
    final idCard = EmployeeIdCard(
      cpf: _cpfController.text.trim(),
      motherName: _motherNameController.text.trim(),
      fatherName: _fatherNameController.text.trim(),
      dateOfBirth: _dateOfBirthController.text.trim(),
      birthCity: _birthCityController.text.trim(),
      birthState: _birthStateController.text.trim().toUpperCase(),
      nationality: _nationalityController.text.trim(),
    );
    await widget.viewModel.saveIdCard(idCard);
    if (mounted && widget.viewModel.idCardStatus == SectionLoadStatus.loaded) {
      setState(() => _isEditing = false);
    }
  }

  void _cancel() => setState(() => _isEditing = false);

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.idCardStatus;
        return ExpandableSectionCard(
          title: 'Documento (Identidade)',
          onExpand: widget.viewModel.loadIdCard,
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
              child: Text('Não foi possível carregar os dados do documento.'),
            ),
          ],
        ),
      );
    }

    final idCard = widget.viewModel.idCard;
    final isSaving = status == SectionLoadStatus.saving;

    if (_isEditing) {
      return _IdCardEditForm(
        formKey: _formKey,
        cpfController: _cpfController,
        motherNameController: _motherNameController,
        fatherNameController: _fatherNameController,
        dateOfBirthController: _dateOfBirthController,
        birthCityController: _birthCityController,
        birthStateController: _birthStateController,
        nationalityController: _nationalityController,
        cpfMask: _cpfMask,
        dobMask: _dobMask,
        isSaving: isSaving,
        onSave: _save,
        onCancel: _cancel,
        validateCpf: widget.viewModel.validateCpf,
        validateDate: widget.viewModel.validateDateOfBirth,
        validateMotherName: widget.viewModel.validateMotherName,
        validateFatherName: widget.viewModel.validateFatherName,
        validateBirthCity: widget.viewModel.validateBirthCity,
        validateState: widget.viewModel.validateBirthState,
        validateNationality: widget.viewModel.validateNationality,
      );
    }

    // ── View mode ─────────────────────────────────────────────────────────────
    final naturalidade = idCard?.formattedBirthPlace ?? 'Não informado';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        ContactInfoRow(
          icon: Icons.badge_outlined,
          label: 'CPF',
          value: idCard?.cpf.isNotEmpty == true ? idCard!.cpf : 'Não informado',
        ),
        const Divider(height: AppSpacing.xl),
        ContactInfoRow(
          icon: Icons.cake_outlined,
          label: 'Data de nascimento',
          value: idCard?.dateOfBirth.isNotEmpty == true
              ? idCard!.dateOfBirth
              : 'Não informado',
        ),
        const Divider(height: AppSpacing.xl),
        ContactInfoRow(
          icon: Icons.person_outlined,
          label: 'Nome da mãe',
          value: idCard?.motherName.isNotEmpty == true
              ? idCard!.motherName
              : 'Não informado',
        ),
        const Divider(height: AppSpacing.xl),
        ContactInfoRow(
          icon: Icons.person_outline,
          label: 'Nome do pai',
          value: idCard?.fatherName.isNotEmpty == true
              ? idCard!.fatherName
              : 'Não informado',
        ),
        const Divider(height: AppSpacing.xl),
        ContactInfoRow(
          icon: Icons.location_city_outlined,
          label: 'Naturalidade',
          value: naturalidade,
        ),
        const Divider(height: AppSpacing.xl),
        ContactInfoRow(
          icon: Icons.flag_outlined,
          label: 'Nacionalidade',
          value: idCard?.nationality.isNotEmpty == true
              ? idCard!.nationality
              : 'Não informado',
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

/// The ID card edit form, extracted to keep [_IdCardSectionState] readable.
class _IdCardEditForm extends StatelessWidget {
  const _IdCardEditForm({
    required this.formKey,
    required this.cpfController,
    required this.motherNameController,
    required this.fatherNameController,
    required this.dateOfBirthController,
    required this.birthCityController,
    required this.birthStateController,
    required this.nationalityController,
    required this.cpfMask,
    required this.dobMask,
    required this.isSaving,
    required this.onSave,
    required this.onCancel,
    required this.validateCpf,
    required this.validateDate,
    required this.validateMotherName,
    required this.validateFatherName,
    required this.validateBirthCity,
    required this.validateState,
    required this.validateNationality,
  });

  final GlobalKey<FormState> formKey;
  final TextEditingController cpfController;
  final TextEditingController motherNameController;
  final TextEditingController fatherNameController;
  final TextEditingController dateOfBirthController;
  final TextEditingController birthCityController;
  final TextEditingController birthStateController;
  final TextEditingController nationalityController;
  final MaskTextInputFormatter cpfMask;
  final MaskTextInputFormatter dobMask;
  final bool isSaving;
  final VoidCallback onSave;
  final VoidCallback onCancel;
  final String? Function(String?) validateCpf;
  final String? Function(String?) validateDate;
  final String? Function(String?) validateMotherName;
  final String? Function(String?) validateFatherName;
  final String? Function(String?) validateBirthCity;
  final String? Function(String?) validateState;
  final String? Function(String?) validateNationality;

  @override
  Widget build(BuildContext context) {
    return Form(
      key: formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // ── CPF ──────────────────────────────────────────────────────────
          TextFormField(
            controller: cpfController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'CPF',
              prefixIcon: Icon(Icons.badge_outlined),
              border: OutlineInputBorder(),
              helperText: 'Ex: 123.456.789-00',
            ),
            keyboardType: TextInputType.number,
            inputFormatters: [cpfMask],
            validator: validateCpf,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Data de nascimento ────────────────────────────────────────────
          TextFormField(
            controller: dateOfBirthController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Data de nascimento',
              prefixIcon: Icon(Icons.cake_outlined),
              border: OutlineInputBorder(),
              helperText: 'Ex: 15/06/1990',
            ),
            keyboardType: TextInputType.datetime,
            inputFormatters: [dobMask],
            validator: validateDate,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Nome da mãe ───────────────────────────────────────────────────
          TextFormField(
            controller: motherNameController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Nome da mãe',
              prefixIcon: Icon(Icons.person_outlined),
              border: OutlineInputBorder(),
            ),
            textCapitalization: TextCapitalization.words,
            validator: validateMotherName,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Nome do pai ───────────────────────────────────────────────────
          TextFormField(
            controller: fatherNameController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Nome do pai',
              prefixIcon: Icon(Icons.person_outline),
              border: OutlineInputBorder(),
            ),
            textCapitalization: TextCapitalization.words,
            validator: validateFatherName,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Naturalidade ──────────────────────────────────────────────────
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                flex: 3,
                child: TextFormField(
                  controller: birthCityController,
                  enabled: !isSaving,
                  decoration: const InputDecoration(
                    labelText: 'Município de nascimento',
                    prefixIcon: Icon(Icons.location_city_outlined),
                    border: OutlineInputBorder(),
                  ),
                  textCapitalization: TextCapitalization.words,
                  validator: validateBirthCity,
                ),
              ),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                flex: 2,
                child: TextFormField(
                  controller: birthStateController,
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

          // ── Nacionalidade ─────────────────────────────────────────────────
          TextFormField(
            controller: nationalityController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Nacionalidade',
              prefixIcon: Icon(Icons.flag_outlined),
              border: OutlineInputBorder(),
            ),
            textCapitalization: TextCapitalization.words,
            validator: validateNationality,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Actions ───────────────────────────────────────────────────────
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
