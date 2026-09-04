import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';

void main() {
  const childKey = Key('section-body');

  /// Renders [child] on a viewport [width] logical pixels wide.
  Future<void> pumpAt(WidgetTester tester, Widget child, double width) {
    return tester.pumpWidget(
      MaterialApp(
        home: MediaQuery(
          data: MediaQueryData(size: Size(width, 900)),
          child: Scaffold(body: child),
        ),
      ),
    );
  }

  /// The padding the card itself applies around its content.
  EdgeInsets innerPaddingOf(WidgetTester tester) {
    final padding = tester.widget<Padding>(
      find
          .ancestor(of: find.byKey(childKey), matching: find.byType(Padding))
          .first,
    );
    return padding.padding as EdgeInsets;
  }

  group('SectionCard', () {
    testWidgets('shows its title above its body', (tester) async {
      await pumpAt(
        tester,
        const SectionCard(
          title: 'Endereco',
          child: Text('conteudo', key: childKey),
        ),
        900,
      );

      expect(find.text('Endereco'), findsOneWidget);
      expect(find.byKey(childKey), findsOneWidget);
    });

    testWidgets('shows the trailing widget when one is given', (tester) async {
      await pumpAt(
        tester,
        const SectionCard(
          title: 'Endereco',
          trailing: Text('Editar'),
          child: Text('conteudo', key: childKey),
        ),
        900,
      );

      expect(find.text('Editar'), findsOneWidget);
    });

    testWidgets('renders only the title when there is no trailing widget',
        (tester) async {
      await pumpAt(
        tester,
        const SectionCard(
          title: 'Endereco',
          child: SizedBox(key: childKey),
        ),
        900,
      );

      expect(find.text('Endereco'), findsOneWidget);
      expect(find.text('Editar'), findsNothing);
    });

    testWidgets('tightens its padding on a phone-sized viewport so nested '
        'cards do not stack gutters', (tester) async {
      await pumpAt(
        tester,
        const SectionCard(
          title: 'Endereco',
          child: SizedBox(key: childKey),
        ),
        360,
      );

      expect(innerPaddingOf(tester), const EdgeInsets.all(AppSpacing.sm));
    });

    testWidgets('uses the regular padding from the mobile breakpoint up',
        (tester) async {
      await pumpAt(
        tester,
        const SectionCard(
          title: 'Endereco',
          child: SizedBox(key: childKey),
        ),
        AppBreakpoints.mobile,
      );

      expect(innerPaddingOf(tester), const EdgeInsets.all(AppSpacing.md));
    });
  });

  group('InfoRow', () {
    testWidgets('shows the label, the value and the leading icon',
        (tester) async {
      await pumpAt(
        tester,
        const InfoRow(
          icon: Icons.mail_outline,
          label: 'E-mail',
          value: 'ana@test.com',
        ),
        900,
      );

      expect(find.text('E-mail'), findsOneWidget);
      expect(find.text('ana@test.com'), findsOneWidget);
      expect(find.byIcon(Icons.mail_outline), findsOneWidget);
    });

    testWidgets('shows the value exactly as it was handed over, already '
        'formatted', (tester) async {
      await pumpAt(
        tester,
        const InfoRow(
          icon: Icons.attach_money,
          label: 'Valor',
          value: 'R\$ 1.234,56',
        ),
        900,
      );

      expect(find.text('R\$ 1.234,56'), findsOneWidget);
    });
  });
}
