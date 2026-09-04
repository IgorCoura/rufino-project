/// Dialogs that cut a single scanned page stack into equally sized
/// documents.
///
/// Both dialogs deal in page counts only — the bytes never reach them. The
/// caller owns the pages, asks [showSplitScanDialog] how to cut them, does
/// the cutting with `splitIntoEqualParts`, and confirms the outcome with
/// [showSplitResultDialog].
library;

import 'package:rufino_core/rufino_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../../utils/page_splitter.dart';

/// Asks how many pages each document should have when splitting a stack of
/// [totalPages] scanned pages.
///
/// Returns the chosen page count, or `null` when the user cancels — which
/// leaves the scanning session untouched.
Future<int?> showSplitScanDialog(
  BuildContext context, {
  required int totalPages,
}) {
  return showDialog<int>(
    context: context,
    barrierDismissible: false,
    builder: (_) => _SplitScanDialog(totalPages: totalPages),
  );
}

/// Confirms the outcome of a split of [documentCount] documents of
/// [pagesPerDocument] pages each.
///
/// Returns `true` when the user chooses to process the documents, and
/// `false` when they discard the whole scan.
Future<bool> showSplitResultDialog(
  BuildContext context, {
  required int documentCount,
  required int pagesPerDocument,
}) async {
  final result = await showDialog<bool>(
    context: context,
    barrierDismissible: false,
    builder: (ctx) {
      final colorScheme = Theme.of(ctx).colorScheme;
      final textTheme = Theme.of(ctx).textTheme;
      final totalPages = documentCount * pagesPerDocument;
      return AlertDialog(
        icon: Icon(Icons.content_cut, color: colorScheme.primary),
        title: Text(_documentsLabel(documentCount)),
        content: Text(
          'A digitalização de ${_pagesLabel(totalPages)} foi dividida em '
          '${_documentsLabel(documentCount)} de '
          '${_pagesLabel(pagesPerDocument)}.',
          style: textTheme.bodyMedium,
        ),
        actions: [
          TextButton(
            key: const Key('split-result-discard'),
            onPressed: () => Navigator.of(ctx).pop(false),
            child: Text(
              'Descartar',
              style: TextStyle(color: colorScheme.error),
            ),
          ),
          FilledButton.icon(
            key: const Key('split-result-process'),
            onPressed: () => Navigator.of(ctx).pop(true),
            icon: const Icon(Icons.check, size: 18),
            label: const Text('Processar'),
          ),
        ],
      );
    },
  );
  return result ?? false;
}

class _SplitScanDialog extends StatefulWidget {
  const _SplitScanDialog({required this.totalPages});

  final int totalPages;

  @override
  State<_SplitScanDialog> createState() => _SplitScanDialogState();
}

class _SplitScanDialogState extends State<_SplitScanDialog> {
  final _formKey = GlobalKey<FormState>();
  final _controller = TextEditingController();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  /// The split the current input describes, or `null` while it is invalid.
  ///
  /// Drives the live preview under the field, so the user reads the outcome
  /// before committing to it.
  int? get _pagesPerDocument {
    final error = validatePagesPerDocument(
      _controller.text,
      totalPages: widget.totalPages,
    );
    if (error != null) return null;
    return int.parse(_controller.text.trim());
  }

  void _submit() {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    Navigator.of(context).pop(_pagesPerDocument);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final pagesPerDocument = _pagesPerDocument;

    return AlertDialog(
      icon: Icon(Icons.content_cut, color: theme.colorScheme.primary),
      title: const Text('Dividir digitalização'),
      content: Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              '${_pagesLabel(widget.totalPages)} '
              '${widget.totalPages == 1 ? 'capturada' : 'capturadas'}.',
              style: theme.textTheme.bodyMedium,
            ),
            const SizedBox(height: AppSpacing.md),
            TextFormField(
              key: const Key('split-pages-field'),
              controller: _controller,
              autofocus: true,
              keyboardType: TextInputType.number,
              inputFormatters: [FilteringTextInputFormatter.digitsOnly],
              decoration: const InputDecoration(
                labelText: 'Páginas por documento',
                border: OutlineInputBorder(),
              ),
              validator: (value) => validatePagesPerDocument(
                value,
                totalPages: widget.totalPages,
              ),
              autovalidateMode: AutovalidateMode.onUserInteraction,
              onChanged: (_) => setState(() {}),
              onFieldSubmitted: (_) => _submit(),
            ),
            if (pagesPerDocument != null) ...[
              const SizedBox(height: AppSpacing.sm),
              Text(
                '${_documentsLabel(widget.totalPages ~/ pagesPerDocument)} '
                'de ${_pagesLabel(pagesPerDocument)}',
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: theme.colorScheme.primary,
                ),
              ),
            ],
          ],
        ),
      ),
      actions: [
        TextButton(
          key: const Key('split-scan-cancel'),
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Cancelar'),
        ),
        FilledButton.icon(
          key: const Key('split-scan-confirm'),
          onPressed: _submit,
          icon: const Icon(Icons.content_cut, size: 18),
          label: const Text('Dividir'),
        ),
      ],
    );
  }
}

String _pagesLabel(int count) => '$count ${count == 1 ? 'página' : 'páginas'}';

String _documentsLabel(int count) =>
    '$count ${count == 1 ? 'documento' : 'documentos'}';
