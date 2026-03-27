import 'package:flutter/material.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';

import '../../../../../core/theme/app_spacing.dart';
import '../../../../../domain/entities/employee_dependent.dart';
import '../../../../../domain/entities/selection_option.dart';
import '../../viewmodel/employee_profile_viewmodel.dart';
import 'profile_shared_widgets.dart';

/// Expandable card for listing, creating, editing, and removing employee
/// dependents (Dependentes).
class DependentSection extends StatefulWidget {
  const DependentSection({super.key, required this.viewModel});

  /// The profile view model that manages dependent data and validation.
  final EmployeeProfileViewModel viewModel;

  @override
  State<DependentSection> createState() => _DependentSectionState();
}

class _DependentSectionState extends State<DependentSection> {
  final _formKey = GlobalKey<FormState>();

  // ─── Text controllers ────────────────────────────────────────────────────

  final _nameCtrl = TextEditingController();
  final _cpfCtrl = TextEditingController();
  final _motherNameCtrl = TextEditingController();
  final _fatherNameCtrl = TextEditingController();
  final _dobCtrl = TextEditingController();
  final _birthCityCtrl = TextEditingController();
  final _birthStateCtrl = TextEditingController();
  final _nationalityCtrl = TextEditingController();

  // ─── Mask formatters ─────────────────────────────────────────────────────

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

  // ─── Editing state ───────────────────────────────────────────────────────

  /// Which dependent index is being edited.
  ///
  /// `null` means view mode, `-1` means adding a new dependent, and `>= 0`
  /// means editing the dependent at that index.
  int? _editingIndex;

  /// The currently selected gender id in the form.
  String? _selectedGenderId;

  /// The currently selected dependency type id in the form.
  String? _selectedTypeId;

  @override
  void dispose() {
    _nameCtrl.dispose();
    _cpfCtrl.dispose();
    _motherNameCtrl.dispose();
    _fatherNameCtrl.dispose();
    _dobCtrl.dispose();
    _birthCityCtrl.dispose();
    _birthStateCtrl.dispose();
    _nationalityCtrl.dispose();
    super.dispose();
  }

  // ─── Helpers ─────────────────────────────────────────────────────────────

  /// Resolves a [genderId] to its display name using the loaded personal info
  /// options. Falls back to the raw id when options are not available.
  String _resolveGenderName(String genderId) {
    final options = widget.viewModel.personalInfoOptions;
    if (options == null) return genderId;
    return options.genderLabel(genderId);
  }

  // ─── Form population ────────────────────────────────────────────────────

