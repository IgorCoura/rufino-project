import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_spacing.dart';
import '../../../../domain/entities/department.dart';
import '../../../../domain/entities/position.dart';
import '../../../../domain/entities/role.dart';
import '../../../../domain/entities/workplace.dart';
import '../viewmodel/employee_form_viewmodel.dart';

/// Screen for creating a new employee.
///
/// The user selects a department → position → role cascade and a workplace,
/// then enters the employee's full name before submitting.
class EmployeeFormScreen extends StatefulWidget {
  const EmployeeFormScreen({super.key, required this.viewModel});

  final EmployeeFormViewModel viewModel;

  @override
  State<EmployeeFormScreen> createState() => _EmployeeFormScreenState();
}

class _EmployeeFormScreenState extends State<EmployeeFormScreen> {
  final _formKey = GlobalKey<FormState>();

  @override
  void initState() {
    super.initState();
    widget.viewModel.addListener(_onViewModelChanged);
    widget.viewModel.loadOptions();
  }

  @override
  void dispose() {
    widget.viewModel.removeListener(_onViewModelChanged);
    super.dispose();
  }

  void _onViewModelChanged() {
    if (!mounted) return;
    if (widget.viewModel.status == EmployeeFormStatus.saved) {
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          const SnackBar(
            content: Text('Funcionário criado com sucesso.'),
            behavior: SnackBarBehavior.floating,
          ),
        );
      context.pop();
    }
    if (widget.viewModel.status == EmployeeFormStatus.error) {
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            content: Text(
                widget.viewModel.errorMessage ?? 'Erro desconhecido.'),
            behavior: SnackBarBehavior.floating,
          ),
        );
    }
  }

  Future<void> _onSave() async {
    if (_formKey.currentState?.validate() != true) return;
    await widget.viewModel.save();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Novo Funcionário'),
      ),
      body: ListenableBuilder(
        listenable: widget.viewModel,
        builder: (context, _) {
          if (widget.viewModel.isLoadingOptions) {
            return const Center(child: CircularProgressIndicator());
          }

          return SingleChildScrollView(
            padding: const EdgeInsets.all(AppSpacing.md),
            child: Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  TextFormField(
                    controller: widget.viewModel.nameController,
                    decoration: const InputDecoration(
                      labelText: 'Nome completo',
                      border: OutlineInputBorder(),
                      prefixIcon: Icon(Icons.person_outline),
                    ),
                    textCapitalization: TextCapitalization.words,
                    validator: widget.viewModel.validateName,
                  ),
                  const SizedBox(height: AppSpacing.md),
                  _DepartmentDropdown(viewModel: widget.viewModel),
                  const SizedBox(height: AppSpacing.md),
                  _PositionDropdown(viewModel: widget.viewModel),
                  const SizedBox(height: AppSpacing.md),
                  _RoleDropdown(viewModel: widget.viewModel),
                  const SizedBox(height: AppSpacing.md),
                  _WorkplaceDropdown(viewModel: widget.viewModel),
                  const SizedBox(height: AppSpacing.lg),
                  FilledButton(
                    onPressed: widget.viewModel.isSaving ? null : _onSave,
                    child: Padding(
                      padding:
                          const EdgeInsets.symmetric(vertical: AppSpacing.sm),
                      child: widget.viewModel.isSaving
                          ? const SizedBox(
                              height: 20,
                              width: 20,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : const Text('Criar Funcionário'),
                    ),
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}

class _DepartmentDropdown extends StatelessWidget {
  const _DepartmentDropdown({required this.viewModel});

  final EmployeeFormViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<Department>(
      value: viewModel.selectedDepartment,
      decoration: const InputDecoration(
        labelText: 'Setor',
        border: OutlineInputBorder(),
        prefixIcon: Icon(Icons.business_outlined),
      ),
      hint: const Text('Selecione um setor'),
      items: viewModel.departments
          .map((d) => DropdownMenuItem(value: d, child: Text(d.name)))
          .toList(),
      validator: (v) => v == null ? 'Selecione um setor.' : null,
      onChanged: viewModel.onDepartmentChanged,
    );
  }
}

class _PositionDropdown extends StatelessWidget {
  const _PositionDropdown({required this.viewModel});

  final EmployeeFormViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<Position>(
      value: viewModel.selectedPosition,
      decoration: const InputDecoration(
        labelText: 'Cargo',
        border: OutlineInputBorder(),
        prefixIcon: Icon(Icons.work_outline),
      ),
      hint: const Text('Selecione um cargo'),
      items: viewModel.positions
          .map((p) => DropdownMenuItem(value: p, child: Text(p.name)))
          .toList(),
      validator: (v) => v == null ? 'Selecione um cargo.' : null,
      onChanged: viewModel.selectedDepartment == null
          ? null
          : viewModel.onPositionChanged,
    );
  }
}

class _RoleDropdown extends StatelessWidget {
  const _RoleDropdown({required this.viewModel});

  final EmployeeFormViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<Role>(
      value: viewModel.selectedRole,
      decoration: const InputDecoration(
        labelText: 'Função',
        border: OutlineInputBorder(),
        prefixIcon: Icon(Icons.badge_outlined),
      ),
      hint: const Text('Selecione uma função'),
      items: viewModel.roles
          .map((r) => DropdownMenuItem(value: r, child: Text(r.name)))
          .toList(),
      validator: (v) => v == null ? 'Selecione uma função.' : null,
      onChanged:
          viewModel.selectedPosition == null ? null : viewModel.onRoleChanged,
    );
  }
}

class _WorkplaceDropdown extends StatelessWidget {
  const _WorkplaceDropdown({required this.viewModel});

  final EmployeeFormViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<Workplace>(
      value: viewModel.selectedWorkplace,
      decoration: const InputDecoration(
        labelText: 'Local de trabalho',
        border: OutlineInputBorder(),
        prefixIcon: Icon(Icons.location_on_outlined),
      ),
      hint: const Text('Selecione um local'),
      items: viewModel.workplaces
          .map((w) => DropdownMenuItem(value: w, child: Text(w.name)))
          .toList(),
      validator: (v) => v == null ? 'Selecione um local de trabalho.' : null,
      onChanged: viewModel.onWorkplaceChanged,
    );
  }
}
