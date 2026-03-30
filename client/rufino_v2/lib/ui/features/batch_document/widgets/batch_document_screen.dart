import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';

import '../../../../core/theme/app_breakpoints.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../domain/entities/batch_document_unit.dart';
import '../../../core/widgets/permission_guard.dart';
import '../viewmodel/batch_document_viewmodel.dart';

/// Main screen for batch document management.
///
/// Allows the user to select a document template, view all pending document
/// units across employees, stage files for upload, and perform batch
/// operations (create, update date, upload, send to sign).
class BatchDocumentScreen extends StatefulWidget {
  const BatchDocumentScreen({super.key, required this.viewModel});

  /// The view model that manages state for this screen.
  final BatchDocumentViewModel viewModel;

  @override
  State<BatchDocumentScreen> createState() => _BatchDocumentScreenState();
}

class _BatchDocumentScreenState extends State<BatchDocumentScreen> {
  @override
  void initState() {
    super.initState();
    widget.viewModel.addListener(_onViewModelChanged);
    widget.viewModel.loadGroupsAndTemplates();
  }

  @override
  void dispose() {
    widget.viewModel.removeListener(_onViewModelChanged);
    super.dispose();
  }

  void _onViewModelChanged() {
    if (!mounted) return;
    setState(() {});
    final vm = widget.viewModel;
    if (vm.status == BatchDocumentStatus.error && vm.errorMessage != null) {
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            content: Text(vm.errorMessage!),
            behavior: SnackBarBehavior.floating,
          ),
        );
    }
    if (vm.status == BatchDocumentStatus.uploadComplete) {
      _showUploadResultsDialog();
    }
  }

  @override
  Widget build(BuildContext context) {
    final vm = widget.viewModel;
    final colorScheme = Theme.of(context).colorScheme;

    final width = MediaQuery.sizeOf(context).width;
    final horizontalPadding = width >= AppBreakpoints.tablet
        ? AppSpacing.xl
        : width >= AppBreakpoints.mobile
            ? AppSpacing.lg
            : AppSpacing.md;

    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.go('/home'),
        ),
        title: const Text('Documentos em Lote'),
        centerTitle: false,
      ),
      body: SafeArea(
        child: Column(
          children: [
            if (vm.status == BatchDocumentStatus.uploading)
              LinearProgressIndicator(
                color: colorScheme.primary,
                backgroundColor: colorScheme.surfaceContainerHigh,
              ),
            Expanded(
              child: Center(
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 960),
                  child: Padding(
                    padding: EdgeInsets.symmetric(
                      horizontal: horizontalPadding,
                    ),
                    child: _DocumentContent(
                      vm: vm,
                      onPickFile: _pickFileForUnit,
                      onCreateMissing: _showCreateMissingDialog,
                      onBatchUpdateDate: _showBatchDateDialog,
                      onSendToSign: _showSignDateDialog,
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _pickFileForUnit(BatchDocumentUnitItem unit) async {
    final result = await FilePicker.platform.pickFiles(withData: true);
    if (result == null || result.files.isEmpty) return;
    final file = result.files.first;
    if (file.bytes == null) return;
    widget.viewModel.stageFile(
      unit.documentUnitId,
      unit.documentId,
      unit.employeeId,
      file.bytes!,
      file.name,
    );
  }

  Future<void> _showCreateMissingDialog() async {
    await widget.viewModel.loadMissingEmployees();
    if (!mounted) return;
    final selected = <String>{};
    var searchQuery = '';
    await showDialog<void>(
      context: context,
      builder: (ctx) {
        return StatefulBuilder(
          builder: (ctx, setDialogState) {
            final allEmployees = widget.viewModel.missingEmployees;
            final filtered = searchQuery.isEmpty
                ? allEmployees
                : allEmployees
                    .where((e) => e.employeeName
                        .toLowerCase()
                        .contains(searchQuery.toLowerCase()))
                    .toList();
            final colorScheme = Theme.of(ctx).colorScheme;
            final textTheme = Theme.of(ctx).textTheme;
            return AlertDialog(
              icon: Icon(
                Icons.person_add_outlined,
                color: colorScheme.primary,
              ),
              title: const Text('Criar Documentos Faltantes'),
              content: SizedBox(
                width: 420,
                height: 480,
                child: Column(
                  children: [
                    TextField(
                      decoration: InputDecoration(
                        labelText: 'Buscar por nome',
                        prefixIcon: const Icon(Icons.search),
                        border: const OutlineInputBorder(),
                        isDense: true,
                        filled: true,
                        fillColor: colorScheme.surfaceContainerLow,
                      ),
                      onChanged: (value) {
                        setDialogState(() => searchQuery = value);
                      },
                    ),
                    const SizedBox(height: AppSpacing.sm),
                    if (selected.isNotEmpty)
                      Padding(
                        padding: const EdgeInsets.only(
                          bottom: AppSpacing.sm,
                        ),
                        child: Row(
                          children: [
                            Icon(
                              Icons.check_circle_outline,
                              size: 16,
                              color: colorScheme.primary,
                            ),
                            const SizedBox(width: AppSpacing.xs),
                            Text(
                              '${selected.length} selecionado(s)',
                              style: textTheme.labelMedium?.copyWith(
                                color: colorScheme.primary,
                              ),
                            ),
                            const Spacer(),
                            TextButton(
                              onPressed: () {
                                setDialogState(selected.clear);
                              },
                              child: const Text('Limpar'),
                            ),
                          ],
                        ),
                      ),
                    Expanded(
                      child: filtered.isEmpty
                          ? Center(
                              child: Column(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  Icon(
                                    Icons.person_off_outlined,
                                    size: 48,
                                    color: colorScheme.onSurfaceVariant,
                                  ),
                                  const SizedBox(height: AppSpacing.sm),
                                  Text(
                                    'Nenhum funcionário encontrado.',
                                    style: textTheme.bodyMedium?.copyWith(
                                      color: colorScheme.onSurfaceVariant,
                                    ),
                                  ),
                                ],
                              ),
                            )
                          : ListView.separated(
                              itemCount: filtered.length,
                              separatorBuilder: (_, __) =>
                                  const SizedBox(height: 2),
                              itemBuilder: (ctx, i) {
                                final emp = filtered[i];
                                final isChecked =
                                    selected.contains(emp.employeeId);
                                return Card.outlined(
                                  color: isChecked
                                      ? colorScheme.primaryContainer
                                          .withValues(alpha: 0.3)
                                      : null,
                                  child: CheckboxListTile(
                                    title: Text(emp.employeeName),
                                    subtitle: Text(emp.employeeStatusName),
                                    secondary: CircleAvatar(
                                      backgroundColor:
                                          colorScheme.primaryContainer,
                                      child: Text(
                                        emp.employeeName.isNotEmpty
                                            ? emp.employeeName[0]
                                                .toUpperCase()
                                            : '?',
                                        style: TextStyle(
                                          color:
                                              colorScheme.onPrimaryContainer,
                                        ),
                                      ),
                                    ),
                                    value: isChecked,
                                    onChanged: (v) {
                                      setDialogState(() {
                                        if (v == true) {
                                          selected.add(emp.employeeId);
                                        } else {
                                          selected.remove(emp.employeeId);
                                        }
                                      });
                                    },
                                  ),
                                );
                              },
                            ),
                    ),
                  ],
                ),
              ),
              actions: [
                TextButton(
                  onPressed: () => Navigator.pop(ctx),
                  child: const Text('Cancelar'),
                ),
                FilledButton.icon(
                  onPressed: selected.isEmpty
                      ? null
                      : () {
                          Navigator.pop(ctx);
                          widget.viewModel
                              .batchCreateDocumentUnits(selected.toList());
                        },
                  icon: const Icon(Icons.add, size: 18),
                  label: Text('Criar (${selected.length})'),
                ),
              ],
            );
          },
        );
      },
    );
  }

  Future<void> _showSignDateDialog() async {
    if (widget.viewModel.stagedFileCount == 0) return;

    final dateText = await showDialog<String>(
      context: context,
      builder: (ctx) => const _DateInputDialog(
        title: 'Configurar Assinatura',
        fieldLabel: 'Data limite para assinatura',
        icon: Icons.draw_outlined,
      ),
    );

    if (dateText == null || dateText.isEmpty || !mounted) return;

    final parts = dateText.split('/');
    final isoDate = DateTime(
      int.parse(parts[2]),
      int.parse(parts[1]),
      int.parse(parts[0]),
    ).toUtc().toIso8601String();

    widget.viewModel.setGlobalSignDeadline(isoDate);
    widget.viewModel.setGlobalReminderDays(0);
    await widget.viewModel.uploadAllStagedToSign();
  }

  Future<void> _showBatchDateDialog() async {
    final dateText = await showDialog<String>(
      context: context,
      builder: (ctx) => const _DateInputDialog(
        title: 'Atualizar Data em Lote',
        icon: Icons.calendar_month_outlined,
      ),
    );

    if (dateText == null || dateText.isEmpty || !mounted) return;

    final parts = dateText.split('/');
    final apiDate = '${parts[2]}-${parts[1]}-${parts[0]}';
    await widget.viewModel.batchUpdateDate(apiDate);
  }

  void _showUploadResultsDialog() {
    final results = widget.viewModel.uploadResults;
    final successCount = results.where((r) => r.success).length;
    final failCount = results.length - successCount;
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    showDialog<void>(
      context: context,
      builder: (ctx) {
        return AlertDialog(
          icon: Icon(
            failCount == 0 ? Icons.check_circle_outline : Icons.info_outline,
            color: failCount == 0 ? colorScheme.primary : colorScheme.error,
          ),
          title: const Text('Resultado do Envio'),
          content: SizedBox(
            width: 420,
            height: 340,
            child: Column(
              children: [
                Row(
                  children: [
                    Expanded(
                      child: _ResultSummaryChip(
                        icon: Icons.check_circle,
                        label: '$successCount enviado(s)',
                        color: colorScheme.primary,
                        backgroundColor: colorScheme.primaryContainer,
                      ),
                    ),
                    if (failCount > 0) ...[
                      const SizedBox(width: AppSpacing.sm),
                      Expanded(
                        child: _ResultSummaryChip(
                          icon: Icons.error,
                          label: '$failCount falha(s)',
                          color: colorScheme.error,
                          backgroundColor: colorScheme.errorContainer,
                        ),
                      ),
                    ],
                  ],
                ),
                const SizedBox(height: AppSpacing.md),
                const Divider(height: 1),
                const SizedBox(height: AppSpacing.sm),
                Expanded(
                  child: ListView.separated(
                    itemCount: results.length,
                    separatorBuilder: (_, __) =>
                        const SizedBox(height: AppSpacing.xs),
                    itemBuilder: (ctx, i) {
                      final r = results[i];
                      return ListTile(
                        dense: true,
                        leading: Icon(
                          r.success
                              ? Icons.check_circle_rounded
                              : Icons.error_rounded,
                          color: r.success
                              ? colorScheme.primary
                              : colorScheme.error,
                          size: 20,
                        ),
                        title: Text(
                          r.documentUnitId,
                          style: textTheme.bodySmall,
                          overflow: TextOverflow.ellipsis,
                        ),
                        subtitle: r.errorMessage != null
                            ? Text(
                                r.errorMessage!,
                                style: textTheme.bodySmall?.copyWith(
                                  color: colorScheme.error,
                                ),
                              )
                            : null,
                      );
                    },
                  ),
                ),
              ],
            ),
          ),
          actions: [
            FilledButton(
              onPressed: () => Navigator.pop(ctx),
              child: const Text('Fechar'),
            ),
          ],
        );
      },
    );
  }
}

/// Summary chip used in the upload results dialog.
class _ResultSummaryChip extends StatelessWidget {
  const _ResultSummaryChip({
    required this.icon,
    required this.label,
    required this.color,
    required this.backgroundColor,
  });

  final IconData icon;
  final String label;
  final Color color;
  final Color backgroundColor;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: AppSpacing.sm,
      ),
      decoration: BoxDecoration(
        color: backgroundColor.withValues(alpha: 0.4),
        borderRadius: BorderRadius.circular(AppSpacing.sm),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(icon, size: 18, color: color),
          const SizedBox(width: AppSpacing.xs),
          Flexible(
            child: Text(
              label,
              overflow: TextOverflow.ellipsis,
              style: Theme.of(context)
                  .textTheme
                  .labelMedium
                  ?.copyWith(color: color, fontWeight: FontWeight.w600),
            ),
          ),
        ],
      ),
    );
  }
}

