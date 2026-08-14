import 'package:flutter/material.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../../domain/tenant.dart';
import '../../../tenant_permissions.dart';
import '../tenant_detail_viewmodel.dart';
import 'section_edit_actions.dart';

/// E-mail and phone, read then edited in place.
class ContactSection extends StatefulWidget {
  /// Creates the section.
  const ContactSection({super.key, required this.viewModel});

  /// Drives the section.
  final TenantDetailViewModel viewModel;

  @override
  State<ContactSection> createState() => _ContactSectionState();
}

class _ContactSectionState extends State<ContactSection> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _phoneController = TextEditingController();

  final _phoneMask = MaskTextInputFormatter(
    mask: '(##) #####-####',
    filter: {'#': RegExp(r'[0-9]')},
    type: MaskAutoCompletionType.lazy,
  );

  bool _isEditing = false;

  @override
  void dispose() {
    _emailController.dispose();
    _phoneController.dispose();
    super.dispose();
  }

  void _startEdit(TenantContact contact) {
    _emailController.text = contact.email;

    final digits = contact.phone.replaceAll(RegExp(r'[^\d]'), '');
    _phoneMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: digits),
    );
    _phoneController.text = digits.isEmpty ? '' : _phoneMask.getMaskedText();

    setState(() => _isEditing = true);
  }

  void _cancel() => setState(() => _isEditing = false);

  Future<void> _save() async {
    if (_formKey.currentState?.validate() != true) return;

    final saved = await widget.viewModel.saveContact(
      email: _emailController.text.trim(),
      phone: _phoneController.text.replaceAll(RegExp(r'[^\d]'), ''),
    );

    if (mounted && saved) setState(() => _isEditing = false);
  }

  @override
  Widget build(BuildContext context) {
    final tenant = widget.viewModel.tenant;
    if (tenant == null) return const SizedBox.shrink();

    final isSaving =
        widget.viewModel.contactStatus == TenantSectionStatus.saving;

    return SectionCard(
      title: 'Contato',
      child: _isEditing
          ? Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  TextFormField(
                    controller: _emailController,
                    enabled: !isSaving,
                    decoration: const InputDecoration(
                      labelText: 'E-mail',
                      prefixIcon: Icon(Icons.email_outlined),
                      border: OutlineInputBorder(),
                    ),
                    keyboardType: TextInputType.emailAddress,
                    validator: Tenant.validateEmail,
                  ),
                  const SizedBox(height: AppSpacing.md),
                  TextFormField(
                    controller: _phoneController,
                    enabled: !isSaving,
                    decoration: const InputDecoration(
                      labelText: 'Telefone (opcional)',
                      prefixIcon: Icon(Icons.phone_outlined),
                      border: OutlineInputBorder(),
                    ),
                    keyboardType: TextInputType.phone,
                    inputFormatters: [_phoneMask],
                    validator: Tenant.validatePhone,
                  ),
                  SectionEditActions(
                    isSaving: isSaving,
                    error: widget.viewModel.contactError,
                    onCancel: _cancel,
                    onSave: _save,
                  ),
                ],
              ),
            )
          : Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                InfoRow(
                  icon: Symbols.mail,
                  label: 'E-mail',
                  value: tenant.contact.email.isEmpty
                      ? 'Não informado'
                      : tenant.contact.email,
                ),
                const Divider(height: AppSpacing.xl),
                InfoRow(
                  icon: Symbols.call,
                  label: 'Telefone',
                  value: tenant.contact.hasPhone
                      ? tenant.contact.formattedPhone
                      : 'Não informado',
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
                          : () => _startEdit(tenant.contact),
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
