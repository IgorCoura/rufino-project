/// Confirmation dialog that previews the dates of a batch of document units
/// before the user dispatches them to the backend.
///
/// Shows a table with Funcionário | Competência | Data | Data do Documento
/// | Status. The "Data do Documento" column is rendered only when bytes are
/// supplied via [attachments]; for each row, the date is extracted
/// asynchronously from the attached file and compared against the system
/// date — divergences are highlighted but never block the send.
library;

import 'dart:typed_data';

import 'package:flutter/material.dart';

import '../../../../core/theme/app_breakpoints.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/utils/document_date_extractor.dart';
import '../../../../domain/entities/batch_document_unit.dart';

/// Raw bytes of a file attached to a document unit, paired with its name.
class AttachedDocumentBytes {
  /// Creates an [AttachedDocumentBytes].
  const AttachedDocumentBytes({required this.bytes, required this.fileName});

  /// The file content.
  final Uint8List bytes;

  /// The original file name (including extension — used to choose between
  /// PDF parsing and OCR).
  final String fileName;
}

/// Signature for the date extraction function injected into the dialog.
typedef DocumentDateExtractor = Future<String?> Function({
  required Uint8List bytes,
  required String fileName,
});

/// Shows the confirmation dialog and returns `true` when the user confirms.
///
/// [items] are the document units that will be sent. [attachments] is an
/// optional map keyed by `documentUnitId`; when provided, the dialog shows
/// the "Data do Documento" column and runs [extractor] for each row. When
/// omitted, the column is hidden entirely.
///
/// [extractor] defaults to [extractLastDocumentDate]; tests pass a fake.
Future<bool> showConfirmDocumentDatesDialog(
  BuildContext context, {
  required String title,
  required String confirmLabel,
  required IconData icon,
  required List<BatchDocumentUnitItem> items,
  Map<String, AttachedDocumentBytes>? attachments,
  @visibleForTesting DocumentDateExtractor? extractor,
}) async {
  final result = await showDialog<bool>(
    context: context,
    builder: (_) => _ConfirmDocumentDatesDialog(
      title: title,
      confirmLabel: confirmLabel,
      icon: icon,
      items: items,
      attachments: attachments,
      extractor: extractor ?? extractLastDocumentDate,
    ),
  );
  return result ?? false;
}

class _ConfirmDocumentDatesDialog extends StatefulWidget {
  const _ConfirmDocumentDatesDialog({
    required this.title,
    required this.confirmLabel,
    required this.icon,
    required this.items,
    required this.attachments,
    required this.extractor,
  });

  final String title;
  final String confirmLabel;
  final IconData icon;
  final List<BatchDocumentUnitItem> items;
  final Map<String, AttachedDocumentBytes>? attachments;
  final DocumentDateExtractor extractor;

  @override
  State<_ConfirmDocumentDatesDialog> createState() =>
      _ConfirmDocumentDatesDialogState();
}

class _ConfirmDocumentDatesDialogState
    extends State<_ConfirmDocumentDatesDialog> {
  /// Extracted date per documentUnitId, or `null` when no date was found.
  final Map<String, String?> _extractedDates = {};

  /// Whether extraction completed for a given documentUnitId.
  final Set<String> _extractionDone = {};

  bool get _showDocumentDateColumn => widget.attachments != null;

  @override
  void initState() {
    super.initState();
    if (widget.attachments == null) return;
    for (final item in widget.items) {
      final att = widget.attachments![item.documentUnitId];
      if (att == null) {
        _extractionDone.add(item.documentUnitId);
        continue;
      }
      widget
          .extractor(bytes: att.bytes, fileName: att.fileName)
          .then((date) {
        if (!mounted) return;
        setState(() {
          _extractedDates[item.documentUnitId] = date;
          _extractionDone.add(item.documentUnitId);
        });
      });
    }
  }

  int get _invalidCount =>
      widget.items.where((i) => !i.hasValidDate).length;

  int get _divergentCount {
    if (!_showDocumentDateColumn) return 0;
    return widget.items.where((item) {
      if (!_extractionDone.contains(item.documentUnitId)) return false;
      final extracted = _extractedDates[item.documentUnitId];
      if (extracted == null) return false;
      if (!item.hasValidDate) return false;
      return extracted != item.date;
    }).length;
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final width = MediaQuery.sizeOf(context).width;
    final isWide = width >= AppBreakpoints.mobile;
    final maxHeight = MediaQuery.sizeOf(context).height * 0.6;

    return AlertDialog(
      icon: Icon(widget.icon),
      title: Text(widget.title),
      content: ConstrainedBox(
        constraints: BoxConstraints(maxHeight: maxHeight, maxWidth: 720),
        child: SingleChildScrollView(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                'Revise os ${widget.items.length} documento(s) que serão '
                'enviados:',
                style: theme.textTheme.bodyMedium,
              ),
              const SizedBox(height: AppSpacing.md),
              if (isWide)
                _DocumentsTable(
                  items: widget.items,
                  showDocumentDateColumn: _showDocumentDateColumn,
                  extractedDates: _extractedDates,
                  extractionDone: _extractionDone,
                )
              else
                _DocumentsList(
                  items: widget.items,
                  showDocumentDate: _showDocumentDateColumn,
                  extractedDates: _extractedDates,
                  extractionDone: _extractionDone,
                ),
              if (_invalidCount > 0) ...[
                const SizedBox(height: AppSpacing.md),
                _WarningLine(
                  text: '$_invalidCount documento(s) com data inválida — '
                      'envio prossegue.',
                ),
              ],
              if (_divergentCount > 0) ...[
                const SizedBox(height: AppSpacing.xs),
                _WarningLine(
                  text: '$_divergentCount documento(s) com data divergente '
                      'da data do documento.',
                ),
              ],
            ],
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(false),
          child: const Text('Cancelar'),
        ),
        FilledButton.icon(
          onPressed: () => Navigator.of(context).pop(true),
          icon: Icon(widget.icon, size: 18),
          label: Text(widget.confirmLabel),
        ),
      ],
    );
  }
}