/// Card section with group and template dropdowns.
///
/// Displays the two dropdowns side-by-side on wider screens and stacked
/// vertically on narrow screens.
class _GroupAndTemplateSection extends StatelessWidget {
  const _GroupAndTemplateSection({required this.vm});

  final BatchDocumentViewModel vm;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;

    return Card.outlined(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Selecione o Documento',
              style: textTheme.titleSmall,
            ),
            const SizedBox(height: AppSpacing.sm),
            _buildGroupDropdown(),
            const SizedBox(height: AppSpacing.md),
            _buildTemplateDropdown(),
          ],
        ),
      ),
    );
  }

  Widget _buildGroupDropdown() {
    return DropdownButtonFormField<String>(
      isExpanded: true,
      decoration: const InputDecoration(
        labelText: 'Grupo de Documentos',
        border: OutlineInputBorder(),
      ),
      initialValue: vm.selectedGroupId,
      items: vm.groups
          .map((g) => DropdownMenuItem(
                value: g.id,
                child: Text(g.name, overflow: TextOverflow.ellipsis),
              ))
          .toList(),
      onChanged: (id) {
        if (id != null) vm.selectGroup(id);
      },
    );
  }

  Widget _buildTemplateDropdown() {
    return DropdownButtonFormField<String>(
      isExpanded: true,
      key: ValueKey(vm.selectedGroupId),
      decoration: const InputDecoration(
        labelText: 'Documento',
        border: OutlineInputBorder(),
      ),
      initialValue: vm.selectedTemplateId,
      items: vm.templates
          .map((t) => DropdownMenuItem(
                value: t.id,
                child: Text(t.name, overflow: TextOverflow.ellipsis),
              ))
          .toList(),
      onChanged: vm.selectedGroupId == null
          ? null
          : (id) {
              if (id != null) vm.selectTemplate(id);
            },
    );
  }
}

