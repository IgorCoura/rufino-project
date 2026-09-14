import 'dart:typed_data';

import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/bills/bill_list_screen.dart';
import 'package:bill_payment/src/ui/bills/bill_list_viewmodel.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../fakes/fakes.dart';

/// Selecionar boletos na lista e baixar os documentos deles de uma vez.
///
/// Decisões do usuário em 2026-09-14 que estes testes fixam: a ordem do arquivo
/// é a ordem em que os boletos foram marcados, e trocar o filtro limpa a
/// seleção.
void main() {
  late FakeBillRepository repository;
  late List<({String fileName, Uint8List bytes})> saved;
  late bool saverAnswer;
  late Object? saverThrows;

  setUp(() {
    repository = FakeBillRepository()
      ..bills = [
        bill(id: 'b1', amount: 100),
        bill(id: 'b2', amount: 200),
        bill(id: 'b3', amount: 300),
      ];
    saved = [];
    saverAnswer = true;
    saverThrows = null;
  });

  Future<bool> saver({required String fileName, required Uint8List bytes}) async {
    if (saverThrows != null) throw saverThrows!;
    saved.add((fileName: fileName, bytes: bytes));
    return saverAnswer;
  }

  BillListViewModel viewModel({DocumentSaver? onSave}) {
    final vm = BillListViewModel(
      repository: repository,
      onSaveDocument: onSave ?? saver,
      reporter: FakeErrorReporter(),
    );
    addTearDown(vm.dispose);
    return vm;
  }

  const options = (
    pages: BillDocumentPages.firstPage,
    includeReceipts: true,
    packaging: BillDocumentPackagings.pdfPerBill,
  );

  group('selection', () {
    // A ordem da seleção é a ordem do arquivo — não a da lista.
    test('keeps the order in which the bills were selected', () async {
      final vm = viewModel();
      await vm.load();

      vm
        ..toggleSelection(vm.items[2])
        ..toggleSelection(vm.items[0]);

      expect(vm.selectedBills.map((b) => b.id), ['b3', 'b1']);
    });

    // Marcar de novo desmarca — e desmarcar o último NÃO desliga a seleção:
    // quem desliga é o X ou o download concluído.
    test('unselecting the last bill keeps selection mode on', () async {
      final vm = viewModel();
      await vm.load();

      vm
        ..toggleSelection(vm.items[0])
        ..toggleSelection(vm.items[0]);

      expect(vm.selectedBills, isEmpty);
      expect(vm.isSelecting, isTrue);
    });

    // O botão liga a seleção vazia; o X a desliga e esquece o que foi marcado.
    test('the button turns selection on and the close turns it off', () async {
      final vm = viewModel();
      await vm.load();

      vm.startSelection();
      expect(vm.isSelecting, isTrue);
      expect(vm.selectedBills, isEmpty);

      vm
        ..toggleSelection(vm.items[0])
        ..clearSelection();

      expect(vm.isSelecting, isFalse);
      expect(vm.selectedBills, isEmpty);
    });

    // "Selecionar todos" preserva quem já estava marcado na frente e acrescenta
    // os carregados na ordem da lista.
    test('select all keeps the earlier picks first', () async {
      final vm = viewModel();
      await vm.load();

      vm
        ..toggleSelection(vm.items[1])
        ..selectAllLoaded();

      expect(vm.selectedBills.map((b) => b.id), ['b2', 'b1', 'b3']);
    });

    // Trocar o filtro limpa o que estava marcado (baixar o que saiu da vista
    // seria surpresa), mas mantém a seleção ligada para marcar outros.
    test('changing the filter clears the picks and keeps the mode', () async {
      final vm = viewModel();
      await vm.load();
      vm.toggleSelection(vm.items[0]);

      await vm.selectStatus(BillStatuses.paid);

      expect(vm.selectedBills, isEmpty);
      expect(vm.isSelecting, isTrue);
    });
  });

  group('export', () {
    // O pedido leva os ids na ordem da seleção e as três escolhas; salvou,
    // avisa e DESLIGA a seleção.
    test('sends the selection and turns it off once the file is saved', () async {
      final vm = viewModel();
      await vm.load();
      vm
        ..toggleSelection(vm.items[1])
        ..toggleSelection(vm.items[0]);

      await vm.exportSelected(options);

      expect(repository.lastExport?.billIds, ['b2', 'b1']);
      expect(repository.lastExport?.pages, BillDocumentPages.firstPage);
      expect(repository.lastExport?.includeReceipts, isTrue);
      expect(repository.lastExport?.packaging, BillDocumentPackagings.pdfPerBill);
      expect(saved.single.fileName, 'boletos-2026-09-14.pdf');
      expect(vm.exportMessage, 'Arquivo salvo.');
      expect(vm.isSelecting, isFalse);
    });

    // Fechar a caixa de diálogo do sistema não é sucesso: nada de "Arquivo
    // salvo.", e a seleção continua para tentar de novo.
    test('a dismissed save dialog announces nothing and keeps the selection',
        () async {
      saverAnswer = false;
      final vm = viewModel();
      await vm.load();
      vm.toggleSelection(vm.items[0]);

      await vm.exportSelected(options);

      expect(vm.exportMessage, isNull);
      expect(vm.isSelecting, isTrue);
      expect(vm.selectedBills, hasLength(1));
    });

    // Recusa do servidor mostra a mensagem da regra e mantém a seleção.
    test('a server refusal shows the rule message and keeps the selection',
        () async {
      repository.exportShouldFail = true;
      final vm = viewModel();
      await vm.load();
      vm.toggleSelection(vm.items[0]);

      await vm.exportSelected(options);

      expect(vm.exportMessage, 'regra disse não');
      expect(vm.isSelecting, isTrue);
    });

    // Plugin de plataforma que falha vira aviso, não derruba a tela.
    test('a failing saver becomes a warning', () async {
      saverThrows = StateError('disco cheio');
      final vm = viewModel();
      await vm.load();
      vm.toggleSelection(vm.items[0]);

      await vm.exportSelected(options);

      expect(vm.exportMessage, 'Não foi possível salvar o arquivo.');
      expect(vm.isExporting, isFalse);
    });
  });

  group('screen', () {
    Future<BillListViewModel> pumpList(WidgetTester tester, {double width = 500}) async {
      final vm = viewModel();
      final permissions = await billPaymentPermissions([
        const Permission(resource: BillPaymentResources.bill, scopes: ['view']),
      ]);
      addTearDown(permissions.dispose);

      tester.view.physicalSize = Size(width, 1600);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.reset);

      await tester.pumpWidget(
        ChangeNotifierProvider<BillPaymentPermissionNotifier>.value(
          value: permissions,
          child: MaterialApp(
            home: BillListScreen(
              viewModel: vm,
              backFallback: '/bill-payment/pending',
              onOpenBill: (id) => repository.calls.add('open:$id'),
              onScheduleBill: (_) {},
              onImportBill: () {},
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      return vm;
    }

    // No celular o toque longo inicia a seleção; a partir daí o toque marca em
    // vez de abrir o boleto.
    testWidgets('long press starts the selection and taps then select',
        (tester) async {
      await pumpList(tester);

      expect(find.byType(Checkbox), findsNothing);
      await tester.longPress(find.textContaining('100,00'));
      await tester.pumpAndSettle();

      expect(find.text('1 selecionado'), findsOneWidget);

      await tester.tap(find.textContaining('200,00'));
      await tester.pumpAndSettle();

      expect(find.text('2 selecionados'), findsOneWidget);
      expect(repository.calls.where((c) => c.startsWith('open:')), isEmpty);
    });

    // Regra do usuário (2026-09-14): a seleção NÃO fica sempre ligada, nem em
    // tela larga — as caixas só aparecem depois do botão "Baixar documentos".
    testWidgets('checkboxes stay hidden until the button turns selection on',
        (tester) async {
      await pumpList(tester, width: 1000);

      expect(find.byType(Checkbox), findsNothing);

      await tester.tap(find.text('Baixar documentos'));
      await tester.pumpAndSettle();

      expect(find.byType(Checkbox), findsNWidgets(3));
      expect(find.text('Selecione os boletos'), findsOneWidget);
      // Com nada marcado, o botão de baixar existe mas não faz nada.
      final baixar = tester.widget<FloatingActionButton>(
        find.widgetWithText(FloatingActionButton, 'Baixar'),
      );
      expect(baixar.onPressed, isNull);
    });

    // A folha manda as escolhas feitas, o arquivo é salvo e a seleção desliga.
    testWidgets('the sheet sends the chosen options', (tester) async {
      await pumpList(tester);

      await tester.tap(find.text('Baixar documentos'));
      await tester.pumpAndSettle();
      await tester.tap(find.textContaining('300,00'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Baixar (1)'));
      await tester.pumpAndSettle();

      expect(find.text('Baixar documentos de 1 boleto'), findsOneWidget);
      // O boleto semeado nasceu de importação manual: a folha avisa da
      // página que entra no lugar do documento.
      expect(find.textContaining('entra uma página de aviso'), findsOneWidget);

      await tester.tap(find.text('Somente a primeira página'));
      await tester.tap(find.text('Anexar comprovantes de pagamento'));
      await tester.tap(find.text('Um PDF por boleto'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Baixar'));
      await tester.pumpAndSettle();

      expect(repository.lastExport?.billIds, ['b3']);
      expect(repository.lastExport?.pages, BillDocumentPages.firstPage);
      expect(repository.lastExport?.includeReceipts, isTrue);
      expect(repository.lastExport?.packaging, BillDocumentPackagings.pdfPerBill);
      expect(saved, hasLength(1));
      expect(find.text('Arquivo salvo.'), findsOneWidget);
      expect(find.text('Boletos'), findsOneWidget);
      expect(find.byType(Checkbox), findsNothing);
    });

    // Cancelar a folha não baixa nada e mantém a seleção.
    testWidgets('cancelling the sheet downloads nothing', (tester) async {
      await pumpList(tester);

      await tester.longPress(find.textContaining('100,00'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Baixar (1)'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();

      expect(repository.lastExport, isNull);
      expect(find.text('1 selecionado'), findsOneWidget);
    });

    // O X da barra sai da seleção.
    testWidgets('the close button leaves the selection', (tester) async {
      await pumpList(tester);

      await tester.longPress(find.textContaining('100,00'));
      await tester.pumpAndSettle();
      await tester.tap(find.byTooltip('Sair da seleção'));
      await tester.pumpAndSettle();

      expect(find.text('Boletos'), findsOneWidget);
    });
  });
}
