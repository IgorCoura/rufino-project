import 'package:flutter/material.dart';

import '../../../core/theme/app_spacing.dart';

/// Opens a modal bottom sheet that hosts a set of filter fields.
///
/// The sheet uses [showModalBottomSheet] with `isScrollControlled: true` so it
/// can grow up to the full available height and animate above the keyboard
/// without clipping its fields.
///
/// The [builder] receives a [BuildContext] and a `setSheetState` callback that
/// triggers a rebuild of the sheet content — useful when toggling fields based
/// on dropdown changes (e.g. period type).
///
/// Tapping "Aplicar" pops the sheet and invokes [onApply]. Tapping "Limpar"
/// invokes [onClear] without closing the sheet so users can review the cleared
/// fields before re-applying. Returns the value popped from the sheet.
Future<T?> showFilterSheet<T>({
  required BuildContext context,
  required String title,
  required Widget Function(BuildContext, void Function(VoidCallback))
      builder,
  required VoidCallback onApply,
  required VoidCallback onClear,
}) {
  return showModalBottomSheet<T>(
    context: context,
    isScrollControlled: true,
    useSafeArea: true,
    showDragHandle: true,
    clipBehavior: Clip.antiAlias,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
    ),
    builder: (sheetContext) {
      return _FilterSheetContent(
        title: title,
        builder: builder,
        onApply: onApply,
        onClear: onClear,
      );
    },
  );
}

class _FilterSheetContent extends StatefulWidget {
  const _FilterSheetContent({
    required this.title,
    required this.builder,
    required this.onApply,
    required this.onClear,
  });

  final String title;
  final Widget Function(BuildContext, void Function(VoidCallback)) builder;
  final VoidCallback onApply;
  final VoidCallback onClear;

  @override
  State<_FilterSheetContent> createState() => _FilterSheetContentState();
}

class _FilterSheetContentState extends State<_FilterSheetContent> {
  void _setSheetState(VoidCallback fn) {
    if (!mounted) return;
    setState(fn);
  }

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    final viewInsets = MediaQuery.viewInsetsOf(context);

    return Padding(
      padding: EdgeInsets.only(bottom: viewInsets.bottom),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.lg,
              0,
              AppSpacing.sm,
              AppSpacing.sm,
            ),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    widget.title,
                    style: textTheme.titleMedium,
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close),
                  tooltip: 'Fechar',
                  onPressed: () => Navigator.of(context).pop(),
                ),
              ],
            ),
          ),
          Flexible(
            child: SingleChildScrollView(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.lg,
                AppSpacing.sm,
                AppSpacing.lg,
                AppSpacing.md,
              ),
              child: widget.builder(context, _setSheetState),
            ),
          ),
          const Divider(height: 1),
          Padding(
            padding: const EdgeInsets.symmetric(
              horizontal: AppSpacing.lg,
              vertical: AppSpacing.md,
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                OutlinedButton.icon(
                  icon: const Icon(Icons.filter_alt_off, size: 18),
                  label: const Text('Limpar'),
                  onPressed: () {
                    widget.onClear();
                    _setSheetState(() {});
                  },
                ),
                const SizedBox(width: AppSpacing.sm),
                FilledButton.icon(
                  icon: const Icon(Icons.check, size: 18),
                  label: const Text('Aplicar'),
                  onPressed: () {
                    widget.onApply();
                    Navigator.of(context).pop();
                  },
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
