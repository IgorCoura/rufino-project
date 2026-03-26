import 'dart:io';
import 'dart:typed_data';

import 'package:file_picker/file_picker.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';

import '../../../../../core/theme/app_spacing.dart';
import '../../../../../domain/entities/document_group_with_documents.dart';
import '../../../../../domain/entities/employee_document.dart';
import '../../viewmodel/employee_profile_viewmodel.dart';
import 'profile_shared_widgets.dart';

/// Expandable card that displays the employee's required documents grouped
/// by document group, with nested expansion for document units, status
/// badges, pagination with status filter, page size selector, range
/// selection, and actions (edit date, not applicable, generate, send,
/// download).
class DocumentsSection extends StatefulWidget {
  const DocumentsSection({super.key, required this.viewModel});

  /// The profile view model that owns the documents state and exposes
  /// load / mutate operations.
  final EmployeeProfileViewModel viewModel;

  @override
  State<DocumentsSection> createState() => _DocumentsSectionState();
}

class _DocumentsSectionState extends State<DocumentsSection> {
  /// Tracks which document ids are currently expanded.
  final Set<String> _expandedDocIds = {};

  /// Tracks the current pagination page per document id.
  final Map<String, int> _pageMap = {};

  /// Tracks the selected status filter per document id.
  final Map<String, int?> _statusFilterMap = {};

  /// Whether an async operation is in progress (disables buttons).
  bool _isBusy = false;

  /// Available statuses for filtering.
  static const _statusOptions = [
    (id: null, label: 'Todos'),
    (id: 1, label: 'Pendente'),
    (id: 2, label: 'OK'),
    (id: 3, label: 'Obsoleto'),
    (id: 4, label: 'Inválido'),
    (id: 5, label: 'Requer Validação'),
    (id: 6, label: 'Não Aplicável'),
    (id: 7, label: 'Aguardando Assinatura'),
  ];

  /// Available page size options.
  static const _pageSizeOptions = [5, 10, 20, 50];

  // ─── Status badge helpers ──────────────────────────────────────────────────

  /// Returns a display label and color for a document-group-level status.
  ({String label, Color color}) _groupStatus(String statusId) {
    return switch (statusId) {
      '1' => (label: 'OK', color: Colors.green),
      '2' => (label: 'Pendente', color: Colors.orange),
      '3' => (label: 'Inválido', color: Colors.red),
      _ => (label: statusId, color: Colors.grey),
    };
  }

  /// Returns a display label and color for a document-level status.
  ({String label, Color color}) _documentStatus(String statusId) {
    return switch (statusId) {
      '1' => (label: 'Pendente', color: Colors.orange),
      '2' => (label: 'Requer Validação', color: Colors.amber),
      '3' => (label: 'OK', color: Colors.green),
      '4' => (label: 'Obsoleto', color: Colors.grey),
      '5' => (label: 'Aguardando Assinatura', color: Colors.blue),
      _ => (label: statusId, color: Colors.grey),
    };
  }

  /// Returns a color for a document-unit-level status.
  Color _unitStatusColor(String statusId) {
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

  // ─── Helpers ──────────────────────────────────────────────────────────────

  /// Prompts the user to choose a save location and writes [bytes] to disk.
  ///
  /// On web, `FilePicker.platform.saveFile` handles the download via the
  /// `bytes` parameter. On desktop, it only returns a path — the file must
  /// be written manually.
  ///
  /// Returns `true` if the file was saved, `false` if the user cancelled.
  Future<bool> _saveFile({
    required String dialogTitle,
    required String fileName,
    required Uint8List bytes,
  }) async {
    final savePath = await FilePicker.platform.saveFile(
      dialogTitle: dialogTitle,
      fileName: fileName,
      bytes: bytes,
    );
    if (savePath == null) return false;

    if (!kIsWeb) {
      await File(savePath).writeAsBytes(bytes);
    }
    return true;
  }

  /// Builds a download file name from the unit date and document name.
  ///
  /// Converts `dd/MM/yyyy` to `yyyy-MM-dd` and slugifies the document name,
  /// e.g. `"01/03/2026"` + `"Contrato de Trabalho"` → `"2026-03-01-contrato-de-trabalho"`.
  String _downloadFileName(DocumentUnit unit, EmployeeDocument doc,
      {String extension = 'pdf'}) {
    final datePart = unit.date.isNotEmpty && unit.date.length == 10
        ? '${unit.date.substring(6)}-${unit.date.substring(3, 5)}-${unit.date.substring(0, 2)}'
        : 'sem-data';
    final namePart = doc.name
        .toLowerCase()
        .replaceAll(RegExp(r'[^\w\s-]'), '')
        .replaceAll(RegExp(r'\s+'), '-');
    return '$datePart-$namePart.$extension';
  }

  /// Reloads units for [docId] using current page, filter and page size.
  void _reloadUnits(String docId) {
    widget.viewModel.loadDocumentUnits(
      docId,
      pageNumber: _pageMap[docId] ?? 1,
      pageSize: widget.viewModel.getPageSize(docId),
      statusId: _statusFilterMap[docId],
    );
  }

  // ─── Dialogs ───────────────────────────────────────────────────────────────

  /// Shows a dialog to edit the date of a document unit.
  Future<void> _showEditDateDialog(
    EmployeeDocument doc,
    DocumentUnit unit,
  ) async {
    final dateCtrl = TextEditingController(text: unit.date);
    final dateMask = MaskTextInputFormatter(
      mask: '##/##/####',
      filter: {'#': RegExp(r'[0-9]')},
      type: MaskAutoCompletionType.lazy,
    );

    final rawDigits = unit.date.replaceAll(RegExp(r'[^\d]'), '');
    dateMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: rawDigits),
    );
    dateCtrl.text = dateMask.getMaskedText();

