import 'package:flutter/material.dart';
import 'package:rufino_core/rufino_core.dart';

/// A centered message for error and empty states, with an optional action.
class MessagePanel extends StatelessWidget {
  /// Creates the panel.
  const MessagePanel({
    super.key,
    required this.icon,
    required this.title,
    this.action,
  });

  /// The leading icon.
  final IconData icon;

  /// The message.
  final String title;

  /// An optional action button (retry, clear filters, ...).
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 56, color: theme.colorScheme.outline),
            const SizedBox(height: AppSpacing.md),
            Text(
              title,
              style: theme.textTheme.titleMedium,
              textAlign: TextAlign.center,
            ),
            if (action != null) ...[
              const SizedBox(height: AppSpacing.md),
              action!,
            ],
          ],
        ),
      ),
    );
  }
}
