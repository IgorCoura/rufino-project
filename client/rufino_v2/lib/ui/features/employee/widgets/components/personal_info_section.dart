import 'package:flutter/material.dart';

import '../../../../../core/theme/app_spacing.dart';
import '../../../../../domain/entities/employee_personal_info.dart';
import '../../../../../domain/entities/personal_info_options.dart';
import '../../../../../domain/entities/selection_option.dart';
import '../../viewmodel/employee_profile_viewmodel.dart';
import 'profile_shared_widgets.dart';
import '../../../../core/widgets/permission_guard.dart';

/// Expandable card for viewing and editing employee personal information.
class PersonalInfoSection extends StatefulWidget {
  const PersonalInfoSection({super.key, required this.viewModel});

  final EmployeeProfileViewModel viewModel;

  @override
  State<PersonalInfoSection> createState() => _PersonalInfoSectionState();
}

class _PersonalInfoSectionState extends State<PersonalInfoSection> {
  String? _selectedGenderId;
  String? _selectedMaritalStatusId;
  String? _selectedEthnicityId;
  String? _selectedEducationLevelId;
  List<String> _selectedDisabilityIds = [];
  final _observationController = TextEditingController();
  final _formKey = GlobalKey<FormState>();
  bool _isEditing = false;

  @override
  void dispose() {
    _observationController.dispose();
    super.dispose();
  }

  void _startEdit() {
    final info = widget.viewModel.personalInfo;
    final options = widget.viewModel.personalInfoOptions;
    if (info == null || options == null) return;

    // Validate each stored ID against the loaded options list so the
    // dropdowns never receive a value that is absent from their items.
    _selectedGenderId = SelectionOption.safeId(info.genderId, options.genders);
    _selectedMaritalStatusId =
        SelectionOption.safeId(info.maritalStatusId, options.maritalStatuses);
    _selectedEthnicityId =
        SelectionOption.safeId(info.ethnicityId, options.ethnicities);
    _selectedEducationLevelId =
        SelectionOption.safeId(info.educationLevelId, options.educationLevels);
    _selectedDisabilityIds = info.disabilityIds
        .where((id) => options.disabilities.any((o) => o.id == id))
        .toList();
    _observationController.text = info.disabilityObservation;
    setState(() => _isEditing = true);
  }

  Future<void> _save() async {
    if (_formKey.currentState?.validate() != true) return;
    final personalInfo = EmployeePersonalInfo(
      genderId: _selectedGenderId ?? '',
      maritalStatusId: _selectedMaritalStatusId ?? '',
      ethnicityId: _selectedEthnicityId ?? '',
      educationLevelId: _selectedEducationLevelId ?? '',
      disabilityIds: _selectedDisabilityIds,
      disabilityObservation: _observationController.text.trim(),
    );
    await widget.viewModel.savePersonalInfo(personalInfo);
    if (mounted &&
        widget.viewModel.personalInfoStatus == SectionLoadStatus.loaded) {
      setState(() => _isEditing = false);
    }
  }

  void _cancel() => setState(() => _isEditing = false);

  /// Adds [id] to the selected disability list if not already present.
  void _addDisability(String id) {
    if (!_selectedDisabilityIds.contains(id)) {
      setState(() => _selectedDisabilityIds.add(id));
    }
  }

