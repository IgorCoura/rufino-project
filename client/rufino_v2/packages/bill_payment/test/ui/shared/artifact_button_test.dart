import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/bills/bill_detail_screen.dart';
import 'package:bill_payment/src/ui/bills/bill_detail_viewmodel.dart';
import 'package:bill_payment/src/ui/capture_items/capture_item_detail_screen.dart';
import 'package:bill_payment/src/ui/capture_items/capture_item_detail_viewmodel.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../fakes/fakes.dart';

/// Puts a screen on a tree with the permissions the guards read.
Future<void> pumpScreen(
  WidgetTester tester,
  Widget screen,
  BillPaymentPermissionNotifier permissions,
) async {
  await tester.pumpWidget(
    ChangeNotifierProvider<BillPaymentPermissionNotifier>.value(
      value: permissions,
      child: MaterialApp(home: screen),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  late BillPaymentPermissionNotifier permissions;

  setUp(() async {
    permissions = await billPaymentPermissions(const [
      Permission(
        resource: BillPaymentResources.captureItem,
        scopes: ['view', 'claim', 'reprocess'],
      ),
      Permission(
        resource: BillPaymentResources.bill,
        scopes: ['view', 'validate', 'approve', 'deny', 'cancel'],
      ),
    ]);
  });

  tearDown(() => permissions.dispose());

  group('quarantine item', () {
    late FakeCaptureItemRepository repository;

    setUp(() => repository = FakeCaptureItemRepository());

    Future<void> pumpItem(
      WidgetTester tester, {
      required bool hasArtifact,
      VoidCallback? onOpenArtifact,
      String? sourceUrl,
      Future<bool> Function(String url)? onOpenLink,
    }) async {
      repository.items = [
        captureItem(
          hasArtifact: hasArtifact,
          status: sourceUrl == null
              ? CaptureItemStatuses.unrouted
              : CaptureItemStatuses.unrecognized,
          sourceUrl: sourceUrl,
          linkHost: sourceUrl == null ? null : 'www.asaas.com',
        ),
      ];
      final viewModel = CaptureItemDetailViewModel(
        repository: repository,
        itemId: 'item-1',
      );
      addTearDown(viewModel.dispose);

      await pumpScreen(
        tester,
        CaptureItemDetailScreen(
          viewModel: viewModel,
          backFallback: '/home',
          onOpenBill: (_) {},
          onOpenArtifact: onOpenArtifact ?? () {},
          onOpenEmail: () {},
          onPickDocument: () async => null,
          onOpenLink: onOpenLink ?? (_) async => true,
        ),
        permissions,
      );
    }

    // O endereço do emissor é CLICÁVEL: ele é longo e opaco (token de capability),
    // e copiar à mão convida a erro de seleção. Sem este teste o link pode voltar a
    // ser texto puro sem quebrar nada.
    testWidgets('opens the issuer link when tapped', (tester) async {
      const url = 'https://www.asaas.com/i/55p08vsad5vci3g7';
      final opened = <String>[];

      await pumpItem(
        tester,
        hasArtifact: false,
        sourceUrl: url,
        onOpenLink: (value) async {
          opened.add(value);
          return true;
        },
      );

      await tester.tap(find.text(url));
      await tester.pumpAndSettle();

      expect(opened, [url]);
    });

    // E avisa quando não dá: um toque que não faz nada deixaria a pessoa achando
    // que o link está quebrado, quando o que faltou foi navegador disponível.
    testWidgets('warns when the link cannot be opened', (tester) async {
      const url = 'https://www.asaas.com/i/55p08vsad5vci3g7';

      await pumpItem(
        tester,
        hasArtifact: false,
        sourceUrl: url,
        onOpenLink: (_) async => false,
      );

      await tester.tap(find.text(url));
      await tester.pumpAndSettle();

      expect(
        find.text('Não foi possível abrir o endereço neste dispositivo.'),
        findsOneWidget,
      );
    });

    // É o ponto do problema: sem ver o papel, a pessoa reivindica no escuro.
    testWidgets('offers the document when there is one to show',
        (tester) async {
      await pumpItem(tester, hasArtifact: true);

      expect(find.text('Ver documento'), findsOneWidget);
    });

    // Some, não desabilita: prometer um documento que nunca vai chegar é pior
    // que não oferecer.
    testWidgets('hides the button when no file was kept', (tester) async {
      await pumpItem(tester, hasArtifact: false);

      expect(find.text('Ver documento'), findsNothing);
    });

    testWidgets('tapping it asks the shell to open the document',
        (tester) async {
      var opened = 0;
      await pumpItem(tester, hasArtifact: true, onOpenArtifact: () => opened++);

      await tester.tap(find.text('Ver documento'));
      await tester.pump();

      expect(opened, 1);
    });

    // O botão vem ANTES da decisão, e a ordem é a mensagem.
    testWidgets('comes before the claim action', (tester) async {
      await pumpItem(tester, hasArtifact: true);

      final document = tester.getTopLeft(find.text('Ver documento')).dy;
      final claim = tester.getTopLeft(find.text('Reivindicar este boleto')).dy;

      expect(document, lessThan(claim));
    });
  });

  group('bill approval screen', () {
    late FakeBillRepository repository;

    setUp(() => repository = FakeBillRepository());

    Future<void> pumpBill(
      WidgetTester tester, {
      required bool hasArtifact,
    }) async {
      repository.detail = billDetail(hasArtifact: hasArtifact);
      final viewModel = BillDetailViewModel(
        repository: repository,
        billId: 'bill-1',
      );
      addTearDown(viewModel.dispose);

      await pumpScreen(
        tester,
        BillDetailScreen(
          viewModel: viewModel,
          backFallback: '/bill-payment/bills',
          onOpenArtifact: () {},
          onOpenEmail: () {},
        ),
        permissions,
      );
    }

    testWidgets('offers the paper the checks talk about', (tester) async {
      await pumpBill(tester, hasArtifact: true);

      expect(find.text('Ver documento'), findsOneWidget);
    });

    // Importação manual nasce só com os dígitos — estado normal, sem botão.
    testWidgets('hides the button for a hand-imported bill', (tester) async {
      await pumpBill(tester, hasArtifact: false);

      expect(find.text('Ver documento'), findsNothing);
    });
  });
}
