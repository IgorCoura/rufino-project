import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/bills/bill_detail_screen.dart';
import 'package:bill_payment/src/ui/bills/bill_detail_viewmodel.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../fakes/fakes.dart';

/// The "Consulta oficial" section: the due date of each rail's snapshot and
/// the payer the Pix decode returned.
///
/// Only the QR decode carries a payer — the bank-slip registry does not — so
/// the payer rows belong to the Pix block alone.
void main() {
  late BillPaymentPermissionNotifier permissions;

  final consultedAt = DateTime(2026, 6, 20, 9);

  setUp(() async {
    permissions = await billPaymentPermissions(const [
      Permission(resource: BillPaymentResources.bill, scopes: ['view']),
    ]);
  });

  tearDown(() => permissions.dispose());

  PixLookup pix({PixPayer? payer, DateTime? dueDate}) => PixLookup(
        isDynamic: true,
        canBePaid: true,
        consultedAt: consultedAt,
        receiver: const BillParty(name: 'EDP SAO PAULO S.A.'),
        dueDate: dueDate,
        payer: payer,
      );

  BankSlipLookup bankSlip({DateTime? dueDate}) => BankSlipLookup(
        allowChangeValue: false,
        isOverdue: false,
        consultedAt: consultedAt,
        beneficiary: const BillParty(name: 'EDP SAO PAULO S.A.'),
        dueDate: dueDate,
      );

  Future<void> pumpBill(
    WidgetTester tester, {
    BankSlipLookup? bankSlipLookup,
    PixLookup? pixLookup,
  }) async {
    // The section sits below the checks; a tall surface keeps the lazy list
    // from leaving it unbuilt.
    tester.view.physicalSize = const Size(1000, 4000);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);

    final repository = FakeBillRepository()
      ..detail = billDetail(
        bankSlipLookup: bankSlipLookup,
        pixLookup: pixLookup,
      );

    final viewModel = BillDetailViewModel(
      repository: repository,
      billId: 'bill-1',
    );
    addTearDown(viewModel.dispose);

    await tester.pumpWidget(
      ChangeNotifierProvider<BillPaymentPermissionNotifier>.value(
        value: permissions,
        child: MaterialApp(
          home: BillDetailScreen(
            viewModel: viewModel,
            backFallback: '/bill-payment/bills',
            onOpenArtifact: () {},
            onOpenEmail: () {},
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('official lookup section — Pix decode', () {
    testWidgets('shows the payer name and the complete document',
        (tester) async {
      await pumpBill(
        tester,
        pixLookup: pix(
          payer: const PixPayer(
            name: 'RUFINO EMPREITEIRA LTDA',
            taxId: '45.678.901/0001-75',
            isTaxIdComplete: true,
          ),
        ),
      );

      expect(find.text('Pagador'), findsOneWidget);
      expect(find.text('RUFINO EMPREITEIRA LTDA'), findsOneWidget);
      expect(find.text('CPF/CNPJ do pagador'), findsOneWidget);
      expect(find.text('45.678.901/0001-75'), findsOneWidget);
    });

    // Four visible digits are shared by millions of documents: the mask must
    // not pass for an identification.
    testWidgets('marks a masked document as partial', (tester) async {
      await pumpBill(
        tester,
        pixLookup: pix(
          payer: const PixPayer(
            name: 'Fulano',
            taxId: '***.982.247-**',
            isTaxIdComplete: false,
          ),
        ),
      );

      expect(find.text('***.982.247-** (parcial)'), findsOneWidget);
    });

    testWidgets('hides the payer rows when the decode brought none',
        (tester) async {
      await pumpBill(tester, pixLookup: pix());

      expect(find.text('Pagador'), findsNothing);
      expect(find.text('CPF/CNPJ do pagador'), findsNothing);
    });

    testWidgets('shows the name alone when the document is absent',
        (tester) async {
      await pumpBill(
        tester,
        pixLookup: pix(
          payer: const PixPayer(name: 'Fulano', isTaxIdComplete: false),
        ),
      );

      expect(find.text('Fulano'), findsOneWidget);
      expect(find.text('CPF/CNPJ do pagador'), findsNothing);
    });

    testWidgets('shows the due date the decode returned', (tester) async {
      await pumpBill(tester, pixLookup: pix(dueDate: DateTime(2026, 6, 25)));

      expect(find.text('25/06/2026'), findsOneWidget);
    });
  });

  group('official lookup section — bank-slip registry', () {
    // The registry does not return a payer, so no payer row can appear here.
    testWidgets('never shows payer rows', (tester) async {
      await pumpBill(
        tester,
        bankSlipLookup: bankSlip(dueDate: DateTime(2026, 6, 25)),
      );

      expect(find.text('Registro do boleto'), findsOneWidget);
      expect(find.text('Pagador'), findsNothing);
      expect(find.text('CPF/CNPJ do pagador'), findsNothing);
    });

    testWidgets('shows the registered due date', (tester) async {
      await pumpBill(
        tester,
        bankSlipLookup: bankSlip(dueDate: DateTime(2026, 6, 25)),
      );

      expect(find.text('25/06/2026'), findsOneWidget);
    });
  });

  // A missing row read the same as "the screen does not show due dates".
  testWidgets('says the due date was not informed on both rails',
      (tester) async {
    await pumpBill(tester, bankSlipLookup: bankSlip(), pixLookup: pix());

    expect(find.text('Não informado'), findsNWidgets(2));
  });
}
