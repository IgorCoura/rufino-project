import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../fakes/fakes.dart';

/// The first of the two gates a feature has to pass: is this product turned on
/// for this customer?
void main() {
  late TenantContextNotifier context;

  setUp(() {
    context = TenantContextNotifier(storage: FakeSecureStorage());
    addTearDown(context.dispose);
  });

  Widget wrap(Widget child) {
    return ChangeNotifierProvider<TenantContextNotifier>.value(
      value: context,
      child: Directionality(textDirection: TextDirection.ltr, child: child),
    );
  }

  const guard = ProductGuard(
    product: TenantProducts.billPayment,
    child: Text('bill payment card'),
  );

  group('ProductGuard', () {
    testWidgets('shows its child when the tenant has the product enabled',
        (tester) async {
      await context.select(
        tenant(activeProducts: const [TenantProducts.billPayment]),
      );

      await tester.pumpWidget(wrap(guard));

      expect(find.text('bill payment card'), findsOneWidget);
    });

    testWidgets('hides its child when the tenant did not buy the product',
        (tester) async {
      await context.select(
        tenant(activeProducts: const [TenantProducts.peopleManagement]),
      );

      await tester.pumpWidget(wrap(guard));

      expect(find.text('bill payment card'), findsNothing);
    });

    testWidgets('hides its child while no tenant is selected', (tester) async {
      await tester.pumpWidget(wrap(guard));

      expect(find.text('bill payment card'), findsNothing);
    });

    testWidgets('collapses to nothing rather than leaving a gap',
        (tester) async {
      await tester.pumpWidget(wrap(guard));

      final box = tester.widget<SizedBox>(find.byType(SizedBox));
      expect(box.width, 0);
      expect(box.height, 0);
    });

    testWidgets('reveals its child as soon as a tenant with the product is '
        'selected', (tester) async {
      await tester.pumpWidget(wrap(guard));
      expect(find.text('bill payment card'), findsNothing);

      await context.select(
        tenant(activeProducts: const [TenantProducts.billPayment]),
      );
      await tester.pump();

      expect(find.text('bill payment card'), findsOneWidget);
    });

    testWidgets('hides its child again when the context is cleared',
        (tester) async {
      await context.select(
        tenant(activeProducts: const [TenantProducts.billPayment]),
      );
      await tester.pumpWidget(wrap(guard));
      expect(find.text('bill payment card'), findsOneWidget);

      await context.clear();
      await tester.pump();

      expect(find.text('bill payment card'), findsNothing);
    });
  });
}
