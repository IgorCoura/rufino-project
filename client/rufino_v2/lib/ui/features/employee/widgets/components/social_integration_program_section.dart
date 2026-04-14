import 'package:flutter/material.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';

import '../../../../../core/theme/app_spacing.dart';
import '../../../../core/widgets/permission_guard.dart';
import '../../viewmodel/employee_profile_viewmodel.dart';
import 'profile_shared_widgets.dart';

/// Expandable card for viewing and editing an employee's PIS/PASEP
/// (Programa de Integração Social) registration.
class SocialIntegrationProgramSection extends StatefulWidget {
  const SocialIntegrationProgramSection({super.key, required this.viewModel});

  final EmployeeProfileViewModel viewModel;

  @override
  State<SocialIntegrationProgramSection> createState() =>
      _SocialIntegrationProgramSectionState();
}

class _SocialIntegrationProgramSectionState
    extends State<SocialIntegrationProgramSection> {
  final _formKey = GlobalKey<FormState>();
  final _numberController = TextEditingController();

  /// PIS mask: `###.#####.##-#` (11 digits grouped as 3-5-2-1).
  final _pisMask = MaskTextInputFormatter(
    mask: '###.#####.##-#',
    filter: {'#': RegExp(r'[0-9]')},
    type: MaskAutoCompletionType.lazy,
  );

  bool _isEditing = false;

  @override
  void dispose() {
    _numberController.dispose();
    super.dispose();
  }

  void _startEdit() {
    final pis = widget.viewModel.socialIntegrationProgram;
    final rawDigits =
        (pis?.number ?? '').replaceAll(RegExp(r'[^\d]'), '');
    _pisMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: rawDigits),
    );
    _numberController.text = _pisMask.getMaskedText();
    setState(() => _isEditing = true);
  }

  Future<void> _save() async {
    if (_formKey.currentState?.validate() != true) return;
    final rawDigits =
        _numberController.text.replaceAll(RegExp(r'[^\d]'), '');
    await widget.viewModel.saveSocialIntegrationProgram(rawDigits);
    if (mounted &&
        widget.viewModel.socialIntegrationProgramStatus ==
            SectionLoadStatus.loaded) {
      setState(() => _isEditing = false);
    }
  }

  void _cancel() => setState(() => _isEditing = false);

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.socialIntegrationProgramStatus;
        return SectionCard(
          title: 'PIS / PASEP',
          onLoad: widget.viewModel.loadSocialIntegrationProgram,
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
              child: Text('Não foi possível carregar o PIS.'),
            ),
          ],
        ),
      );
    }

    final pis = widget.viewModel.socialIntegrationProgram;
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
                labelText: 'Número do PIS',
                prefixIcon: Icon(Icons.badge_outlined),
                border: OutlineInputBorder(),
                helperText: 'Ex: 000.00000.00-0',
              ),
              keyboardType: TextInputType.number,
              inputFormatters: [_pisMask],
              validator:
                  widget.viewModel.validateSocialIntegrationProgramNumber,
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

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        ContactInfoRow(
          icon: Icons.badge_outlined,
          label: 'Número do PIS',
          value: pis?.hasNumber == true ? pis!.formatted : 'Não informado',
        ),
        const SizedBox(height: AppSpacing.sm),
        Align(
          alignment: Alignment.centerRight,
          child: PermissionGuard(
            resource: 'employee',
            scope: 'edit',
            child: TextButton.icon(
              onPressed: _startEdit,
              icon: const Icon(Icons.edit_outlined, size: 18),
              label: const Text('Editar'),
            ),
          ),
        ),
      ],
    );
  }
}
