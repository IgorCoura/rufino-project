import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/bills/bill_import_screen.dart';
import 'package:bill_payment/src/ui/bills/bill_import_viewmodel.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../fakes/fakes.dart';

void main() {
  late FakeBillRepository repository;
  late BillImportViewModel viewModel;

  const picked = (
    bytes: <int>[1, 2, 3],
    fileName: 'conta-de-luz.pdf',
    contentType: 'application/pdf',
  );

  setUp(() {
    repository = FakeBillRepository();
    viewModel = BillImportViewModel(repository: repository);
  });

  tearDown(() => viewModel.dispose());

  Future<void> pumpScreen(
    WidgetTester tester, {
    PickedDocument? offers = picked,
  }) async {
    await tester.pumpWidget(
      MaterialApp(
        home: BillImportScreen(
          viewModel: viewModel,
          backFallback: '/bill-payment/bills',
          onImported: (_) {},
          onPickDocument: () async => offers,
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('BillImportScreen', () {
    testWidgets('refuses to submit with no line, no Pix and no file',
        (tester) async {
      await pumpScreen(tester);

      await tester.tap(find.text('Importar'));
      await tester.pumpAndSettle();

      expect(
        find.text('Informe a linha digitável, o código Pix ou anexe o arquivo.'),
        findsOneWidget,
      );
      expect(repository.calls, isEmpty);
    });

    testWidgets('the attached file alone satisfies the form', (tester) async {
      await pumpScreen(tester);

      await tester.tap(find.text('Anexar arquivo do boleto'));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Importar'));
      await tester.pumpAndSettle();

      expect(repository.calls, contains('importBill'));
      expect(repository.lastImport!.documentBytes, picked.bytes);
    });

    testWidgets('shows the chosen file and drops it when removed',
        (tester) async {
      await pumpScreen(tester);

      await tester.tap(find.text('Anexar arquivo do boleto'));
      await tester.pumpAndSettle();

      expect(find.text('conta-de-luz.pdf'), findsOneWidget);

      await tester.tap(find.byTooltip('Remover arquivo'));
      await tester.pumpAndSettle();

      expect(find.text('conta-de-luz.pdf'), findsNothing);
      expect(find.text('Anexar arquivo do boleto'), findsOneWidget);
    });

    testWidgets('giving up on the picker leaves the form as it was',
        (tester) async {
      await pumpScreen(tester, offers: null);

      await tester.tap(find.text('Anexar arquivo do boleto'));
      await tester.pumpAndSettle();

      expect(viewModel.document, isNull);
      expect(find.text('Anexar arquivo do boleto'), findsOneWidget);
    });

    testWidgets('the file rides along with the digits when both are given',
        (tester) async {
      await pumpScreen(tester);

      await tester.enterText(
        find.widgetWithText(TextFormField, 'Linha digitável'),
        '34191234546789012345767890123457314880000061507',
      );
      await tester.tap(find.text('Anexar arquivo do boleto'));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Importar'));
      await tester.pumpAndSettle();

      expect(
        repository.lastImport!.digitableLine,
        '34191234546789012345767890123457314880000061507',
      );
      expect(repository.lastImport!.documentBytes, picked.bytes);
    });
  });
}