  /// Removes [id] from the selected disability list.
  void _removeDisability(String id) =>
      setState(() => _selectedDisabilityIds.remove(id));

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.personalInfoStatus;
        return SectionCard(
          title: 'Informações Pessoais',
          onLoad: widget.viewModel.loadPersonalInfo,
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
              child:
                  Text('Não foi possível carregar as informações pessoais.'),
            ),
          ],
        ),
      );
    }

    final info = widget.viewModel.personalInfo;
    final options = widget.viewModel.personalInfoOptions;
    final isSaving = status == SectionLoadStatus.saving;

    if (_isEditing && options != null) {
      return _PersonalInfoEditForm(
        formKey: _formKey,
        selectedGenderId: _selectedGenderId,
        selectedMaritalStatusId: _selectedMaritalStatusId,
        selectedEthnicityId: _selectedEthnicityId,
        selectedEducationLevelId: _selectedEducationLevelId,
        selectedDisabilityIds: _selectedDisabilityIds,
        observationController: _observationController,
        options: options,
        isSaving: isSaving,
        onGenderChanged: (v) => setState(() => _selectedGenderId = v),
        onMaritalStatusChanged: (v) =>
            setState(() => _selectedMaritalStatusId = v),
        onEthnicityChanged: (v) => setState(() => _selectedEthnicityId = v),
        onEducationLevelChanged: (v) =>
            setState(() => _selectedEducationLevelId = v),
        onAddDisability: _addDisability,
        onRemoveDisability: _removeDisability,
        onSave: _save,
        onCancel: _cancel,
      );
    }

    // ── View mode ────────────────────────────────────────────────────────────
    final genderLabel = options != null
        ? options.genderLabel(info?.genderId ?? '')
        : 'Não informado';
    final maritalLabel = options != null
        ? options.maritalStatusLabel(info?.maritalStatusId ?? '')
        : 'Não informado';
    final ethnicityLabel = options != null
        ? options.ethnicityLabel(info?.ethnicityId ?? '')
        : 'Não informado';
    final educationLabel = options != null
        ? options.educationLevelLabel(info?.educationLevelId ?? '')
        : 'Não informado';

    final disabilityLabels = (info?.disabilityIds.isNotEmpty == true &&
            options != null)
        ? info!.disabilityIds
            .map((id) => options.disabilityLabel(id))
            .toList()
        : <String>[];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        ContactInfoRow(
          icon: Icons.wc_outlined,
          label: 'Gênero',
          value: genderLabel,
        ),
        const Divider(height: AppSpacing.xl),
        ContactInfoRow(
          icon: Icons.favorite_border,
          label: 'Estado Civil',
          value: maritalLabel,
        ),
        const Divider(height: AppSpacing.xl),
        ContactInfoRow(
          icon: Icons.people_outline,
          label: 'Etnia',
          value: ethnicityLabel,
        ),
        const Divider(height: AppSpacing.xl),
        ContactInfoRow(
          icon: Icons.school_outlined,
          label: 'Escolaridade',
          value: educationLabel,
        ),
        const Divider(height: AppSpacing.xl),
        _PersonalInfoDisabilityRow(
          labels: disabilityLabels,
          observation: info?.disabilityObservation ?? '',
        ),
        const SizedBox(height: AppSpacing.sm),
        Align(
          alignment: Alignment.centerRight,
          child: PermissionGuard(
            resource: 'employee',
            scope: 'edit',
            child: TextButton.icon(
              onPressed: _isEditing
                  ? null
                  : (options != null ? _startEdit : null),
              icon: const Icon(Icons.edit_outlined, size: 18),
              label: const Text('Editar'),
            ),
          ),
        ),
      ],
    );
  }
}

// ─── Personal info helpers ─────────────────────────────────────────────────

/// View-mode row for the disabilities field with optional chip display.
class _PersonalInfoDisabilityRow extends StatelessWidget {
  const _PersonalInfoDisabilityRow({
    required this.labels,
    required this.observation,
  });

  final List<String> labels;
  final String observation;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final tt = Theme.of(context).textTheme;

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(Icons.accessibility_new_outlined, size: 22, color: cs.primary),
        const SizedBox(width: AppSpacing.md),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Deficiências',
                style: tt.labelSmall?.copyWith(
                  color: cs.onSurfaceVariant,
                  letterSpacing: 0.4,
                ),
              ),
              const SizedBox(height: AppSpacing.xs),
              if (labels.isEmpty)
                Text('Nenhuma declarada', style: tt.bodyMedium)
              else ...[
                Wrap(
                  spacing: AppSpacing.xs,
                  runSpacing: AppSpacing.xs,
                  children: labels
                      .map(
                        (l) => Chip(
                          label: Text(l),
                          padding: EdgeInsets.zero,
                          materialTapTargetSize:
                              MaterialTapTargetSize.shrinkWrap,
                          visualDensity: VisualDensity.compact,
                        ),
                      )
                      .toList(),
                ),
                if (observation.isNotEmpty) ...[
                  const SizedBox(height: AppSpacing.xs),
                  Text(observation,
                      style: tt.bodySmall
                          ?.copyWith(color: cs.onSurfaceVariant)),
                ],
              ],
            ],
          ),
        ),
      ],
    );
  }
}

