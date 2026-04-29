import 'package:flutter/material.dart';

import '../../../../core/theme/app_breakpoints.dart';
import '../../../../core/theme/app_spacing.dart';
import '../viewmodel/batch_download_viewmodel.dart';
import 'filter_sheet.dart';

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
  void initState() {
    super.initState();
    final vm = widget.viewModel;
    _dateFromCtrl.text = vm.dateFromFilter ?? '';
    _dateToCtrl.text = vm.dateToFilter ?? '';
  }

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

  Future<void> _openFiltersSheet() async {
    final vm = widget.viewModel;
    await showFilterSheet<void>(
      context: context,
      title: 'Filtros',
      builder: (sheetContext, setSheetState) {
        return _UnitFiltersBody(
          vm: vm,
          dateFromCtrl: _dateFromCtrl,
          dateToCtrl: _dateToCtrl,
          yearCtrl: _yearCtrl,
          monthCtrl: _monthCtrl,
          dayCtrl: _dayCtrl,
          weekCtrl: _weekCtrl,
          onChanged: () => setSheetState(() {}),
          onPeriodTypeChanged: () {
            _yearCtrl.clear();
            _monthCtrl.clear();
            _dayCtrl.clear();
            _weekCtrl.clear();
            setSheetState(() {});
          },
        );
      },
      onApply: _applyFilters,
      onClear: _clearFilters,
    );
    if (mounted) setState(() {});
  }

  @override
  Widget build(BuildContext context) {
    final vm = widget.viewModel;
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;
    final filterCount = vm.activeUnitFilterCount;
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
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  _DocumentFilterRowInline(
                    vm: vm,
                    onApply: _applyFilters,
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  _DateFilterRowInline(
                    dateFromCtrl: _dateFromCtrl,
                    dateToCtrl: _dateToCtrl,
                    onSubmitted: _applyFilters,
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  _PeriodFilterRowInline(
                    vm: vm,
                    yearCtrl: _yearCtrl,
                    monthCtrl: _monthCtrl,
                    dayCtrl: _dayCtrl,
                    weekCtrl: _weekCtrl,
                    onSubmitted: _applyFilters,
                    onPeriodTypeChanged: () {
                      _yearCtrl.clear();
                      _monthCtrl.clear();
                      _dayCtrl.clear();
                      _weekCtrl.clear();
                    },
                  ),
                ],
              ),
            );
          },
        ),
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
        // Selection actions header
        LayoutBuilder(
          builder: (context, constraints) {
            final isWide = constraints.maxWidth >= AppBreakpoints.mobile;
            final counterText = Text(
              '${vm.selectedUnitCount} selecionado(s)',
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
                  IconButton.outlined(
                    icon: const Icon(Icons.filter_alt_off, size: 18),
                    tooltip: 'Limpar filtros',
                    onPressed: _clearFilters,
                    visualDensity: VisualDensity.compact,
                  ),
                  TextButton(
                    onPressed: vm.selectAllUnitsOnPage,
                    child: const Text('Selecionar Todos'),
                  ),
                  TextButton(
                    onPressed: vm.hasSelectedUnits
                        ? vm.clearUnitSelection
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
                  onPressed: vm.selectAllUnitsOnPage,
                ),
                IconButton(
                  icon: const Icon(Icons.clear),
                  tooltip: 'Limpar',
                  onPressed:
                      vm.hasSelectedUnits ? vm.clearUnitSelection : null,
                ),
              ],
            );
          },
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
          child: LayoutBuilder(
            builder: (context, constraints) {
              final isWide = constraints.maxWidth >= AppBreakpoints.mobile;

              final pagination = Row(
                mainAxisSize: MainAxisSize.min,
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
              );

              final navButtons = [
                OutlinedButton(
                  onPressed: vm.goBack,
                  child: const Text('Voltar'),
                ),
                OutlinedButton.icon(
                  onPressed:
                      vm.hasSelectedUnits ? vm.addToCombination : null,
                  icon: const Icon(Icons.playlist_add, size: 18),
                  label: const Text('Adicionar a combinacao'),
                ),
                FilledButton.icon(
                  onPressed: vm.hasSelectedUnits || vm.isCombineMode
                      ? widget.onNext
                      : null,
                  icon: const Icon(Icons.arrow_forward, size: 18),
                  label: const Text('Proximo'),
                ),
              ];

              if (isWide) {
                return Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    pagination,
                    Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        navButtons[0],
                        const SizedBox(width: AppSpacing.sm),
                        navButtons[1],
                        const SizedBox(width: AppSpacing.sm),
                        navButtons[2],
                      ],
                    ),
                  ],
                );
              }

              return Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Center(child: pagination),
                  const SizedBox(height: AppSpacing.sm),
                  Wrap(
                    alignment: WrapAlignment.end,
                    spacing: AppSpacing.sm,
                    runSpacing: AppSpacing.sm,
                    children: navButtons,
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

/// Inline row with document group, template, and status dropdowns.
class _DocumentFilterRowInline extends StatelessWidget {
  const _DocumentFilterRowInline({
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
    final groupDropdown = vm.groups.isNotEmpty
        ? DropdownButtonFormField<String?>(
            isExpanded: true,
            initialValue: vm.unitGroupFilter,
            decoration: const InputDecoration(
              labelText: 'Grupo',
              border: OutlineInputBorder(),
            ),
            items: [
              const DropdownMenuItem(
                value: null,
                child: Text('Todos', overflow: TextOverflow.ellipsis),
              ),
              ...vm.groups.map(
                (g) => DropdownMenuItem(
                  value: g.id,
                  child: Text(g.name, overflow: TextOverflow.ellipsis),
                ),
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
            initialValue: vm.unitTemplateFilter,
            decoration: const InputDecoration(
              labelText: 'Documento',
              border: OutlineInputBorder(),
            ),
            items: [
              const DropdownMenuItem(
                value: null,
                child: Text('Todos', overflow: TextOverflow.ellipsis),
              ),
              ...vm.availableTemplates.map(
                (t) => DropdownMenuItem(
                  value: t.id,
                  child: Text(t.name, overflow: TextOverflow.ellipsis),
                ),
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
      initialValue: vm.unitStatusFilter,
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
}

/// Inline row with date range filters (Data De / Data Ate).
class _DateFilterRowInline extends StatelessWidget {
  const _DateFilterRowInline({
    required this.dateFromCtrl,
    required this.dateToCtrl,
    required this.onSubmitted,
  });

  final TextEditingController dateFromCtrl;
  final TextEditingController dateToCtrl;
  final VoidCallback onSubmitted;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          child: TextField(
            controller: dateFromCtrl,
            decoration: const InputDecoration(
              labelText: 'Data De (yyyy-MM-dd)',
              border: OutlineInputBorder(),
            ),
            keyboardType: TextInputType.datetime,
            onSubmitted: (_) => onSubmitted(),
          ),
        ),
        const SizedBox(width: AppSpacing.md),
        Expanded(
          child: TextField(
            controller: dateToCtrl,
            decoration: const InputDecoration(
              labelText: 'Data Ate (yyyy-MM-dd)',
              border: OutlineInputBorder(),
            ),
            keyboardType: TextInputType.datetime,
            onSubmitted: (_) => onSubmitted(),
          ),
        ),
      ],
    );
  }
}

/// Inline row of period-related filter fields used on wide screens.
class _PeriodFilterRowInline extends StatelessWidget {
  const _PeriodFilterRowInline({
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
    final periodDropdown = DropdownButtonFormField<int?>(
      isExpanded: true,
      initialValue: vm.periodTypeFilter,
      decoration: const InputDecoration(
        labelText: 'Competencia',
        border: OutlineInputBorder(),
      ),
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
}

/// Single-column body for the unit filters bottom sheet.
class _UnitFiltersBody extends StatelessWidget {
  const _UnitFiltersBody({
    required this.vm,
    required this.dateFromCtrl,
    required this.dateToCtrl,
    required this.yearCtrl,
    required this.monthCtrl,
    required this.dayCtrl,
    required this.weekCtrl,
    required this.onChanged,
    required this.onPeriodTypeChanged,
  });

  final BatchDownloadViewModel vm;
  final TextEditingController dateFromCtrl;
  final TextEditingController dateToCtrl;
  final TextEditingController yearCtrl;
  final TextEditingController monthCtrl;
  final TextEditingController dayCtrl;
  final TextEditingController weekCtrl;
  final VoidCallback onChanged;
  final VoidCallback onPeriodTypeChanged;

  static const _statusOptions = [
    (id: null, label: 'Todos'),
    (id: 1, label: 'Pendente'),
    (id: 2, label: 'OK'),
    (id: 3, label: 'Obsoleto'),
    (id: 4, label: 'Invalido'),
    (id: 5, label: 'Requer Validacao'),
    (id: 7, label: 'Aguardando Assinatura'),
  ];

  static const _periodTypeOptions = [
    (id: null, label: 'Todos'),
    (id: 1, label: 'Diario'),
    (id: 2, label: 'Semanal'),
    (id: 3, label: 'Mensal'),
    (id: 4, label: 'Anual'),
  ];

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (vm.groups.isNotEmpty) ...[
          DropdownButtonFormField<String?>(
            isExpanded: true,
            initialValue: vm.unitGroupFilter,
            decoration: const InputDecoration(
              labelText: 'Grupo',
              border: OutlineInputBorder(),
            ),
            items: [
              const DropdownMenuItem(
                value: null,
                child: Text('Todos', overflow: TextOverflow.ellipsis),
              ),
              ...vm.groups.map(
                (g) => DropdownMenuItem(
                  value: g.id,
                  child: Text(g.name, overflow: TextOverflow.ellipsis),
                ),
              ),
            ],
            onChanged: (value) {
              vm.setUnitGroupFilter(value);
              onChanged();
            },
          ),
          const SizedBox(height: AppSpacing.md),
        ],
        if (vm.availableTemplates.isNotEmpty) ...[
          DropdownButtonFormField<String?>(
            isExpanded: true,
            initialValue: vm.unitTemplateFilter,
            decoration: const InputDecoration(
              labelText: 'Documento',
              border: OutlineInputBorder(),
            ),
            items: [
              const DropdownMenuItem(
                value: null,
                child: Text('Todos', overflow: TextOverflow.ellipsis),
              ),
              ...vm.availableTemplates.map(
                (t) => DropdownMenuItem(
                  value: t.id,
                  child: Text(t.name, overflow: TextOverflow.ellipsis),
                ),
              ),
            ],
            onChanged: (value) {
              vm.setUnitTemplateFilter(value);
              onChanged();
            },
          ),
          const SizedBox(height: AppSpacing.md),
        ],
        DropdownButtonFormField<int?>(
          isExpanded: true,
          initialValue: vm.unitStatusFilter,
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
            vm.setUnitStatusFilter(value);
            onChanged();
          },
        ),
        const SizedBox(height: AppSpacing.md),
        TextField(
          controller: dateFromCtrl,
          decoration: const InputDecoration(
            labelText: 'Data De (yyyy-MM-dd)',
            border: OutlineInputBorder(),
          ),
          keyboardType: TextInputType.datetime,
          onChanged: (_) => onChanged(),
        ),
        const SizedBox(height: AppSpacing.md),
        TextField(
          controller: dateToCtrl,
          decoration: const InputDecoration(
            labelText: 'Data Ate (yyyy-MM-dd)',
            border: OutlineInputBorder(),
          ),
          keyboardType: TextInputType.datetime,
          onChanged: (_) => onChanged(),
        ),
        const SizedBox(height: AppSpacing.md),
        DropdownButtonFormField<int?>(
          isExpanded: true,
          initialValue: vm.periodTypeFilter,
          decoration: const InputDecoration(
            labelText: 'Competencia',
            border: OutlineInputBorder(),
          ),
          items: _periodTypeOptions
              .map(
                (o) => DropdownMenuItem(
                  value: o.id,
                  child: Text(o.label, overflow: TextOverflow.ellipsis),
                ),
              )
              .toList(),
          onChanged: (v) {
            vm.setPeriodFilter(typeId: v);
            onPeriodTypeChanged();
          },
        ),
        if (vm.periodTypeFilter != null) ...[
          const SizedBox(height: AppSpacing.md),
          TextField(
            controller: yearCtrl,
            decoration: const InputDecoration(
              labelText: 'Ano',
              border: OutlineInputBorder(),
            ),
            keyboardType: TextInputType.number,
            onChanged: (_) => onChanged(),
          ),
          if (vm.periodTypeFilter != 4) ...[
            const SizedBox(height: AppSpacing.md),
            TextField(
              controller: monthCtrl,
              decoration: const InputDecoration(
                labelText: 'Mes',
                border: OutlineInputBorder(),
              ),
              keyboardType: TextInputType.number,
              onChanged: (_) => onChanged(),
            ),
          ],
          if (vm.periodTypeFilter == 1) ...[
            const SizedBox(height: AppSpacing.md),
            TextField(
              controller: dayCtrl,
              decoration: const InputDecoration(
                labelText: 'Dia',
                border: OutlineInputBorder(),
              ),
              keyboardType: TextInputType.number,
              onChanged: (_) => onChanged(),
            ),
          ],
          if (vm.periodTypeFilter == 2) ...[
            const SizedBox(height: AppSpacing.md),
            TextField(
              controller: weekCtrl,
              decoration: const InputDecoration(
                labelText: 'Semana',
                border: OutlineInputBorder(),
              ),
              keyboardType: TextInputType.number,
              onChanged: (_) => onChanged(),
            ),
          ],
        ],
      ],
    );
  }
}
