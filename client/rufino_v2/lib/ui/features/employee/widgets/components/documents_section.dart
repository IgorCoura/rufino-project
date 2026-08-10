import 'dart:async';

import 'package:file_picker/file_picker.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';

import '../../../../../core/utils/file_saver_stub.dart'
    if (dart.library.io) '../../../../../core/utils/file_saver.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';
import 'package:syncfusion_flutter_pdfviewer/pdfviewer.dart';

import '../../../../../core/errors/document_scanner_exception.dart';
import '../../../../../core/result.dart';
import '../../../../../core/theme/app_breakpoints.dart';
import '../../../../../core/theme/app_spacing.dart';
import '../../../../../core/utils/image_to_pdf_converter.dart';
import '../../../../../core/utils/pdf_merger.dart';
import '../../../../../domain/entities/document_group_with_documents.dart';
import '../../../../../domain/entities/employee_document.dart';
import '../../../../core/widgets/outdated_content_dialog.dart';
import '../../../../core/widgets/permission_guard.dart';
import '../../../../core/widgets/scanner_error_handler.dart';
import '../../viewmodel/employee_profile_viewmodel.dart';
import '../../../batch_document/widgets/document_scan_dialog.dart';
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
    (id: 8, label: 'A Vencer'),
    (id: 9, label: 'Vencido'),
  ];

  /// Available page size options.
  static const _pageSizeOptions = [5, 10, 20, 50];

  // ─── Status color helpers (visual concern — stays in UI) ────────────────

  /// Returns a color for a document-group-level status.
  ///
  /// Three-valued compliance scale, same ids as the employee list
  /// (0=Okay, 1=A Vencer, 2=Requer Atenção).
  Color _groupStatusColor(String statusId) {
    return switch (statusId) {
      '0' => Colors.green,
      '1' => Colors.orange,
      '2' => Colors.red,
      _ => Colors.grey,
    };
  }

  /// Returns a color for a document-level status (1–7).
  Color _documentStatusColor(String statusId) {
    return switch (statusId) {
      '1' => Colors.orange,
      '2' => Colors.amber,
      '3' => Colors.green,
      '4' => Colors.grey,
      '5' => Colors.blue,
      '6' => Colors.deepOrange,
      '7' => Colors.redAccent,
      _ => Colors.grey,
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
      '8' => Colors.deepOrange,
      // Vencido é o único que representa falta de cobertura AGORA — cor mais forte que a de "a vencer".
      '9' => Colors.redAccent,
      _ => Colors.grey,
    };
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────

  /// Saves [bytes] as a file with [fileName].
  ///
  /// On web, triggers a browser download. On desktop, opens a native
  /// save-file dialog.
  Future<bool> _saveFile({
    required String dialogTitle,
    required String fileName,
    required Uint8List bytes,
  }) async {
    await saveFile(fileName: fileName, bytes: bytes);
    return true;
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
              validator: widget.viewModel.validateDocumentUnitDate,
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

  /// Asks the user to confirm a destructive status change on a unit.
  ///
  /// Returns whether the user confirmed. All three status actions go through
  /// here — they change what the document proves, so none of them is a
  /// single-tap operation.
  Future<bool> _confirmUnitStatusChange({
    required String title,
    required String message,
    required String confirmLabel,
    required Key confirmKey,
  }) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) {
        return AlertDialog(
          title: Text(title),
          content: Text(message),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(ctx).pop(false),
              child: const Text('Cancelar'),
            ),
            FilledButton(
              key: confirmKey,
              onPressed: () => Navigator.of(ctx).pop(true),
              child: Text(confirmLabel),
            ),
          ],
        );
      },
    );

    return confirmed == true && mounted;
  }

  /// Shows a confirmation dialog before marking a unit as not applicable.
  Future<void> _showNotApplicableDialog(
    EmployeeDocument doc,
    DocumentUnit unit,
  ) async {
    final confirmed = await _confirmUnitStatusChange(
      title: 'Marcar como não aplicável',
      message: 'Este documento deixa de ser exigido deste funcionário. '
          'Deseja continuar?',
      confirmLabel: 'Marcar',
      confirmKey: const ValueKey('unit-not-applicable-confirm'),
    );

    if (confirmed) {
      await widget.viewModel.setDocumentUnitNotApplicable(doc.id, unit.id);
    }
  }

  /// Shows a confirmation dialog before deprecating a unit.
  Future<void> _showDeprecateDialog(
    EmployeeDocument doc,
    DocumentUnit unit,
  ) async {
    final confirmed = await _confirmUnitStatusChange(
      title: 'Depreciar documento',
      message: 'O documento sai de vigência mas continua guardado como '
          'comprovação do período que cobriu. Uma nova pendência será criada '
          'no lugar. Deseja continuar?',
      confirmLabel: 'Depreciar',
      confirmKey: const ValueKey('unit-deprecate-confirm'),
    );

    if (confirmed) {
      await widget.viewModel.deprecateDocumentUnit(doc.id, unit.id);
    }
  }

  /// Shows a confirmation dialog before invalidating a unit.
  ///
  /// A unit marked as not applicable gets its own wording: there is no error to
  /// undo, the document simply became required again.
  Future<void> _showInvalidateDialog(
    EmployeeDocument doc,
    DocumentUnit unit,
  ) async {
    final confirmed = await _confirmUnitStatusChange(
      title: unit.isNotApplicable
          ? 'Voltar a exigir documento'
          : 'Invalidar documento',
      message: unit.isNotApplicable
          ? 'Este documento voltou a ser exigido deste funcionário. A marcação '
              'de "não aplicável" deixa de valer e uma nova pendência será '
              'criada no lugar. Deseja continuar?'
          : 'Use quando o documento tem erro ou foi enviado por engano — '
              'ele deixa de ter qualquer valor legal. Uma nova pendência será '
              'criada no lugar. Deseja continuar?',
      confirmLabel: unit.isNotApplicable ? 'Voltar a exigir' : 'Invalidar',
      confirmKey: const ValueKey('unit-invalidate-confirm'),
    );

    if (confirmed) {
      await widget.viewModel.invalidateDocumentUnit(doc.id, unit.id);
    }
  }

  /// Whether the employee can receive files for digital signature.
  ///
  /// Physical-signature (or unset) employees must never see the
  /// send-to-signature actions.
  bool get _canSendToSign =>
      widget.viewModel.profile?.canReceiveDocumentsToSign ?? false;

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
            if (doc.isSignable && _canSendToSign)
              PermissionGuard(
                resource: 'document',
                scope: 'send2sign',
                child: OutlinedButton(
                  key: const ValueKey('generate-dialog-schedule-sign'),
                  onPressed: () => Navigator.of(ctx).pop('schedule_sign'),
                  child: const Text('Agendar envio'),
                ),
              ),
            if (doc.isSignable && _canSendToSign)
              PermissionGuard(
                resource: 'document',
                scope: 'send2sign',
                child: FilledButton(
                  onPressed: () => Navigator.of(ctx).pop('generate_sign'),
                  child: const Text('Gerar e enviar para assinatura'),
                ),
              ),
          ],
        );
      },
    );

    if (!mounted || action == null) return;

    // Agendar não passa pelo aviso de snapshot: o PDF só é montado na data do
    // disparo, então quem vale é o cadastro daquele momento — avisar agora
    // sobre um dado que ainda vai mudar seria informação errada.
    if (action == 'schedule_sign') {
      await _showScheduleSignDialog(doc, unit);
      return;
    }

    // O PDF é montado a partir do snapshot gravado na unidade, então os dois
    // caminhos (gerar e gerar+assinar) passam pelo mesmo aviso.
    final proceed = await _confirmSnapshotFreshness([
      SelectedDocumentUnit(
        documentId: doc.id,
        documentUnitId: unit.id,
        documentName: doc.name,
        documentUnitDate: unit.date,
        canGenerate: true,
        hasFile: unit.hasFile,
      ),
    ]);
    if (!proceed || !mounted) return;

    if (action == 'generate') {
      setState(() => _isBusy = true);
      final bytes = await widget.viewModel.generateDocument(doc.id, unit.id);
      if (bytes != null && mounted) {
        final saved = await _saveFile(
          dialogTitle: 'Salvar documento',
          fileName: unit.downloadFileName(
            doc.name,
            employeeName: widget.viewModel.profile?.name ?? '',
          ),
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

  /// Checks whether the snapshot of [units] is still current and, when it is
  /// not, asks the user how to proceed.
  ///
  /// Returns whether the caller should carry on with the generation. Every
  /// unit is listed so the user can see which ones are affected; only the
  /// outdated ones are refreshed when the user asks for it.
  Future<bool> _confirmSnapshotFreshness(
    List<SelectedDocumentUnit> units,
  ) async {
    final outdated =
        await widget.viewModel.checkOutdatedDocumentContent(units);
    if (outdated.isEmpty) return true;
    if (!mounted) return false;

    final action = await showOutdatedContentDialog(
      context,
      rows: units
          .map((u) => OutdatedDocumentRow(
                title: u.documentName,
                subtitle: u.documentUnitDate,
                isOutdated: outdated.contains(u.documentUnitId),
              ))
          .toList(),
    );
    if (!mounted) return false;

    switch (action) {
      case OutdatedContentAction.cancel:
        return false;
      case OutdatedContentAction.continueAnyway:
        return true;
      case OutdatedContentAction.refreshAndContinue:
        return widget.viewModel.refreshDocumentContent(
          units.where((u) => outdated.contains(u.documentUnitId)).toList(),
        );
    }
  }

  /// Shows a dialog to choose between send file, scan, or send for signature.
  Future<void> _showSendDialog(
    EmployeeDocument doc,
    DocumentUnit unit,
  ) async {
    final scanSupported = widget.viewModel.isScanSupported || kIsWeb;

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
            if (scanSupported)
              PermissionGuard(
                resource: 'document',
                scope: 'upload',
                child: OutlinedButton.icon(
                  onPressed: () => Navigator.of(ctx).pop('scan'),
                  icon: const Icon(Icons.document_scanner_outlined, size: 18),
                  label: const Text('Digitalizar'),
                ),
              ),
            OutlinedButton(
              onPressed: () => Navigator.of(ctx).pop('send'),
              child: const Text('Enviar arquivo'),
            ),
            if (doc.isSignable && _canSendToSign)
              PermissionGuard(
                resource: 'document',
                scope: 'send2sign',
                child: FilledButton(
                  onPressed: () => Navigator.of(ctx).pop('send_sign'),
                  child: const Text('Enviar para assinatura'),
                ),
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
    } else if (action == 'scan') {
      await _scanAndUploadFile(doc, unit);
    }
  }

  /// Picks one or more files (PDF or images) and uploads to the document unit.
  ///
  /// Image files are automatically converted to PDF. When multiple files are
  /// selected, shows a confirmation dialog and merges them into a single PDF
  /// before uploading.
  Future<void> _pickAndUploadFile(
    EmployeeDocument doc,
    DocumentUnit unit, {
    required bool forSign,
  }) async {
    final result = await FilePicker.platform.pickFiles(
      withData: true,
      allowMultiple: true,
      type: FileType.custom,
      allowedExtensions: kAllowedUploadExtensions,
    );
    if (result == null || result.files.isEmpty) return;

    final validFiles = result.files.where((f) => f.bytes != null).toList();
    if (validFiles.isEmpty) return;

    Uint8List fileBytes;
    String fileName;

    final hasImages = validFiles.any(
      (f) => isImageExtension(f.extension?.toLowerCase() ?? ''),
    );

    if (validFiles.length == 1) {
      final file = validFiles.first;
      final ext = file.extension?.toLowerCase() ?? '';

      if (isImageExtension(ext)) {
        await _showProcessingDialog('Convertendo imagem para PDF...');
        try {
          fileBytes = await convertImagesToPdf([file.bytes!]);
        } finally {
          _dismissProcessingDialog();
        }
        if (!mounted) return;
        final baseName = file.name.replaceAll(
          RegExp(r'\.[^.]+$'),
          '',
        );
        fileName = '$baseName.pdf';
      } else {
        fileBytes = file.bytes!;
        fileName = file.name;
      }
    } else {
      final confirmed = await _showMergeConfirmationDialog(validFiles);
      if (!confirmed || !mounted) return;

      await _showProcessingDialog(
        hasImages
            ? 'Convertendo e combinando arquivos...'
            : 'Combinando arquivos...',
      );

      try {
        // Separate images from PDFs and convert images to PDF first.
        final pdfEntries = <PdfFileEntry>[];
        final imageBytes = <Uint8List>[];

        for (final file in validFiles) {
          final ext = file.extension?.toLowerCase() ?? '';
          if (isImageExtension(ext)) {
            imageBytes.add(file.bytes!);
          } else {
            pdfEntries.add(
              PdfFileEntry(name: file.name, bytes: file.bytes!),
            );
          }
        }

        if (imageBytes.isNotEmpty) {
          final convertedPdf = await convertImagesToPdf(imageBytes);
          pdfEntries.add(
            PdfFileEntry(
              name: 'imagens_convertidas.pdf',
              bytes: convertedPdf,
            ),
          );
        }

        final mergeResult = await mergePdfFiles(pdfEntries);

        final merged = mergeResult.fold(
          onSuccess: (bytes) => bytes,
          onError: (error, _) {
            if (mounted) {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  content: Text(
                    'Erro ao processar o arquivo "$error". '
                    'Verifique se é um arquivo válido (PDF ou imagem).',
                  ),
                  behavior: SnackBarBehavior.floating,
                ),
              );
            }
            return null;
          },
        );
        if (merged == null) return;

        fileBytes = merged;
        final baseName = validFiles.first.name.replaceAll(
          RegExp(r'\.[^.]+$'),
          '',
        );
        fileName = '${baseName}_combinado.pdf';
      } finally {
        _dismissProcessingDialog();
      }
      if (!mounted) return;
    }

    if (fileBytes.lengthInBytes > 10 * 1024 * 1024) {
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
          fileBytes: fileBytes,
          fileName: fileName,
        );
      }
    } else {
      await _showUploadProgressDialog();
      try {
        await widget.viewModel.uploadDocumentUnit(
          doc.id,
          unit.id,
          fileBytes,
          fileName,
        );
      } finally {
        _dismissProcessingDialog();
      }
    }
  }

  /// Scans document pages with the camera and uploads as a PDF.
  ///
  /// On mobile, opens the native document scanner. On web, opens the
  /// [DocumentScanDialog] with a camera preview. Captured pages are
  /// converted to a single PDF and uploaded to the given [unit].
  Future<void> _scanAndUploadFile(
    EmployeeDocument doc,
    DocumentUnit unit,
  ) async {
    List<Uint8List>? pages;

    if (kIsWeb) {
      pages = await showDialog<List<Uint8List>>(
        context: context,
        barrierDismissible: false,
        builder: (_) => const DocumentScanDialog(),
      );
    } else {
      final result = await widget.viewModel.scanPages();
      if (!mounted) return;
      switch (result) {
        case Success(value: final scanned):
          pages = scanned;
        case Failure(:final error):
          if (error is DocumentScannerException) {
            await presentScannerError(context, error);
          }
          return;
      }
    }

    if (!mounted || pages == null || pages.isEmpty) return;

    await _showProcessingDialog('Convertendo imagem para PDF...');
    Uint8List fileBytes;
    try {
      fileBytes = await convertImagesToPdf(pages);
    } finally {
      _dismissProcessingDialog();
    }
    if (!mounted) return;

    final fileName =
        'digitalizado_${DateTime.now().millisecondsSinceEpoch}.pdf';

    if (fileBytes.lengthInBytes > 10 * 1024 * 1024) {
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

    await _showUploadProgressDialog();
    try {
      await widget.viewModel.uploadDocumentUnit(
        doc.id,
        unit.id,
        fileBytes,
        fileName,
      );
    } finally {
      _dismissProcessingDialog();
    }
  }

  /// Shows a dialog listing the selected files and asking the user to
  /// confirm that they should be combined into a single PDF.
  Future<bool> _showMergeConfirmationDialog(
    List<PlatformFile> files,
  ) async {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) {
        return AlertDialog(
          title: Row(
            children: [
              Icon(Icons.merge_type, color: colorScheme.primary),
              const SizedBox(width: AppSpacing.sm),
              const Text('Combinar arquivos'),
            ],
          ),
          content: SizedBox(
            width: double.maxFinite,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Card.filled(
                  color: colorScheme.primaryContainer,
                  child: Padding(
                    padding: const EdgeInsets.all(AppSpacing.md),
                    child: Row(
                      children: [
                        Icon(
                          Icons.info_outline,
                          color: colorScheme.onPrimaryContainer,
                          size: 20,
                        ),
                        const SizedBox(width: AppSpacing.sm),
                        Expanded(
                          child: Text(
                            'Os arquivos selecionados serão convertidos '
                            '(se necessário) e combinados em um único '
                            'PDF na ordem abaixo.',
                            style: textTheme.bodySmall?.copyWith(
                              color: colorScheme.onPrimaryContainer,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: AppSpacing.md),
                ConstrainedBox(
                  constraints: const BoxConstraints(maxHeight: 240),
                  child: ListView.separated(
                    shrinkWrap: true,
                    itemCount: files.length,
                    separatorBuilder: (_, __) => const Divider(height: 1),
                    itemBuilder: (_, index) {
                      final file = files[index];
                      return ListTile(
                        leading: CircleAvatar(
                          radius: 14,
                          backgroundColor: colorScheme.primaryContainer,
                          child: Text(
                            '${index + 1}',
                            style: textTheme.labelSmall?.copyWith(
                              color: colorScheme.onPrimaryContainer,
                            ),
                          ),
                        ),
                        title: Text(
                          file.name,
                          style: textTheme.bodyMedium,
                          overflow: TextOverflow.ellipsis,
                        ),
                        trailing: Text(
                          _formatFileSize(file.size),
                          style: textTheme.bodySmall?.copyWith(
                            color: colorScheme.onSurfaceVariant,
                          ),
                        ),
                        dense: true,
                        contentPadding: EdgeInsets.zero,
                      );
                    },
                  ),
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(ctx).pop(false),
              child: const Text('Cancelar'),
            ),
            FilledButton.icon(
              onPressed: () => Navigator.of(ctx).pop(true),
              icon: const Icon(Icons.merge_type, size: 18),
              label: const Text('Combinar e enviar'),
            ),
          ],
        );
      },
    );

    return confirmed ?? false;
  }

  /// Formats a file size in bytes to a human-readable string.
  String _formatFileSize(int bytes) {
    if (bytes < 1024) return '$bytes B';
    if (bytes < 1024 * 1024) {
      return '${(bytes / 1024).toStringAsFixed(1)} KB';
    }
    return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
  }

  /// Shows a non-dismissible dialog with a spinner and [message] while
  /// a long-running file conversion or merge is in progress.
  ///
  /// Waits for the dialog to be fully rendered before returning, so that
  /// callers can start heavy work immediately after `await` without the
  /// dialog being invisible.
  Future<void> _showProcessingDialog(String message) async {
    // We don't await the dialog's own future (it resolves when the dialog
    // is popped). Instead we show it and yield a frame so it renders.
    unawaited(showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (_) => PopScope(
        canPop: false,
        child: AlertDialog(
          content: Row(
            children: [
              const CircularProgressIndicator(),
              const SizedBox(width: AppSpacing.lg),
              Expanded(child: Text(message)),
            ],
          ),
        ),
      ),
    ));
    // Let the framework pump a frame so the dialog is visible.
    await Future<void>.delayed(const Duration(milliseconds: 50));
  }

  /// Dismisses the processing dialog opened by [_showProcessingDialog].
  void _dismissProcessingDialog() {
    if (mounted) Navigator.of(context, rootNavigator: true).pop();
  }

  /// Shows a non-dismissible dialog with a determinate progress indicator
  /// driven by [EmployeeProfileViewModel.uploadProgress].
  ///
  /// The dialog automatically updates as the ViewModel notifies listeners
  /// during the upload. Callers must pop this dialog manually via
  /// [_dismissProcessingDialog] when the upload completes.
  Future<void> _showUploadProgressDialog() async {
    unawaited(showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (_) => PopScope(
        canPop: false,
        child: _UploadProgressDialog(viewModel: widget.viewModel),
      ),
    ));
    await Future<void>.delayed(const Duration(milliseconds: 50));
  }

  /// Shows a dialog to schedule the signature send for a future date.
  ///
  /// The send date is prefilled with [EmployeeDocument.suggestedSignatureScheduleDate]
  /// — when the current coverage expires — so the common case (renew exactly on
  /// the expiry day) is one confirmation away.
  Future<void> _showScheduleSignDialog(
    EmployeeDocument doc,
    DocumentUnit unit,
  ) async {
    final schedule = await showDialog<_ScheduleSignResult>(
      context: context,
      builder: (_) => _ScheduleSignDialog(
        suggestedSendOn: doc.suggestedSignatureScheduleDate,
      ),
    );

    if (schedule == null || !mounted) return;

    setState(() => _isBusy = true);
    try {
      await widget.viewModel.scheduleSendToSign(
        doc.id,
        unit.id,
        schedule.sendOn,
        schedule.dateLimitToSign,
        0,
      );
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  /// Asks the user to confirm cancelling a scheduled signature send.
  Future<void> _confirmCancelScheduledSend(
    EmployeeDocument doc,
    DocumentUnit unit,
  ) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Cancelar Agendamento'),
        content: Text(
          'O envio agendado para ${unit.scheduledSignatureSendOn} não será '
          'realizado. O documento continua pendente.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: const Text('Voltar'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(ctx).pop(true),
            child: const Text('Cancelar agendamento'),
          ),
        ],
      ),
    );

    if (confirmed != true || !mounted) return;

    setState(() => _isBusy = true);
    try {
      await widget.viewModel.cancelScheduledSendToSign(doc.id, unit.id);
    } finally {
      if (mounted) setState(() => _isBusy = false);
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
    final dateMask = MaskTextInputFormatter(
      mask: '##/##/####',
      filter: {'#': RegExp(r'[0-9]')},
      type: MaskAutoCompletionType.lazy,
    );
    final formKey = GlobalKey<FormState>();

    /// Formats [date] as dd/MM/yyyy.
    String formatDate(DateTime date) {
      final d = date.day.toString().padLeft(2, '0');
      final m = date.month.toString().padLeft(2, '0');
      return '$d/$m/${date.year}';
    }

    /// Sets the date field to today + [days].
    void setDateFromDays(int days) {
      final target = DateTime.now().add(Duration(days: days));
      final formatted = formatDate(target);
      final rawDigits = formatted.replaceAll(RegExp(r'[^\d]'), '');
      dateMask.formatEditUpdate(
        TextEditingValue.empty,
        TextEditingValue(text: rawDigits),
      );
      dateCtrl.text = dateMask.getMaskedText();
    }

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
                  validator: widget.viewModel.validateDocumentUnitDate,
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
                          onPressed: () => setDateFromDays(days),
                          child: Text('+$days dias'),
                        ),
                      ),
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
      return;
    }

    final dateLimitToSign = dateCtrl.text.trim();
    const reminderDays = 0;

    await _showUploadProgressDialog();
    try {
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
    } finally {
      _dismissProcessingDialog();
    }
    dateCtrl.dispose();
  }

  /// Downloads and displays a document unit in a full-screen PDF viewer dialog.
  ///
  /// Shows a loading indicator while the document is being fetched from the
  /// server, then opens the viewer only after the download completes.
  Future<void> _viewUnit(
    EmployeeDocument doc,
    DocumentUnit unit,
  ) async {
    setState(() => _isBusy = true);

    showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (_) => const _PdfLoadingDialog(),
    );

    final bytes = await widget.viewModel.downloadDocumentUnit(doc.id, unit.id);

    if (!mounted) return;
    Navigator.of(context).pop();

    if (bytes != null && mounted) {
      await showDialog<void>(
        context: context,
        builder: (ctx) => _PdfViewerDialog(
          title: unit.downloadFileName(
            doc.name,
            employeeName: widget.viewModel.profile?.name ?? '',
          ),
          bytes: bytes,
        ),
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
                          padding: const EdgeInsets.symmetric(vertical: 2),
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
                          padding: const EdgeInsets.symmetric(vertical: 2),
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
                      style: TextStyle(color: Theme.of(ctx).colorScheme.error),
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
                child: Text('Confirmar ${isGenerate ? 'Geração' : 'Download'}'),
              ),
          ],
        );
      },
    );

    if (confirmed != true || !mounted) return;

    // Só a geração lê o snapshot; o download entrega o arquivo já existente.
    if (isGenerate) {
      final proceed = await _confirmSnapshotFreshness(canExecute);
      if (!proceed || !mounted) return;
    }

    setState(() => _isBusy = true);
    final bytes = isGenerate
        ? await widget.viewModel.generateDocumentRange()
        : await widget.viewModel.downloadDocumentRange();

    if (bytes != null && mounted) {
      await _saveFile(
        dialogTitle:
            isGenerate ? 'Salvar documentos gerados' : 'Salvar documentos',
        fileName:
            isGenerate ? 'documentos_gerados.zip' : 'documentos_download.zip',
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
        return SectionCard(
          title: 'Documentos',
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
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
            decoration: BoxDecoration(
              color:
                  Theme.of(context).colorScheme.primary.withValues(alpha: 0.1),
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
          onPressed: _isBusy ? null : () => vm.toggleRangeSelectionMode(),
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
    final badgeLabel = group.groupStatusLabel;
    final badgeColor = _groupStatusColor(group.statusId);
    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: Card.outlined(
        clipBehavior: Clip.antiAlias,
        child: ExpansionTile(
          controlAffinity: ListTileControlAffinity.leading,
          title: Row(
            children: [
              _StatusBadge(label: badgeLabel, color: badgeColor),
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
    // statusLabel, nunca statusName: o servidor manda o nome do smart enum em
    // inglês ("RequiresDocument"), e o statusName só serve de fallback dentro
    // do próprio statusLabel.
    final docStatusLabel = doc.statusLabel;
    final docStatusColor = _documentStatusColor(doc.statusId);
    final isExpanded = _expandedDocIds.contains(doc.id);
    final titleStyle = Theme.of(context).textTheme.titleSmall;
    final statusBadge =
        _StatusBadge(label: docStatusLabel, color: docStatusColor);
    const previousPeriodBadge = _StatusBadge(
      label: 'Usa período anterior',
      color: Colors.deepPurple,
    );

    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: Card.outlined(
        clipBehavior: Clip.antiAlias,
        child: LayoutBuilder(
          builder: (context, constraints) {
            final isWide = constraints.maxWidth >= AppBreakpoints.mobile;

            final title = isWide
                ? Text(doc.name, style: titleStyle)
                : Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(doc.name, style: titleStyle),
                      const SizedBox(height: AppSpacing.xs),
                      Wrap(
                        spacing: AppSpacing.xs,
                        runSpacing: AppSpacing.xs,
                        children: [
                          statusBadge,
                          if (doc.usePreviousPeriod) previousPeriodBadge,
                        ],
                      ),
                    ],
                  );

            final trailing = isWide
                ? Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      statusBadge,
                      if (doc.usePreviousPeriod) ...[
                        const SizedBox(width: AppSpacing.xs),
                        previousPeriodBadge,
                      ],
                      const SizedBox(width: AppSpacing.xs),
                      Icon(
                        isExpanded ? Icons.expand_less : Icons.expand_more,
                      ),
                    ],
                  )
                : Icon(
                    isExpanded ? Icons.expand_less : Icons.expand_more,
                  );

            return ExpansionTile(
              title: title,
              trailing: trailing,
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
            );
          },
        ),
      ),
    );
  }

  Widget _buildStatusFilter(BuildContext context, EmployeeDocument doc) {
    final currentFilter = _statusFilterMap[doc.id];

    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: DropdownButtonFormField<int?>(
        isExpanded: true,
        initialValue: currentFilter,
        decoration: const InputDecoration(
          labelText: 'Status',
          prefixIcon: Icon(Icons.filter_list),
          border: OutlineInputBorder(),
          isDense: true,
        ),
        items: _statusOptions.map((opt) {
          return DropdownMenuItem<int?>(
            value: opt.id,
            child: Text(opt.label, overflow: TextOverflow.ellipsis),
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
        child: Center(child: Text('Nenhuma unidade encontrada para o filtro.')),
      );
    }

    final currentPage = _pageMap[doc.id] ?? 1;
    final pageSize = widget.viewModel.getPageSize(doc.id);
    final totalPages =
        doc.totalUnitsCount == 0 ? 1 : (doc.totalUnitsCount / pageSize).ceil();

    // Não há botão de criar unidade avulsa: pendência nasce do evento de admissão, da renovação por
    // vencimento, ou de depreciar/invalidar a vigente — cada um desses já deixa a substituta no lugar.
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        ...units.map((unit) => _buildUnitRow(context, doc, unit)),
        const SizedBox(height: AppSpacing.sm),
        _buildPaginationBar(context, doc, currentPage, totalPages, pageSize),
      ],
    );
  }

  /// Builds the pagination bar with navigation, summary and page size
  /// dropdown — single row on wide screens, stacked on mobile.
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

    void changePage(int page) {
      setState(() => _pageMap[doc.id] = page);
      _reloadUnits(doc.id);
    }

    Widget buildPageNumberWidget(int? page) {
      if (page == null) {
        return Padding(
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.xs),
          child: Text('…', style: textStyle),
        );
      }
      final isActive = page == currentPage;
      return GestureDetector(
        onTap: () => changePage(page),
        child: Container(
          margin: const EdgeInsets.symmetric(horizontal: 2),
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
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
              fontWeight: isActive ? FontWeight.bold : FontWeight.normal,
            ),
          ),
        ),
      );
    }

    String summaryText() => hasMultiplePages
        ? 'Página $currentPage de $totalPages '
            '(${doc.totalUnitsCount} itens)'
        : '${doc.totalUnitsCount} '
            '${doc.totalUnitsCount == 1 ? 'item' : 'itens'}';

    Widget buildPageSizeDropdown({required bool showLabel}) {
      return Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (showLabel) ...[
            Text('Por página:', style: textStyle),
            const SizedBox(width: AppSpacing.xs),
          ],
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

    return LayoutBuilder(
      builder: (context, constraints) {
        final isWide = constraints.maxWidth >= AppBreakpoints.mobile;
        final pageNumbers = hasMultiplePages
            ? _buildPageNumbers(
                currentPage,
                totalPages,
                maxVisible: isWide ? 5 : 3,
              )
            : <int?>[];

        final navButtons = <Widget>[
          if (hasMultiplePages) ...[
            IconButton(
              icon: const Icon(Icons.chevron_left, size: 20),
              tooltip: 'Página anterior',
              visualDensity: VisualDensity.compact,
              onPressed:
                  currentPage > 1 ? () => changePage(currentPage - 1) : null,
            ),
            ...pageNumbers.map(buildPageNumberWidget),
            IconButton(
              icon: const Icon(Icons.chevron_right, size: 20),
              tooltip: 'Próxima página',
              visualDensity: VisualDensity.compact,
              onPressed: currentPage < totalPages
                  ? () => changePage(currentPage + 1)
                  : null,
            ),
          ],
        ];

        if (isWide) {
          return Row(
            children: [
              Expanded(
                child: Text(summaryText(), style: textStyle),
              ),
              ...navButtons,
              buildPageSizeDropdown(showLabel: true),
            ],
          );
        }

        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (hasMultiplePages)
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: navButtons,
              ),
            if (hasMultiplePages) const SizedBox(height: AppSpacing.xs),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Flexible(
                  child: Text(
                    summaryText(),
                    style: textStyle,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                buildPageSizeDropdown(showLabel: false),
              ],
            ),
          ],
        );
      },
    );
  }

  /// Returns page numbers with `null` representing ellipsis gaps.
  static List<int?> _buildPageNumbers(
    int currentPage,
    int totalPages, {
    int maxVisible = 5,
  }) {
    if (totalPages <= maxVisible) {
      return List.generate(totalPages, (i) => i + 1);
    }
    final pages = <int>{1, totalPages, currentPage};
    if (maxVisible >= 5) {
      for (var i = currentPage - 1; i <= currentPage + 1; i++) {
        if (i >= 1 && i <= totalPages) pages.add(i);
      }
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
    final hasValidDate = unit.date.isNotEmpty && unit.date != '01/01/0001';
    final canGenerate =
        unit.isPending && hasValidDate && doc.canGenerateDocument;
    final canSend = unit.isPending && hasValidDate;

    final actions = <Widget>[
      if (unit.isPending) ...[
        PermissionGuard(
          resource: 'document',
          scope: 'edit',
          child: IconButton(
            icon: const Icon(Icons.edit, size: 20),
            tooltip: 'Editar data',
            onPressed: _isBusy ? null : () => _showEditDateDialog(doc, unit),
          ),
        ),
        if (unit.canBeMarkedNotApplicable)
          PermissionGuard(
            resource: 'document',
            scope: 'mark-not-applicable',
            child: IconButton(
              key: const ValueKey('unit-not-applicable'),
              icon: const Icon(Icons.block, size: 20),
              tooltip: 'Não aplicável',
              onPressed:
                  _isBusy ? null : () => _showNotApplicableDialog(doc, unit),
            ),
          ),
        if (canGenerate)
          PermissionGuard(
            resource: 'document',
            scope: 'generate',
            child: IconButton(
              icon: const Icon(Icons.sim_card_download_outlined, size: 20),
              tooltip: 'Gerar',
              onPressed: _isBusy ? null : () => _showGenerateDialog(doc, unit),
            ),
          ),
        if (canSend)
          PermissionGuard(
            resource: 'document',
            scope: 'upload',
            child: IconButton(
              icon: const Icon(Icons.upload_file_outlined, size: 20),
              tooltip: 'Enviar',
              onPressed: _isBusy ? null : () => _showSendDialog(doc, unit),
            ),
          ),
        if (unit.isSignatureScheduled)
          PermissionGuard(
            resource: 'document',
            scope: 'send2sign',
            child: IconButton(
              key: const ValueKey('unit-cancel-scheduled-send'),
              icon: const Icon(Icons.event_busy_outlined, size: 20),
              tooltip: 'Cancelar agendamento',
              onPressed:
                  _isBusy ? null : () => _confirmCancelScheduledSend(doc, unit),
            ),
          ),
      ] else if (unit.hasFile)
        PermissionGuard(
          resource: 'document',
          scope: 'download',
          child: IconButton(
            icon: const Icon(Icons.search, size: 20),
            tooltip: 'Visualizar',
            onPressed: _isBusy ? null : () => _viewUnit(doc, unit),
          ),
        ),
      // Depreciar e invalidar valem para a unidade em vigência, então ficam fora do bloco de pendente.
      if (unit.canBeDeprecated)
        PermissionGuard(
          resource: 'document',
          scope: 'deprecate',
          child: IconButton(
            key: const ValueKey('unit-deprecate'),
            icon: const Icon(Icons.history_toggle_off, size: 20),
            tooltip: 'Depreciar',
            onPressed: _isBusy ? null : () => _showDeprecateDialog(doc, unit),
          ),
        ),
      if (unit.canBeInvalidated)
        PermissionGuard(
          resource: 'document',
          scope: 'reject',
          child: IconButton(
            key: const ValueKey('unit-invalidate'),
            icon: const Icon(Icons.report_gmailerrorred_outlined, size: 20),
            tooltip: unit.isNotApplicable ? 'Voltar a exigir' : 'Invalidar',
            onPressed: _isBusy ? null : () => _showInvalidateDialog(doc, unit),
          ),
        ),
    ];

    final infoColumn = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          unit.statusLabel,
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
        if (unit.period != null && unit.period!.formattedPeriod.isNotEmpty)
          Text(
            'Competência: ${unit.period!.formattedPeriod}',
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
        if (unit.isSignatureScheduled) ...[
          const SizedBox(height: AppSpacing.xs),
          _StatusBadge(
            key: const ValueKey('unit-scheduled-send-badge'),
            label: 'Envio agendado: ${unit.scheduledSignatureSendOn}',
            color: cs.tertiary,
          ),
        ],
      ],
    );

    final checkbox = widget.viewModel.isSelectingRange
        ? Checkbox(
            value: widget.viewModel.isDocumentUnitSelected(unit.id),
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
          )
        : null;

    final statusDot = Container(
      width: 12,
      height: 12,
      decoration: BoxDecoration(
        color: statusColor,
        shape: BoxShape.circle,
      ),
    );

    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.xs),
      child: Card.outlined(
        child: Padding(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.md,
            vertical: AppSpacing.sm,
          ),
          child: LayoutBuilder(
            builder: (context, constraints) {
              final isWide = constraints.maxWidth >= AppBreakpoints.mobile;

              if (isWide) {
                return Row(
                  children: [
                    if (checkbox != null) checkbox,
                    statusDot,
                    const SizedBox(width: AppSpacing.md),
                    Expanded(child: infoColumn),
                    ...actions,
                  ],
                );
              }

              return Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      if (checkbox != null) checkbox,
                      Padding(
                        padding: const EdgeInsets.only(top: 6),
                        child: statusDot,
                      ),
                      const SizedBox(width: AppSpacing.md),
                      Expanded(child: infoColumn),
                    ],
                  ),
                  if (actions.isNotEmpty) ...[
                    const SizedBox(height: AppSpacing.xs),
                    Wrap(
                      alignment: WrapAlignment.end,
                      spacing: AppSpacing.xs,
                      runSpacing: AppSpacing.xs,
                      children: actions,
                    ),
                  ],
                ],
              );
            },
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
              PermissionGuard(
                resource: 'document',
                scope: 'generate',
                child: OutlinedButton.icon(
                  onPressed: selected.isEmpty || _isBusy
                      ? null
                      : () => _showRangeConfirmationDialog(
                            isGenerate: true,
                          ),
                  icon: const Icon(Icons.sim_card_download_outlined, size: 18),
                  label: const Text('Gerar Selecionados'),
                ),
              ),
              const SizedBox(width: AppSpacing.sm),
              PermissionGuard(
                resource: 'document',
                scope: 'download',
                child: OutlinedButton.icon(
                  onPressed: selected.isEmpty || _isBusy
                      ? null
                      : () => _showRangeConfirmationDialog(
                            isGenerate: false,
                          ),
                  icon: const Icon(Icons.download, size: 18),
                  label: const Text('Baixar Selecionados'),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

/// Small coloured badge used to display a document or unit status.
/// The dates chosen in [_ScheduleSignDialog].
typedef _ScheduleSignResult = ({String sendOn, String dateLimitToSign});

/// Asks for the date the document should be sent for signature and the deadline
/// the employee has to sign it.
///
/// A StatefulWidget, and not controllers built inside the caller, so the fields
/// live and die with the dialog route — disposing them from the caller kills
/// them mid exit-animation, and the deadline validator reads the send-date
/// controller while the route is still tearing down.
class _ScheduleSignDialog extends StatefulWidget {
  const _ScheduleSignDialog({required this.suggestedSendOn});

  /// Date to prefill the send field with, or empty when there is no suggestion.
  final String suggestedSendOn;

  @override
  State<_ScheduleSignDialog> createState() => _ScheduleSignDialogState();
}

class _ScheduleSignDialogState extends State<_ScheduleSignDialog> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _sendOnCtrl;
  final _deadlineCtrl = TextEditingController();
  final _sendOnMask = _dateMask();
  final _deadlineMask = _dateMask();

  static MaskTextInputFormatter _dateMask() => MaskTextInputFormatter(
        mask: '##/##/####',
        filter: {'#': RegExp(r'[0-9]')},
        type: MaskAutoCompletionType.lazy,
      );

  @override
  void initState() {
    super.initState();
    _sendOnCtrl = TextEditingController(text: widget.suggestedSendOn);
  }

  @override
  void dispose() {
    _sendOnCtrl.dispose();
    _deadlineCtrl.dispose();
    super.dispose();
  }

  /// Writes the send date + [days] into the deadline field.
  ///
  /// Counted from the send date, not from today, because that is how the
  /// deadline itself is counted.
  void _setDeadlineFromSendDate(int days) {
    final base = DocumentUnit.parseDate(_sendOnCtrl.text) ?? DateTime.now();
    final target = base.add(Duration(days: days));
    final d = target.day.toString().padLeft(2, '0');
    final m = target.month.toString().padLeft(2, '0');
    _deadlineMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: '$d$m${target.year}'),
    );
    _deadlineCtrl.text = _deadlineMask.getMaskedText();
  }

  void _submit() {
    if (_formKey.currentState?.validate() != true) return;
    Navigator.of(context).pop((
      sendOn: _sendOnCtrl.text.trim(),
      dateLimitToSign: _deadlineCtrl.text.trim(),
    ));
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Agendar Envio para Assinatura'),
      content: Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              'O documento será gerado e enviado ao funcionário somente na '
              'data escolhida.',
              style: Theme.of(context).textTheme.bodySmall,
            ),
            const SizedBox(height: AppSpacing.md),
            TextFormField(
              key: const ValueKey('schedule-send-on-field'),
              controller: _sendOnCtrl,
              decoration: InputDecoration(
                labelText: 'Data do envio',
                prefixIcon: const Icon(Icons.schedule_send_outlined),
                border: const OutlineInputBorder(),
                helperText: widget.suggestedSendOn.isNotEmpty
                    ? 'Sugestão: vencimento do documento atual'
                    : 'Ex: 15/03/2026',
              ),
              keyboardType: TextInputType.number,
              inputFormatters: [_sendOnMask],
              validator: DocumentUnit.validateScheduleSendDate,
            ),
            const SizedBox(height: AppSpacing.md),
            TextFormField(
              key: const ValueKey('schedule-deadline-field'),
              controller: _deadlineCtrl,
              decoration: const InputDecoration(
                labelText: 'Data limite para assinatura',
                prefixIcon: Icon(Icons.event_outlined),
                border: OutlineInputBorder(),
                helperText: 'Contada a partir da data do envio',
              ),
              keyboardType: TextInputType.number,
              inputFormatters: [_deadlineMask],
              validator: (value) =>
                  DocumentUnit.validateSignDeadline(value, _sendOnCtrl.text),
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
                      onPressed: () => _setDeadlineFromSendDate(days),
                      child: Text('+$days dias'),
                    ),
                  ),
                );
              }).toList(),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Cancelar'),
        ),
        FilledButton(
          onPressed: _submit,
          child: const Text('Agendar'),
        ),
      ],
    );
  }
}

