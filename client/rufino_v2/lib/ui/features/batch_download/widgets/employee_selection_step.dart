import 'package:flutter/material.dart';

import '../../../../core/theme/app_breakpoints.dart';
import '../../../../core/theme/app_spacing.dart';
import '../viewmodel/batch_download_viewmodel.dart';
import 'filter_sheet.dart';

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
  void initState() {
    super.initState();
    _nameController.text = widget.viewModel.employeeNameFilter ?? '';
  }

  @override
  void dispose() {
    _nameController.dispose();
    super.dispose();
  }

  Future<void> _openFiltersSheet() async {
    final vm = widget.viewModel;
    await showFilterSheet<void>(
      context: context,
      title: 'Filtros',
      builder: (sheetContext, setSheetState) {
        return _EmployeeFiltersBody(
          nameController: _nameController,
          viewModel: vm,
          onChanged: () => setSheetState(() {}),
        );
      },
      onApply: () {
        vm.setEmployeeNameFilter(_nameController.text);
        vm.applyEmployeeFilters();
      },
      onClear: () {
        _nameController.clear();
        vm.clearEmployeeFilters();
      },
    );
    if (mounted) setState(() {});
  }

  void _applyInlineFilters() {
    final vm = widget.viewModel;
    vm.setEmployeeNameFilter(_nameController.text);
    vm.applyEmployeeFilters();
  }

  void _clearInlineFilters() {
    _nameController.clear();
    widget.viewModel.clearEmployeeFilters();
  }

  @override
  Widget build(BuildContext context) {
    final vm = widget.viewModel;
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;
    final filterCount = vm.activeEmployeeFilterCount;
    final filterLabel =
        filterCount > 0 ? 'Filtros ($filterCount)' : 'Filtros';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const SizedBox(height: AppSpacing.sm),
        // Inline filters (large screens only)
        LayoutBuilder(
          builder: (context, constraints) {
            final isWide = constraints.maxWidth >= AppBreakpoints.mobile;
            if (!isWide) return const SizedBox.shrink();
            return Padding(
              padding: const EdgeInsets.only(bottom: AppSpacing.sm),
              child: _EmployeeFiltersInline(
                nameController: _nameController,
                viewModel: vm,
                onApply: _applyInlineFilters,
                onClear: _clearInlineFilters,
              ),
            );
          },
        ),
        // Selection actions header
        LayoutBuilder(
          builder: (context, constraints) {
            final isWide = constraints.maxWidth >= AppBreakpoints.mobile;
            final counterText = Text(
              '${vm.selectedEmployeeCount} selecionado(s)',
              style: textTheme.labelMedium?.copyWith(
                color: colorScheme.onSurfaceVariant,
              ),
              overflow: TextOverflow.ellipsis,
            );

            if (isWide) {
              return Row(
                children: [
                  counterText,
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
              );
            }
            return Row(
              children: [
                Expanded(child: counterText),
                OutlinedButton.icon(
                  icon: const Icon(Icons.tune, size: 18),
                  label: Text(filterLabel),
                  onPressed: _openFiltersSheet,
                ),
                IconButton(
                  icon: const Icon(Icons.select_all),
                  tooltip: 'Selecionar Todos',
                  onPressed: vm.selectAllEmployeesOnPage,
                ),
                IconButton(
                  icon: const Icon(Icons.clear),
                  tooltip: 'Limpar',
                  onPressed: vm.hasSelectedEmployees
                      ? vm.clearEmployeeSelection
                      : null,
                ),
              ],
            );
          },
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
          child: LayoutBuilder(
            builder: (context, constraints) {
              final isWide = constraints.maxWidth >= AppBreakpoints.mobile;
              final pagination = Row(
                mainAxisSize: MainAxisSize.min,
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
              );
              final nextButton = FilledButton.icon(
                onPressed: vm.hasSelectedEmployees ? widget.onNext : null,
                icon: const Icon(Icons.arrow_forward, size: 18),
                label: const Text('Proximo'),
              );

              if (isWide) {
                return Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [pagination, nextButton],
                );
              }
              return Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Center(child: pagination),
                  const SizedBox(height: AppSpacing.sm),
                  Align(
                    alignment: Alignment.centerRight,
                    child: nextButton,
                  ),
                ],
              );
            },
          ),
        ),
      ],
    );
  }
}

/// Inline employee filter row used on wide screens (>= mobile breakpoint).
class _EmployeeFiltersInline extends StatelessWidget {
  const _EmployeeFiltersInline({
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
            initialValue: viewModel.employeeStatusFilter,
            decoration: const InputDecoration(
              labelText: 'Status',
              border: OutlineInputBorder(),
            ),
            items: _statusOptions
                .map(
                  (o) => DropdownMenuItem(
                    value: o.id,
                    child:
                        Text(o.label, overflow: TextOverflow.ellipsis),
                  ),
                )
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
              initialValue: viewModel.employeeWorkplaceFilter,
              decoration: const InputDecoration(
                labelText: 'Local de Trabalho',
                border: OutlineInputBorder(),
              ),
              items: [
                const DropdownMenuItem(
                  value: null,
                  child: Text('Todos', overflow: TextOverflow.ellipsis),
                ),
                ...viewModel.workplaces.map(
                  (w) => DropdownMenuItem(
                    value: w.id,
                    child: Text(w.name, overflow: TextOverflow.ellipsis),
                  ),
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
}

/// Single-column body for the employee filters bottom sheet.
class _EmployeeFiltersBody extends StatelessWidget {
  const _EmployeeFiltersBody({
    required this.nameController,
    required this.viewModel,
    required this.onChanged,
  });

  final TextEditingController nameController;
  final BatchDownloadViewModel viewModel;

  /// Called whenever a field that affects sheet rendering is changed.
  final VoidCallback onChanged;

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
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        TextField(
          controller: nameController,
          decoration: const InputDecoration(
            labelText: 'Nome',
            prefixIcon: Icon(Icons.search, size: 20),
            border: OutlineInputBorder(),
          ),
          onChanged: (_) => onChanged(),
        ),
        const SizedBox(height: AppSpacing.md),
        DropdownButtonFormField<int?>(
          isExpanded: true,
          initialValue: viewModel.employeeStatusFilter,
          decoration: const InputDecoration(
            labelText: 'Status',
            border: OutlineInputBorder(),
          ),
          items: _statusOptions
              .map(
                (o) => DropdownMenuItem(
                  value: o.id,
                  child: Text(o.label, overflow: TextOverflow.ellipsis),
                ),
              )
              .toList(),
          onChanged: (value) {
            viewModel.setEmployeeStatusFilter(value);
            onChanged();
          },
        ),
        if (viewModel.workplaces.isNotEmpty) ...[
          const SizedBox(height: AppSpacing.md),
          DropdownButtonFormField<String?>(
            isExpanded: true,
            initialValue: viewModel.employeeWorkplaceFilter,
            decoration: const InputDecoration(
              labelText: 'Local de Trabalho',
              border: OutlineInputBorder(),
            ),
            items: [
              const DropdownMenuItem(
                value: null,
                child: Text('Todos', overflow: TextOverflow.ellipsis),
              ),
              ...viewModel.workplaces.map(
                (w) => DropdownMenuItem(
                  value: w.id,
                  child: Text(w.name, overflow: TextOverflow.ellipsis),
                ),
              ),
            ],
            onChanged: (value) {
              viewModel.setEmployeeWorkplaceFilter(value);
              onChanged();
            },
          ),
        ],
      ],
    );
  }
}
