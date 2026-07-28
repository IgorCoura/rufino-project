import 'package:flutter/material.dart';

import '../../../../../core/theme/app_breakpoints.dart';
import '../../../../../core/theme/app_spacing.dart';

/// Returns the inner padding used by profile section cards.
///
/// Mobile viewports get a tighter padding so the nested cards don't stack
/// large gutters on top of each other.
double _sectionInnerSpacing(BuildContext context) {
  final width = MediaQuery.sizeOf(context).width;
  return width < AppBreakpoints.mobile ? AppSpacing.sm : AppSpacing.md;
}

/// A card that frames a profile section with a title header.
///
/// Section data is fetched by the tab orchestration
/// (`EmployeeProfileViewModel.openTab`), not by this card.
class SectionCard extends StatelessWidget {
  const SectionCard({
    super.key,
    required this.title,
    required this.child,
    this.trailing,
  });

  final String title;
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

/// A labelled info row with a leading icon, used inside profile section view modes.
class ContactInfoRow extends StatelessWidget {
  const ContactInfoRow({
    super.key,
    required this.icon,
    required this.label,
    required this.value,
  });

  final IconData icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Row(
      crossAxisAlignment: CrossAxisAlignment.center,
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
              Text(
                value,
                style: Theme.of(context).textTheme.bodyMedium,
              ),
            ],
          ),
        ),
      ],
    );
  }
}
