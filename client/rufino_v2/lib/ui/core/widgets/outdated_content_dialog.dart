/// Warning dialog shown when a document about to be generated was built from
/// employee data that has changed since the snapshot was taken.
///
/// The dialog lists every document in the operation and flags the outdated
/// ones individually, so the user sees exactly which ones are affected. It
/// never blocks: generating with the old data is always an option.
library;

import 'package:flutter/material.dart';

import '../../../core/theme/app_breakpoints.dart';
import '../../../core/theme/app_spacing.dart';
import 'permission_guard.dart';

/// What the user chose in [showOutdatedContentDialog].
enum OutdatedContentAction {
  /// Abort the operation.
  cancel,

  /// Proceed using the snapshot as stored.
  continueAnyway,

  /// Rewrite the snapshot with the current data, then proceed.
  refreshAndContinue,
}

/// One document row rendered by [showOutdatedContentDialog].
class OutdatedDocumentRow {
  /// Creates an [OutdatedDocumentRow].
  const OutdatedDocumentRow({
    required this.title,
    required this.subtitle,
    required this.isOutdated,
  });

  /// Primary label — the employee name in batch, the document name in the
  /// employee profile.
  final String title;

  /// Secondary label (document name, date, or competência).
  final String subtitle;

  /// Whether this document's snapshot diverges from the current data.
  final bool isOutdated;
}

/// Shows the outdated-snapshot warning and returns the user's choice.
///
/// [rows] should carry every document in the operation, not only the outdated
/// ones — seeing which ones are fine is what makes the flagged ones readable.
///
/// When [allowRefresh] is false the dialog offers only cancel and continue;
/// the batch screen uses that, because refreshing there is done per document
/// by editing it.
Future<OutdatedContentAction> showOutdatedContentDialog(
  BuildContext context, {
  required List<OutdatedDocumentRow> rows,
  bool allowRefresh = true,
}) async {
  final result = await showDialog<OutdatedContentAction>(
    context: context,
    builder: (_) => _OutdatedContentDialog(
      rows: rows,
      allowRefresh: allowRefresh,
    ),
  );
  return result ?? OutdatedContentAction.cancel;
}

class _OutdatedContentDialog extends StatelessWidget {
  const _OutdatedContentDialog({
    required this.rows,
    required this.allowRefresh,
  });

  final List<OutdatedDocumentRow> rows;
  final bool allowRefresh;

  int get _outdatedCount => rows.where((r) => r.isOutdated).length;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = theme.colorScheme;
    final isWide = MediaQuery.sizeOf(context).width >= AppBreakpoints.mobile;
    final maxHeight = MediaQuery.sizeOf(context).height * 0.6;

    return AlertDialog(
      icon: Icon(Icons.warning_amber_rounded, color: colors.error),
      title: const Text('Informações desatualizadas'),
      content: ConstrainedBox(
        constraints: BoxConstraints(maxHeight: maxHeight, maxWidth: 640),
        child: SingleChildScrollView(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                _outdatedCount == rows.length && rows.length == 1
                    ? 'Os dados do funcionário mudaram depois que este '
                        'documento foi preparado.'
                    : '$_outdatedCount de ${rows.length} documento(s) foram '
                        'preparados com dados que mudaram desde então.',
                style: theme.textTheme.bodyMedium,
              ),
              const SizedBox(height: AppSpacing.md),
              for (final row in rows) ...[
                _DocumentRowTile(row: row, isWide: isWide),
                const SizedBox(height: AppSpacing.xs),
              ],
              const SizedBox(height: AppSpacing.sm),
              Text(
                allowRefresh
                    ? 'Atualizar regrava as informações do documento com os '
                        'dados atuais. A data do documento não muda.'
                    : 'Para atualizar, edite o documento do funcionário '
                        'individualmente antes de gerar.',
                style: theme.textTheme.bodySmall?.copyWith(
                  color: colors.onSurfaceVariant,
                ),
              ),
            ],
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: () =>
              Navigator.of(context).pop(OutdatedContentAction.cancel),
          child: const Text('Cancelar'),
        ),
        OutlinedButton(
          onPressed: () =>
              Navigator.of(context).pop(OutdatedContentAction.continueAnyway),
          child: Text(allowRefresh ? 'Gerar com os dados atuais' : 'Gerar assim mesmo'),
        ),
        if (allowRefresh)
          PermissionGuard(
            resource: 'document',
            scope: 'edit',
            child: FilledButton.icon(
              onPressed: () => Navigator.of(context)
                  .pop(OutdatedContentAction.refreshAndContinue),
              icon: const Icon(Icons.sync, size: 18),
              label: const Text('Atualizar e gerar'),
            ),
          ),
      ],
    );
  }
}

class _DocumentRowTile extends StatelessWidget {
  const _DocumentRowTile({required this.row, required this.isWide});

  final OutdatedDocumentRow row;
  final bool isWide;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = theme.colorScheme;

    return Container(
      padding: const EdgeInsets.all(AppSpacing.sm),
      decoration: BoxDecoration(
        border: Border.all(
          color: row.isOutdated ? colors.error : colors.outlineVariant,
        ),
        color: row.isOutdated
            ? colors.errorContainer.withValues(alpha: 0.4)
            : null,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(row.title, style: theme.textTheme.titleSmall),
                if (row.subtitle.isNotEmpty)
                  Text(
                    row.subtitle,
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: colors.onSurfaceVariant,
                    ),
                  ),
              ],
            ),
          ),
          const SizedBox(width: AppSpacing.sm),
          if (row.isOutdated)
            _OutdatedBadge(compact: !isWide)
          else
            Icon(Icons.check_circle_outline, size: 18, color: colors.outline),
        ],
      ),
    );
  }
}

class _OutdatedBadge extends StatelessWidget {
  const _OutdatedBadge({required this.compact});

  final bool compact;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;

    if (compact) {
      return Semantics(
        label: 'Informações desatualizadas',
        child: Icon(Icons.priority_high, size: 18, color: colors.error),
      );
    }

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: 2,
      ),
      decoration: BoxDecoration(
        color: colors.errorContainer,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        'Desatualizado',
        style: Theme.of(context).textTheme.labelSmall?.copyWith(
              color: colors.onErrorContainer,
              fontWeight: FontWeight.w600,
            ),
      ),
    );
  }
}
