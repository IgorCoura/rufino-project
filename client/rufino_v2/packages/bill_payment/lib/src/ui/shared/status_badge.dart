import 'package:flutter/material.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../domain/bill_payment_enums.dart';

/// The visual weight of a [StatusBadge].
enum BadgeTone {
  /// Informational — the default chip.
  neutral,

  /// Deserves a look, does not block.
  attention,

  /// Something is wrong or blocked.
  problem,

  /// A good outcome.
  positive,
}

/// A small status chip, colored by [tone] from the theme's containers.
class StatusBadge extends StatelessWidget {
  /// Creates the badge.
  const StatusBadge({
    super.key,
    required this.label,
    this.tone = BadgeTone.neutral,
  });

  /// The text inside the chip.
  final String label;

  /// The visual weight.
  final BadgeTone tone;

  /// The badge for a bill status.
  factory StatusBadge.billStatus(String status) => StatusBadge(
        label: BillStatuses.label(status),
        tone: switch (status) {
          BillStatuses.awaitingApproval => BadgeTone.attention,
          BillStatuses.rejected ||
          BillStatuses.denied ||
          BillStatuses.failed =>
            BadgeTone.problem,
          BillStatuses.approved ||
          BillStatuses.paid ||
          BillStatuses.scheduled =>
            BadgeTone.positive,
          _ => BadgeTone.neutral,
        },
      );

  /// The badge for a capture item status.
  factory StatusBadge.captureItemStatus(String status) => StatusBadge(
        label: CaptureItemStatuses.label(status),
        tone: switch (status) {
          CaptureItemStatuses.unrouted => BadgeTone.attention,
          CaptureItemStatuses.locked ||
          CaptureItemStatuses.linkFailed ||
          CaptureItemStatuses.unrecognized ||
          CaptureItemStatuses.foreignPayer =>
            BadgeTone.problem,
          CaptureItemStatuses.promoted => BadgeTone.positive,
          _ => BadgeTone.neutral,
        },
      );

  /// The badge for an alert level.
  factory StatusBadge.alertLevel(String level) => StatusBadge(
        label: AlertLevels.label(level),
        tone: switch (level) {
          AlertLevels.urgent || AlertLevels.overdue => BadgeTone.problem,
          AlertLevels.warning => BadgeTone.attention,
          _ => BadgeTone.neutral,
        },
      );

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final (background, foreground) = switch (tone) {
      BadgeTone.neutral => (cs.secondaryContainer, cs.onSecondaryContainer),
      BadgeTone.attention => (cs.tertiaryContainer, cs.onTertiaryContainer),
      BadgeTone.problem => (cs.errorContainer, cs.onErrorContainer),
      BadgeTone.positive => (cs.primaryContainer, cs.onPrimaryContainer),
    };

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: 2,
      ),
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        label,
        style: Theme.of(context)
            .textTheme
            .labelSmall
            ?.copyWith(color: foreground),
      ),
    );
  }
}