class _StatusBadge extends StatelessWidget {
  const _StatusBadge({required this.label, required this.color, super.key});

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

/// Non-dismissible dialog that shows a determinate upload progress bar.
///
/// Listens to [EmployeeProfileViewModel.uploadProgress] to update the
/// [LinearProgressIndicator] and percentage label in real time.
class _UploadProgressDialog extends StatelessWidget {
  const _UploadProgressDialog({required this.viewModel});

  /// The view model that exposes the current [uploadProgress].
  final EmployeeProfileViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return ListenableBuilder(
      listenable: viewModel,
      builder: (context, _) {
        final progress = viewModel.uploadProgress;
        final percent = (progress * 100).toInt();

        return AlertDialog(
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(
                Icons.cloud_upload_outlined,
                size: 40,
                color: colorScheme.primary,
              ),
              const SizedBox(height: AppSpacing.md),
              Text(
                'Enviando arquivo...',
                style: textTheme.titleSmall,
              ),
              const SizedBox(height: AppSpacing.lg),
              LinearProgressIndicator(
                value: progress > 0 ? progress : null,
                borderRadius: BorderRadius.circular(4),
              ),
              const SizedBox(height: AppSpacing.sm),
              Text(
                progress > 0 ? '$percent%' : 'Preparando...',
                style: textTheme.bodySmall?.copyWith(
                  color: colorScheme.onSurfaceVariant,
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

/// Centered loading indicator shown while a PDF is being downloaded.
class _PdfLoadingDialog extends StatelessWidget {
  const _PdfLoadingDialog();

  @override
  Widget build(BuildContext context) {
    return const Center(
      child: Card(
        child: Padding(
          padding: EdgeInsets.all(AppSpacing.xl),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              CircularProgressIndicator(),
              SizedBox(height: AppSpacing.md),
              Text('Carregando documento...'),
            ],
          ),
        ),
      ),
    );
  }
}

/// Full-screen dialog that displays a PDF using [SfPdfViewer].
class _PdfViewerDialog extends StatelessWidget {
  const _PdfViewerDialog({required this.title, required this.bytes});

  /// The document title shown in the app bar.
  final String title;

  /// The raw PDF bytes to render.
  final Uint8List bytes;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    return Dialog.fullscreen(
      child: Scaffold(
        appBar: AppBar(
          title: Text(title),
          leading: IconButton(
            icon: const Icon(Icons.close),
            tooltip: 'Fechar',
            onPressed: () => Navigator.of(context).pop(),
          ),
          actions: [
            IconButton(
              icon: const Icon(Icons.download),
              tooltip: 'Baixar',
              onPressed: () => saveFile(fileName: title, bytes: bytes),
            ),
          ],
        ),
        body: Container(
          decoration: BoxDecoration(
            color: Colors.white,
            border: Border.all(color: cs.outlineVariant),
            borderRadius: BorderRadius.circular(AppSpacing.sm),
          ),
          clipBehavior: Clip.antiAlias,
          child: SfPdfViewer.memory(
            bytes,
            canShowScrollHead: false,
            canShowPaginationDialog: false,
            pageSpacing: 4,
          ),
        ),
      ),
    );
  }
}
