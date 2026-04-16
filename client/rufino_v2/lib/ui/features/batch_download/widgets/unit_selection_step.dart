import 'package:flutter/material.dart';

import '../../../../core/theme/app_breakpoints.dart';
import '../../../../core/theme/app_spacing.dart';
import '../viewmodel/batch_download_viewmodel.dart';

/// Step 2: Document unit selection with filters.
///
/// Displays a filterable, paginated list of document units for the
/// previously selected employees. Only units with files can be selected.
class UnitSelectionStep extends StatefulWidget {
  const UnitSelectionStep({
    super.key,
    required this.viewModel,
    required this.onNext,
  });

  /// The parent view model.
  final BatchDownloadViewModel viewModel;

  /// Callback invoked when the user taps "Proximo".
  final VoidCallback onNext;

  @override
  State<UnitSelectionStep> createState() => _UnitSelectionStepState();
}

class _UnitSelectionStepState extends State<UnitSelectionStep> {
  final _dateFromCtrl = TextEditingController();
  final _dateToCtrl = TextEditingController();
  final _yearCtrl = TextEditingController();
  final _monthCtrl = TextEditingController();
  final _dayCtrl = TextEditingController();
  final _weekCtrl = TextEditingController();

  @override
  void dispose() {
    _dateFromCtrl.dispose();
    _dateToCtrl.dispose();
    _yearCtrl.dispose();
    _monthCtrl.dispose();
    _dayCtrl.dispose();
    _weekCtrl.dispose();
    super.dispose();
  }

  void _applyFilters() {
    final vm = widget.viewModel;
    vm.setDateFromFilter(_dateFromCtrl.text);
    vm.setDateToFilter(_dateToCtrl.text);
    vm.setPeriodFilter(
      typeId: vm.periodTypeFilter,
      year: int.tryParse(_yearCtrl.text),
      month: int.tryParse(_monthCtrl.text),
      day: int.tryParse(_dayCtrl.text),
      week: int.tryParse(_weekCtrl.text),
    );
    vm.applyUnitFilters();
  }

  void _clearFilters() {
    _dateFromCtrl.clear();
    _dateToCtrl.clear();
    _yearCtrl.clear();
    _monthCtrl.clear();
    _dayCtrl.clear();
    _weekCtrl.clear();
    widget.viewModel.clearUnitFilters();
  }