/// Edit form for the personal info section, extracted to keep
/// [_PersonalInfoSectionState] concise.
///
/// Wraps all fields in a [Form] with validators for dropdowns (required) and
/// the observation field (max 100 characters). Disabilities are managed
/// via an add-dialog / remove-button pattern matching the rufino app UX.
class _PersonalInfoEditForm extends StatelessWidget {
  const _PersonalInfoEditForm({
    required this.formKey,
    required this.selectedGenderId,
    required this.selectedMaritalStatusId,
    required this.selectedEthnicityId,
    required this.selectedEducationLevelId,
    required this.selectedDisabilityIds,
    required this.observationController,
    required this.options,
    required this.isSaving,
    required this.onGenderChanged,
    required this.onMaritalStatusChanged,
    required this.onEthnicityChanged,
    required this.onEducationLevelChanged,
    required this.onAddDisability,
    required this.onRemoveDisability,
    required this.onSave,
    required this.onCancel,
  });

  final GlobalKey<FormState> formKey;
  final String? selectedGenderId;
  final String? selectedMaritalStatusId;
  final String? selectedEthnicityId;
  final String? selectedEducationLevelId;
  final List<String> selectedDisabilityIds;
  final TextEditingController observationController;
  final PersonalInfoOptions options;
  final bool isSaving;
  final ValueChanged<String?> onGenderChanged;
  final ValueChanged<String?> onMaritalStatusChanged;
  final ValueChanged<String?> onEthnicityChanged;
  final ValueChanged<String?> onEducationLevelChanged;

  /// Called with the id of the disability the user wants to add.
  final ValueChanged<String> onAddDisability;

  /// Called with the id of the disability the user wants to remove.
  final ValueChanged<String> onRemoveDisability;
  final VoidCallback onSave;
  final VoidCallback onCancel;

