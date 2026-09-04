import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/payees/payee_list_screen.dart';
import 'package:bill_payment/src/ui/payees/payee_list_viewmodel.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../fakes/fakes.dart';

void main() {
  late FakePayeeRepository repository;
  late PayeeListViewModel viewModel;

  setUp(() {
    repository = FakePayeeRepository();
    viewModel = PayeeListViewModel(repository: repository);
  });

  tearDown(() => viewModel.dispose());

  Future<void> pumpScreen(
    WidgetTester tester, {
    required BillPaymentPermissionNotifier permissions,
    List<String> navigations = const [],
  }) async {
    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<BillPaymentPermissionNotifier>.value(
            value: permissions,
          ),
        ],
        child: MaterialApp(
          home: PayeeListScreen(
            viewModel: viewModel,
            backFallback: '/home',
            onOpenPayee: (id) => navigations.add('open:$id'),
            onCreatePayee: () => navigations.add('create'),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('PayeeListScreen', () {
    testWidgets('shows the rows and the register FAB for a manager',
        (tester) async {
      repository.payees = [payee()];
      final permissions = await billPaymentPermissions(const [
        Permission(
          resource: BillPaymentResources.payee,
          scopes: [BillPaymentScopes.view, BillPaymentScopes.manage],
        ),
      ]);

      await pumpScreen(tester, permissions: permissions);

      expect(find.text('EDP SAO PAULO SA'), findsOneWidget);
      expect(find.text('Cadastrar'), findsOneWidget);

      permissions.dispose();
    });

    testWidgets('flags a blacklisted payee in red and a whitelisted one in '
        'green, right on the row', (tester) async {
      repository.payees = [
        payee(standing: PayeeStandings.blacklisted),
        payee(
          id: 'payee-2',
          legalName: 'DAE JUNDIAI',
          standing: PayeeStandings.whitelisted,
        ),
      ];
      final permissions = await billPaymentPermissions(const [
        Permission(
          resource: BillPaymentResources.payee,
          scopes: [BillPaymentScopes.view],
        ),
      ]);

      await pumpScreen(tester, permissions: permissions);

      expect(find.text('Blacklist'), findsOneWidget);
      expect(find.text('Whitelist'), findsOneWidget);

      permissions.dispose();
    });

    testWidgets('hides the register FAB from a viewer — the element goes '
        'away, it is never disabled', (tester) async {
      repository.payees = [payee()];
      final permissions = await billPaymentPermissions(const [
        Permission(
          resource: BillPaymentResources.payee,
          scopes: [BillPaymentScopes.view],
        ),
      ]);

      await pumpScreen(tester, permissions: permissions);

      expect(find.text('EDP SAO PAULO SA'), findsOneWidget);
      expect(find.text('Cadastrar'), findsNothing);

      permissions.dispose();
    });

    testWidgets('shows the empty state with the discard warning',
        (tester) async {
      final permissions = await billPaymentPermissions(const [
        Permission(
          resource: BillPaymentResources.payee,
          scopes: [BillPaymentScopes.view],
        ),
      ]);

      await pumpScreen(tester, permissions: permissions);

      expect(
        find.textContaining('Nenhum beneficiário cadastrado'),
        findsOneWidget,
      );

      permissions.dispose();
    });

    testWidgets('tapping a row navigates through the callback',
        (tester) async {
      repository.payees = [payee()];
      final permissions = await billPaymentPermissions(const [
        Permission(
          resource: BillPaymentResources.payee,
          scopes: [BillPaymentScopes.view],
        ),
      ]);
      final navigations = <String>[];

      await pumpScreen(
        tester,
        permissions: permissions,
        navigations: navigations,
      );
      await tester.tap(find.text('EDP SAO PAULO SA'));

      expect(navigations, ['open:payee-1']);

      permissions.dispose();
    });
  });
}
