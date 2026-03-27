import 'package:flutter/material.dart';

import '../../../../../core/theme/app_spacing.dart';
import '../../../../../domain/entities/department.dart';
import '../../../../../domain/entities/position.dart';
import '../../../../../domain/entities/role.dart';
import '../../viewmodel/employee_profile_viewmodel.dart';
import 'profile_shared_widgets.dart';

/// Expandable card for viewing and editing the employee's role assignment
/// (Informacoes de Funcao).
///
/// View mode displays department, position, role, CBO, and salary info.
/// Edit mode shows three cascading dropdowns (Department -> Position -> Role).
class RoleInfoSection extends StatefulWidget {
  const RoleInfoSection({super.key, required this.viewModel});

  final EmployeeProfileViewModel viewModel;

  @override
  State<RoleInfoSection> createState() => _RoleInfoSectionState();
}

class _RoleInfoSectionState extends State<RoleInfoSection> {
  bool _isEditing = false;
  String? _selectedDeptId;
  String? _selectedPosId;
  String? _selectedRoleId;

  void _startEdit() {
    final vm = widget.viewModel;
    setState(() {
      _isEditing = true;
      _selectedDeptId =
          vm.currentDepartmentId.isNotEmpty ? vm.currentDepartmentId : null;
      _selectedPosId =
          vm.currentPositionId.isNotEmpty ? vm.currentPositionId : null;
      _selectedRoleId =
          vm.profile?.roleId.isNotEmpty == true ? vm.profile!.roleId : null;
    });
  }