/// Always-visible filter section wrapped in a [Card.outlined].
///
/// Follows the same pattern as the employee list search section:
/// search field on top with responsive layout, dropdowns below.
class _FilterSection extends StatefulWidget {
  const _FilterSection({required this.vm});

  final BatchDocumentViewModel vm;

  @override
  State<_FilterSection> createState() => _FilterSectionState();
}

class _FilterSectionState extends State<_FilterSection> {
  final _nameCtrl = TextEditingController();
  final _yearCtrl = TextEditingController();
  final _monthCtrl = TextEditingController();
  final _dayCtrl = TextEditingController();
  final _weekCtrl = TextEditingController();

  static const _employeeStatusOptions = [
    (id: null, label: 'Todos'),
    (id: 1, label: 'Pendente'),
    (id: 2, label: 'Ativo'),
    (id: 3, label: 'Férias'),
    (id: 4, label: 'Afastado'),
    (id: 5, label: 'Inativo'),
  ];

  @override
  void dispose() {
    _nameCtrl.dispose();
    _yearCtrl.dispose();
    _monthCtrl.dispose();
    _dayCtrl.dispose();
    _weekCtrl.dispose();
    super.dispose();
  }

  void _applyAllFilters() {
    final vm = widget.vm;
    vm.setEmployeeNameFilter(
        _nameCtrl.text.isEmpty ? null : _nameCtrl.text);
    vm.setPeriodFilter(
      typeId: vm.periodTypeFilter,
      year: int.tryParse(_yearCtrl.text),
      month: int.tryParse(_monthCtrl.text),
      day: int.tryParse(_dayCtrl.text),
      week: int.tryParse(_weekCtrl.text),
    );
    vm.applyFilters();
  }

  void _clearAllFilters() {
    _nameCtrl.clear();
    _yearCtrl.clear();
    _monthCtrl.clear();
    _dayCtrl.clear();
    _weekCtrl.clear();
    widget.vm.clearFilters();
  }

