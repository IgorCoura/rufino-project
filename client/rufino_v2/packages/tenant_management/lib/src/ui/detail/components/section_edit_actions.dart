import 'package:flutter/material.dart';
import 'package:rufino_core/rufino_core.dart';

/// The Cancelar/Salvar pair every editable block ends with, plus the error
/// the last save produced.
///
/// The error stays **inside the block**: the message is about these fields,
/// and showing it here is what lets the user fix the one thing that was
/// refused without retyping the rest.
class SectionEditActions extends StatelessWidget {
  /// Creates the action row.
  const SectionEditActions({
    super.key,
    required this.isSaving,
    required this.error,
    required this.onCancel,
    required this.onSave,
  });

  /// Whether a save is in flight.
  final bool isSaving;

  /// The message of the last failed save, if any.
  final String? error;

  /// Discards the draft.
  final VoidCallback onCancel;

  /// Submits the block.
  final Future<void> Function() onSave;

  @override
  Widget build(BuildContext context) {
    final message = error;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (message != null) ...[
          const SizedBox(height: AppSpacing.md),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(
                Icons.error_outline,
                size: 20,
                color: Theme.of(context).colorScheme.error,
              ),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: Text(
                  message,
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: Theme.of(context).colorScheme.error,
                      ),
                ),
              ),
            ],
          ),
        ],
        const SizedBox(height: AppSpacing.md),
        Row(
          mainAxisAlignment: MainAxisAlignment.end,
          children: [
            TextButton(
              onPressed: isSaving ? null : onCancel,
              child: const Text('Cancelar'),
            ),
            const SizedBox(width: AppSpacing.sm),
            if (isSaving)
              const SizedBox(
                width: 24,
                height: 24,
                child: CircularProgressIndicator(strokeWidth: 2),
              )
            else
              FilledButton(onPressed: onSave, child: const Text('Salvar')),
          ],
        ),
      ],
    );
  }
}
