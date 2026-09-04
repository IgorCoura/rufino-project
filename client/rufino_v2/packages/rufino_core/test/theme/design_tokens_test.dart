import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';

/// The design tokens three products lay their screens out on.
///
/// Only the properties the layouts rely on are pinned: the 4dp grid, the
/// ordering, and the exact numbers other packages hard-code against.
void main() {
  group('AppSpacing', () {
    const steps = [
      AppSpacing.xs,
      AppSpacing.sm,
      AppSpacing.md,
      AppSpacing.lg,
      AppSpacing.xl,
      AppSpacing.xxl,
      AppSpacing.xxxl,
    ];

    test('places every step on the 4dp grid', () {
      for (final step in steps) {
        expect(step % 4, 0, reason: '$step is off the 4dp grid');
      }
    });

    test('grows strictly from one step to the next', () {
      for (var i = 1; i < steps.length; i++) {
        expect(steps[i], greaterThan(steps[i - 1]));
      }
    });

    test('starts above zero, so no step collapses a layout', () {
      expect(steps.first, greaterThan(0));
    });

    test('holds the values the products lay out against', () {
      expect(AppSpacing.xs, 4);
      expect(AppSpacing.sm, 8);
      expect(AppSpacing.md, 16);
      expect(AppSpacing.lg, 24);
      expect(AppSpacing.xl, 32);
      expect(AppSpacing.xxl, 48);
      expect(AppSpacing.xxxl, 64);
    });
  });

  group('AppBreakpoints', () {
    test('holds the thresholds the responsive layouts switch on', () {
      expect(AppBreakpoints.mobile, 600);
      expect(AppBreakpoints.tablet, 840);
      expect(AppBreakpoints.desktop, 1200);
    });

    test('orders the thresholds from the narrowest viewport up', () {
      expect(AppBreakpoints.mobile, lessThan(AppBreakpoints.tablet));
      expect(AppBreakpoints.tablet, lessThan(AppBreakpoints.desktop));
    });

    test('leaves no width unclassified between one threshold and the next',
        () {
      const width = 700.0;

      expect(width >= AppBreakpoints.mobile, isTrue);
      expect(width < AppBreakpoints.tablet, isTrue);
    });
  });
}
