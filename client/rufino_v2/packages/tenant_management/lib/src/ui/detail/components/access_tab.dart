import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../../domain/tenant.dart';
import '../../../domain/tenant_enums.dart';
import '../../../tenant_permissions.dart';
import '../tenant_detail_viewmodel.dart';
import 'section_edit_actions.dart';

/// Who can open this tenant, and whether the identity provider knows it.
///
/// Granting is a form, so it expands **inline** at the top of the list.
/// Revoking is destructive, so it asks for confirmation in a dialog — the one
/// place a dialog earns its keep here.
class AccessTab extends StatefulWidget {
  /// Creates the tab.
  const AccessTab({super.key, required this.viewModel});

  /// Drives the tab.
  final TenantDetailViewModel viewModel;

  @override
  State<AccessTab> createState() => _AccessTabState();
}

class _AccessTabState extends State<AccessTab> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();

  bool _isGranting = false;
  String _role = MembershipRoles.member;

  @override
  void dispose() {
    _emailController.dispose();
    super.dispose();
  }

  void _startGrant() {
    _emailController.clear();
    _role = MembershipRoles.member;
    setState(() => _isGranting = true);
  }

  void _cancelGrant() => setState(() => _isGranting = false);

  Future<void> _grant() async {
    if (_formKey.currentState?.validate() != true) return;

    final granted = await widget.viewModel.grantMembership(
      email: _emailController.text.trim(),
      role: _role,
    );

    if (mounted && granted) setState(() => _isGranting = false);
  }

  Future<void> _confirmRevoke(TenantMembership membership) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Revogar acesso'),
        content: Text(
          '${membership.email} deixa de acessar este cliente. '
          'O convite pode ser concedido de novo depois.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Revogar'),
          ),
        ],
      ),
    );

    if (confirmed ?? false) {
      await widget.viewModel.revokeMembership(membership.email);
    }
  }

  @override
  Widget build(BuildContext context) {
    final tenant = widget.viewModel.tenant;
    if (tenant == null) return const SizedBox.shrink();

    final isSaving =
        widget.viewModel.accessStatus == TenantSectionStatus.saving;

    return ListView(
      padding: const EdgeInsets.all(AppSpacing.md),
      children: [
        if (tenant.needsReprovisioning)
          Padding(
            padding: const EdgeInsets.only(bottom: AppSpacing.md),
            child: _ProvisioningBanner(
              tenant: tenant,
              isSaving: isSaving,
              onReprovision: widget.viewModel.reprovisionAccess,
            ),
          ),
        if (_isGranting)
          Padding(
            padding: const EdgeInsets.only(bottom: AppSpacing.md),
            child: SectionCard(
              title: 'Conceder acesso',
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    TextFormField(
                      controller: _emailController,
                      enabled: !isSaving,
                      decoration: const InputDecoration(
                        labelText: 'E-mail',
                        border: OutlineInputBorder(),
                        helperText: 'Quem receber o convite entra por aqui.',
                      ),
                      keyboardType: TextInputType.emailAddress,
                      validator: Tenant.validateEmail,
                    ),
                    const SizedBox(height: AppSpacing.md),
                    DropdownButtonFormField<String>(
                      initialValue: _role,
                      decoration: const InputDecoration(
                        labelText: 'Papel',
                        border: OutlineInputBorder(),
                      ),
                      items: const [
                        DropdownMenuItem(
                          value: MembershipRoles.member,
                          child: Text('Membro'),
                        ),
                        DropdownMenuItem(
                          value: MembershipRoles.owner,
                          child: Text('Responsável'),
                        ),
                      ],
                      onChanged: isSaving
                          ? null
                          : (value) =>
                              setState(() => _role = value ?? _role),
                    ),
                    SectionEditActions(
                      isSaving: isSaving,
                      error: widget.viewModel.accessError,
                      onCancel: _cancelGrant,
                      onSave: _grant,
                    ),
                  ],
                ),
              ),
            ),
          )
        else
          Align(
            alignment: Alignment.centerLeft,
            child: TenantPermissionGuard(
              resource: TenantResources.tenantAccess,
              scope: TenantScopes.edit,
              child: Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.sm),
                child: FilledButton.tonalIcon(
                  onPressed: widget.viewModel.isFrozen ? null : _startGrant,
                  icon: const Icon(Icons.person_add_alt),
                  label: const Text('Conceder acesso'),
                ),
              ),
            ),
          ),
        for (final membership in tenant.memberships)
          Padding(
            padding: const EdgeInsets.only(bottom: AppSpacing.sm),
            child: _MembershipRow(
              membership: membership,
              canRevoke: tenant.canRevoke(membership) &&
                  !widget.viewModel.isFrozen,
              isLastOwner: membership.isActive &&
                  membership.isOwner &&
                  !tenant.canRevoke(membership),
              onRevoke: () => _confirmRevoke(membership),
            ),
          ),
      ],
    );
  }
}