  @override
  Widget build(BuildContext context) {
    final vm = widget.vm;
    final textTheme = Theme.of(context).textTheme;

    return Card.outlined(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Busca e filtros',
              style: textTheme.titleSmall,
            ),
            const SizedBox(height: AppSpacing.sm),
            LayoutBuilder(
              builder: (context, constraints) {
                final isWide =
                    constraints.maxWidth >= AppBreakpoints.mobile;

                return Column(
                  children: [
                    // Search field — full width on narrow, row with
                    // competency type on wide.
                    if (isWide)
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Expanded(
                            flex: 3,
                            child: _buildNameField(),
                          ),
                          const SizedBox(width: AppSpacing.md),
                          Expanded(
                            child: _buildStatusDropdown(vm),
                          ),
                        ],
                      )
                    else ...[
                      _buildNameField(),
                      const SizedBox(height: AppSpacing.md),
                      _buildStatusDropdown(vm),
                    ],
                    const SizedBox(height: AppSpacing.md),
                    // Period filters row.
                    _PeriodFilterRow(
                      vm: vm,
                      yearCtrl: _yearCtrl,
                      monthCtrl: _monthCtrl,
                      dayCtrl: _dayCtrl,
                      weekCtrl: _weekCtrl,
                      onSubmitted: _applyAllFilters,
                      onPeriodTypeChanged: () {
                        _yearCtrl.clear();
                        _monthCtrl.clear();
                        _dayCtrl.clear();
                        _weekCtrl.clear();
                      },
                    ),
                    const SizedBox(height: AppSpacing.md),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.end,
                      children: [
                        TextButton.icon(
                          onPressed: _clearAllFilters,
                          icon: const Icon(Icons.clear_all, size: 18),
                          label: const Text('Limpar'),
                        ),
                        const SizedBox(width: AppSpacing.sm),
                        FilledButton.icon(
                          onPressed: _applyAllFilters,
                          icon: const Icon(Icons.search, size: 18),
                          label: const Text('Buscar'),
                        ),
                      ],
                    ),
                  ],
                );
              },
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildNameField() {
    return TextField(
      controller: _nameCtrl,
      textInputAction: TextInputAction.search,
      decoration: InputDecoration(
        labelText: 'Nome do Funcionário',
        hintText: 'Buscar por nome',
        border: const OutlineInputBorder(),
        prefixIcon: const Icon(Icons.search),
        suffixIcon: ListenableBuilder(
          listenable: _nameCtrl,
          builder: (context, _) {
            if (_nameCtrl.text.isEmpty) return const SizedBox.shrink();
            return IconButton(
              tooltip: 'Limpar busca',
              onPressed: () {
                _nameCtrl.clear();
                _applyAllFilters();
              },
              icon: const Icon(Icons.close),
            );
          },
        ),
      ),
      onSubmitted: (_) => _applyAllFilters(),
    );
  }

  Widget _buildStatusDropdown(BatchDocumentViewModel vm) {
    return DropdownButtonFormField<int?>(
      isExpanded: true,
      decoration: const InputDecoration(
        labelText: 'Status Funcionário',
        border: OutlineInputBorder(),
      ),
      initialValue: vm.employeeStatusFilter,
      items: _employeeStatusOptions
          .map((o) => DropdownMenuItem(
                value: o.id,
                child: Text(o.label, overflow: TextOverflow.ellipsis),
              ))
          .toList(),
      onChanged: (v) => vm.setEmployeeStatusFilter(v),
    );
  }
}

/// Responsive row of period-related filter fields.
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

  final BatchDocumentViewModel vm;
  final TextEditingController yearCtrl;
  final TextEditingController monthCtrl;
  final TextEditingController dayCtrl;
  final TextEditingController weekCtrl;
  final VoidCallback onSubmitted;
  final VoidCallback onPeriodTypeChanged;

  static const _periodTypeOptions = [
    (id: null, label: 'Todos'),
    (id: 1, label: 'Diário'),
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
            labelText: 'Competência',
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
                    labelText: 'Mês',
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

/// Action bar with batch operation buttons.
class _ActionBar extends StatelessWidget {
  const _ActionBar({
    required this.vm,
    required this.onCreateMissing,
    required this.onBatchUpdateDate,
    required this.onSendToSign,
  });

  final BatchDocumentViewModel vm;
  final VoidCallback onCreateMissing;
  final VoidCallback onBatchUpdateDate;
  final VoidCallback onSendToSign;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: AppSpacing.sm,
      runSpacing: AppSpacing.sm,
      crossAxisAlignment: WrapCrossAlignment.center,
      children: [
        FilterChip(
          avatar: Icon(
            vm.selectedUnitIds.length == vm.pendingUnits.length &&
                    vm.pendingUnits.isNotEmpty
                ? Icons.check_box
                : Icons.check_box_outline_blank,
            size: 18,
          ),
          label: const Text('Selecionar Todos'),
          selected: vm.selectedUnitIds.length == vm.pendingUnits.length &&
              vm.pendingUnits.isNotEmpty,
          onSelected: (v) {
            if (v) {
              vm.selectAll();
            } else {
              vm.clearSelection();
            }
          },
        ),
        const SizedBox(width: AppSpacing.xs),
        PermissionGuard(
          resource: 'document',
          scope: 'create',
          child: OutlinedButton.icon(
            onPressed: onCreateMissing,
            icon: const Icon(Icons.person_add_outlined, size: 18),
            label: const Text('Criar Docs Faltantes'),
          ),
        ),
        PermissionGuard(
          resource: 'document',
          scope: 'edit',
          child: OutlinedButton.icon(
            onPressed:
                vm.selectedUnitIds.isEmpty ? null : onBatchUpdateDate,
            icon: const Icon(Icons.calendar_today, size: 18),
            label: const Text('Atualizar Data'),
          ),
        ),
        const SizedBox(width: AppSpacing.sm),
        PermissionGuard(
          resource: 'document',
          scope: 'upload',
          child: FilledButton.icon(
            onPressed: vm.stagedFileCount == 0 ||
                    vm.status == BatchDocumentStatus.uploading
                ? null
                : () => vm.uploadAllStaged(),
            icon: const Icon(Icons.cloud_upload_outlined, size: 18),
            label: Text('Enviar (${vm.stagedFileCount})'),
          ),
        ),
        PermissionGuard(
          resource: 'document',
          scope: 'send2sign',
          child: FilledButton.tonalIcon(
            onPressed: vm.stagedFileCount == 0 ||
                    vm.status == BatchDocumentStatus.uploading
                ? null
                : onSendToSign,
            icon: const Icon(Icons.draw_outlined, size: 18),
            label: const Text('Enviar para Assinar'),
          ),
        ),
      ],
    );
  }
}

