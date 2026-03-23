import 'package:flutter/material.dart';

import '../../../../../core/theme/app_spacing.dart';
import '../../viewmodel/employee_profile_viewmodel.dart';
import 'profile_shared_widgets.dart';

/// Expandable card for viewing and editing employee military document
/// (Documento Militar) data.
class MilitaryDocumentSection extends StatefulWidget {
  const MilitaryDocumentSection({super.key, required this.viewModel});

  final EmployeeProfileViewModel viewModel;

  @override
  State<MilitaryDocumentSection> createState() =>
      _MilitaryDocumentSectionState();
}

class _MilitaryDocumentSectionState extends State<MilitaryDocumentSection> {
  final _formKey = GlobalKey<FormState>();
  final _numberController = TextEditingController();
  final _typeController = TextEditingController();

  bool _isEditing = false;

  @override
  void dispose() {
    _numberController.dispose();
    _typeController.dispose();
    super.dispose();
  }

  void _startEdit() {
    final doc = widget.viewModel.militaryDocument;
    if (doc == null) return;
    _numberController.text = doc.number;
    _typeController.text = doc.type;
    setState(() => _isEditing = true);
  }

  Future<void> _save() async {
    if (_formKey.currentState?.validate() != true) return;
    await widget.viewModel.saveMilitaryDocument(
      _numberController.text.trim(),
      _typeController.text.trim(),
    );
    if (mounted &&
        widget.viewModel.militaryDocumentStatus == SectionLoadStatus.loaded) {
      setState(() => _isEditing = false);
    }
  }

  void _cancel() => setState(() => _isEditing = false);

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.militaryDocumentStatus;
        return ExpandableSectionCard(
          title: 'Documento Militar',
          onExpand: widget.viewModel.loadMilitaryDocument,
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
              child: Text('Não foi possível carregar o documento militar.'),
            ),
          ],
        ),
      );
    }

    final doc = widget.viewModel.militaryDocument;

    // When the military document is not required for this employee.
    if (doc != null && !doc.isRequired) {
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
        child: Row(
          children: [
            Icon(
              Icons.info_outline,
              color: Theme.of(context).colorScheme.onSurfaceVariant,
              size: 20,
            ),
            const SizedBox(width: AppSpacing.sm),
            const Expanded(
              child: Text('Não se aplica a este funcionário.'),
            ),
          ],
        ),
      );
    }

    final isSaving = status == SectionLoadStatus.saving;

    if (_isEditing) {
      return Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextFormField(
              controller: _numberController,
              enabled: !isSaving,
              decoration: const InputDecoration(
                labelText: 'Número do documento',
                prefixIcon: Icon(Icons.badge_outlined),
                border: OutlineInputBorder(),
                counterText: '',
              ),
              maxLength: 20,
              keyboardType: TextInputType.number,
              validator: widget.viewModel.validateMilitaryNumber,
            ),
            const SizedBox(height: AppSpacing.md),
            TextFormField(
              controller: _typeController,
              enabled: !isSaving,
              decoration: const InputDecoration(
                labelText: 'Tipo de documento',
                prefixIcon: Icon(Icons.category_outlined),
                border: OutlineInputBorder(),
                counterText: '',
              ),
              maxLength: 50,
              validator: widget.viewModel.validateMilitaryType,
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
          icon: Icons.badge_outlined,
          label: 'Número do documento',
          value: doc?.number.isNotEmpty == true
              ? doc!.number
              : 'Não informado',
        ),
        const SizedBox(height: AppSpacing.xs),
        ContactInfoRow(
          icon: Icons.category_outlined,
          label: 'Tipo de documento',
          value: doc?.type.isNotEmpty == true
              ? doc!.type
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