  void _onPeriodTypeChanged() {
    _yearCtrl.clear();
    _monthCtrl.clear();
    _dayCtrl.clear();
    _weekCtrl.clear();
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
        _DocumentFilterRow(
          vm: vm,
          onApply: _applyFilters,
        ),
        const SizedBox(height: AppSpacing.sm),
        _DateFilterRow(
          dateFromCtrl: _dateFromCtrl,
          dateToCtrl: _dateToCtrl,
          onSubmitted: _applyFilters,
        ),
        const SizedBox(height: AppSpacing.sm),
        _PeriodFilterRow(
          vm: vm,
          yearCtrl: _yearCtrl,
          monthCtrl: _monthCtrl,
          dayCtrl: _dayCtrl,
          weekCtrl: _weekCtrl,
          onSubmitted: _applyFilters,
          onPeriodTypeChanged: _onPeriodTypeChanged,
        ),
        const SizedBox(height: AppSpacing.sm),
        // Combination indicator
        if (vm.isCombineMode)
          Padding(
            padding: const EdgeInsets.only(bottom: AppSpacing.sm),
            child: Chip(
              avatar: Icon(
                Icons.merge_type_rounded,
                size: 18,
                color: colorScheme.primary,
              ),
              label: Text(
                '${vm.combinationGroupCount} grupo(s) | '
                '${vm.combinedTotalUnitCount} documento(s)',
              ),
              backgroundColor: colorScheme.primaryContainer,
              side: BorderSide.none,
            ),
          ),
        // Clear + selection actions
        Row(
          children: [
            Text(
              '${vm.selectedUnitCount} selecionado(s)',
              style: textTheme.labelMedium?.copyWith(
                color: colorScheme.onSurfaceVariant,
              ),
            ),
            const SizedBox(width: AppSpacing.sm),
            IconButton.outlined(
              icon: const Icon(Icons.filter_alt_off, size: 18),
              tooltip: 'Limpar filtros',
              onPressed: _clearFilters,
              visualDensity: VisualDensity.compact,
            ),
            const Spacer(),
            TextButton(
              onPressed: vm.selectAllUnitsOnPage,
              child: const Text('Selecionar Todos'),
            ),
            TextButton(
              onPressed: vm.hasSelectedUnits ? vm.clearUnitSelection : null,
              child: const Text('Limpar'),
            ),
          ],
        ),
        const Divider(height: 1),
        // Unit list
        Expanded(
          child: vm.status == BatchDownloadStatus.loading
              ? const Center(child: CircularProgressIndicator())
              : vm.units.isEmpty
                  ? Center(
                      child: Text(
                        'Nenhum documento encontrado',
                        style: textTheme.bodyLarge?.copyWith(
                          color: colorScheme.onSurfaceVariant,
                        ),
                      ),
                    )
                  : ListView.builder(
                      itemCount: vm.units.length,
                      itemBuilder: (context, index) {
                        final unit = vm.units[index];
                        final isSelected =
                            vm.selectedUnitKeys.contains(unit.selectionKey);
                        final canSelect = unit.hasFile;
                        return CheckboxListTile(
                          value: isSelected,
                          onChanged: canSelect
                              ? (_) => vm.toggleUnitSelection(
                                  unit.documentId, unit.documentUnitId)
                              : null,
                          title: Text(
                            '${unit.employeeName} - ${unit.documentTemplateName}',
                            overflow: TextOverflow.ellipsis,
                            style: canSelect
                                ? null
                                : textTheme.bodyLarge?.copyWith(
                                    color: colorScheme.onSurface
                                        .withValues(alpha: 0.38),
                                  ),
                          ),
                          subtitle: Text(
                            [
                              unit.date,
                              unit.statusLabel,
                              if (unit.period != null)
                                unit.period!.formattedPeriod,
                              if (!canSelect) '(sem arquivo)',
                            ].join(' | '),
                            overflow: TextOverflow.ellipsis,
                            style: canSelect
                                ? null
                                : textTheme.bodySmall?.copyWith(
                                    color: colorScheme.onSurface
                                        .withValues(alpha: 0.38),
                                  ),
                          ),
                          dense: true,
                        );
                      },
                    ),
        ),
        // Pagination + Navigation
        const Divider(height: 1),
        Padding(
          padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  IconButton(
                    icon: const Icon(Icons.chevron_left),
                    onPressed: vm.unitPageNumber > 1
                        ? () => vm.setUnitPage(vm.unitPageNumber - 1)
                        : null,
                  ),
                  Text(
                    '${vm.unitPageNumber} / ${vm.unitTotalPages}',
                    style: textTheme.bodySmall,
                  ),
                  IconButton(
                    icon: const Icon(Icons.chevron_right),
                    onPressed: vm.unitPageNumber < vm.unitTotalPages
                        ? () => vm.setUnitPage(vm.unitPageNumber + 1)
                        : null,
                  ),
                  Text(
                    '(${vm.unitsTotalCount} total)',
                    style: textTheme.bodySmall?.copyWith(
                      color: colorScheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
              Row(
                children: [
                  OutlinedButton(
                    onPressed: vm.goBack,
                    child: const Text('Voltar'),
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  OutlinedButton.icon(
                    onPressed: vm.hasSelectedUnits
                        ? vm.addToCombination
                        : null,
                    icon: const Icon(Icons.playlist_add, size: 18),
                    label: const Text('Adicionar a combinacao'),
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  FilledButton.icon(
                    onPressed: vm.hasSelectedUnits || vm.isCombineMode
                        ? widget.onNext
                        : null,
                    icon: const Icon(Icons.arrow_forward, size: 18),
                    label: const Text('Proximo'),
                  ),
                ],
              ),
            ],
          ),
        ),
      ],
    );
  }
}