/// Renders the complete scrollable body: dropdown section, filters, action
/// bar, and the document list with pagination.
///
/// Uses a single [CustomScrollView] so the header sections scroll away
/// naturally, keeping the layout compact on small screens.
class _DocumentContent extends StatelessWidget {
  const _DocumentContent({
    required this.vm,
    required this.onPickFile,
    required this.onCreateMissing,
    required this.onBatchUpdateDate,
    required this.onSendToSign,
  });

  final BatchDocumentViewModel vm;
  final Future<void> Function(BatchDocumentUnitItem unit) onPickFile;
  final VoidCallback onCreateMissing;
  final VoidCallback onBatchUpdateDate;
  final VoidCallback onSendToSign;

  @override
  Widget build(BuildContext context) {
    return CustomScrollView(
      slivers: [
        const SliverToBoxAdapter(
          child: SizedBox(height: AppSpacing.md),
        ),
        SliverToBoxAdapter(
          child: _GroupAndTemplateSection(vm: vm),
        ),
        if (vm.selectedTemplateId != null) ...[
          const SliverToBoxAdapter(
            child: SizedBox(height: AppSpacing.md),
          ),
          SliverToBoxAdapter(
            child: _FilterSection(vm: vm),
          ),
          const SliverToBoxAdapter(
            child: SizedBox(height: AppSpacing.md),
          ),
          SliverToBoxAdapter(
            child: _ActionBar(
              vm: vm,
              onCreateMissing: onCreateMissing,
              onBatchUpdateDate: onBatchUpdateDate,
              onSendToSign: onSendToSign,
            ),
          ),
          const SliverToBoxAdapter(
            child: SizedBox(height: AppSpacing.md),
          ),
          ..._buildListSlivers(context),
        ],
        const SliverToBoxAdapter(
          child: SizedBox(height: AppSpacing.md),
        ),
      ],
    );
  }

  List<Widget> _buildListSlivers(BuildContext context) {
    if (vm.status == BatchDocumentStatus.loading) {
      return [
        const SliverToBoxAdapter(
          child: Padding(
            padding: EdgeInsets.symmetric(vertical: AppSpacing.xxxl),
            child: Center(child: CircularProgressIndicator()),
          ),
        ),
      ];
    }

    if (vm.pendingUnits.isEmpty) {
      return [
        const SliverToBoxAdapter(
          child: _EmptyStateCard(
            icon: Icons.inbox_outlined,
            title: 'Nenhum documento pendente encontrado.',
            message:
                'Todos os documentos para este template estão em dia, ou os filtros aplicados não retornaram resultados.',
          ),
        ),
      ];
    }

    return [
      SliverToBoxAdapter(
        child: LayoutBuilder(
          builder: (context, constraints) {
            if (constraints.maxWidth >= AppBreakpoints.mobile) {
              return const Column(
                children: [
                  _DocumentListLegend(),
                  Divider(height: 1),
                ],
              );
            }
            return const SizedBox.shrink();
          },
        ),
      ),
      SliverList.separated(
        itemCount: vm.pendingUnits.length,
        separatorBuilder: (_, __) => const Divider(height: 1),
        itemBuilder: (context, index) {
          final unit = vm.pendingUnits[index];
          return _DocumentListItem(
            unit: unit,
            isSelected:
                vm.selectedUnitIds.contains(unit.documentUnitId),
            isStaged: vm.hasStaged(unit.documentUnitId),
            stagedFileName: vm.stagedFileName(unit.documentUnitId),
            onToggleSelection: () =>
                vm.toggleSelection(unit.documentUnitId),
            onPickFile: () => onPickFile(unit),
            onRemoveFile: () =>
                vm.unstageFile(unit.documentUnitId),
          );
        },
      ),
      SliverToBoxAdapter(
        child: _PaginationBar(vm: vm),
      ),
    ];
  }
}

/// Labels the fixed-width columns used by the wide document list rows.
class _DocumentListLegend extends StatelessWidget {
  const _DocumentListLegend();

  @override
  Widget build(BuildContext context) {
    final labelStyle = Theme.of(context).textTheme.labelMedium?.copyWith(
          color: Theme.of(context).colorScheme.onSurfaceVariant,
        );

    return DefaultTextStyle.merge(
      style: labelStyle,
      child: const Padding(
        padding: EdgeInsets.fromLTRB(
          AppSpacing.md,
          AppSpacing.xs,
          AppSpacing.lg,
          AppSpacing.xs,
        ),
        child: Row(
          children: [
            SizedBox(width: 48),
            Expanded(
              flex: 3,
              child: Text('Funcionário'),
            ),
            Expanded(
              flex: 2,
              child: Text('Data / Competência'),
            ),
            Expanded(
              flex: 2,
              child: Text('Status', textAlign: TextAlign.center),
            ),
            Expanded(
              flex: 2,
              child: Text('Arquivo', textAlign: TextAlign.center),
            ),
          ],
        ),
      ),
    );
  }
}

