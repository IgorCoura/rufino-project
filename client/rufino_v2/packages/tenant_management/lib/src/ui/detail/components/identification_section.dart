import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../../domain/tenant.dart';
import '../../../tenant_permissions.dart';
import '../tenant_detail_viewmodel.dart';
import 'section_edit_actions.dart';

/// Name and document, read then edited **in place**.
///
/// The document is not editable: a wrong document is another cadastro, not an
/// edit — the backend has no endpoint for it, and pretending otherwise would
/// be a field that always fails to save.
class IdentificationSection extends StatefulWidget {
  /// Creates the section.
  const IdentificationSection({super.key, required this.viewModel});

  /// Drives the section.
  final TenantDetailViewModel viewModel;

  @override
  State<IdentificationSection> createState() => _IdentificationSectionState();
}

class _IdentificationSectionState extends State<IdentificationSection> {
  final _formKey = GlobalKey<FormState>();
  final _legalNameController = TextEditingController();
  final _tradeNameController = TextEditingController();

  bool _isEditing = false;

  @override
  void dispose() {
    _legalNameController.dispose();
    _tradeNameController.dispose();
    super.dispose();
  }

  void _startEdit(Tenant tenant) {
    _legalNameController.text = tenant.legalName;
    _tradeNameController.text = tenant.tradeName;
    setState(() => _isEditing = true);
  }

  void _cancel() => setState(() => _isEditing = false);

  Future<void> _save() async {
    if (_formKey.currentState?.validate() != true) return;

    final saved = await widget.viewModel.saveIdentification(
      legalName: _legalNameController.text.trim(),
      tradeName: _tradeNameController.text.trim(),
    );

    if (mounted && saved) setState(() => _isEditing = false);
  }

  @override
  Widget build(BuildContext context) {
    final tenant = widget.viewModel.tenant;
    if (tenant == null) return const SizedBox.shrink();

    final isSaving =
        widget.viewModel.identificationStatus == TenantSectionStatus.saving;

    return SectionCard(
      title: 'Identificação',
      child: _isEditing
          ? _EditMode(
              formKey: _formKey,
              tenant: tenant,
              legalNameController: _legalNameController,
              tradeNameController: _tradeNameController,
              isSaving: isSaving,
              error: widget.viewModel.identificationError,
              onCancel: _cancel,
              onSave: _save,
            )
          : _ViewMode(
              tenant: tenant,
              isFrozen: widget.viewModel.isFrozen,
              onEdit: () => _startEdit(tenant),
            ),
    );
  }
}

class _ViewMode extends StatelessWidget {
  const _ViewMode({
    required this.tenant,
    required this.isFrozen,
    required this.onEdit,
  });

  final Tenant tenant;
  final bool isFrozen;
  final VoidCallback onEdit;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        InfoRow(
          icon: tenant.isIndividual ? Symbols.person : Symbols.apartment,
          label: tenant.isIndividual ? 'Nome completo' : 'Razão social',
          value: tenant.legalName,
        ),
        if (!tenant.isIndividual) ...[
          const Divider(height: AppSpacing.xl),
          InfoRow(
            icon: Symbols.sell,
            label: 'Nome fantasia',
            value: tenant.tradeName.isEmpty ? 'Não informado' : tenant.tradeName,
          ),
        ],
        const Divider(height: AppSpacing.xl),
        InfoRow(
          icon: Symbols.badge,
          label: tenant.taxIdLabel,
          value: tenant.formattedTaxId,
        ),
        const SizedBox(height: AppSpacing.sm),
        Align(
          alignment: Alignment.centerRight,
          child: TenantPermissionGuard(
            resource: TenantResources.tenant,
            scope: TenantScopes.edit,
            child: TextButton.icon(
              // Suspenso desabilita, não esconde: a causa é o estado do
              // cadastro, não falta de permissão, e some sem explicação seria
              // esconder a razão.
              onPressed: isFrozen ? null : onEdit,
              icon: const Icon(Icons.edit_outlined, size: 18),
              label: const Text('Editar'),
            ),
          ),
        ),
      ],
    );
  }
}

class _EditMode extends StatelessWidget {
  const _EditMode({
    required this.formKey,
    required this.tenant,
    required this.legalNameController,
    required this.tradeNameController,
    required this.isSaving,
    required this.error,
    required this.onCancel,
    required this.onSave,
  });

  final GlobalKey<FormState> formKey;
  final Tenant tenant;
  final TextEditingController legalNameController;
  final TextEditingController tradeNameController;
  final bool isSaving;
  final String? error;
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
            controller: legalNameController,
            enabled: !isSaving,
            decoration: InputDecoration(
              labelText: tenant.isIndividual ? 'Nome completo' : 'Razão social',
              border: const OutlineInputBorder(),
            ),
            validator: Tenant.validateLegalName,
          ),
          if (!tenant.isIndividual) ...[
            const SizedBox(height: AppSpacing.md),
            TextFormField(
              controller: tradeNameController,
              enabled: !isSaving,
              decoration: const InputDecoration(
                labelText: 'Nome fantasia',
                border: OutlineInputBorder(),
              ),
              validator: (value) =>
                  Tenant.validateTradeName(tenant.kind, value),
            ),
          ],
          const SizedBox(height: AppSpacing.md),
          Text(
            '${tenant.taxIdLabel} ${tenant.formattedTaxId} — não editável. '
            'Trocar o documento é outro cadastro, não uma edição.',
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
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
