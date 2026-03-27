import 'package:flutter/material.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';

import '../../../../../core/theme/app_spacing.dart';
import '../../viewmodel/employee_profile_viewmodel.dart';
import 'profile_shared_widgets.dart';
import '../../../../core/widgets/permission_guard.dart';

/// Expandable card for viewing and editing employee contact information.
class ContactSection extends StatefulWidget {
  const ContactSection({super.key, required this.viewModel});

  final EmployeeProfileViewModel viewModel;

  @override
  State<ContactSection> createState() => _ContactSectionState();
}

class _ContactSectionState extends State<ContactSection> {
  final _formKey = GlobalKey<FormState>();
  final _cellphoneController = TextEditingController();
  final _emailController = TextEditingController();

  /// Mask formatter for Brazilian mobile numbers: `## #####-####`.
  ///
  /// The `+55` country code is shown via [InputDecoration.prefixText] so it is
  /// never part of the editable text, keeping the controller value clean.
  final _phoneMask = MaskTextInputFormatter(
    mask: '## #####-####',
    filter: {'#': RegExp(r'[0-9]')},
    type: MaskAutoCompletionType.lazy,
  );

  bool _isEditing = false;

  @override
  void dispose() {
    _cellphoneController.dispose();
    _emailController.dispose();
    super.dispose();
  }

  void _startEdit() {
    final contact = widget.viewModel.contact;
    if (contact == null) return;

    // Apply the mask to the stored raw digits so the field shows formatted text.
    final rawDigits = contact.cellphone.replaceAll(RegExp(r'[^\d]'), '');
    _phoneMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: rawDigits),
    );
    _cellphoneController.text = _phoneMask.getMaskedText();
    _emailController.text = contact.email;
    setState(() => _isEditing = true);
  }

  Future<void> _save() async {
    if (_formKey.currentState?.validate() != true) return;

    // Strip mask characters — send only the 11 raw digits to the API.
    final rawDigits =
        _cellphoneController.text.replaceAll(RegExp(r'[^\d]'), '');
    await widget.viewModel.saveContact(rawDigits, _emailController.text.trim());

    if (mounted && widget.viewModel.contactStatus == SectionLoadStatus.loaded) {
      setState(() => _isEditing = false);
    }
  }

  void _cancel() => setState(() => _isEditing = false);

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.contactStatus;
        return SectionCard(
          title: 'Contato',
          onLoad: widget.viewModel.loadContact,
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
              child: Text('Não foi possível carregar o contato.'),
            ),
          ],
        ),
      );
    }

    final contact = widget.viewModel.contact;
    final isSaving = status == SectionLoadStatus.saving;

    if (_isEditing) {
      return Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextFormField(
              controller: _cellphoneController,
              enabled: !isSaving,
              decoration: const InputDecoration(
                labelText: 'Celular',
                prefixText: '+55 ',
                prefixIcon: Icon(Icons.phone_outlined),
                border: OutlineInputBorder(),
                helperText: 'Ex: 11 98765-4321',
              ),
              keyboardType: TextInputType.phone,
              inputFormatters: [_phoneMask],
              validator: widget.viewModel.validatePhone,
            ),
            const SizedBox(height: AppSpacing.md),
            TextFormField(
              controller: _emailController,
              enabled: !isSaving,
              decoration: const InputDecoration(
                labelText: 'E-mail',
                prefixIcon: Icon(Icons.email_outlined),
                border: OutlineInputBorder(),
              ),
              keyboardType: TextInputType.emailAddress,
              validator: widget.viewModel.validateEmail,
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

    // ── View mode ────────────────────────────────────────────────────────────
    final formattedPhone = contact?.hasPhone == true
        ? contact!.formattedPhone
        : null;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        ContactInfoRow(
          icon: Icons.phone_outlined,
          label: 'Celular',
          value: formattedPhone ?? 'Não informado',
        ),
        const Divider(height: AppSpacing.xl),
        ContactInfoRow(
          icon: Icons.email_outlined,
          label: 'E-mail',
          value: contact?.email.isNotEmpty == true
              ? contact!.email
              : 'Não informado',
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
