import 'package:flutter/material.dart';

import '../../../../../core/theme/app_spacing.dart';
import '../../../../../domain/entities/signing_option.dart';
import '../../viewmodel/employee_profile_viewmodel.dart';
import 'profile_shared_widgets.dart';

/// Expandable card for viewing and editing the employee's document signing
/// option (Opções de Assinatura de Documentos).
///
/// View mode displays the current option. Edit mode shows a dropdown to select
/// from available signing methods.
class SigningOptionsSection extends StatefulWidget {
  const SigningOptionsSection({super.key, required this.viewModel});

  final EmployeeProfileViewModel viewModel;

  @override
  State<SigningOptionsSection> createState() => _SigningOptionsSectionState();
}

class _SigningOptionsSectionState extends State<SigningOptionsSection> {
  bool _isEditing = false;
  String? _selectedOptionId;

  void _startEdit() {
    final currentId =
        widget.viewModel.profile?.documentSigningOptionsId ?? '';
    setState(() {
      _isEditing = true;
      _selectedOptionId = currentId.isNotEmpty ? currentId : null;
    });
  }

  Future<void> _save() async {
    if (_selectedOptionId == null || _selectedOptionId!.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
            content: Text('Por favor, selecione uma opção de assinatura.')),
      );
      return;
    }
    await widget.viewModel.saveSigningOption(_selectedOptionId!);
    if (mounted &&
        widget.viewModel.signingOptionsStatus == SectionLoadStatus.loaded) {
      setState(() => _isEditing = false);
    }
  }

  void _cancel() => setState(() => _isEditing = false);

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.signingOptionsStatus;
        return ExpandableSectionCard(
          title: 'Opções de Assinatura de Documentos',
          onExpand: widget.viewModel.loadSigningOptions,
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
            Icon(Icons.error_outline,
                color: Theme.of(context).colorScheme.error, size: 20),
            const SizedBox(width: AppSpacing.sm),
            const Expanded(
              child: Text(
                  'Não foi possível carregar as opções de assinatura.'),
            ),
          ],
        ),
      );
    }

    final isSaving = status == SectionLoadStatus.saving;
    final currentId =
        widget.viewModel.profile?.documentSigningOptionsId ?? '';

    if (_isEditing) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          DropdownButtonFormField<String>(
            initialValue: _selectedOptionId,
            decoration: const InputDecoration(
              labelText: 'Tipo de assinatura',
              prefixIcon: Icon(Icons.draw_outlined),
              border: OutlineInputBorder(),
            ),
            items: widget.viewModel.signingOptions
                .where((o) => o.id != '0')
                .map<DropdownMenuItem<String>>((o) =>
                    DropdownMenuItem<String>(
                        value: o.id, child: Text(SigningOption.labelForId(o.id))))
                .toList(),
            onChanged:
                isSaving ? null : (v) => setState(() => _selectedOptionId = v),
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
      );
    }

    // ── View mode ─────────────────────────────────────────────────────────────
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        ContactInfoRow(
          icon: Icons.draw_outlined,
          label: 'Tipo de assinatura',
          value: currentId.isNotEmpty
              ? SigningOption.labelForId(currentId)
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
