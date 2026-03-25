import 'package:flutter/material.dart';

import '../../../../../core/theme/app_spacing.dart';

/// A helper that wraps content in a card with an [ExpansionTile].
///
/// Triggers [onExpand] on first expansion so the section can lazy-load its data.
class ExpandableSectionCard extends StatefulWidget {
  const ExpandableSectionCard({
    super.key,
    required this.title,
    required this.child,
    required this.onExpand,
    this.trailing,
  });

  final String title;
  final Widget child;
  final VoidCallback onExpand;

  /// Optional trailing widget shown in the tile header.
  final Widget? trailing;

  @override
  State<ExpandableSectionCard> createState() => _ExpandableSectionCardState();
}

class _ExpandableSectionCardState extends State<ExpandableSectionCard> {
  /// Whether [widget.onExpand] has been triggered at least once.
  bool _loadTriggered = false;

  @override
  Widget build(BuildContext context) {
    return Card.outlined(
      clipBehavior: Clip.antiAlias,
      child: ExpansionTile(
        title: Row(
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
        onExpansionChanged: (expanded) {
          if (expanded && !_loadTriggered) {
            _loadTriggered = true;
            widget.onExpand();
          }
        },
        childrenPadding: const EdgeInsets.fromLTRB(
          AppSpacing.md,
          0,
          AppSpacing.md,
          AppSpacing.md,
        ),
        children: [widget.child],
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