  /// Populates the form fields with [dependent] data for editing.
  void _populateForm(EmployeeDependent dependent) {
    _nameCtrl.text = dependent.name;

    final rawCpf = dependent.cpf.replaceAll(RegExp(r'[^\d]'), '');
    _cpfMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: rawCpf),
    );
    _cpfCtrl.text = _cpfMask.getMaskedText();

    _motherNameCtrl.text = dependent.motherName;
    _fatherNameCtrl.text = dependent.fatherName;

    final rawDob = dependent.dateOfBirth.replaceAll(RegExp(r'[^\d]'), '');
    _dobMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: rawDob),
    );
    _dobCtrl.text = _dobMask.getMaskedText();

    _birthCityCtrl.text = dependent.birthCity;
    _birthStateCtrl.text = dependent.birthState;
    _nationalityCtrl.text = dependent.nationality;
    _selectedGenderId = dependent.genderId;
    _selectedTypeId = dependent.dependencyTypeId;
  }

  /// Clears all form fields and resets dropdown selections.
  void _clearForm() {
    _nameCtrl.clear();
    _cpfCtrl.clear();
    _motherNameCtrl.clear();
    _fatherNameCtrl.clear();
    _dobCtrl.clear();
    _birthCityCtrl.clear();
    _birthStateCtrl.clear();
    _nationalityCtrl.clear();
    _cpfMask.clear();
    _dobMask.clear();
    _selectedGenderId = null;
    _selectedTypeId = null;
  }

  // ─── Actions ─────────────────────────────────────────────────────────────

  /// Starts editing an existing dependent at [index].
  void _startEdit(int index) {
    final dependent = widget.viewModel.dependents[index];
    _populateForm(dependent);
    setState(() => _editingIndex = index);
  }

  /// Starts adding a new dependent.
  void _startAdd() {
    _clearForm();
    setState(() => _editingIndex = -1);
  }

  /// Cancels the current add/edit operation.
  void _cancel() {
    setState(() => _editingIndex = null);
  }

  /// Validates the form and saves the dependent (create or edit).
  Future<void> _save() async {
    if (_formKey.currentState?.validate() != true) return;

    final originalName = _editingIndex != null && _editingIndex! >= 0
        ? widget.viewModel.dependents[_editingIndex!].originalName
        : '';

    final dependent = EmployeeDependent(
      originalName: originalName,
      name: _nameCtrl.text.trim(),
      genderId: _selectedGenderId ?? '',
      dependencyTypeId: _selectedTypeId ?? '',
      cpf: _cpfCtrl.text.trim(),
      motherName: _motherNameCtrl.text.trim(),
      fatherName: _fatherNameCtrl.text.trim(),
      dateOfBirth: _dobCtrl.text.trim(),
      birthCity: _birthCityCtrl.text.trim(),
      birthState: _birthStateCtrl.text.trim(),
      nationality: _nationalityCtrl.text.trim(),
    );

    if (_editingIndex != null && _editingIndex! >= 0) {
      await widget.viewModel.editDependentData(dependent);
    } else {
      await widget.viewModel.createDependent(dependent);
    }

    if (mounted &&
        widget.viewModel.dependentsStatus != SectionLoadStatus.error) {
      setState(() => _editingIndex = null);
    }
  }

  /// Removes the dependent identified by [dependentName].
  Future<void> _remove(String dependentName) async {
    await widget.viewModel.removeDependent(dependentName);
  }

  // ─── Build ───────────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.dependentsStatus;
        return ExpandableSectionCard(
          title: 'Dependentes',
          onExpand: widget.viewModel.loadDependents,
          child: Padding(
            padding: const EdgeInsets.all(8.0),
            child: _buildContent(context, status),
          ),
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
              child: Text('Não foi possível carregar os dependentes.'),
            ),
          ],
        ),
      );
    }

    final dependents = widget.viewModel.dependents;
    final isSaving = status == SectionLoadStatus.saving;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (dependents.isEmpty && _editingIndex == null)
          const Padding(
            padding: EdgeInsets.symmetric(vertical: AppSpacing.sm),
            child: Text('Nenhum dependente cadastrado.'),
          ),
        for (int i = 0; i < dependents.length; i++)
          if (_editingIndex == i)
            _buildForm(context, isSaving)
          else
            _buildDependentCard(context, dependents[i], i, isSaving),
        if (_editingIndex == -1) _buildForm(context, isSaving),
        if (_editingIndex == null)
          Padding(
            padding: const EdgeInsets.only(top: AppSpacing.sm),
            child: Align(
              alignment: Alignment.centerRight,
              child: FilledButton.tonalIcon(
                onPressed: isSaving ? null : _startAdd,
                icon: const Icon(Icons.add),
                label: const Text('Adicionar Dependente'),
              ),
            ),
          ),
      ],
    );
  }

  // ─── View mode card for a single dependent ──────────────────────────────

  Widget _buildDependentCard(
    BuildContext context,
    EmployeeDependent dependent,
    int index,
    bool isSaving,
  ) {
    final cs = Theme.of(context).colorScheme;

    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: Card.outlined(
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              ContactInfoRow(
                icon: Icons.person_outline,
                label: 'Nome',
                value: dependent.name,
              ),
              const SizedBox(height: AppSpacing.xs),
              ContactInfoRow(
                icon: Icons.wc_outlined,
                label: 'Sexo',
                value: _resolveGenderName(dependent.genderId),
              ),
              const SizedBox(height: AppSpacing.xs),
              ContactInfoRow(
                icon: Icons.family_restroom_outlined,
                label: 'Tipo de dependência',
                value: dependent.dependencyTypeLabel,
              ),
              if (dependent.cpf.isNotEmpty) ...[
                const SizedBox(height: AppSpacing.xs),
                ContactInfoRow(
                  icon: Icons.badge_outlined,
                  label: 'CPF',
                  value: dependent.cpf,
                ),
              ],
              const SizedBox(height: AppSpacing.sm),
              Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  TextButton.icon(
                    onPressed: isSaving ? null : () => _startEdit(index),
                    icon: const Icon(Icons.edit_outlined, size: 18),
                    label: const Text('Editar'),
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  TextButton.icon(
                    onPressed: isSaving
                        ? null
                        : () => _remove(dependent.originalName),
                    icon: Icon(Icons.delete_outline, size: 18, color: cs.error),
                    label: Text(
                      'Remover',
                      style: TextStyle(color: cs.error),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  // ─── Add/Edit form ──────────────────────────────────────────────────────

  Widget _buildForm(BuildContext context, bool isSaving) {
    final genders = widget.viewModel.personalInfoOptions?.genders ?? [];
    const typeOptions = EmployeeProfileViewModel.dependencyTypeOptions;

    return Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          TextFormField(
            controller: _nameCtrl,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Nome',
              prefixIcon: Icon(Icons.person_outline),
              border: OutlineInputBorder(),
            ),
            validator: widget.viewModel.validateDependentName,
          ),
          const SizedBox(height: AppSpacing.md),
          DropdownButtonFormField<String>(
            initialValue: _selectedGenderId,
            decoration: const InputDecoration(
              labelText: 'Sexo',
              prefixIcon: Icon(Icons.wc_outlined),
              border: OutlineInputBorder(),
            ),
            items: genders
                .map<DropdownMenuItem<String>>((g) => DropdownMenuItem<String>(
                      value: g.id,
                      child: Text(g.name),
                    ))
                .toList(),
            validator: (value) {
              if (value == null || value.isEmpty) {
                return 'Selecione o sexo.';
              }
              return null;
            },
            onChanged: isSaving
                ? null
                : (value) => setState(() => _selectedGenderId = value),
          ),
          const SizedBox(height: AppSpacing.md),
          DropdownButtonFormField<String>(
            initialValue: _selectedTypeId,
            decoration: const InputDecoration(
              labelText: 'Tipo de dependência',
              prefixIcon: Icon(Icons.family_restroom_outlined),
              border: OutlineInputBorder(),
            ),
            items: typeOptions
                .map<DropdownMenuItem<String>>(
                    (SelectionOption opt) => DropdownMenuItem<String>(
                          value: opt.id,
                          child: Text(opt.name),
                        ))
                .toList(),
            validator: (value) {
              if (value == null || value.isEmpty) {
                return 'Selecione o tipo de dependência.';
              }
              return null;
            },
            onChanged: isSaving
                ? null
                : (value) => setState(() => _selectedTypeId = value),
          ),
          const SizedBox(height: AppSpacing.md),
          TextFormField(
            controller: _cpfCtrl,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'CPF',
              prefixIcon: Icon(Icons.badge_outlined),
              border: OutlineInputBorder(),
              helperText: 'Ex: 123.456.789-00',
            ),
            keyboardType: TextInputType.number,
            inputFormatters: [_cpfMask],
            validator: widget.viewModel.validateCpf,
          ),
          const SizedBox(height: AppSpacing.md),
          TextFormField(
            controller: _motherNameCtrl,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Nome da mãe',
              prefixIcon: Icon(Icons.person_outline),
              border: OutlineInputBorder(),
            ),
            validator: widget.viewModel.validateMotherName,
          ),
          const SizedBox(height: AppSpacing.md),
          TextFormField(
            controller: _fatherNameCtrl,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Nome do pai',
              prefixIcon: Icon(Icons.person_outline),
              border: OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: AppSpacing.md),
          TextFormField(
            controller: _dobCtrl,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Data de nascimento',
              prefixIcon: Icon(Icons.cake_outlined),
              border: OutlineInputBorder(),
              helperText: 'Ex: 15/03/2000',
            ),
            keyboardType: TextInputType.number,
            inputFormatters: [_dobMask],
            validator: widget.viewModel.validateDateOfBirth,
          ),
          const SizedBox(height: AppSpacing.md),
          TextFormField(
            controller: _birthCityCtrl,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Cidade de nascimento',
              prefixIcon: Icon(Icons.location_city_outlined),
              border: OutlineInputBorder(),
            ),
            validator: widget.viewModel.validateBirthCity,
          ),
          const SizedBox(height: AppSpacing.md),
          TextFormField(
            controller: _birthStateCtrl,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Estado de nascimento',
              prefixIcon: Icon(Icons.map_outlined),
              border: OutlineInputBorder(),
              helperText: 'Ex: SP',
            ),
            maxLength: 2,
            textCapitalization: TextCapitalization.characters,
            validator: widget.viewModel.validateBirthState,
          ),
          const SizedBox(height: AppSpacing.md),
          TextFormField(
            controller: _nationalityCtrl,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Nacionalidade',
              prefixIcon: Icon(Icons.flag_outlined),
              border: OutlineInputBorder(),
            ),
            validator: widget.viewModel.validateNationality,
          ),
          const SizedBox(height: AppSpacing.md),
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              TextButton(
                onPressed: isSaving ? null : _cancel,
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
                      onPressed: _save,
                      child: const Text('Salvar'),
                    ),
            ],
          ),
        ],
      ),
    );
  }
}