/// Wide-layout table.
class _DocumentsTable extends StatelessWidget {
  const _DocumentsTable({
    required this.items,
    required this.showDocumentDateColumn,
    required this.extractedDates,
    required this.extractionDone,
  });

  final List<BatchDocumentUnitItem> items;
  final bool showDocumentDateColumn;
  final Map<String, String?> extractedDates;
  final Set<String> extractionDone;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    final headerStyle = textTheme.labelLarge?.copyWith(
      fontWeight: FontWeight.w600,
    );
    Widget headerCell(String label) => Padding(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.sm,
            vertical: AppSpacing.sm,
          ),
          child: Text(label, style: headerStyle),
        );

    return DecoratedBox(
      decoration: BoxDecoration(
        border: Border.all(color: colors.outlineVariant),
        borderRadius: BorderRadius.circular(8),
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(8),
        child: Table(
          columnWidths: showDocumentDateColumn
              ? const {
                  0: FlexColumnWidth(3),
                  1: FlexColumnWidth(2),
                  2: FlexColumnWidth(2),
                  3: FlexColumnWidth(2),
                  4: IntrinsicColumnWidth(),
                }
              : const {
                  0: FlexColumnWidth(3),
                  1: FlexColumnWidth(2),
                  2: FlexColumnWidth(2),
                  3: IntrinsicColumnWidth(),
                },
          defaultVerticalAlignment: TableCellVerticalAlignment.middle,
          children: [
            TableRow(
              decoration: BoxDecoration(
                color: colors.surfaceContainerHigh,
              ),
              children: [
                headerCell('Funcionário'),
                headerCell('Competência'),
                headerCell('Data'),
                if (showDocumentDateColumn) headerCell('Data do Documento'),
                headerCell('Status'),
              ],
            ),
            for (final item in items)
              _buildRow(context, item, colors, textTheme),
          ],
        ),
      ),
    );
  }

  TableRow _buildRow(
    BuildContext context,
    BatchDocumentUnitItem item,
    ColorScheme colors,
    TextTheme textTheme,
  ) {
    final invalidDate = !item.hasValidDate;
    final extracted = extractedDates[item.documentUnitId];
    final done = extractionDone.contains(item.documentUnitId);
    final divergent = done &&
        extracted != null &&
        item.hasValidDate &&
        extracted != item.date;
    final rowColor = invalidDate ? colors.errorContainer : null;

    Widget cell(Widget child) => Padding(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.sm,
            vertical: AppSpacing.sm,
          ),
          child: child,
        );

    return TableRow(
      decoration: rowColor == null
          ? null
          : BoxDecoration(color: rowColor.withValues(alpha: 0.6)),
      children: [
        cell(Text(item.employeeName, style: textTheme.bodyMedium)),
        cell(Text(
          item.period?.formattedPeriod ?? '—',
          style: textTheme.bodyMedium,
        )),
        cell(Text(
          item.date.isEmpty ? '—' : item.date,
          style: textTheme.bodyMedium?.copyWith(
            color: invalidDate ? colors.onErrorContainer : null,
            fontWeight: invalidDate ? FontWeight.w600 : null,
          ),
        )),
        if (showDocumentDateColumn)
          cell(_DocumentDateCell(
            extracted: extracted,
            done: done,
            divergent: divergent,
          )),
        cell(_MiniStatusBadge(
          statusLabel: item.statusLabel,
          statusId: item.statusId,
        )),
      ],
    );
  }
}