/// Row with document group, template, and status filters.
class _DocumentFilterRow extends StatelessWidget {
  const _DocumentFilterRow({
    required this.vm,
    required this.onApply,
  });

  final BatchDownloadViewModel vm;
  final VoidCallback onApply;

  static const _statusOptions = [
    (id: null, label: 'Todos'),
    (id: 1, label: 'Pendente'),
    (id: 2, label: 'OK'),
    (id: 3, label: 'Obsoleto'),
    (id: 4, label: 'Invalido'),
    (id: 5, label: 'Requer Validacao'),
    (id: 7, label: 'Aguardando Assinatura'),
  ];

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final isWide = constraints.maxWidth >= AppBreakpoints.mobile;

        final groupDropdown = vm.groups.isNotEmpty
            ? DropdownButtonFormField<String?>(
                isExpanded: true,
                decoration: const InputDecoration(
                  labelText: 'Grupo',
                  border: OutlineInputBorder(),
                ),
                items: [
                  const DropdownMenuItem(
                      value: null,
                      child: Text('Todos', overflow: TextOverflow.ellipsis)),
                  ...vm.groups.map(
                    (g) => DropdownMenuItem(
                        value: g.id,
                        child: Text(g.name, overflow: TextOverflow.ellipsis)),
                  ),
                ],
                onChanged: (value) {
                  vm.setUnitGroupFilter(value);
                  onApply();
                },
              )
            : null;

        final templateDropdown = vm.availableTemplates.isNotEmpty
            ? DropdownButtonFormField<String?>(
                isExpanded: true,
                decoration: const InputDecoration(
                  labelText: 'Documento',
                  border: OutlineInputBorder(),
                ),
                items: [
                  const DropdownMenuItem(
                      value: null,
                      child: Text('Todos', overflow: TextOverflow.ellipsis)),
                  ...vm.availableTemplates.map(
                    (t) => DropdownMenuItem(
                        value: t.id,
                        child: Text(t.name, overflow: TextOverflow.ellipsis)),
                  ),
                ],
                onChanged: (value) {
                  vm.setUnitTemplateFilter(value);
                  onApply();
                },
              )
            : null;

        final statusDropdown = DropdownButtonFormField<int?>(
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
            vm.setUnitStatusFilter(value);
            onApply();
          },
        );

        if (isWide) {
          return Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (groupDropdown != null) ...[
                Expanded(child: groupDropdown),
                const SizedBox(width: AppSpacing.md),
              ],
              if (templateDropdown != null) ...[
                Expanded(child: templateDropdown),
                const SizedBox(width: AppSpacing.md),
              ],
              Expanded(child: statusDropdown),
            ],
          );
        }

        return Column(
          children: [
            if (groupDropdown != null) ...[
              groupDropdown,
              const SizedBox(height: AppSpacing.sm),
            ],
            if (templateDropdown != null) ...[
              templateDropdown,
              const SizedBox(height: AppSpacing.sm),
            ],
            statusDropdown,
          ],
        );
      },
    );
  }
}

/// Row with date range filters (Data De / Data Ate).
class _DateFilterRow extends StatelessWidget {
  const _DateFilterRow({
    required this.dateFromCtrl,
    required this.dateToCtrl,
    required this.onSubmitted,
  });

  final TextEditingController dateFromCtrl;
  final TextEditingController dateToCtrl;
  final VoidCallback onSubmitted;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final isWide = constraints.maxWidth >= AppBreakpoints.mobile;

        final fromField = TextField(
          controller: dateFromCtrl,
          decoration: const InputDecoration(
            labelText: 'Data De (yyyy-MM-dd)',
            border: OutlineInputBorder(),
          ),
          keyboardType: TextInputType.datetime,
          onSubmitted: (_) => onSubmitted(),
        );

        final toField = TextField(
          controller: dateToCtrl,
          decoration: const InputDecoration(
            labelText: 'Data Ate (yyyy-MM-dd)',
            border: OutlineInputBorder(),
          ),
          keyboardType: TextInputType.datetime,
          onSubmitted: (_) => onSubmitted(),
        );

