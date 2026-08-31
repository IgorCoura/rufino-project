import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/payees/payee_detail_screen.dart';
import 'package:bill_payment/src/ui/payees/payee_detail_viewmodel.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../fakes/fakes.dart';

/// The trust mark on the payee detail: read it in red, change it under the
/// manage scope, and never lose the confirmation before a blacklist.
void main() {
  late FakePayeeRepository repository;
  late PayeeDetailViewModel viewModel;

  setUp(() {
    repository = FakePayeeRepository();
    viewModel = PayeeDetailViewModel(repository: repository, payeeId: 'payee-1');
  });

  tearDown(() => viewModel.dispose());

  Future<BillPaymentPermissionNotifier> permissionsAsync(
    List<String> scopes,
  ) =>
      billPaymentPermissions([
        Permission(resource: BillPaymentResources.payee, scopes: scopes),
      ]);

  Future<void> pumpScreen(
    WidgetTester tester,
    BillPaymentPermissionNotifier permissions,
  ) async {
    // A seção da marca fica abaixo da dobra do ListView; sem alongar a
    // superfície o builder preguiçoso nem a constrói.
    tester.view.physicalSize = const Size(800, 2400);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);
    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<BillPaymentPermissionNotifier>.value(
            value: permissions,
          ),
        ],
        child: MaterialApp(
          home: PayeeDetailScreen(
            viewModel: viewModel,
            backFallback: '/home',
            onDeleted: () {},
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('Marca de confiança', () {
    testWidgets('blacklist aparece em vermelho, com o aviso do efeito',
        (tester) async {
      repository.payees = [payee(standing: PayeeStandings.blacklisted)];
      final permissions = await permissionsAsync(const [
        BillPaymentScopes.view,
      ]);

      await pumpScreen(tester, permissions);

      expect(find.text('Blacklist'), findsOneWidget);
      expect(find.textContaining('sinalizado como Perigo'), findsOneWidget);

      permissions.dispose();
    });

    testWidgets('marcar blacklist pede confirmação e grava só depois dela',
        (tester) async {
      repository.payees = [payee()];
      final permissions = await permissionsAsync(const [
        BillPaymentScopes.view,
        BillPaymentScopes.manage,
      ]);

      await pumpScreen(tester, permissions);
      await tester.tap(find.text('Marcar como blacklist'));
      await tester.pumpAndSettle();

      expect(find.text('Marcar na blacklist?'), findsOneWidget);
      expect(repository.calls, isEmpty);

      await tester.tap(find.text('Marcar'));
      await tester.pumpAndSettle();

      expect(repository.calls, contains('setStanding:Blacklisted'));
      expect(find.text('Blacklist'), findsWidgets);

      permissions.dispose();
    });

    testWidgets('remover a marca volta o beneficiário ao estado sem marca',
        (tester) async {
      repository.payees = [payee(standing: PayeeStandings.whitelisted)];
      final permissions = await permissionsAsync(const [
        BillPaymentScopes.view,
        BillPaymentScopes.manage,
      ]);

      await pumpScreen(tester, permissions);
      await tester.tap(find.text('Remover marca'));
      await tester.pumpAndSettle();

      expect(repository.calls, contains('setStanding:Normal'));
      expect(find.text('Sem marca'), findsOneWidget);

      permissions.dispose();
    });

    testWidgets('sem o escopo de gestão os botões somem, o estado fica',
        (tester) async {
      repository.payees = [payee(standing: PayeeStandings.whitelisted)];
      final permissions = await permissionsAsync(const [
        BillPaymentScopes.view,
      ]);

      await pumpScreen(tester, permissions);

      expect(find.text('Whitelist'), findsOneWidget);
      expect(find.text('Marcar como blacklist'), findsNothing);
      expect(find.text('Remover marca'), findsNothing);

      permissions.dispose();
    });
  });
}