/// A single document unit row — responsive between wide (table-like) and
/// narrow (card-like stacked) layouts.
class _DocumentListItem extends StatelessWidget {
  const _DocumentListItem({
    required this.unit,
    required this.isSelected,
    required this.isStaged,
    required this.stagedFileName,
    required this.onToggleSelection,
    required this.onPickFile,
    required this.onRemoveFile,
  });

  final BatchDocumentUnitItem unit;
  final bool isSelected;
  final bool isStaged;
  final String? stagedFileName;
  final VoidCallback onToggleSelection;
  final VoidCallback onPickFile;
  final VoidCallback onRemoveFile;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final isWide = constraints.maxWidth >= AppBreakpoints.mobile;

        if (isWide) {
          return _buildWideRow(context);
        }
        return _buildNarrowRow(context);
      },
    );
  }

  Widget _buildWideRow(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return InkWell(
      onTap: onToggleSelection,
      child: Container(
        color: isSelected
            ? colorScheme.primaryContainer.withValues(alpha: 0.15)
            : null,
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.md,
          AppSpacing.sm,
          AppSpacing.lg,
          AppSpacing.sm,
        ),
        child: Row(
          children: [
            SizedBox(
              width: 48,
              child: Checkbox(
                value: isSelected,
                onChanged: (_) => onToggleSelection(),
              ),
            ),
            Expanded(
              flex: 3,
              child: Row(
                children: [
                  CircleAvatar(
                    radius: 16,
                    backgroundColor: colorScheme.primaryContainer,
                    child: Text(
                      unit.employeeName.isNotEmpty
                          ? unit.employeeName[0].toUpperCase()
                          : '?',
                      style: textTheme.labelSmall?.copyWith(
                        color: colorScheme.onPrimaryContainer,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          unit.employeeName,
                          overflow: TextOverflow.ellipsis,
                        ),
                        const SizedBox(height: 2),
                        _EmployeeStatusBadge(
                          statusId: unit.employeeStatusId,
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            Expanded(
              flex: 2,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    unit.date,
                    style: textTheme.bodySmall,
                    overflow: TextOverflow.ellipsis,
                  ),
                  if (unit.period != null)
                    Text(
                      unit.period!.formattedPeriod,
                      style: textTheme.bodySmall?.copyWith(
                        color: colorScheme.onSurfaceVariant,
                      ),
                      overflow: TextOverflow.ellipsis,
                    ),
                ],
              ),
            ),
            Expanded(
              flex: 2,
              child: Center(
                child: _DocumentStatusBadge(statusId: unit.statusId),
              ),
            ),
            Expanded(
              flex: 2,
              child: Center(
                child: _FileCell(
                  isStaged: isStaged,
                  fileName: stagedFileName,
                  onPick: onPickFile,
                  onRemove: onRemoveFile,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildNarrowRow(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return InkWell(
      onTap: onToggleSelection,
      child: Container(
        color: isSelected
            ? colorScheme.primaryContainer.withValues(alpha: 0.15)
            : null,
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.md,
          vertical: AppSpacing.sm,
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Checkbox(
              value: isSelected,
              onChanged: (_) => onToggleSelection(),
              visualDensity: VisualDensity.compact,
            ),
            CircleAvatar(
              radius: 16,
              backgroundColor: colorScheme.primaryContainer,
              child: Text(
                unit.employeeName.isNotEmpty
                    ? unit.employeeName[0].toUpperCase()
                    : '?',
                style: textTheme.labelSmall?.copyWith(
                  color: colorScheme.onPrimaryContainer,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
            const SizedBox(width: AppSpacing.sm),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    unit.employeeName,
                    style: textTheme.bodyMedium?.copyWith(
                      fontWeight: FontWeight.w600,
                    ),
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: AppSpacing.xs),
                  Wrap(
                    spacing: AppSpacing.sm,
                    runSpacing: AppSpacing.xs,
                    children: [
                      _EmployeeStatusBadge(
                        statusId: unit.employeeStatusId,
                      ),
                      _DocumentStatusBadge(statusId: unit.statusId),
                    ],
                  ),
                  const SizedBox(height: AppSpacing.xs),
                  Text(
                    'Data: ${unit.date}${unit.period != null ? ' · ${unit.period!.formattedPeriod}' : ''}',
                    style: textTheme.bodySmall?.copyWith(
                      color: colorScheme.onSurfaceVariant,
                    ),
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: AppSpacing.xs),
                  _FileCell(
                    isStaged: isStaged,
                    fileName: stagedFileName,
                    onPick: onPickFile,
                    onRemove: onRemoveFile,
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Presents empty and error states inside a centered Material card.
class _EmptyStateCard extends StatelessWidget {
  const _EmptyStateCard({
    required this.icon,
    required this.title,
    required this.message,
  });

  final IconData icon;
  final String title;
  final String message;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(vertical: AppSpacing.md),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 420),
          child: Card.outlined(
            child: Padding(
              padding: const EdgeInsets.all(AppSpacing.xl),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(
                    icon,
                    size: 48,
                    color: colorScheme.primary,
                  ),
                  const SizedBox(height: AppSpacing.md),
                  Text(
                    title,
                    style: Theme.of(context).textTheme.titleMedium,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  Text(
                    message,
                    style:
                        Theme.of(context).textTheme.bodyMedium?.copyWith(
                              color: colorScheme.onSurfaceVariant,
                            ),
                    textAlign: TextAlign.center,
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// Employee status badge using theme-aware colors.
class _EmployeeStatusBadge extends StatelessWidget {
  const _EmployeeStatusBadge({required this.statusId});

  final String statusId;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final (label, fg, bg) = switch (statusId) {
      '1' => ('Pendente', colorScheme.onTertiaryContainer,
          colorScheme.tertiaryContainer),
      '2' => ('Ativo', colorScheme.onPrimaryContainer,
          colorScheme.primaryContainer),
      '3' => ('Férias', colorScheme.onSecondaryContainer,
          colorScheme.secondaryContainer),
      '4' => (
          'Afastado',
          colorScheme.onTertiaryContainer,
          colorScheme.tertiaryContainer
        ),
      '5' => ('Inativo', colorScheme.onSurfaceVariant,
          colorScheme.surfaceContainerHigh),
      _ => ('Desconhecido', colorScheme.onSurfaceVariant,
          colorScheme.surfaceContainerHigh),
    };

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: 2,
      ),
      decoration: BoxDecoration(
        color: bg.withValues(alpha: 0.6),
        borderRadius: BorderRadius.circular(AppSpacing.md),
      ),
      child: Text(
        label,
        style: Theme.of(context).textTheme.labelSmall?.copyWith(
              color: fg,
              fontWeight: FontWeight.w500,
            ),
      ),
    );
  }
}

/// Document status badge using theme-aware colors.
///
/// Uses [Flexible]-friendly layout so that long labels like
/// "Aguardando Assinatura" ellipsize instead of overflowing.
class _DocumentStatusBadge extends StatelessWidget {
  const _DocumentStatusBadge({required this.statusId});

  final String statusId;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final (label, fg, bg) = switch (statusId) {
      '1' => ('Pendente', colorScheme.onTertiaryContainer,
          colorScheme.tertiaryContainer),
      '2' =>
        ('OK', colorScheme.onPrimaryContainer, colorScheme.primaryContainer),
      '3' => ('Obsoleto', colorScheme.onSurfaceVariant,
          colorScheme.surfaceContainerHigh),
      '4' => ('Inválido', colorScheme.onErrorContainer,
          colorScheme.errorContainer),
      '5' => ('Requer Validação', colorScheme.onTertiaryContainer,
          colorScheme.tertiaryContainer),
      '6' => ('N/A', colorScheme.onSurfaceVariant,
          colorScheme.surfaceContainerHigh),
      '7' => ('Aguard. Assinatura', colorScheme.onSecondaryContainer,
          colorScheme.secondaryContainer),
      _ => ('Desconhecido', colorScheme.onSurfaceVariant,
          colorScheme.surfaceContainerHigh),
    };

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: 2,
      ),
      decoration: BoxDecoration(
        color: bg.withValues(alpha: 0.6),
        borderRadius: BorderRadius.circular(AppSpacing.md),
      ),
      child: Text(
        label,
        overflow: TextOverflow.ellipsis,
        maxLines: 1,
        style: Theme.of(context).textTheme.labelSmall?.copyWith(
              color: fg,
              fontWeight: FontWeight.w500,
            ),
      ),
    );
  }
}

/// File attachment cell — shows staged file or a pick button.
class _FileCell extends StatelessWidget {
  const _FileCell({
    required this.isStaged,
    required this.fileName,
    required this.onPick,
    required this.onRemove,
  });

  final bool isStaged;
  final String? fileName;
  final VoidCallback onPick;
  final VoidCallback onRemove;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    if (isStaged) {
      return Container(
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.sm,
          vertical: AppSpacing.xs,
        ),
        decoration: BoxDecoration(
          color: colorScheme.primaryContainer.withValues(alpha: 0.3),
          borderRadius: BorderRadius.circular(AppSpacing.sm),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              Icons.check_circle_rounded,
              color: colorScheme.primary,
              size: 16,
            ),
            const SizedBox(width: AppSpacing.xs),
            Flexible(
              child: Text(
                fileName ?? '',
                overflow: TextOverflow.ellipsis,
                style: textTheme.bodySmall?.copyWith(
                  color: colorScheme.primary,
                ),
              ),
            ),
            const SizedBox(width: AppSpacing.xs),
            InkWell(
              borderRadius: BorderRadius.circular(12),
              onTap: onRemove,
              child: Padding(
                padding: const EdgeInsets.all(2),
                child: Icon(
                  Icons.close_rounded,
                  size: 16,
                  color: colorScheme.onSurfaceVariant,
                ),
              ),
            ),
          ],
        ),
      );
    }

    return TextButton.icon(
      onPressed: onPick,
      icon: const Icon(Icons.attach_file_rounded, size: 16),
      label: const Text('Anexar'),
      style: TextButton.styleFrom(
        visualDensity: VisualDensity.compact,
        padding: const EdgeInsets.symmetric(horizontal: AppSpacing.sm),
      ),
    );
  }
}

/// Pagination bar with page size selector and navigation controls.
class _PaginationBar extends StatelessWidget {
  const _PaginationBar({required this.vm});

  final BatchDocumentViewModel vm;

  static const _pageSizeOptions = [10, 25, 50, 100];

  @override
  Widget build(BuildContext context) {
    if (vm.totalCount == 0) return const SizedBox.shrink();
    final totalPages = (vm.totalCount / vm.pageSize).ceil();
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
      child: Wrap(
        alignment: WrapAlignment.center,
        crossAxisAlignment: WrapCrossAlignment.center,
        spacing: AppSpacing.sm,
        runSpacing: AppSpacing.sm,
        children: [
          Text(
            'Itens por página:',
            style: textTheme.bodySmall?.copyWith(
              color: colorScheme.onSurfaceVariant,
            ),
          ),
          DropdownButton<int>(
              value: vm.pageSize,
              underline: const SizedBox.shrink(),
              borderRadius: BorderRadius.circular(AppSpacing.sm),
              isDense: true,
              alignment: AlignmentDirectional.center,
              style: textTheme.bodySmall?.copyWith(
                color: colorScheme.onSurface,
              ),
              items: _pageSizeOptions
                  .map((size) => DropdownMenuItem(
                        value: size,
                        child: Text('$size'),
                      ))
                  .toList(),
              onChanged: (size) {
                if (size != null) vm.setPageSize(size);
              },
            ),
          const SizedBox(width: AppSpacing.sm),
          IconButton.outlined(
            icon: const Icon(Icons.chevron_left),
            onPressed: vm.pageNumber > 1
                ? () => vm.setPage(vm.pageNumber - 1)
                : null,
            visualDensity: VisualDensity.compact,
            tooltip: 'Página anterior',
          ),
          Container(
            padding: const EdgeInsets.symmetric(
              horizontal: AppSpacing.md,
              vertical: AppSpacing.sm,
            ),
            decoration: BoxDecoration(
              color: colorScheme.surfaceContainerHigh,
              borderRadius: BorderRadius.circular(AppSpacing.sm),
            ),
            child: Text(
              '${vm.pageNumber} / $totalPages',
              style: textTheme.labelLarge?.copyWith(
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          IconButton.outlined(
            icon: const Icon(Icons.chevron_right),
            onPressed: vm.pageNumber < totalPages
                ? () => vm.setPage(vm.pageNumber + 1)
                : null,
            visualDensity: VisualDensity.compact,
            tooltip: 'Próxima página',
          ),
          const SizedBox(width: AppSpacing.sm),
          Text(
            '${vm.totalCount} itens',
            style: textTheme.bodySmall?.copyWith(
              color: colorScheme.onSurfaceVariant,
            ),
          ),
        ],
      ),
    );
  }
}

/// A self-contained dialog that collects a date value.
///
/// Uses its own [TextEditingController] managed in [State.dispose],
/// ensuring the controller is never accessed after disposal.
/// Returns the date text (dd/MM/yyyy) via [Navigator.pop], or null
/// when cancelled.
class _DateInputDialog extends StatefulWidget {
  const _DateInputDialog({
    this.title = 'Informe a Data',
    this.fieldLabel = 'Data',
    this.icon = Icons.event_outlined,
  });

  /// The dialog title.
  final String title;

  /// The label for the date input field.
  final String fieldLabel;

  /// The icon displayed in the dialog header.
  final IconData icon;

  @override
  State<_DateInputDialog> createState() => _DateInputDialogState();
}

class _DateInputDialogState extends State<_DateInputDialog> {
  final _dateCtrl = TextEditingController();
  final _dateMask = MaskTextInputFormatter(
    mask: '##/##/####',
    filter: {'#': RegExp(r'[0-9]')},
    type: MaskAutoCompletionType.lazy,
  );
  final _formKey = GlobalKey<FormState>();

  @override
  void dispose() {
    _dateCtrl.dispose();
    super.dispose();
  }

  String _formatDate(DateTime date) {
    final d = date.day.toString().padLeft(2, '0');
    final m = date.month.toString().padLeft(2, '0');
    return '$d/$m/${date.year}';
  }

  void _setDateFromDays(int days) {
    final target = DateTime.now().add(Duration(days: days));
    final formatted = _formatDate(target);
    final rawDigits = formatted.replaceAll(RegExp(r'[^\d]'), '');
    _dateMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: rawDigits),
    );
    _dateCtrl.text = _dateMask.getMaskedText();
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return AlertDialog(
      icon: Icon(widget.icon, color: colorScheme.primary),
      title: Text(widget.title),
      content: SizedBox(
        width: 340,
        child: Form(
          key: _formKey,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextFormField(
                  controller: _dateCtrl,
                  decoration: InputDecoration(
                    labelText: widget.fieldLabel,
                    prefixIcon: const Icon(Icons.event_outlined),
                    border: const OutlineInputBorder(),
                    helperText: 'Ex: 15/03/2026',
                  ),
                  keyboardType: TextInputType.number,
                  inputFormatters: [_dateMask],
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Informe a data limite';
                    }
                    final parts = value.split('/');
                    if (parts.length != 3 || parts[2].length != 4) {
                      return 'Data inválida';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: AppSpacing.md),
                Row(
                  children: [3, 5, 10].map((days) {
                    return Expanded(
                      child: Padding(
                        padding: EdgeInsets.only(
                          right: days == 10 ? 0 : AppSpacing.sm,
                        ),
                        child: FilledButton.tonal(
                          onPressed: () => _setDateFromDays(days),
                          style: FilledButton.styleFrom(
                            visualDensity: VisualDensity.compact,
                          ),
                          child: Text('+$days dias'),
                        ),
                      ),
                    );
                  }).toList(),
                ),
              ],
            ),
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(null),
          child: const Text('Cancelar'),
        ),
        FilledButton(
          onPressed: () {
            if (_formKey.currentState?.validate() == true) {
              Navigator.of(context).pop(_dateCtrl.text.trim());
            }
          },
          child: const Text('Confirmar'),
        ),
      ],
    );
  }
}
