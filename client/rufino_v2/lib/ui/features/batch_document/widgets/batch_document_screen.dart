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
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.go('/home'),
        ),
        title: const Text('Gestão de Documentos em Lote'),
      ),
      body: SafeArea(
        child: LayoutBuilder(
          builder: (context, constraints) {
            final isWide = constraints.maxWidth >= AppBreakpoints.tablet;
            return Padding(
              padding:
                  EdgeInsets.all(isWide ? AppSpacing.lg : AppSpacing.md),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _GroupAndTemplateDropdowns(vm: vm),
                  const SizedBox(height: AppSpacing.md),
                  if (vm.selectedTemplateId != null) ...[
                    _FilterBar(vm: vm),
                    const SizedBox(height: AppSpacing.md),
                    _ActionBar(
                      vm: vm,
                      onCreateMissing: _showCreateMissingDialog,
                      onBatchUpdateDate: _showBatchDateDialog,
                      onSendToSign: _showSignDateDialog,
                    ),
                    const SizedBox(height: AppSpacing.md),
                    Expanded(
                      child: vm.status == BatchDocumentStatus.loading
                          ? const Center(
                              child: CircularProgressIndicator())
                          : _DataTable(
                              vm: vm,
                              onPickFile: _pickFileForUnit,
                            ),
                    ),
                    _PaginationBar(vm: vm),
                  ],
                ],
              ),
            );
          },
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
            return AlertDialog(
              title: const Text('Criar Documentos Faltantes'),
              content: SizedBox(
                width: 400,
                height: 450,
                child: Column(
                  children: [
                    TextField(
                      decoration: const InputDecoration(
                        labelText: 'Buscar por nome',
                        prefixIcon: Icon(Icons.search),
                        border: OutlineInputBorder(),
                        isDense: true,
                      ),
                      onChanged: (value) {
                        setDialogState(() => searchQuery = value);
                      },
                    ),
                    const SizedBox(height: AppSpacing.sm),
                    Expanded(
                      child: filtered.isEmpty
                          ? const Center(
                              child:
                                  Text('Nenhum funcionário encontrado.'))
                          : ListView.builder(
                              itemCount: filtered.length,
                              itemBuilder: (ctx, i) {
                                final emp = filtered[i];
                                return CheckboxListTile(
                                  title: Text(emp.employeeName),
                                  subtitle:
                                      Text(emp.employeeStatusName),
                                  value: selected
                                      .contains(emp.employeeId),
                                  onChanged: (v) {
                                    setDialogState(() {
                                      if (v == true) {
                                        selected.add(emp.employeeId);
                                      } else {
                                        selected
                                            .remove(emp.employeeId);
                                      }
                                    });
                                  },
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
                FilledButton(
                  onPressed: selected.isEmpty
                      ? null
                      : () {
                          Navigator.pop(ctx);
                          widget.viewModel.batchCreateDocumentUnits(
                              selected.toList());
                        },
                  child: Text('Criar (${selected.length})'),
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

    // Show dialog and collect the date string. The controller lives
    // entirely inside the dialog scope and is disposed before pop,
    // preventing "used after being disposed" when notifyListeners
    // triggers a rebuild of the parent.
    final dateText = await showDialog<String>(
      context: context,
      builder: (ctx) => const _DateInputDialog(
        title: 'Configurar Assinatura',
        fieldLabel: 'Data limite para assinatura',
      ),
    );

    if (dateText == null || dateText.isEmpty || !mounted) return;

    // Convert dd/MM/yyyy to ISO 8601 UTC
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
      ),
    );

    if (dateText == null || dateText.isEmpty || !mounted) return;

    final parts = dateText.split('/');
    final apiDate =
        '${parts[2]}-${parts[1]}-${parts[0]}';
    await widget.viewModel.batchUpdateDate(apiDate);
  }

  void _showUploadResultsDialog() {
    final results = widget.viewModel.uploadResults;
    final successCount = results.where((r) => r.success).length;
    final failCount = results.length - successCount;
    showDialog<void>(
      context: context,
      builder: (ctx) {
        return AlertDialog(
          title: const Text('Resultado do Envio'),
          content: SizedBox(
            width: 400,
            height: 300,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Sucesso: $successCount'),
                if (failCount > 0) Text('Falha: $failCount'),
                const SizedBox(height: AppSpacing.md),
                Expanded(
                  child: ListView.builder(
                    itemCount: results.length,
                    itemBuilder: (ctx, i) {
                      final r = results[i];
                      return ListTile(
                        leading: Icon(
                          r.success ? Icons.check_circle : Icons.error,
                          color: r.success ? Colors.green : Colors.red,
                        ),
                        title: Text(r.documentUnitId),
                        subtitle:
                            r.errorMessage != null
                                ? Text(r.errorMessage!)
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

class _GroupAndTemplateDropdowns extends StatelessWidget {
  const _GroupAndTemplateDropdowns({required this.vm});

  final BatchDocumentViewModel vm;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        DropdownButtonFormField<String>(
          decoration: const InputDecoration(
            labelText: 'Grupo de Documentos',
            border: OutlineInputBorder(),
          ),
          initialValue: vm.selectedGroupId,
          items: vm.groups
              .map((g) =>
                  DropdownMenuItem(value: g.id, child: Text(g.name)))
              .toList(),
          onChanged: (id) {
            if (id != null) vm.selectGroup(id);
          },
        ),
        const SizedBox(height: AppSpacing.sm),
        DropdownButtonFormField<String>(
          key: ValueKey(vm.selectedGroupId),
          decoration: const InputDecoration(
            labelText: 'Documento',
            border: OutlineInputBorder(),
          ),
          initialValue: vm.selectedTemplateId,
          items: vm.templates
              .map((t) =>
                  DropdownMenuItem(value: t.id, child: Text(t.name)))
              .toList(),
          onChanged: vm.selectedGroupId == null
              ? null
              : (id) {
                  if (id != null) vm.selectTemplate(id);
                },
        ),
      ],
    );
  }
}

class _FilterBar extends StatefulWidget {
  const _FilterBar({required this.vm});

  final BatchDocumentViewModel vm;

  @override
  State<_FilterBar> createState() => _FilterBarState();
}

class _FilterBarState extends State<_FilterBar> {
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

  static const _periodTypeOptions = [
    (id: null, label: 'Todos'),
    (id: 1, label: 'Diário'),
    (id: 2, label: 'Semanal'),
    (id: 3, label: 'Mensal'),
    (id: 4, label: 'Anual'),
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

  @override
  Widget build(BuildContext context) {
    final vm = widget.vm;
    return Wrap(
      spacing: AppSpacing.sm,
      runSpacing: AppSpacing.sm,
      crossAxisAlignment: WrapCrossAlignment.end,
      children: [
        SizedBox(
          width: 200,
          child: TextField(
            controller: _nameCtrl,
            decoration: const InputDecoration(
              labelText: 'Nome do Funcionário',
              border: OutlineInputBorder(),
              isDense: true,
            ),
            onSubmitted: (_) => _applyAllFilters(),
          ),
        ),
        SizedBox(
          width: 160,
          child: DropdownButtonFormField<int?>(
            decoration: const InputDecoration(
              labelText: 'Status Funcionário',
              border: OutlineInputBorder(),
              isDense: true,
            ),
            initialValue: vm.employeeStatusFilter,
            items: _employeeStatusOptions
                .map((o) =>
                    DropdownMenuItem(value: o.id, child: Text(o.label)))
                .toList(),
            onChanged: (v) => vm.setEmployeeStatusFilter(v),
          ),
        ),
        SizedBox(
          width: 140,
          child: DropdownButtonFormField<int?>(
            decoration: const InputDecoration(
              labelText: 'Competência',
              border: OutlineInputBorder(),
              isDense: true,
            ),
            initialValue: vm.periodTypeFilter,
            items: _periodTypeOptions
                .map((o) =>
                    DropdownMenuItem(value: o.id, child: Text(o.label)))
                .toList(),
            onChanged: (v) {
              vm.setPeriodFilter(typeId: v);
              _yearCtrl.clear();
              _monthCtrl.clear();
              _dayCtrl.clear();
              _weekCtrl.clear();
            },
          ),
        ),
        if (vm.periodTypeFilter != null) ...[
          SizedBox(
            width: 100,
            child: TextField(
              controller: _yearCtrl,
              decoration: const InputDecoration(
                labelText: 'Ano',
                border: OutlineInputBorder(),
                isDense: true,
              ),
              keyboardType: TextInputType.number,
              onSubmitted: (_) => _applyAllFilters(),
            ),
          ),
          if (vm.periodTypeFilter != 4)
            SizedBox(
              width: 100,
              child: TextField(
                controller: _monthCtrl,
                decoration: const InputDecoration(
                  labelText: 'Mês',
                  border: OutlineInputBorder(),
                  isDense: true,
                ),
                keyboardType: TextInputType.number,
                onSubmitted: (_) => _applyAllFilters(),
              ),
            ),
          if (vm.periodTypeFilter == 1)
            SizedBox(
              width: 100,
              child: TextField(
                controller: _dayCtrl,
                decoration: const InputDecoration(
                  labelText: 'Dia',
                  border: OutlineInputBorder(),
                  isDense: true,
                ),
                keyboardType: TextInputType.number,
                onSubmitted: (_) => _applyAllFilters(),
              ),
            ),
          if (vm.periodTypeFilter == 2)
            SizedBox(
              width: 100,
              child: TextField(
                controller: _weekCtrl,
                decoration: const InputDecoration(
                  labelText: 'Semana',
                  border: OutlineInputBorder(),
                  isDense: true,
                ),
                keyboardType: TextInputType.number,
                onSubmitted: (_) => _applyAllFilters(),
              ),
            ),
        ],
        FilledButton.icon(
          onPressed: _applyAllFilters,
          icon: const Icon(Icons.search, size: 18),
          label: const Text('Buscar'),
        ),
      ],
    );
  }
}

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
        PermissionGuard(
          resource: 'document',
          scope: 'edit',
          child: OutlinedButton.icon(
            onPressed: vm.selectedUnitIds.isEmpty
                ? null
                : onBatchUpdateDate,
            icon: const Icon(Icons.calendar_today, size: 18),
            label: const Text('Atualizar Data'),
          ),
        ),
        PermissionGuard(
          resource: 'document',
          scope: 'create',
          child: OutlinedButton.icon(
            onPressed: onCreateMissing,
            icon: const Icon(Icons.person_add_outlined, size: 18),
            label: const Text('Criar Docs Faltantes'),
          ),
        ),
        const SizedBox(width: AppSpacing.md),
        PermissionGuard(
          resource: 'document',
          scope: 'upload',
          child: FilledButton.icon(
            onPressed: vm.stagedFileCount == 0 ||
                    vm.status == BatchDocumentStatus.uploading
                ? null
                : () => vm.uploadAllStaged(),
            icon: const Icon(Icons.upload_file, size: 18),
            label: Text('Enviar Arquivos (${vm.stagedFileCount})'),
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

class _DataTable extends StatelessWidget {
  const _DataTable({required this.vm, required this.onPickFile});

  final BatchDocumentViewModel vm;
  final Future<void> Function(BatchDocumentUnitItem unit) onPickFile;

  @override
  Widget build(BuildContext context) {
    if (vm.pendingUnits.isEmpty) {
      return const Center(
        child: Text('Nenhum documento pendente encontrado.'),
      );
    }
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: SingleChildScrollView(
        child: DataTable(
          columns: const [
            DataColumn(label: Text('Sel.')),
            DataColumn(label: Text('Funcionário')),
            DataColumn(label: Text('Status Func.')),
            DataColumn(label: Text('Data')),
            DataColumn(label: Text('Competência')),
            DataColumn(label: Text('Status Doc.')),
            DataColumn(label: Text('Arquivo')),
          ],
          rows: vm.pendingUnits.map((unit) {
            final isSelected =
                vm.selectedUnitIds.contains(unit.documentUnitId);
            final isStaged = vm.hasStaged(unit.documentUnitId);
            return DataRow(
              selected: isSelected,
              cells: [
                DataCell(
                  Checkbox(
                    value: isSelected,
                    onChanged: (_) =>
                        vm.toggleSelection(unit.documentUnitId),
                  ),
                ),
                DataCell(Text(unit.employeeName)),
                DataCell(
                  Text(
                    unit.employeeStatusLabel,
                    style: TextStyle(
                        color: _employeeStatusColor(unit.employeeStatusId)),
                  ),
                ),
                DataCell(Text(unit.date)),
                DataCell(
                    Text(unit.period?.formattedPeriod ?? '')),
                DataCell(
                  Text(
                    unit.statusLabel,
                    style: TextStyle(color: _statusColor(unit.statusId)),
                  ),
                ),
                DataCell(
                  isStaged
                      ? Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            const Icon(Icons.check_circle,
                                color: Colors.green, size: 18),
                            const SizedBox(width: 4),
                            Text(
                              vm.stagedFileName(unit.documentUnitId) ?? '',
                              overflow: TextOverflow.ellipsis,
                            ),
                            IconButton(
                              icon: const Icon(Icons.close, size: 18),
                              onPressed: () =>
                                  vm.unstageFile(unit.documentUnitId),
                              tooltip: 'Remover arquivo',
                            ),
                          ],
                        )
                      : IconButton(
                          icon: const Icon(Icons.attach_file),
                          onPressed: () => onPickFile(unit),
                          tooltip: 'Selecionar arquivo',
                        ),
                ),
              ],
            );
          }).toList(),
        ),
      ),
    );
  }

  Color _statusColor(String statusId) {
    return switch (statusId) {
      '1' => Colors.orange,
      '2' => Colors.green,
      '3' => Colors.grey,
      '4' => Colors.red,
      '5' => Colors.amber,
      '6' => Colors.blueGrey,
      '7' => Colors.blue,
      _ => Colors.grey,
    };
  }

  Color _employeeStatusColor(String statusId) {
    return switch (statusId) {
      '1' => Colors.orange,
      '2' => Colors.green,
      '3' => Colors.blue,
      '4' => Colors.amber,
      '5' => Colors.grey,
      _ => Colors.grey,
    };
  }
}

class _PaginationBar extends StatelessWidget {
  const _PaginationBar({required this.vm});

  final BatchDocumentViewModel vm;

  @override
  Widget build(BuildContext context) {
    if (vm.totalCount == 0) return const SizedBox.shrink();
    final totalPages = (vm.totalCount / vm.pageSize).ceil();
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          IconButton(
            icon: const Icon(Icons.chevron_left),
            onPressed:
                vm.pageNumber > 1 ? () => vm.setPage(vm.pageNumber - 1) : null,
          ),
          Text('Página ${vm.pageNumber} de $totalPages '
              '(${vm.totalCount} itens)'),
          IconButton(
            icon: const Icon(Icons.chevron_right),
            onPressed: vm.pageNumber < totalPages
                ? () => vm.setPage(vm.pageNumber + 1)
                : null,
          ),
        ],
      ),
    );
  }
}

/// A self-contained dialog that collects the signature date limit.
///
/// Uses its own [TextEditingController] managed in [State.dispose],
/// ensuring the controller is never accessed after disposal.
/// Returns the date text (dd/MM/yyyy) via [Navigator.pop], or null
/// when cancelled.
class _DateInputDialog extends StatefulWidget {
  const _DateInputDialog({
    this.title = 'Informe a Data',
    this.fieldLabel = 'Data',
  });

  /// The dialog title.
  final String title;

  /// The label for the date input field.
  final String fieldLabel;

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
    return AlertDialog(
      title: Text(widget.title),
      content: SizedBox(
        width: 320,
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
                const SizedBox(height: AppSpacing.sm),
                Row(
                  children: [3, 5, 10].map((days) {
                    return Expanded(
                      child: Padding(
                        padding: EdgeInsets.only(
                          right: days == 10 ? 0 : AppSpacing.sm,
                        ),
                        child: OutlinedButton(
                          onPressed: () => _setDateFromDays(days),
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