  /// Shows a dialog with a dropdown of unselected disabilities so the user
  /// can pick one to add.
  void _showAddDisabilityDialog(BuildContext context) {
    final available = options.disabilities
        .where((o) => !selectedDisabilityIds.contains(o.id))
        .toList();
    if (available.isEmpty) return;

    SelectionOption? chosen = available.first;
    showDialog<void>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (ctx, setDialogState) => AlertDialog(
          title: const Text('Adicionar Deficiência'),
          content: SizedBox(
            width: 400,
            child: DropdownButtonFormField<SelectionOption>(
              initialValue: chosen,
              isExpanded: true,
              decoration: const InputDecoration(
                labelText: 'Deficiência',
                border: OutlineInputBorder(),
              ),
              items: available
                  .map((o) => DropdownMenuItem(value: o, child: Text(o.name)))
                  .toList(),
              onChanged: (v) => setDialogState(() => chosen = v),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(dialogContext).pop(),
              child: const Text('Cancelar'),
            ),
            FilledButton(
              onPressed: () {
                if (chosen != null) onAddDisability(chosen!.id);
                Navigator.of(dialogContext).pop();
              },
              child: const Text('Adicionar'),
            ),
          ],
        ),
      ),
    );
  }

  /// Builds a required dropdown field for a single enum property.
  DropdownButtonFormField<String> _dropdown({
    required String label,
    required IconData icon,
    required String? value,
    required List<SelectionOption> optionList,
    required ValueChanged<String?> onChanged,
  }) {
    return DropdownButtonFormField<String>(
      // Guard: pass null when the stored id is absent from the option list
      // to avoid Flutter's "value must be in items" assertion.
      initialValue: SelectionOption.safeId(value, optionList),
      decoration: InputDecoration(
        labelText: label,
        prefixIcon: Icon(icon),
        border: const OutlineInputBorder(),
      ),
      hint: const Text('Não informado'),
      isExpanded: true,
      items: optionList
          .map((o) => DropdownMenuItem(value: o.id, child: Text(o.name)))
          .toList(),
      onChanged: isSaving ? null : onChanged,
      validator: (v) =>
          (v == null || v.isEmpty) ? 'Por favor, selecione uma opção.' : null,
    );
  }

  @override
  Widget build(BuildContext context) {
    final tt = Theme.of(context).textTheme;
    final cs = Theme.of(context).colorScheme;

    return Form(
      key: formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // ── Gênero ──────────────────────────────────────────────────────
          _dropdown(
            label: 'Gênero',
            icon: Icons.wc_outlined,
            value: selectedGenderId,
            optionList: options.genders,
            onChanged: onGenderChanged,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Estado Civil ────────────────────────────────────────────────
          _dropdown(
            label: 'Estado Civil',
            icon: Icons.favorite_border,
            value: selectedMaritalStatusId,
            optionList: options.maritalStatuses,
            onChanged: onMaritalStatusChanged,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Etnia ────────────────────────────────────────────────────────
          _dropdown(
            label: 'Etnia',
            icon: Icons.people_outline,
            value: selectedEthnicityId,
            optionList: options.ethnicities,
            onChanged: onEthnicityChanged,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Escolaridade ────────────────────────────────────────────────
          _dropdown(
            label: 'Escolaridade',
            icon: Icons.school_outlined,
            value: selectedEducationLevelId,
            optionList: options.educationLevels,
            onChanged: onEducationLevelChanged,
          ),
          const SizedBox(height: AppSpacing.lg),

          // ── Deficiências ─────────────────────────────────────────────────
          Row(
            children: [
              Icon(
                Icons.accessibility_new_outlined,
                size: 20,
                color: cs.onSurfaceVariant,
              ),
              const SizedBox(width: AppSpacing.sm),
              Text(
                'Deficiências',
                style: tt.labelLarge?.copyWith(color: cs.onSurfaceVariant),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.sm),
          if (selectedDisabilityIds.isEmpty)
            Padding(
              padding: const EdgeInsets.only(bottom: AppSpacing.sm),
              child: Text(
                'Nenhuma declarada',
                style: tt.bodyMedium?.copyWith(color: cs.onSurfaceVariant),
              ),
            )
          else
            ...selectedDisabilityIds.map((id) {
              final name = options.disabilityLabel(id);
              return Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.xs),
                child: Row(
                  children: [
                    Expanded(child: Text(name, style: tt.bodyMedium)),
                    IconButton(
                      icon: Icon(Icons.close, size: 18, color: cs.error),
                      tooltip: 'Remover deficiência',
                      visualDensity: VisualDensity.compact,
                      onPressed:
                          isSaving ? null : () => onRemoveDisability(id),
                    ),
                  ],
                ),
              );
            }),
          const SizedBox(height: AppSpacing.sm),
          OutlinedButton.icon(
            onPressed: isSaving ||
                    selectedDisabilityIds.length == options.disabilities.length
                ? null
                : () => _showAddDisabilityDialog(context),
            icon: const Icon(Icons.add, size: 18),
            label: const Text('Adicionar Deficiência'),
          ),

          // Observation — shown only when at least one disability is selected.
          if (selectedDisabilityIds.isNotEmpty) ...[
            const SizedBox(height: AppSpacing.md),
            TextFormField(
              controller: observationController,
              enabled: !isSaving,
              decoration: const InputDecoration(
                labelText: 'Observações sobre a deficiência',
                prefixIcon: Icon(Icons.notes_outlined),
                border: OutlineInputBorder(),
                alignLabelWithHint: true,
                helperText: 'Máximo 100 caracteres',
                counterText: '',
              ),
              maxLines: 3,
              minLines: 2,
              maxLength: 100,
              textCapitalization: TextCapitalization.sentences,
              validator: (v) => (v != null && v.length > 100)
                  ? 'Observação não pode ultrapassar 100 caracteres.'
                  : null,
            ),
          ],
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