/// Narrow-layout list (one card per document).
class _DocumentsList extends StatelessWidget {
  const _DocumentsList({
    required this.items,
    required this.showDocumentDate,
    required this.extractedDates,
    required this.extractionDone,
  });

  final List<BatchDocumentUnitItem> items;
  final bool showDocumentDate;
  final Map<String, String?> extractedDates;
  final Set<String> extractionDone;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return Column(
      children: [
        for (final item in items) ...[
          _buildCard(context, item, colors, textTheme),
          const SizedBox(height: AppSpacing.sm),
        ],
      ],
    );
  }

  Widget _buildCard(
    BuildContext context,
    BatchDocumentUnitItem item,
    ColorScheme colors,
    TextTheme textTheme,
  ) {
    final invalidDate = !item.hasValidDate;
    final extracted = extractedDates[item.documentUnitId];
    final done = extractionDone.contains(item.documentUnitId);
    final divergent = done &&
        extracted != null &&
        item.hasValidDate &&
        extracted != item.date;

    return Container(
      padding: const EdgeInsets.all(AppSpacing.sm),
      decoration: BoxDecoration(
        border: Border.all(
          color: invalidDate ? colors.error : colors.outlineVariant,
        ),
        color: invalidDate ? colors.errorContainer.withValues(alpha: 0.4) : null,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  item.employeeName,
                  style: textTheme.titleSmall,
                ),
              ),
              _MiniStatusBadge(
                statusLabel: item.statusLabel,
                statusId: item.statusId,
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.xs),
          Wrap(
            spacing: AppSpacing.md,
            runSpacing: AppSpacing.xs,
            children: [
              Text(
                'Competência: ${item.period?.formattedPeriod ?? '—'}',
                style: textTheme.bodySmall,
              ),
              Text(
                'Data: ${item.date.isEmpty ? '—' : item.date}',
                style: textTheme.bodySmall?.copyWith(
                  color: invalidDate ? colors.error : null,
                  fontWeight: invalidDate ? FontWeight.w600 : null,
                ),
              ),
            ],
          ),
          if (showDocumentDate) ...[
            const SizedBox(height: AppSpacing.xs),
            Row(
              children: [
                Text(
                  'Doc.: ',
                  style: textTheme.bodySmall,
                ),
                _DocumentDateCell(
                  extracted: extracted,
                  done: done,
                  divergent: divergent,
                ),
              ],
            ),
          ],
        ],
      ),
    );
  }
}

class _DocumentDateCell extends StatelessWidget {
  const _DocumentDateCell({
    required this.extracted,
    required this.done,
    required this.divergent,
  });

  final String? extracted;
  final bool done;
  final bool divergent;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    if (!done) {
      return const SizedBox(
        height: 14,
        width: 14,
        child: CircularProgressIndicator(strokeWidth: 2),
      );
    }
    if (extracted == null) {
      return Text('—', style: textTheme.bodyMedium);
    }

    final text = Text(
      extracted!,
      style: textTheme.bodyMedium?.copyWith(
        color: divergent ? colors.error : null,
        fontWeight: divergent ? FontWeight.w600 : null,
      ),
    );
    if (!divergent) return text;
    return Tooltip(
      message: 'Data divergente do documento',
      child: Wrap(
        spacing: AppSpacing.xs,
        crossAxisAlignment: WrapCrossAlignment.center,
        children: [
          text,
          Icon(Icons.priority_high, size: 16, color: colors.error),
        ],
      ),
    );
  }
}

class _MiniStatusBadge extends StatelessWidget {
  const _MiniStatusBadge({required this.statusLabel, required this.statusId});

  final String statusLabel;
  final String statusId;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    final palette = _colorsFor(statusId, colors);
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: 2,
      ),
      decoration: BoxDecoration(
        color: palette.$1,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        statusLabel,
        style: Theme.of(context).textTheme.labelSmall?.copyWith(
              color: palette.$2,
              fontWeight: FontWeight.w600,
            ),
      ),
    );
  }

  (Color background, Color foreground) _colorsFor(
    String statusId,
    ColorScheme colors,
  ) {
    switch (statusId) {
      case '1': // Pendente
        return (colors.secondaryContainer, colors.onSecondaryContainer);
      case '2': // OK
        return (colors.primaryContainer, colors.onPrimaryContainer);
      case '4': // Inválido
        return (colors.errorContainer, colors.onErrorContainer);
      case '7': // Aguardando Assinatura
        return (colors.tertiaryContainer, colors.onTertiaryContainer);
      default:
        return (colors.surfaceContainerHighest, colors.onSurfaceVariant);
    }
  }
}

class _WarningLine extends StatelessWidget {
  const _WarningLine({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(Icons.warning_amber_rounded, size: 16, color: colors.error),
        const SizedBox(width: AppSpacing.xs),
        Expanded(
          child: Text(
            text,
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: colors.error,
                ),
          ),
        ),
      ],
    );
  }
}
