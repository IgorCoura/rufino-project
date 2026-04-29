import 'package:flutter/widgets.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/ui/core/widgets/rufino_logo.dart';

void main() {
  group('RufinoLogo', () {
    testWidgets('renders the bundled SVG with the configured size',
        (tester) async {
      await tester.pumpWidget(
        const Directionality(
          textDirection: TextDirection.ltr,
          child: RufinoLogo(size: 64),
        ),
      );

      final picture = tester.widget<SvgPicture>(find.byType(SvgPicture));
      expect(picture.width, 64);
      expect(picture.height, 64);
    });

    testWidgets('exposes a semantic label for screen readers',
        (tester) async {
      await tester.pumpWidget(
        const Directionality(
          textDirection: TextDirection.ltr,
          child: RufinoLogo(semanticLabel: 'Rufino brand'),
        ),
      );

      expect(find.bySemanticsLabel('Rufino brand'), findsOneWidget);
    });
  });
}
