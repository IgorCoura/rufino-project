import 'package:flutter/material.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';

import '../../../../../core/theme/app_spacing.dart';
import '../../../../../domain/entities/employee_contract.dart';
import '../../../../core/widgets/permission_guard.dart';
import '../../viewmodel/employee_profile_viewmodel.dart';
import 'profile_shared_widgets.dart';

/// Expandable card for viewing and managing employee contracts (Contratos).
///
/// Shows the contract history in view mode. Provides dialogs to create a new
/// contract or finish the current active contract.
class ContractSection extends StatefulWidget {
  const ContractSection({
    super.key,
    required this.viewModel,
    this.canMarkAsInactive = false,
    this.onMarkAsInactive,
  });

  final EmployeeProfileViewModel viewModel;

  /// Whether the "Marcar como inativo" action should be available.
  final bool canMarkAsInactive;

  /// Called when the user confirms the "Marcar como inativo" action.
  final VoidCallback? onMarkAsInactive;

  @override
  State<ContractSection> createState() => _ContractSectionState();
}

class _ContractSectionState extends State<ContractSection> {
  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.contractsStatus;
        return SectionCard(
          title: 'Contratos',
          onLoad: widget.viewModel.loadContracts,
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
              child: Text('Não foi possível carregar os contratos.'),
            ),
          ],
        ),
      );
    }

    final contracts = widget.viewModel.contracts;
    final isSaving = status == SectionLoadStatus.saving;
    final hasActive = contracts.any((c) => c.isActive);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (contracts.isEmpty)
          const Padding(
            padding: EdgeInsets.symmetric(vertical: AppSpacing.sm),
            child: Text('Nenhum contrato encontrado.'),
          ),
        for (final contract in contracts)
          _buildContractCard(context, contract, isSaving),
        const SizedBox(height: AppSpacing.sm),
        Wrap(
          alignment: WrapAlignment.end,
          spacing: AppSpacing.sm,
          runSpacing: AppSpacing.sm,
          children: [
            if (hasActive)
              PermissionGuard(
                resource: 'employee',
                scope: 'edit',
                child: TextButton.icon(
                  onPressed:
                      isSaving ? null : () => _showFinishDialog(context),
                  icon: const Icon(Icons.cancel_outlined, size: 18),
                  label: const Text('Finalizar Contrato'),
                ),
              ),
            if (!hasActive)
              PermissionGuard(
                resource: 'employee',
                scope: 'edit',
                child: FilledButton.tonalIcon(
                  onPressed:
                      isSaving ? null : () => _showNewContractDialog(context),
                  icon: const Icon(Icons.add),
                  label: const Text('Novo Contrato'),
                ),
              ),
            if (!hasActive && widget.canMarkAsInactive)
              PermissionGuard(
                resource: 'employee',
                scope: 'edit',
                child: FilledButton.tonalIcon(
                  onPressed: isSaving ? null : widget.onMarkAsInactive,
                  icon: isSaving
                      ? const SizedBox(
                          width: 18,
                          height: 18,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : const Icon(Icons.person_off_outlined),
                  label: const Text('Marcar como inativo'),
                ),
              ),
          ],
        ),
      ],
    );
  }

  Widget _buildContractCard(
    BuildContext context,
    EmployeeContractInfo contract,
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
              Row(
                children: [
                  Icon(
                    contract.isActive
                        ? Icons.check_circle_outline
                        : Icons.history,
                    color: contract.isActive ? cs.primary : cs.outline,
                    size: 20,
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  Text(
                    contract.typeName.isNotEmpty
                        ? contract.typeName
                        : 'Contrato',
                    style: Theme.of(context).textTheme.titleSmall,
                  ),
                  if (contract.isActive) ...[
                    const SizedBox(width: AppSpacing.sm),
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 8, vertical: 2),
                      decoration: BoxDecoration(
                        color: cs.primaryContainer,
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Text(
                        'Ativo',
                        style:
                            Theme.of(context).textTheme.labelSmall?.copyWith(
                                  color: cs.onPrimaryContainer,
                                ),
                      ),
                    ),
                  ],
                ],
              ),
              const SizedBox(height: AppSpacing.sm),
              ContactInfoRow(
                icon: Icons.calendar_today_outlined,
                label: 'Data de início',
                value: contract.initDate.isNotEmpty
                    ? contract.initDate
                    : 'Não informado',
              ),
              if (contract.finalDate.isNotEmpty) ...[
                const SizedBox(height: AppSpacing.xs),
                ContactInfoRow(
                  icon: Icons.event_busy_outlined,
                  label: 'Data de término',
                  value: contract.finalDate,
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  // ─── Dialogs ────────────────────────────────────────────────────────────

  void _showFinishDialog(BuildContext context) {
    final formKey = GlobalKey<FormState>();
    final dateCtrl = TextEditingController();
    final dateMask = MaskTextInputFormatter(
      mask: '##/##/####',
      filter: {'#': RegExp(r'[0-9]')},
      type: MaskAutoCompletionType.lazy,
    );

    showDialog(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Finalizar Contrato'),
        content: Form(
          key: formKey,
          child: TextFormField(
            controller: dateCtrl,
            decoration: const InputDecoration(
              labelText: 'Data de término',
              prefixIcon: Icon(Icons.event_busy_outlined),
              border: OutlineInputBorder(),
              helperText: 'Ex: 15/03/2026',
            ),
            keyboardType: TextInputType.number,
            inputFormatters: [dateMask],
            validator: widget.viewModel.validateContractFinalDate,
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () {
              if (formKey.currentState?.validate() != true) return;
              Navigator.pop(dialogContext);
              widget.viewModel.finishContract(dateCtrl.text.trim());
            },
            child: const Text('Confirmar'),
          ),
        ],
      ),
    );
  }

  void _showNewContractDialog(BuildContext context) {
    final formKey = GlobalKey<FormState>();
    final dateCtrl = TextEditingController();
    final registrationCtrl = TextEditingController();
    final dateMask = MaskTextInputFormatter(
      mask: '##/##/####',
      filter: {'#': RegExp(r'[0-9]')},
      type: MaskAutoCompletionType.lazy,
    );
    String? selectedTypeId;

    showDialog(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (dialogContext, setDialogState) => AlertDialog(
          title: const Text('Novo Contrato'),
          content: SizedBox(
            width: 400,
            child: Form(
              key: formKey,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  TextFormField(
                    controller: registrationCtrl,
                    decoration: const InputDecoration(
                      labelText: 'Matrícula',
                      prefixIcon: Icon(Icons.numbers_outlined),
                      border: OutlineInputBorder(),
                    ),
                    validator: (v) {
                      if (v == null || v.trim().isEmpty) {
                        return 'A Matrícula não pode ser vazia.';
                      }
                      return null;
                    },
                  ),
                  const SizedBox(height: AppSpacing.md),
                  TextFormField(
                    controller: dateCtrl,
                    decoration: const InputDecoration(
                      labelText: 'Data de início',
                      prefixIcon: Icon(Icons.calendar_today_outlined),
                      border: OutlineInputBorder(),
                      helperText: 'Ex: 15/03/2026',
                    ),
                    keyboardType: TextInputType.number,
                    inputFormatters: [dateMask],
                    validator: widget.viewModel.validateContractInitDate,
                  ),
                  const SizedBox(height: AppSpacing.md),
                  DropdownButtonFormField<String>(
                    initialValue: selectedTypeId,
                    decoration: const InputDecoration(
                      labelText: 'Tipo de contrato',
                      prefixIcon: Icon(Icons.description_outlined),
                      border: OutlineInputBorder(),
                    ),
                    items: widget.viewModel.contractTypes
                        .map<DropdownMenuItem<String>>((t) =>
                            DropdownMenuItem<String>(
                                value: t.id, child: Text(t.name)))
                        .toList(),
                    validator: (v) {
                      if (v == null || v.isEmpty) {
                        return 'Selecione o tipo de contrato.';
                      }
                      return null;
                    },
                    onChanged: (v) =>
                        setDialogState(() => selectedTypeId = v),
                  ),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialogContext),
              child: const Text('Cancelar'),
            ),
            FilledButton(
              onPressed: () {
                if (formKey.currentState?.validate() != true) return;
                Navigator.pop(dialogContext);
                widget.viewModel.createContract(
                  dateCtrl.text.trim(),
                  selectedTypeId!,
                  registrationCtrl.text.trim(),
                );
              },
              child: const Text('Confirmar'),
            ),
          ],
        ),
      ),
    );
  }
}