    final formKey = GlobalKey<FormState>();

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) {
        return AlertDialog(
          title: const Text('Editar data'),
          content: Form(
            key: formKey,
            child: TextFormField(
              controller: dateCtrl,
              decoration: const InputDecoration(
                labelText: 'Data',
                prefixIcon: Icon(Icons.event_outlined),
                border: OutlineInputBorder(),
                helperText: 'Ex: 15/03/2026',
              ),
              keyboardType: TextInputType.number,
              inputFormatters: [dateMask],
              validator: widget.viewModel.validateContractDate,
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(ctx).pop(false),
              child: const Text('Cancelar'),
            ),
            FilledButton(
              onPressed: () {
                if (formKey.currentState?.validate() == true) {
                  Navigator.of(ctx).pop(true);
                }
              },
              child: const Text('Salvar'),
            ),
          ],
        );
      },
    );

    if (confirmed == true && mounted) {
      await widget.viewModel.editDocumentUnitDate(
        doc.id,
        unit.id,
        dateCtrl.text.trim(),
      );
    }

    dateCtrl.dispose();
  }

  /// Shows a confirmation dialog before marking a unit as not applicable.
  Future<void> _showNotApplicableDialog(
    EmployeeDocument doc,
    DocumentUnit unit,
  ) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) {
        return AlertDialog(
          title: const Text('Confirmar'),
          content: const Text(
            'Deseja marcar este documento como não aplicável?',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(ctx).pop(false),
              child: const Text('Cancelar'),
            ),
            FilledButton(
              onPressed: () => Navigator.of(ctx).pop(true),
              child: const Text('Confirmar'),
            ),
          ],
        );
      },
    );

    if (confirmed == true && mounted) {
      await widget.viewModel.setDocumentUnitNotApplicable(doc.id, unit.id);
    }
  }

  /// Shows a dialog to choose between generate or generate & send for
  /// signature.
  Future<void> _showGenerateDialog(
    EmployeeDocument doc,
    DocumentUnit unit,
  ) async {
    final action = await showDialog<String>(
      context: context,
      builder: (ctx) {
        return AlertDialog(
          title: const Text('Gerar Documento'),
          content: const Text('Escolha uma opção:'),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(ctx).pop(),
              child: const Text('Cancelar'),
            ),
            OutlinedButton(
              onPressed: () => Navigator.of(ctx).pop('generate'),
              child: const Text('Gerar arquivo'),
            ),
            if (doc.isSignable)
              FilledButton(
                onPressed: () => Navigator.of(ctx).pop('generate_sign'),
                child: const Text('Gerar e enviar para assinatura'),
              ),
          ],
        );
      },
    );

    if (!mounted || action == null) return;

    if (action == 'generate') {
      setState(() => _isBusy = true);
      final bytes =
          await widget.viewModel.generateDocument(doc.id, unit.id);
      if (bytes != null && mounted) {
        final saved = await _saveFile(
          dialogTitle: 'Salvar documento',
          fileName: _downloadFileName(unit, doc),
          bytes: bytes,
        );
        if (!saved && mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Salvamento cancelado.'),
              behavior: SnackBarBehavior.floating,
            ),
          );
        }
      }
      setState(() => _isBusy = false);
    } else if (action == 'generate_sign') {
      await _showSignDateDialog(doc, unit, isUpload: false);
    }
  }

  /// Shows a dialog to choose between send file or send file for signature.
  Future<void> _showSendDialog(
    EmployeeDocument doc,
    DocumentUnit unit,
  ) async {
    final action = await showDialog<String>(
      context: context,
      builder: (ctx) {
        return AlertDialog(
          title: const Text('Enviar Documento'),
          content: const Text('Escolha uma opção:'),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(ctx).pop(),
              child: const Text('Cancelar'),
            ),
            OutlinedButton(
              onPressed: () => Navigator.of(ctx).pop('send'),
              child: const Text('Enviar arquivo'),
            ),
            if (doc.isSignable)
              FilledButton(
                onPressed: () => Navigator.of(ctx).pop('send_sign'),
                child: const Text('Enviar para assinatura'),
              ),
          ],
        );
      },
    );

    if (!mounted || action == null) return;

    if (action == 'send') {
      await _pickAndUploadFile(doc, unit, forSign: false);
    } else if (action == 'send_sign') {
      await _pickAndUploadFile(doc, unit, forSign: true);
    }
  }

  /// Picks a file and uploads it to the document unit.
  Future<void> _pickAndUploadFile(
    EmployeeDocument doc,
    DocumentUnit unit, {
    required bool forSign,
  }) async {
    final result = await FilePicker.platform.pickFiles(withData: true);
    if (result == null || result.files.isEmpty) return;

    final file = result.files.first;
    if (file.bytes == null) return;

    if (file.bytes!.lengthInBytes > 10 * 1024 * 1024) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('O arquivo não pode ser maior que 10 MB.'),
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
      return;
    }

    if (forSign) {
      if (mounted) {
        await _showSignDateDialog(
          doc,
          unit,
          isUpload: true,
          fileBytes: file.bytes,
          fileName: file.name,
        );
      }
    } else {
      setState(() => _isBusy = true);
      await widget.viewModel.uploadDocumentUnit(
        doc.id,
        unit.id,
        file.bytes!,
        file.name,
      );
      setState(() => _isBusy = false);
    }
  }

  /// Shows a dialog to configure the sign date limit and reminder interval.
  Future<void> _showSignDateDialog(
    EmployeeDocument doc,
    DocumentUnit unit, {
    required bool isUpload,
    Uint8List? fileBytes,
    String? fileName,
  }) async {
    final dateCtrl = TextEditingController();
    final reminderCtrl = TextEditingController(text: '3');
    final dateMask = MaskTextInputFormatter(
      mask: '##/##/####',
      filter: {'#': RegExp(r'[0-9]')},
      type: MaskAutoCompletionType.lazy,
    );
    final formKey = GlobalKey<FormState>();

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) {
        return AlertDialog(
          title: const Text('Configurar Assinatura'),
          content: Form(
            key: formKey,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextFormField(
                  controller: dateCtrl,
                  decoration: const InputDecoration(
                    labelText: 'Data limite para assinatura',
                    prefixIcon: Icon(Icons.event_outlined),
                    border: OutlineInputBorder(),
                    helperText: 'Ex: 15/03/2026',
                  ),
                  keyboardType: TextInputType.number,
                  inputFormatters: [dateMask],
                  validator: widget.viewModel.validateContractDate,
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: reminderCtrl,
                  decoration: const InputDecoration(
                    labelText: 'Lembrete a cada N dias',
                    prefixIcon: Icon(Icons.notifications_outlined),
                    border: OutlineInputBorder(),
                  ),
                  keyboardType: TextInputType.number,
                  validator: (v) {
                    if (v == null || v.trim().isEmpty) {
                      return 'Informe o intervalo.';
                    }
                    final n = int.tryParse(v.trim());
                    if (n == null || n < 1 || n > 365) {
                      return 'Informe um valor entre 1 e 365.';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: AppSpacing.sm),
                Wrap(
                  spacing: AppSpacing.sm,
                  children: [3, 5, 10].map((days) {
                    return ActionChip(
                      label: Text('$days dias'),
                      onPressed: () =>
                          reminderCtrl.text = days.toString(),
                    );
                  }).toList(),
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(ctx).pop(false),
              child: const Text('Cancelar'),
            ),
            FilledButton(
              onPressed: () {
                if (formKey.currentState?.validate() == true) {
                  Navigator.of(ctx).pop(true);
                }
              },
              child: const Text('Confirmar'),
            ),
          ],
        );
      },
    );

    if (confirmed != true || !mounted) {
      dateCtrl.dispose();
      reminderCtrl.dispose();
      return;
    }

    final dateLimitToSign = dateCtrl.text.trim();
    final reminderDays = int.tryParse(reminderCtrl.text.trim()) ?? 3;

    setState(() => _isBusy = true);

    if (isUpload && fileBytes != null && fileName != null) {
      await widget.viewModel.uploadDocumentUnitToSign(
        doc.id,
        unit.id,
        fileBytes,
        fileName,
        dateLimitToSign,
        reminderDays,
      );
    } else {
      await widget.viewModel.generateAndSendToSign(
        doc.id,
        unit.id,
        dateLimitToSign,
        reminderDays,
      );
    }

    setState(() => _isBusy = false);
    dateCtrl.dispose();
    reminderCtrl.dispose();
  }

  /// Downloads an existing document unit file.
  Future<void> _downloadUnit(
    EmployeeDocument doc,
    DocumentUnit unit,
  ) async {
    setState(() => _isBusy = true);

    final bytes =
        await widget.viewModel.downloadDocumentUnit(doc.id, unit.id);
    if (bytes != null && mounted) {
      final ext = unit.name.contains('.')
          ? unit.name.split('.').last
          : 'pdf';
      await _saveFile(
        dialogTitle: 'Salvar documento',
        fileName: _downloadFileName(unit, doc, extension: ext),
        bytes: bytes,
      );
    }

    setState(() => _isBusy = false);
  }

  /// Shows the range confirmation dialog listing processable vs
  /// non-processable items, then executes the batch operation.
  Future<void> _showRangeConfirmationDialog({
    required bool isGenerate,
  }) async {
    final selected = widget.viewModel.selectedDocumentUnits;
    final canExecute = isGenerate
        ? selected.where((e) => e.canGenerate).toList()
        : selected.where((e) => e.hasFile).toList();
    final cannotExecute = isGenerate
        ? selected.where((e) => !e.canGenerate).toList()
        : selected.where((e) => !e.hasFile).toList();

    final actionName = isGenerate ? 'geração' : 'download';

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) {
        return AlertDialog(
          title: Text('Confirmar $actionName'),
          content: SizedBox(
            width: 500,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if (canExecute.isNotEmpty) ...[
                    Text(
                      'Os seguintes documentos serão processados '
                      '(${canExecute.length}):',
                      style: const TextStyle(fontWeight: FontWeight.bold),
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    ...canExecute.map((item) => Padding(
                          padding: const EdgeInsets.symmetric(
                              vertical: 2),
                          child: Row(
                            children: [
                              const Icon(Icons.check_circle,
                                  color: Colors.green, size: 16),
                              const SizedBox(width: AppSpacing.sm),
                              Expanded(
                                child: Text(
                                  '${item.documentName} — '
                                  '${item.documentUnitDate}',
                                ),
                              ),
                            ],
                          ),
                        )),
                  ],
                  if (cannotExecute.isNotEmpty) ...[
                    const SizedBox(height: AppSpacing.md),
                    Text(
                      'Os seguintes documentos NÃO podem ser processados '
                      '(${cannotExecute.length}):',
                      style: TextStyle(
                        fontWeight: FontWeight.bold,
                        color: Theme.of(ctx).colorScheme.error,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    ...cannotExecute.map((item) => Padding(
                          padding: const EdgeInsets.symmetric(
                              vertical: 2),
                          child: Row(
                            children: [
                              Icon(Icons.cancel,
                                  color: Theme.of(ctx).colorScheme.error,
                                  size: 16),
                              const SizedBox(width: AppSpacing.sm),
                              Expanded(
                                child: Text(
                                  '${item.documentName} — '
                                  '${item.documentUnitDate}',
                                ),
                              ),
                            ],
                          ),
                        )),
                  ],
                  if (canExecute.isEmpty) ...[
                    const SizedBox(height: AppSpacing.md),
                    Text(
                      'Nenhum documento selecionado pode ser processado.',
                      style: TextStyle(
                          color: Theme.of(ctx).colorScheme.error),
                    ),
                  ],
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(ctx).pop(false),
              child: const Text('Cancelar'),
            ),
            if (canExecute.isNotEmpty)
              FilledButton(
                onPressed: () => Navigator.of(ctx).pop(true),
                child: Text(
                    'Confirmar ${isGenerate ? 'Geração' : 'Download'}'),
              ),
          ],
        );
      },
    );

    if (confirmed != true || !mounted) return;

    setState(() => _isBusy = true);
    final bytes = isGenerate
        ? await widget.viewModel.generateDocumentRange()
        : await widget.viewModel.downloadDocumentRange();

    if (bytes != null && mounted) {
      await _saveFile(
        dialogTitle: isGenerate
            ? 'Salvar documentos gerados'
            : 'Salvar documentos',
        fileName: isGenerate
            ? 'documentos_gerados.zip'
            : 'documentos_download.zip',
        bytes: bytes,
      );
    }
    setState(() => _isBusy = false);
  }

  // ─── Build ─────────────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.documentsStatus;
        return ExpandableSectionCard(
          title: 'Documentos',
          onExpand: widget.viewModel.loadDocumentGroups,
          trailing: _buildHeaderTrailing(),
          child: _buildContent(context, status),
        );
      },
    );
  }

  /// Builds the selection toggle button shown in the section header.
  Widget? _buildHeaderTrailing() {
    final vm = widget.viewModel;
    if (vm.documentsStatus != SectionLoadStatus.loaded) return null;
    if (vm.documentGroups.isEmpty) return null;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        if (vm.isSelectingRange && vm.selectedDocumentUnits.isNotEmpty) ...[
          Container(
            padding:
                const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
            decoration: BoxDecoration(
              color: Theme.of(context)
                  .colorScheme
                  .primary
                  .withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Text(
              '${vm.selectedDocumentUnits.length} selecionado(s)',
              style: Theme.of(context).textTheme.labelSmall,
            ),
          ),
          const SizedBox(width: AppSpacing.xs),
        ],
        TextButton.icon(
          onPressed:
              _isBusy ? null : () => vm.toggleRangeSelectionMode(),
          icon: Icon(
            vm.isSelectingRange ? Icons.close : Icons.checklist,
            size: 18,
          ),
          label: Text(vm.isSelectingRange ? 'Cancelar' : 'Selecionar'),
        ),
      ],
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
              child: Text('Não foi possível carregar os documentos.'),
            ),
          ],
        ),
      );
    }

    final groups = widget.viewModel.documentGroups;

    if (groups.isEmpty) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: AppSpacing.md),
        child: Center(child: Text('Nenhum documento encontrado.')),
      );
    }

    return Column(
      children: [
        ...groups.map((group) => _buildGroupTile(context, group)),
        if (widget.viewModel.isSelectingRange) _buildRangeActionBar(),
      ],
    );
  }

  // ─── Group level ──────────────────────────────────────────────────────────

  Widget _buildGroupTile(
      BuildContext context, DocumentGroupWithDocuments group) {
    final badge = _groupStatus(group.statusId);
    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: Card.outlined(
        clipBehavior: Clip.antiAlias,
        child: ExpansionTile(
          controlAffinity: ListTileControlAffinity.leading,
          title: Row(
            children: [
              _StatusBadge(label: badge.label, color: badge.color),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: Text(
                  group.name,
                  style: Theme.of(context).textTheme.titleSmall,
                ),
              ),
            ],
          ),
          childrenPadding: const EdgeInsets.fromLTRB(
            AppSpacing.md,
            0,
            AppSpacing.md,
            AppSpacing.md,
          ),
          children: group.documents.isEmpty
              ? [
                  const Padding(
                    padding: EdgeInsets.symmetric(vertical: AppSpacing.sm),
                    child: Text('Nenhum item encontrado'),
                  ),
                ]
              : group.documents
                  .map((doc) => _buildDocumentTile(context, doc))
                  .toList(),
        ),
      ),
    );
  }

  // ─── Document level ───────────────────────────────────────────────────────

  Widget _buildDocumentTile(BuildContext context, EmployeeDocument doc) {
    final badgeInfo = _documentStatus(doc.statusId);
    final isExpanded = _expandedDocIds.contains(doc.id);

    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: Card.outlined(
        clipBehavior: Clip.antiAlias,
        child: ExpansionTile(
          title: Text(
            doc.name,
            style: Theme.of(context).textTheme.titleSmall,
          ),
          trailing: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              _StatusBadge(label: badgeInfo.label, color: badgeInfo.color),
              if (doc.usePreviousPeriod) ...[
                const SizedBox(width: AppSpacing.xs),
                _StatusBadge(
                  label: 'Usa período anterior',
                  color: Colors.deepPurple,
                ),
              ],
              const SizedBox(width: AppSpacing.xs),
              Icon(
                isExpanded ? Icons.expand_less : Icons.expand_more,
              ),
            ],
          ),
          onExpansionChanged: (expanded) {
            setState(() {
              if (expanded) {
                _expandedDocIds.add(doc.id);
                _pageMap[doc.id] = 1;
                _reloadUnits(doc.id);
              } else {
                _expandedDocIds.remove(doc.id);
              }
            });
          },
          childrenPadding: const EdgeInsets.fromLTRB(
            AppSpacing.md,
            0,
            AppSpacing.md,
            AppSpacing.md,
          ),
          children: [
            _buildStatusFilter(context, doc),
            _buildUnitsList(context, doc),
          ],
        ),
      ),
    );
  }

  Widget _buildStatusFilter(BuildContext context, EmployeeDocument doc) {
    final currentFilter = _statusFilterMap[doc.id];

    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: DropdownButtonFormField<int?>(
        value: currentFilter,
        decoration: const InputDecoration(
          labelText: 'Status',
          prefixIcon: Icon(Icons.filter_list),
          border: OutlineInputBorder(),
          isDense: true,
        ),
        items: _statusOptions.map((opt) {
          return DropdownMenuItem<int?>(
            value: opt.id,
            child: Text(opt.label),
          );
        }).toList(),
        onChanged: (value) {
          setState(() {
            _statusFilterMap[doc.id] = value;
            _pageMap[doc.id] = 1;
          });
          _reloadUnits(doc.id);
        },
      ),
    );
  }

  // ─── Units list ───────────────────────────────────────────────────────────

  Widget _buildUnitsList(BuildContext context, EmployeeDocument doc) {
    final units = doc.units;

    if (units.isEmpty) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: AppSpacing.sm),
        child: Center(
            child: Text('Nenhuma unidade encontrada para o filtro.')),
      );
    }

    final currentPage = _pageMap[doc.id] ?? 1;
    final pageSize = widget.viewModel.getPageSize(doc.id);
    final totalPages =
        doc.totalUnitsCount == 0 ? 1 : (doc.totalUnitsCount / pageSize).ceil();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        ...units.map((unit) => _buildUnitRow(context, doc, unit)),
        const SizedBox(height: AppSpacing.sm),
        Align(
          alignment: Alignment.centerLeft,
          child: TextButton.icon(
            onPressed: _isBusy
                ? null
                : () => widget.viewModel.createDocumentUnit(doc.id),
            icon: const Icon(Icons.add, size: 18),
            label: const Text('Adicionar'),
          ),
        ),
        const SizedBox(height: AppSpacing.sm),
        _buildPaginationBar(context, doc, currentPage, totalPages, pageSize),
      ],
    );
  }

  /// Builds the pagination bar with navigation, summary and page size
  /// dropdown in a single row.
  Widget _buildPaginationBar(
    BuildContext context,
    EmployeeDocument doc,
    int currentPage,
    int totalPages,
    int pageSize,
  ) {
    final cs = Theme.of(context).colorScheme;
    final textStyle = Theme.of(context).textTheme.bodySmall?.copyWith(
          color: cs.onSurfaceVariant,
        );
    final hasMultiplePages = totalPages > 1;
    final pageNumbers = hasMultiplePages
        ? _buildPageNumbers(currentPage, totalPages)
        : <int?>[];

    void changePage(int page) {
      setState(() => _pageMap[doc.id] = page);
      _reloadUnits(doc.id);
    }

    return Row(
      children: [
        // Left: item summary.
        Expanded(
          child: Text(
            hasMultiplePages
                ? 'Página $currentPage de $totalPages '
                    '(${doc.totalUnitsCount} itens)'
                : '${doc.totalUnitsCount} '
                    '${doc.totalUnitsCount == 1 ? 'item' : 'itens'}',
            style: textStyle,
          ),
        ),

        // Center: page navigation buttons.
        if (hasMultiplePages) ...[
          IconButton(
            icon: const Icon(Icons.chevron_left, size: 20),
            tooltip: 'Página anterior',
            visualDensity: VisualDensity.compact,
            onPressed: currentPage > 1
                ? () => changePage(currentPage - 1)
                : null,
          ),
          ...pageNumbers.map((page) {
            if (page == null) {
              return Padding(
                padding: const EdgeInsets.symmetric(
                    horizontal: AppSpacing.xs),
                child: Text('…', style: textStyle),
              );
            }
            final isActive = page == currentPage;
            return GestureDetector(
              onTap: () => changePage(page),
              child: Container(
                margin: const EdgeInsets.symmetric(horizontal: 2),
                padding: const EdgeInsets.symmetric(
                    horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: isActive ? cs.primary : Colors.transparent,
                  borderRadius: BorderRadius.circular(4),
                  border: Border.all(
                    color: isActive ? cs.primary : cs.outlineVariant,
                  ),
                ),
                child: Text(
                  '$page',
                  style: textStyle?.copyWith(
                    color: isActive ? cs.onPrimary : null,
                    fontWeight:
                        isActive ? FontWeight.bold : FontWeight.normal,
                  ),
                ),
              ),
            );
          }),
          IconButton(
            icon: const Icon(Icons.chevron_right, size: 20),
            tooltip: 'Próxima página',
            visualDensity: VisualDensity.compact,
            onPressed: currentPage < totalPages
                ? () => changePage(currentPage + 1)
                : null,
          ),
        ],

        // Right: page size dropdown.
        Text('Por página:', style: textStyle),
        const SizedBox(width: AppSpacing.xs),
        DropdownButton<int>(
          value: pageSize,
          isDense: true,
          items: _pageSizeOptions
              .map((s) => DropdownMenuItem<int>(
                    value: s,
                    child: Text('$s', style: textStyle),
                  ))
              .toList(),
          onChanged: (v) {
            if (v != null) {
              setState(() => _pageMap[doc.id] = 1);
              widget.viewModel.changePageSize(doc.id, v);
            }
          },
        ),
      ],
    );
  }

  /// Returns page numbers with `null` representing ellipsis gaps.
  static List<int?> _buildPageNumbers(int currentPage, int totalPages) {
    const maxVisible = 5;
    if (totalPages <= maxVisible) {
      return List.generate(totalPages, (i) => i + 1);
    }
    final pages = <int>{1, totalPages};
    for (var i = currentPage - 1; i <= currentPage + 1; i++) {
      if (i >= 1 && i <= totalPages) pages.add(i);
    }
    final sorted = pages.toList()..sort();
    final result = <int?>[];
    for (var i = 0; i < sorted.length; i++) {
      if (i > 0 && sorted[i] - sorted[i - 1] > 1) result.add(null);
      result.add(sorted[i]);
    }
    return result;
  }

  // ─── Unit row ─────────────────────────────────────────────────────────────

  Widget _buildUnitRow(
    BuildContext context,
    EmployeeDocument doc,
    DocumentUnit unit,
  ) {
    final statusColor = _unitStatusColor(unit.statusId);
    final cs = Theme.of(context).colorScheme;

    // Visibility conditions matching the legacy app logic.
    final hasValidDate =
        unit.date.isNotEmpty && unit.date != '01/01/0001';
    final canGenerate =
        unit.isPending && hasValidDate && doc.canGenerateDocument;
    final canSend = unit.isPending && hasValidDate;

    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.xs),
      child: Card.outlined(
        child: Padding(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.md,
            vertical: AppSpacing.sm,
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  // Range selection checkbox.
                  if (widget.viewModel.isSelectingRange)
                    Checkbox(
                      value: widget.viewModel
                          .isDocumentUnitSelected(unit.id),
                      onChanged: (_) {
                        widget.viewModel.toggleDocumentUnitSelection(
                          SelectedDocumentUnit(
                            documentId: doc.id,
                            documentUnitId: unit.id,
                            documentName: doc.name,
                            documentUnitDate: unit.date,
                            canGenerate: canGenerate,
                            hasFile: unit.hasFile,
                          ),
                        );
                      },
                    ),
                  Container(
                    width: 12,
                    height: 12,
                    decoration: BoxDecoration(
                      color: statusColor,
                      shape: BoxShape.circle,
                    ),
                  ),
                  const SizedBox(width: AppSpacing.md),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          unit.statusName,
                          style: Theme.of(context)
                              .textTheme
                              .bodyMedium
                              ?.copyWith(fontWeight: FontWeight.w600),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          'Data: ${unit.date}',
                          style: Theme.of(context)
                              .textTheme
                              .bodySmall
                              ?.copyWith(color: cs.onSurfaceVariant),
                        ),
                        if (unit.validity.isNotEmpty)
                          Text(
                            'Validade: ${unit.validity}',
                            style: Theme.of(context)
                                .textTheme
                                .bodySmall
                                ?.copyWith(color: cs.onSurfaceVariant),
                          ),
                      ],
                    ),
                  ),
                  // Action icon buttons in the trailing area.
                  if (unit.isPending) ...[
                    IconButton(
                      icon: const Icon(Icons.edit, size: 20),
                      tooltip: 'Editar data',
                      onPressed: _isBusy
                          ? null
                          : () => _showEditDateDialog(doc, unit),
                    ),
                    IconButton(
                      icon: const Icon(Icons.block, size: 20),
                      tooltip: 'Não aplicável',
                      onPressed: _isBusy
                          ? null
                          : () => _showNotApplicableDialog(doc, unit),
                    ),
                    if (canGenerate)
                      IconButton(
                        icon: const Icon(
                            Icons.sim_card_download_outlined,
                            size: 20),
                        tooltip: 'Gerar',
                        onPressed: _isBusy
                            ? null
                            : () => _showGenerateDialog(doc, unit),
                      ),
                    if (canSend)
                      IconButton(
                        icon: const Icon(
                            Icons.upload_file_outlined, size: 20),
                        tooltip: 'Enviar',
                        onPressed: _isBusy
                            ? null
                            : () => _showSendDialog(doc, unit),
                      ),
                  ] else if (unit.hasFile)
                    IconButton(
                      icon: const Icon(Icons.search, size: 20),
                      tooltip: 'Visualizar',
                      onPressed: _isBusy
                          ? null
                          : () => _downloadUnit(doc, unit),
                    ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  // ─── Range action bar ─────────────────────────────────────────────────────

  Widget _buildRangeActionBar() {
    final selected = widget.viewModel.selectedDocumentUnits;
    final canGenerateCount = selected.where((e) => e.canGenerate).length;
    final canDownloadCount = selected.where((e) => e.hasFile).length;

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.md,
        vertical: AppSpacing.md,
      ),
      decoration: BoxDecoration(
        color: Theme.of(context)
            .colorScheme
            .primaryContainer
            .withValues(alpha: 0.3),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              const Icon(Icons.info_outline, size: 16),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: Text(
                  '${selected.length} selecionado(s) · '
                  '$canGenerateCount pode(m) gerar · '
                  '$canDownloadCount pode(m) baixar',
                  style: Theme.of(context).textTheme.bodySmall,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.sm),
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              OutlinedButton.icon(
                onPressed: selected.isEmpty || _isBusy
                    ? null
                    : () => _showRangeConfirmationDialog(
                          isGenerate: true,
                        ),
                icon: const Icon(
                    Icons.sim_card_download_outlined,
                    size: 18),
                label: const Text('Gerar Selecionados'),
              ),
              const SizedBox(width: AppSpacing.sm),
              OutlinedButton.icon(
                onPressed: selected.isEmpty || _isBusy
                    ? null
                    : () => _showRangeConfirmationDialog(
                          isGenerate: false,
                        ),
                icon: const Icon(Icons.download, size: 18),
                label: const Text('Baixar Selecionados'),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

/// Small action button used for document unit operations.
class _ActionButton extends StatelessWidget {
  const _ActionButton({
    required this.icon,
    required this.label,
    required this.onPressed,
  });

  final IconData icon;
  final String label;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) {
    return TextButton.icon(
      onPressed: onPressed,
      icon: Icon(icon, size: 16),
      label: Text(label),
      style: TextButton.styleFrom(
        visualDensity: VisualDensity.compact,
        padding: const EdgeInsets.symmetric(horizontal: AppSpacing.sm),
      ),
    );
  }
}

/// Small coloured badge used to display a document or unit status.
class _StatusBadge extends StatelessWidget {
  const _StatusBadge({required this.label, required this.color});

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: AppSpacing.xs,
      ),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        label,
        style: Theme.of(context).textTheme.labelSmall?.copyWith(
              color: color,
              fontWeight: FontWeight.w600,
            ),
      ),
    );
  }
}