        if (isWide) {
          return Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(child: fromField),
              const SizedBox(width: AppSpacing.md),
              Expanded(child: toField),
            ],
          );
        }

        return Column(
          children: [
            fromField,
            const SizedBox(height: AppSpacing.sm),
            toField,
          ],
        );
      },
    );
  }
}

/// Responsive row of period-related filter fields.
///
/// Follows the same pattern as the batch document screen:
/// first select period type, then fill year/month/day/week dynamically.
class _PeriodFilterRow extends StatelessWidget {
  const _PeriodFilterRow({
    required this.vm,
    required this.yearCtrl,
    required this.monthCtrl,
    required this.dayCtrl,
    required this.weekCtrl,
    required this.onSubmitted,
    required this.onPeriodTypeChanged,
  });

  final BatchDownloadViewModel vm;
  final TextEditingController yearCtrl;
  final TextEditingController monthCtrl;
  final TextEditingController dayCtrl;
  final TextEditingController weekCtrl;
  final VoidCallback onSubmitted;
  final VoidCallback onPeriodTypeChanged;

  static const _periodTypeOptions = [
    (id: null, label: 'Todos'),
    (id: 1, label: 'Diario'),
    (id: 2, label: 'Semanal'),
    (id: 3, label: 'Mensal'),
    (id: 4, label: 'Anual'),
  ];

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final isWide = constraints.maxWidth >= AppBreakpoints.mobile;

        final periodDropdown = DropdownButtonFormField<int?>(
          isExpanded: true,
          decoration: const InputDecoration(
            labelText: 'Competencia',
            border: OutlineInputBorder(),
          ),
          initialValue: vm.periodTypeFilter,
          items: _periodTypeOptions
              .map((o) => DropdownMenuItem(
                    value: o.id,
                    child: Text(o.label, overflow: TextOverflow.ellipsis),
                  ))
              .toList(),
          onChanged: (v) {
            vm.setPeriodFilter(typeId: v);
            onPeriodTypeChanged();
          },
        );

        final periodFields = <Widget>[
          if (vm.periodTypeFilter != null) ...[
            Expanded(
              child: TextField(
                controller: yearCtrl,
                decoration: const InputDecoration(
                  labelText: 'Ano',
                  border: OutlineInputBorder(),
                ),
                keyboardType: TextInputType.number,
                onSubmitted: (_) => onSubmitted(),
              ),
            ),
            if (vm.periodTypeFilter != 4) ...[
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: TextField(
                  controller: monthCtrl,
                  decoration: const InputDecoration(
                    labelText: 'Mes',
                    border: OutlineInputBorder(),
                  ),
                  keyboardType: TextInputType.number,
                  onSubmitted: (_) => onSubmitted(),
                ),
              ),
            ],
            if (vm.periodTypeFilter == 1) ...[
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: TextField(
                  controller: dayCtrl,
                  decoration: const InputDecoration(
                    labelText: 'Dia',
                    border: OutlineInputBorder(),
                  ),
                  keyboardType: TextInputType.number,
                  onSubmitted: (_) => onSubmitted(),
                ),
              ),
            ],
            if (vm.periodTypeFilter == 2) ...[
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: TextField(
                  controller: weekCtrl,
                  decoration: const InputDecoration(
                    labelText: 'Semana',
                    border: OutlineInputBorder(),
                  ),
                  keyboardType: TextInputType.number,
                  onSubmitted: (_) => onSubmitted(),
                ),
              ),
            ],
          ],
        ];

        if (isWide) {
          return Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(child: periodDropdown),
              if (periodFields.isNotEmpty) ...[
                const SizedBox(width: AppSpacing.md),
                ...periodFields,
              ],
            ],
          );
        }

        return Column(
          children: [
            periodDropdown,
            if (vm.periodTypeFilter != null) ...[
              const SizedBox(height: AppSpacing.md),
              Row(children: periodFields),
            ],
          ],
        );
      },
    );
  }
}
