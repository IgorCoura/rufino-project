import 'package:flutter/material.dart';

import '../../../../core/theme/app_breakpoints.dart';
import '../../../../core/theme/app_spacing.dart';
import '../viewmodel/batch_download_viewmodel.dart';

/// Step 1: Employee selection with filters.
///
/// Displays a filterable, paginated list of employees with checkboxes
/// for selection. The user must select at least one employee to proceed.
class EmployeeSelectionStep extends StatefulWidget {
  const EmployeeSelectionStep({
    super.key,
    required this.viewModel,
    required this.onNext,
  });

  /// The parent view model.
  final BatchDownloadViewModel viewModel;

  /// Callback invoked when the user taps "Proximo".
  final VoidCallback onNext;

  @override
  State<EmployeeSelectionStep> createState() => _EmployeeSelectionStepState();
}

class _EmployeeSelectionStepState extends State<EmployeeSelectionStep> {
  final _nameController = TextEditingController();

  @override
  void dispose() {
    _nameController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final vm = widget.viewModel;
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const SizedBox(height: AppSpacing.md),
        // Filters
        _EmployeeFilters(
          nameController: _nameController,
          viewModel: vm,
          onApply: () {
            vm.setEmployeeNameFilter(_nameController.text);
            vm.applyEmployeeFilters();
          },
          onClear: () {
            _nameController.clear();
            vm.clearEmployeeFilters();
          },
        ),
        const SizedBox(height: AppSpacing.sm),
        // Selection actions
        Row(
          children: [
            Text(
              '${vm.selectedEmployeeCount} selecionado(s)',
              style: textTheme.labelMedium?.copyWith(
                color: colorScheme.onSurfaceVariant,
              ),
            ),
            const Spacer(),
            TextButton(
              onPressed: vm.selectAllEmployeesOnPage,
              child: const Text('Selecionar Todos'),
            ),
            TextButton(
              onPressed: vm.hasSelectedEmployees
                  ? vm.clearEmployeeSelection
                  : null,
              child: const Text('Limpar'),
            ),
          ],
        ),
        const Divider(height: 1),
        // Employee list
        Expanded(
          child: vm.status == BatchDownloadStatus.loading
              ? const Center(child: CircularProgressIndicator())
              : vm.employees.isEmpty
                  ? Center(
                      child: Text(
                        'Nenhum funcionario encontrado',
                        style: textTheme.bodyLarge?.copyWith(
                          color: colorScheme.onSurfaceVariant,
                        ),
                      ),
                    )
                  : ListView.builder(
                      itemCount: vm.employees.length,
                      itemBuilder: (context, index) {
                        final emp = vm.employees[index];
                        final isSelected =
                            vm.selectedEmployeeIds.contains(emp.id);
                        return CheckboxListTile(
                          value: isSelected,
                          onChanged: (_) =>
                              vm.toggleEmployeeSelection(emp.id),
                          title: Text(emp.name),
                          subtitle: Text(
                            '${emp.statusLabel} | ${emp.roleName} | ${emp.workplaceName}',
                          ),
                          dense: true,
                        );
                      },
                    ),
        ),
        // Pagination + Next
        const Divider(height: 1),
        Padding(
          padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              // Pagination
              Row(
                children: [
                  IconButton(
                    icon: const Icon(Icons.chevron_left),
                    onPressed: vm.employeePageNumber > 1
                        ? () => vm.setEmployeePage(vm.employeePageNumber - 1)
                        : null,
                  ),
                  Text(
                    '${vm.employeePageNumber} / ${vm.employeeTotalPages}',
                    style: textTheme.bodySmall,
                  ),
                  IconButton(
                    icon: const Icon(Icons.chevron_right),
                    onPressed:
                        vm.employeePageNumber < vm.employeeTotalPages
                            ? () => vm.setEmployeePage(
                                vm.employeePageNumber + 1)
                            : null,
                  ),
                  Text(
                    '(${vm.employeesTotalCount} total)',
                    style: textTheme.bodySmall?.copyWith(
                      color: colorScheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
              // Next button
              FilledButton.icon(
                onPressed: vm.hasSelectedEmployees ? widget.onNext : null,
                icon: const Icon(Icons.arrow_forward, size: 18),
                label: const Text('Proximo'),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

/// Filter row for employee selection.
class _EmployeeFilters extends StatelessWidget {
  const _EmployeeFilters({
    required this.nameController,
    required this.viewModel,
    required this.onApply,
    required this.onClear,
  });

  final TextEditingController nameController;
  final BatchDownloadViewModel viewModel;
  final VoidCallback onApply;
  final VoidCallback onClear;

  static const _statusOptions = [
    (id: null, label: 'Todos'),
    (id: 1, label: 'Pendente'),
    (id: 2, label: 'Ativo'),
    (id: 3, label: 'Ferias'),
    (id: 4, label: 'Afastado'),
    (id: 5, label: 'Inativo'),
  ];

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final isWide = constraints.maxWidth >= AppBreakpoints.mobile;

        if (isWide) {
          return Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: TextField(
                  controller: nameController,
                  decoration: const InputDecoration(
                    labelText: 'Nome',
                    prefixIcon: Icon(Icons.search, size: 20),
                    border: OutlineInputBorder(),
                  ),
                  onSubmitted: (_) => onApply(),
                ),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: DropdownButtonFormField<int?>(
                  isExpanded: true,
                  decoration: const InputDecoration(
                    labelText: 'Status',
                    border: OutlineInputBorder(),
                  ),
                  items: _statusOptions
                      .map((o) => DropdownMenuItem(
                            value: o.id,
                            child: Text(o.label, overflow: TextOverflow.ellipsis),
                          ))
                      .toList(),
                  onChanged: (value) {
                    viewModel.setEmployeeStatusFilter(value);
                    onApply();
                  },
                ),
              ),
              if (viewModel.workplaces.isNotEmpty) ...[
                const SizedBox(width: AppSpacing.md),
                Expanded(
                  child: DropdownButtonFormField<String?>(
                    isExpanded: true,
                    decoration: const InputDecoration(
                      labelText: 'Local de Trabalho',
                      border: OutlineInputBorder(),
                    ),
                    items: [
                      const DropdownMenuItem(
                          value: null,
                          child: Text('Todos', overflow: TextOverflow.ellipsis)),
                      ...viewModel.workplaces.map(
                        (w) => DropdownMenuItem(
                            value: w.id,
                            child: Text(w.name, overflow: TextOverflow.ellipsis)),
                      ),
                    ],
                    onChanged: (value) {
                      viewModel.setEmployeeWorkplaceFilter(value);
                      onApply();
                    },
                  ),
                ),
              ],
              const SizedBox(width: AppSpacing.sm),
              IconButton.outlined(
                icon: const Icon(Icons.filter_alt_off, size: 20),
                tooltip: 'Limpar filtros',
                onPressed: onClear,
              ),
            ],
          );
        }

        return Column(
          children: [
            TextField(
              controller: nameController,
              decoration: const InputDecoration(
                labelText: 'Nome',
                prefixIcon: Icon(Icons.search, size: 20),
                border: OutlineInputBorder(),
              ),
              onSubmitted: (_) => onApply(),
            ),
            const SizedBox(height: AppSpacing.sm),
            DropdownButtonFormField<int?>(
              isExpanded: true,
              decoration: const InputDecoration(
                labelText: 'Status',
                border: OutlineInputBorder(),
              ),
              items: _statusOptions
                  .map((o) => DropdownMenuItem(
                        value: o.id,
                        child: Text(o.label, overflow: TextOverflow.ellipsis),
                      ))
                  .toList(),
              onChanged: (value) {
                viewModel.setEmployeeStatusFilter(value);
                onApply();
              },
            ),
            if (viewModel.workplaces.isNotEmpty) ...[
              const SizedBox(height: AppSpacing.sm),
              DropdownButtonFormField<String?>(
                isExpanded: true,
                decoration: const InputDecoration(
                  labelText: 'Local de Trabalho',
                  border: OutlineInputBorder(),
                ),
                items: [
                  const DropdownMenuItem(
                      value: null,
                      child: Text('Todos', overflow: TextOverflow.ellipsis)),
                  ...viewModel.workplaces.map(
                    (w) => DropdownMenuItem(
                        value: w.id,
                        child: Text(w.name, overflow: TextOverflow.ellipsis)),
                  ),
                ],
                onChanged: (value) {
                  viewModel.setEmployeeWorkplaceFilter(value);
                  onApply();
                },
              ),
            ],
            const SizedBox(height: AppSpacing.sm),
            Align(
              alignment: Alignment.centerRight,
              child: IconButton.outlined(
                icon: const Icon(Icons.filter_alt_off, size: 20),
                tooltip: 'Limpar filtros',
                onPressed: onClear,
              ),
            ),
          ],
        );
      },
    );
  }
}
