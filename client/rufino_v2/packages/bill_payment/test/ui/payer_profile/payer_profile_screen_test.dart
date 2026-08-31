import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/payer_profile/payer_profile_screen.dart';
import 'package:bill_payment/src/ui/payer_profile/payer_profile_viewmodel.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../fakes/fakes.dart';

/// A seção "Conta Asaas": a chave entra escondida, some depois de vinculada,
/// e remover exige confirmação — a chave nunca é reexibida.
void main() {
  late FakePayerProfileRepository repository;
  late PayerProfileViewModel viewModel;

  setUp(() {
    repository = FakePayerProfileRepository();
    viewModel = PayerProfileViewModel(repository: repository);
  });

  tearDown(() => viewModel.dispose());

  Future<void> pumpScreen(
    WidgetTester tester,
    BillPaymentPermissionNotifier permissions,
  ) async {
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
          home: PayerProfileScreen(
            viewModel: viewModel,
            backFallback: '/home',
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  Future<BillPaymentPermissionNotifier> managerAsync() =>
      billPaymentPermissions(const [
        Permission(
          resource: BillPaymentResources.payerProfile,
          scopes: [BillPaymentScopes.view, BillPaymentScopes.manage],
        ),
      ]);

  group('Conta Asaas', () {
    testWidgets('sem chave, a seção avisa que a consulta fica indisponível',
        (tester) async {
      repository.profile = payerProfile();
      final permissions = await managerAsync();

      await pumpScreen(tester, permissions);

      expect(find.text('Não configurada'), findsOneWidget);
      expect(
        find.textContaining('consulta oficial'),
        findsOneWidget,
      );

      permissions.dispose();
    });

    testWidgets('vincular manda a chave e limpa o campo — ela não é reexibida',
        (tester) async {
      repository.profile = payerProfile();
      final permissions = await managerAsync();

      await pumpScreen(tester, permissions);
      await tester.enterText(
        find.widgetWithText(TextField, 'Chave de API da subconta Asaas'),
        r'$aact_prod_chave',
      );
      await tester.tap(find.text('Vincular'));
      await tester.pumpAndSettle();

      expect(repository.calls, contains(r'linkAsaasAccount:$aact_prod_chave'));
      final field = tester.widget<TextField>(
        find.widgetWithText(TextField, 'Chave de API da subconta Asaas'),
      );
      expect(field.controller!.text, isEmpty);

      permissions.dispose();
    });

    testWidgets('remover a chave pede confirmação antes de apagar do cofre',
        (tester) async {
      repository.profile = payerProfile(canSchedulePayments: true);
      final permissions = await managerAsync();

      await pumpScreen(tester, permissions);
      await tester.tap(find.text('Remover chave'));
      await tester.pumpAndSettle();

      expect(find.text('Remover a chave Asaas?'), findsOneWidget);
      expect(repository.calls, isEmpty);

      await tester.tap(find.text('Remover'));
      await tester.pumpAndSettle();

      expect(repository.calls, contains('unlinkAsaasAccount'));

      permissions.dispose();
    });
  });
}
