import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_spacing.dart';
import '../../../../domain/entities/department.dart';
import '../../../core/widgets/permission_guard.dart';
import '../../../../domain/entities/position.dart';
import '../../../../domain/entities/role.dart';
import '../viewmodel/department_list_viewmodel.dart';

/// Displays the hierarchical list of departments, positions, and roles for the
/// currently selected company.
///
/// Uses [ListenableBuilder] to react to [DepartmentListViewModel] state changes.
/// Navigation to create/edit screens is delegated to [GoRouter].
class DepartmentListScreen extends StatefulWidget {
  const DepartmentListScreen({super.key, required this.viewModel});

  final DepartmentListViewModel viewModel;

  @override
  State<DepartmentListScreen> createState() => _DepartmentListScreenState();
}

class _DepartmentListScreenState extends State<DepartmentListScreen> {
  @override
  void initState() {
    super.initState();
    widget.viewModel.loadDepartments();
  }

  @override
  void didUpdateWidget(DepartmentListScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.viewModel != widget.viewModel) {
      widget.viewModel.loadDepartments();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          tooltip: 'Voltar para início',
          onPressed: () => context.go('/home'),
        ),
        title: const Text('Setores'),
      ),
      body: ListenableBuilder(
        listenable: widget.viewModel,
        builder: (context, _) {
          if (widget.viewModel.isLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          if (widget.viewModel.hasError) {
            return _ErrorState(
              message: widget.viewModel.errorMessage ?? 'Erro ao carregar setores.',
              onRetry: widget.viewModel.loadDepartments,
            );
          }

          if (widget.viewModel.departments.isEmpty) {
            return const Center(
              child: Text('Nenhum setor cadastrado.'),
            );
          }

          return RefreshIndicator(
            onRefresh: widget.viewModel.loadDepartments,
            child: ListView(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.md, AppSpacing.md, AppSpacing.md, AppSpacing.md + 80,
              ),
              children: widget.viewModel.departments
                  .map((department) => _DepartmentTile(
                        department: department,
                        onEditDepartment: () => context
                            .push('/department/edit/${department.id}')
                            .then((_) => widget.viewModel.loadDepartments()),
                        onAddPosition: () => context
                            .push('/department/position/create/${department.id}')
                            .then((_) => widget.viewModel.loadDepartments()),
                        onEditPosition: (position) => context
                            .push('/department/position/edit/${department.id}/${position.id}')
                            .then((_) => widget.viewModel.loadDepartments()),
                        onAddRole: (position) => context
                            .push('/department/role/create/${position.id}')
                            .then((_) => widget.viewModel.loadDepartments()),
                        onEditRole: (position, role) => context
                            .push('/department/role/edit/${position.id}/${role.id}')
                            .then((_) => widget.viewModel.loadDepartments()),
                      ))
                  .toList(),
            ),
          );
        },
      ),
      floatingActionButton: PermissionGuard(
        resource: 'department',
        scope: 'create',
        child: FloatingActionButton(
          onPressed: () => context
              .push('/department/create')
              .then((_) => widget.viewModel.loadDepartments()),
          tooltip: 'Adicionar setor',
          child: const Icon(Icons.add),
        ),
      ),
    );
  }
}

class _DepartmentTile extends StatelessWidget {
  const _DepartmentTile({
    required this.department,
    required this.onEditDepartment,
    required this.onAddPosition,
    required this.onEditPosition,
    required this.onAddRole,
    required this.onEditRole,
  });

  final Department department;
  final VoidCallback onEditDepartment;
  final VoidCallback onAddPosition;
  final void Function(Position position) onEditPosition;
  final void Function(Position position) onAddRole;
  final void Function(Position position, Role role) onEditRole;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: AppSpacing.md),
      child: ExpansionTile(
        controlAffinity: ListTileControlAffinity.leading,
        title: Text(
          department.name,
          style: Theme.of(context).textTheme.titleMedium,
        ),
        subtitle: Text(
          department.description,
          style: Theme.of(context).textTheme.bodySmall,
        ),
        trailing: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Semantics(
              label: 'Editar setor ${department.name}',
              button: true,
              child: IconButton(
                icon: const Icon(Icons.edit_outlined),
                onPressed: onEditDepartment,
              ),
            ),
            Semantics(
              label: 'Adicionar cargo ao setor ${department.name}',
              button: true,
              child: IconButton(
                icon: const Icon(Icons.add),
                onPressed: onAddPosition,
              ),
            ),
          ],
        ),
        children: department.positions
            .map((position) => _PositionTile(
                  position: position,
                  onEdit: () => onEditPosition(position),
                  onAddRole: () => onAddRole(position),
                  onEditRole: (role) => onEditRole(position, role),
                ))
            .toList(),
      ),
    );
  }
}

class _PositionTile extends StatelessWidget {
  const _PositionTile({
    required this.position,
    required this.onEdit,
    required this.onAddRole,
    required this.onEditRole,
  });

  final Position position;
  final VoidCallback onEdit;
  final VoidCallback onAddRole;
  final void Function(Role role) onEditRole;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.md, vertical: AppSpacing.xs),
      child: Card.outlined(
        child: ExpansionTile(
          controlAffinity: ListTileControlAffinity.leading,
          title: Text(
            position.name,
            style: Theme.of(context).textTheme.titleSmall,
          ),
          subtitle: Text(
            position.description,
            style: Theme.of(context).textTheme.bodySmall,
          ),
          trailing: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Semantics(
                label: 'Editar cargo ${position.name}',
                button: true,
                child: IconButton(
                  icon: const Icon(Icons.edit_outlined),
                  onPressed: onEdit,
                ),
              ),
              Semantics(
                label: 'Adicionar função ao cargo ${position.name}',
                button: true,
                child: IconButton(
                  icon: const Icon(Icons.add),
                  onPressed: onAddRole,
                ),
              ),
            ],
          ),
          children: position.roles
              .map((role) => _RoleTile(
                    role: role,
                    onEdit: () => onEditRole(role),
                  ))
              .toList(),
        ),
      ),
    );
  }
}

class _RoleTile extends StatelessWidget {
  const _RoleTile({required this.role, required this.onEdit});

  final Role role;
  final VoidCallback onEdit;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      contentPadding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.lg, vertical: AppSpacing.xs),
      title: Text(
        role.name,
        style: Theme.of(context).textTheme.bodyMedium?.copyWith(
              fontWeight: FontWeight.w600,
            ),
      ),
      subtitle: Text(
        role.description,
        style: Theme.of(context).textTheme.bodySmall,
      ),
      trailing: Semantics(
        label: 'Editar função ${role.name}',
        button: true,
        child: IconButton(
          icon: const Icon(Icons.edit_outlined),
          onPressed: onEdit,
        ),
      ),
    );
  }
}

class _ErrorState extends StatelessWidget {
  const _ErrorState({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(message, style: Theme.of(context).textTheme.bodyLarge),
          const SizedBox(height: AppSpacing.md),
          FilledButton.tonal(
            onPressed: onRetry,
            child: const Text('Tentar novamente'),
          ),
        ],
      ),
    );
  }
}