  Future<void> _save() async {
    if (_selectedRoleId == null || _selectedRoleId!.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
            content: Text('Por favor, selecione uma função válida.')),
      );
      return;
    }
    await widget.viewModel.saveEmployeeRole(_selectedRoleId!);
    if (mounted &&
        widget.viewModel.roleInfoStatus == SectionLoadStatus.loaded) {
      setState(() => _isEditing = false);
    }
  }

  void _cancel() => setState(() => _isEditing = false);

  /// Returns the positions available for the currently selected department.
  List<Position> get _positions {
    if (_selectedDeptId == null) return [];
    return widget.viewModel.positionsForDepartment(_selectedDeptId!);
  }

  /// Returns the roles available for the currently selected position.
  List<Role> get _roles {
    if (_selectedDeptId == null || _selectedPosId == null) return [];
    return widget.viewModel.rolesForPosition(_selectedDeptId!, _selectedPosId!);
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.roleInfoStatus;
        return SectionCard(
          title: 'Informações de Função',
          onLoad: widget.viewModel.loadRoleInfo,
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
              child: Text(
                  'Não foi possível carregar as informações de função.'),
            ),
          ],
        ),
      );
    }

    final isSaving = status == SectionLoadStatus.saving;
    final departments = widget.viewModel.allDepartments;
    final roleId = widget.viewModel.profile?.roleId ?? '';

    if (_isEditing) {
      return _buildEditMode(context, departments, isSaving);
    }

    return _buildViewMode(context, roleId);
  }

  Widget _buildViewMode(
    BuildContext context,
    String roleId,
  ) {
    // Find current department, position, role from the hierarchy.
    final (:department, :position, :role) =
        widget.viewModel.findRoleInHierarchy(roleId);
    final dept = department;
    final pos = position;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        ContactInfoRow(
          icon: Icons.business_outlined,
          label: 'Setor',
          value: dept?.name ?? 'Não informado',
        ),
        if (dept?.description.isNotEmpty == true) ...[
          const SizedBox(height: AppSpacing.xs),
          ContactInfoRow(
            icon: Icons.description_outlined,
            label: 'Descrição do setor',
            value: dept!.description,
          ),
        ],
        const SizedBox(height: AppSpacing.xs),
        ContactInfoRow(
          icon: Icons.work_outline,
          label: 'Cargo',
          value: pos?.name ?? 'Não informado',
        ),
        if (pos?.description.isNotEmpty == true) ...[
          const SizedBox(height: AppSpacing.xs),
          ContactInfoRow(
            icon: Icons.description_outlined,
            label: 'Descrição do cargo',
            value: pos!.description,
          ),
        ],
        if (pos?.cbo.isNotEmpty == true) ...[
          const SizedBox(height: AppSpacing.xs),
          ContactInfoRow(
            icon: Icons.tag,
            label: 'CBO do cargo',
            value: pos!.cbo,
          ),
        ],
        const SizedBox(height: AppSpacing.xs),
        ContactInfoRow(
          icon: Icons.badge_outlined,
          label: 'Função',
          value: role?.name ?? 'Não informado',
        ),
        if (role?.description.isNotEmpty == true) ...[
          const SizedBox(height: AppSpacing.xs),
          ContactInfoRow(
            icon: Icons.description_outlined,
            label: 'Descrição da função',
            value: role!.description,
          ),
        ],
        if (role?.cbo.isNotEmpty == true) ...[
          const SizedBox(height: AppSpacing.xs),
          ContactInfoRow(
            icon: Icons.tag,
            label: 'CBO da função',
            value: role!.cbo,
          ),
        ],
        if (role != null) ...[
          const SizedBox(height: AppSpacing.xs),
          ContactInfoRow(
            icon: Icons.payments_outlined,
            label: 'Salário',
            value: widget.viewModel.formatSalary(role),
          ),
          if (role.remuneration.description.isNotEmpty) ...[
            const SizedBox(height: AppSpacing.xs),
            ContactInfoRow(
              icon: Icons.notes_outlined,
              label: 'Descrição do salário',
              value: role.remuneration.description,
            ),
          ],
        ],
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

  Widget _buildEditMode(
    BuildContext context,
    List<Department> departments,
    bool isSaving,
  ) {
    final positions = _positions;
    final roles = _roles;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        DropdownButtonFormField<String>(
          initialValue: _selectedDeptId,
          decoration: const InputDecoration(
            labelText: 'Setor',
            prefixIcon: Icon(Icons.business_outlined),
            border: OutlineInputBorder(),
          ),
          items: departments
              .map<DropdownMenuItem<String>>((d) =>
                  DropdownMenuItem<String>(value: d.id, child: Text(d.name)))
              .toList(),
          onChanged: isSaving
              ? null
              : (id) {
                  setState(() {
                    _selectedDeptId = id;
                    _selectedPosId = null;
                    _selectedRoleId = null;
                  });
                },
        ),
        const SizedBox(height: AppSpacing.md),
        DropdownButtonFormField<String>(
          initialValue: _selectedPosId,
          decoration: const InputDecoration(
            labelText: 'Cargo',
            prefixIcon: Icon(Icons.work_outline),
            border: OutlineInputBorder(),
          ),
          items: positions
              .map<DropdownMenuItem<String>>((p) =>
                  DropdownMenuItem<String>(value: p.id, child: Text(p.name)))
              .toList(),
          onChanged: isSaving
              ? null
              : (id) {
                  setState(() {
                    _selectedPosId = id;
                    _selectedRoleId = null;
                  });
                },
        ),
        const SizedBox(height: AppSpacing.md),
        DropdownButtonFormField<String>(
          initialValue: _selectedRoleId,
          decoration: const InputDecoration(
            labelText: 'Função',
            prefixIcon: Icon(Icons.badge_outlined),
            border: OutlineInputBorder(),
          ),
          items: roles
              .map<DropdownMenuItem<String>>((r) =>
                  DropdownMenuItem<String>(value: r.id, child: Text(r.name)))
              .toList(),
          onChanged: isSaving
              ? null
              : (id) {
                  setState(() => _selectedRoleId = id);
                },
        ),
        // Show salary preview when a role is selected.
        if (_selectedRoleId != null) ...[
          const SizedBox(height: AppSpacing.md),
          Builder(builder: (context) {
            final selectedRole =
                roles.where((r) => r.id == _selectedRoleId).firstOrNull;
            if (selectedRole == null) return const SizedBox.shrink();
            return Card.filled(
              child: Padding(
                padding: const EdgeInsets.all(AppSpacing.sm),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Remuneração da função selecionada',
                      style: Theme.of(context).textTheme.labelMedium?.copyWith(
                            color: Theme.of(context)
                                .colorScheme
                                .onSurfaceVariant,
                          ),
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    ContactInfoRow(
                      icon: Icons.payments_outlined,
                      label: 'Salário',
                      value: widget.viewModel.formatSalary(selectedRole),
                    ),
                    if (selectedRole
                        .remuneration.description.isNotEmpty) ...[
                      const SizedBox(height: AppSpacing.xs),
                      ContactInfoRow(
                        icon: Icons.notes_outlined,
                        label: 'Descrição',
                        value: selectedRole.remuneration.description,
                      ),
                    ],
                  ],
                ),
              ),
            );
          }),
        ],
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
}
