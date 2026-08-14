import 'package:flutter/material.dart';

import '../theme/app_breakpoints.dart';
import '../theme/app_spacing.dart';

/// Returns the inner padding used by section cards.
///
/// Mobile viewports get a tighter padding so nested cards don't stack large
/// gutters on top of each other.
double _sectionInnerSpacing(BuildContext context) {
  final width = MediaQuery.sizeOf(context).width;
  return width < AppBreakpoints.mobile ? AppSpacing.sm : AppSpacing.md;
}

/// A card that frames a section with a title header.
///
/// The frame of the read-then-edit block pattern: the card stays put and its
/// [child] swaps between view mode and an inline form, so editing never
/// leaves the screen the data lives on.
class SectionCard extends StatelessWidget {
  /// Creates a section card titled [title] around [child].
  const SectionCard({
    super.key,
    required this.title,
    required this.child,
    this.trailing,
  });

  /// The section header text.
  final String title;

  /// The section body — view mode or inline form.
  final Widget child;

  /// Optional trailing widget shown in the card header.
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    final spacing = _sectionInnerSpacing(context);
    return Card.outlined(
      clipBehavior: Clip.antiAlias,
      child: Padding(
        padding: EdgeInsets.all(spacing),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    title,
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                ),
                if (trailing != null) trailing!,
              ],
            ),
            const SizedBox(height: AppSpacing.sm),
            child,
          ],
        ),
      ),
    );
  }
}

/// A labelled info row with a leading icon, used inside section view modes.
class InfoRow extends StatelessWidget {
  /// Creates a row showing [value] under [label], led by [icon].
  const InfoRow({
    super.key,
    required this.icon,
    required this.label,
    required this.value,
  });

  /// The leading icon.
  final IconData icon;

  /// The field name.
  final String label;

  /// The field value, already formatted for reading.
  final String value;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Row(
      children: [
        Icon(icon, size: 22, color: cs.primary),
        const SizedBox(width: AppSpacing.md),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label,
                style: Theme.of(context).textTheme.labelSmall?.copyWith(
                      color: cs.onSurfaceVariant,
                      letterSpacing: 0.4,
                    ),
              ),
              const SizedBox(height: 2),
              Text(value, style: Theme.of(context).textTheme.bodyMedium),
            ],
          ),
        ),
      ],
    );
  }
}
