import 'package:flutter/material.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';

import '../../../../../core/theme/app_spacing.dart';
import '../../viewmodel/employee_profile_viewmodel.dart';
import 'profile_shared_widgets.dart';

/// Expandable card for viewing and editing the employee medical admission exam
/// (Exame Medico Admissional / ASO).
class MedicalExamSection extends StatefulWidget {
  const MedicalExamSection({super.key, required this.viewModel});

  final EmployeeProfileViewModel viewModel;

  @override
  State<MedicalExamSection> createState() => _MedicalExamSectionState();
}

class _MedicalExamSectionState extends State<MedicalExamSection> {
  final _formKey = GlobalKey<FormState>();
  final _dateExamController = TextEditingController();
  final _validityController = TextEditingController();

  /// Date mask `dd/MM/yyyy` for the exam date field.
  final _dateExamMask = MaskTextInputFormatter(
    mask: '##/##/####',
    filter: {'#': RegExp(r'[0-9]')},
    type: MaskAutoCompletionType.lazy,
  );

  /// Date mask `dd/MM/yyyy` for the validity field.
  final _validityMask = MaskTextInputFormatter(
    mask: '##/##/####',
    filter: {'#': RegExp(r'[0-9]')},
    type: MaskAutoCompletionType.lazy,
  );

  bool _isEditing = false;

  @override
  void dispose() {
    _dateExamController.dispose();
    _validityController.dispose();
    super.dispose();
  }

  void _startEdit() {
    final exam = widget.viewModel.medicalExam;
    if (exam == null) return;

    // Pre-fill exam date with mask.
    final rawDate = exam.dateExam.replaceAll(RegExp(r'[^\d]'), '');
    _dateExamMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: rawDate),
    );
    _dateExamController.text = _dateExamMask.getMaskedText();

    // Pre-fill validity date with mask.
    final rawValidity = exam.validityExam.replaceAll(RegExp(r'[^\d]'), '');
    _validityMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: rawValidity),
    );
    _validityController.text = _validityMask.getMaskedText();

    setState(() => _isEditing = true);
  }

  Future<void> _save() async {
    if (_formKey.currentState?.validate() != true) return;
    await widget.viewModel.saveMedicalExam(
      _dateExamController.text.trim(),
      _validityController.text.trim(),
    );
    if (mounted &&
        widget.viewModel.medicalExamStatus == SectionLoadStatus.loaded) {
      setState(() => _isEditing = false);
    }
  }

  void _cancel() => setState(() => _isEditing = false);

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.medicalExamStatus;
        return ExpandableSectionCard(
          title: 'Exame Médico Admissional',
          onExpand: widget.viewModel.loadMedicalExam,
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
                  Text('Não foi possível carregar o exame médico admissional.'),
            ),
          ],
        ),
      );
    }

    final exam = widget.viewModel.medicalExam;
    final isSaving = status == SectionLoadStatus.saving;

    if (_isEditing) {
      return Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextFormField(
              controller: _dateExamController,
              enabled: !isSaving,
              decoration: const InputDecoration(
                labelText: 'Data do exame',
                prefixIcon: Icon(Icons.event_outlined),
                border: OutlineInputBorder(),
                helperText: 'Ex: 15/03/2026',
              ),
              keyboardType: TextInputType.number,
              inputFormatters: [_dateExamMask],
              validator: widget.viewModel.validateDateExam,
            ),
            const SizedBox(height: AppSpacing.md),
            TextFormField(
              controller: _validityController,
              enabled: !isSaving,
              decoration: const InputDecoration(
                labelText: 'Validade do exame',
                prefixIcon: Icon(Icons.date_range_outlined),
                border: OutlineInputBorder(),
                helperText: 'Ex: 15/03/2027',
              ),
              keyboardType: TextInputType.number,
              inputFormatters: [_validityMask],
              validator: widget.viewModel.validateExamValidity,
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

    // ── View mode ─────────────────────────────────────────────────────────────
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        ContactInfoRow(
          icon: Icons.event_outlined,
          label: 'Data do exame',
          value: exam?.dateExam.isNotEmpty == true
              ? exam!.dateExam
              : 'Não informado',
        ),
        const SizedBox(height: AppSpacing.xs),
        ContactInfoRow(
          icon: Icons.date_range_outlined,
          label: 'Validade do exame',
          value: exam?.validityExam.isNotEmpty == true
              ? exam!.validityExam
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
