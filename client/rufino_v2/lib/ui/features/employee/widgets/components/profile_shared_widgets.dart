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

/// A card that shows its content directly (no expansion) and triggers
/// [onLoad] once when first built so the section can fetch its data.
class SectionCard extends StatefulWidget {
  const SectionCard({
    super.key,
    required this.title,
    required this.child,
    required this.onLoad,
    this.trailing,
  });

  final String title;
  final Widget child;
  final VoidCallback onLoad;

  /// Optional trailing widget shown in the card header.
  final Widget? trailing;

  @override
  State<SectionCard> createState() => _SectionCardState();
}

class _SectionCardState extends State<SectionCard> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      widget.onLoad();
    });
  }

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
                    widget.title,
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                ),
                if (widget.trailing != null) widget.trailing!,
              ],
            ),
            const SizedBox(height: AppSpacing.sm),
            widget.child,
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