class _ProvisioningBanner extends StatelessWidget {
  const _ProvisioningBanner({
    required this.tenant,
    required this.isSaving,
    required this.onReprovision,
  });

  final Tenant tenant;
  final bool isSaving;
  final Future<bool> Function() onReprovision;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final failed = tenant.hasFailedProvisioning;

    return Card.filled(
      color: failed ? cs.errorContainer : cs.tertiaryContainer,
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(
                  failed ? Symbols.error : Symbols.hourglass_top,
                  color:
                      failed ? cs.onErrorContainer : cs.onTertiaryContainer,
                ),
                const SizedBox(width: AppSpacing.md),
                Expanded(
                  child: Text(
                    failed
                        ? 'O acesso não chegou ao provedor de identidade.'
                        : 'O acesso ainda está a caminho do provedor de '
                            'identidade.',
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          color: failed
                              ? cs.onErrorContainer
                              : cs.onTertiaryContainer,
                        ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: AppSpacing.sm),
            Align(
              alignment: Alignment.centerRight,
              child: TenantPermissionGuard(
                resource: TenantResources.tenantAccess,
                scope: TenantScopes.edit,
                child: isSaving
                    ? const SizedBox(
                        width: 24,
                        height: 24,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : FilledButton.tonal(
                        onPressed: onReprovision,
                        child: const Text('Reenviar acessos'),
                      ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _MembershipRow extends StatelessWidget {
  const _MembershipRow({
    required this.membership,
    required this.canRevoke,
    required this.isLastOwner,
    required this.onRevoke,
  });

  final TenantMembership membership;
  final bool canRevoke;
  final bool isLastOwner;
  final VoidCallback onRevoke;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final cs = theme.colorScheme;

    return Card.outlined(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Row(
          children: [
            Icon(
              membership.isActive ? Symbols.person : Symbols.person_off,
              color: membership.isActive ? cs.primary : cs.outline,
            ),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(membership.email, style: theme.textTheme.bodyLarge),
                  const SizedBox(height: 2),
                  Text(
                    membership.isActive
                        ? '${membership.roleLabel} · '
                            '${membership.provisioningLabel}'
                        : '${membership.roleLabel} · Revogado',
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: membership.hasFailed
                          ? cs.error
                          : cs.onSurfaceVariant,
                    ),
                  ),
                  if (isLastOwner)
                    Padding(
                      padding: const EdgeInsets.only(top: 2),
                      child: Text(
                        'Último responsável — não pode perder o acesso.',
                        style: theme.textTheme.bodySmall?.copyWith(
                          color: cs.onSurfaceVariant,
                        ),
                      ),
                    ),
                ],
              ),
            ),
            if (canRevoke)
              TenantPermissionGuard(
                resource: TenantResources.tenantAccess,
                scope: TenantScopes.edit,
                child: TextButton(
                  onPressed: onRevoke,
                  child: const Text('Revogar'),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
