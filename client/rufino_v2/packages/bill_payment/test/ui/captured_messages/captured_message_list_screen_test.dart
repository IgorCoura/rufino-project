import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/captured_messages/captured_message_list_screen.dart';
import 'package:bill_payment/src/ui/captured_messages/captured_message_list_viewmodel.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../fakes/fakes.dart';

/// Procura dentro da linha do e-mail, e não na tela inteira.
///
/// O rótulo do desfecho aparece duas vezes de propósito — no selo da linha e no
/// chip do filtro —, então um `find.text` solto casaria com os dois e o teste
/// passaria a medir a barra de filtros.
Finder inRow(String subject, String text) => find.descendant(
      of: find.widgetWithText(Card, subject),
      matching: find.text(text),
    );

void main() {
  late FakeCapturedMessageRepository repository;
  late CapturedMessageListViewModel viewModel;

  setUp(() {
    repository = FakeCapturedMessageRepository();
    viewModel = CapturedMessageListViewModel(repository: repository);
  });

  tearDown(() => viewModel.dispose());

  Future<void> pumpScreen(
    WidgetTester tester, {
    required List<String> scopes,
    void Function(String billId)? onOpenBill,
    void Function(String itemId)? onOpenCaptureItem,
  }) async {
    final permissions = await billPaymentPermissions([
      Permission(
        resource: BillPaymentResources.capturedMessage,
        scopes: scopes,
      ),
      Permission(
        resource: BillPaymentResources.captureRetention,
        scopes: scopes.contains('manage') ? ['view', 'manage'] : ['view'],
      ),
    ]);
    addTearDown(permissions.dispose);

    await tester.pumpWidget(
      ChangeNotifierProvider<BillPaymentPermissionNotifier>.value(
        value: permissions,
        child: MaterialApp(
          home: CapturedMessageListScreen(
            viewModel: viewModel,
            backFallback: '/home',
            onOpenBill: onOpenBill ?? (_) {},
            onOpenCaptureItem: onOpenCaptureItem ?? (_) {},
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('CapturedMessageListScreen', () {
    // O cabeçalho responde a primeira pergunta de quem chega aqui: a varredura
    // já rodou depois de eu mandar o e-mail?
    testWidgets('shows when the mailbox was last read', (tester) async {
      repository.messages = [capturedMessage()];

      await pumpScreen(tester, scopes: ['view']);

      expect(find.textContaining('Última sincronização'), findsOneWidget);
    });

    // O e-mail descartado aparece aqui e em lugar nenhum mais — é a razão da
    // tela existir.
    testWidgets('lists a discarded e-mail with its outcome', (tester) async {
      repository.messages = [capturedMessage(subject: 'Newsletter de agosto')];

      await pumpScreen(tester, scopes: ['view']);

      expect(find.text('Newsletter de agosto'), findsOneWidget);
      expect(find.text('faturas@enel.com.br'), findsOneWidget);
      expect(inRow('Newsletter de agosto', 'Descartado'), findsOneWidget);
      expect(find.text('1 anexo'), findsOneWidget);
    });

    testWidgets('offers the bill when the e-mail produced one', (tester) async {
      repository.messages = [
        capturedMessage(outcome: ArtifactOutcomes.promoted, billId: 'bill-7'),
      ];

      await pumpScreen(tester, scopes: ['view']);

      expect(inRow('Sua fatura chegou', 'Virou boleto'), findsOneWidget);
      expect(find.text('Abrir boleto'), findsOneWidget);
    });

    // O defeito de 2026-09-10: um e-mail rende N itens, e a linha oferecia um
    // botão só — o primeiro id que o e-mail encontrasse. Dois boletos no mesmo
    // e-mail deixavam o segundo inalcançável por esta tela.
    testWidgets('offers one bill button per attachment that became a bill',
        (tester) async {
      repository.messages = [
        capturedMessage(
          outcome: ArtifactOutcomes.promoted,
          artifacts: [
            capturedArtifact(
              fileName: 'agua.pdf',
              outcome: ArtifactOutcomes.promoted,
              billId: 'bill-7',
              captureItemId: 'item-7',
            ),
            capturedArtifact(
              fileName: 'luz.pdf',
              outcome: ArtifactOutcomes.promoted,
              billId: 'bill-8',
              captureItemId: 'item-8',
            ),
          ],
        ),
      ];
      final opened = <String>[];

      await pumpScreen(
        tester,
        scopes: ['view'],
        onOpenBill: opened.add,
      );

      expect(find.text('2 anexos'), findsOneWidget);
      expect(find.text('Abrir boleto'), findsNWidgets(2));

      await tester.tap(find.byTooltip('Abrir boleto: agua.pdf'));
      await tester.tap(find.byTooltip('Abrir boleto: luz.pdf'));

      expect(opened, ['bill-7', 'bill-8']);
    });

    // O pior caso do botão único: a condição era exclusiva (`boleto == null &&
    // item != null`), então o e-mail que trouxe um boleto E um anexo para
    // revisão escondia a quarentena por inteiro.
    testWidgets('offers the bill and the quarantine item of the same e-mail',
        (tester) async {
      repository.messages = [
        capturedMessage(
          outcome: ArtifactOutcomes.promoted,
          artifacts: [
            capturedArtifact(
              fileName: 'boleto.pdf',
              outcome: ArtifactOutcomes.promoted,
              billId: 'bill-7',
              captureItemId: 'item-7',
            ),
            capturedArtifact(
              fileName: 'anexo-estranho.pdf',
              outcome: ArtifactOutcomes.quarantined,
              captureItemId: 'item-9',
            ),
          ],
        ),
      ];
      final bills = <String>[];
      final items = <String>[];

      await pumpScreen(
        tester,
        scopes: ['view'],
        onOpenBill: bills.add,
        onOpenCaptureItem: items.add,
      );

      expect(inRow('Sua fatura chegou', 'Não reconhecido'), findsOneWidget);

      await tester.tap(find.byTooltip('Abrir boleto: boleto.pdf'));
      await tester.tap(
        find.byTooltip('Abrir na quarentena: anexo-estranho.pdf'),
      );

      expect(bills, ['bill-7']);
      expect(items, ['item-9']);
    });

    // Descartado não deixa item: o servidor anula o id porque a linha foi
    // apagada, e um botão aqui levaria a um 404.
    testWidgets('the discarded attachment has nowhere to go', (tester) async {
      repository.messages = [
        capturedMessage(
          outcome: ArtifactOutcomes.quarantined,
          artifacts: [
            capturedArtifact(
              fileName: 'boleto.pdf',
              outcome: ArtifactOutcomes.quarantined,
              captureItemId: 'item-3',
            ),
            capturedArtifact(
              fileName: 'propaganda.pdf',
              outcome: ArtifactOutcomes.discarded,
            ),
          ],
        ),
      ];

      await pumpScreen(tester, scopes: ['view']);

      expect(find.text('Abrir na quarentena'), findsOneWidget);
      expect(find.text('propaganda.pdf'), findsOneWidget);
      expect(inRow('Sua fatura chegou', 'Descartado'), findsOneWidget);
    });

    // Com um anexo só o cabeçalho já diz o desfecho — repetir o selo na linha
    // escreveria o mesmo rótulo duas vezes, uma abaixo da outra.
    testWidgets('a single attachment shows its name without a second badge',
        (tester) async {
      repository.messages = [
        capturedMessage(outcome: ArtifactOutcomes.promoted, billId: 'bill-7'),
      ];

      await pumpScreen(tester, scopes: ['view']);

      expect(find.text('boleto.pdf'), findsOneWidget);
      expect(inRow('Sua fatura chegou', 'Virou boleto'), findsOneWidget);
    });

    // Quem decidiu "não reconheço" sai da fila, e o item continua existindo —
    // o rótulo é português, não o nome de arame do Smart Enum do servidor.
    testWidgets('labels the dismissed attachment in Portuguese',
        (tester) async {
      repository.messages = [
        capturedMessage(
          outcome: ArtifactOutcomes.dismissed,
          artifacts: [
            capturedArtifact(
              fileName: 'boleto.pdf',
              outcome: ArtifactOutcomes.dismissed,
              captureItemId: 'item-4',
            ),
            capturedArtifact(
              fileName: 'recibo.pdf',
              outcome: ArtifactOutcomes.dismissed,
              captureItemId: 'item-5',
            ),
          ],
        ),
      ];

      await pumpScreen(tester, scopes: ['view']);

      expect(find.text('Dismissed'), findsNothing);
      expect(
        inRow('Sua fatura chegou', 'Reprovado'),
        findsNWidgets(3),
      );
    });

    // Sem o escopo de recaptura o botão some — esconder é para falta de
    // permissão, desabilitar é para estado do cadastro.
    testWidgets('hides the recapture button without the scope', (tester) async {
      repository.messages = [capturedMessage()];

      await pumpScreen(tester, scopes: ['view']);

      expect(find.text('Reprocessar'), findsNothing);
    });

    testWidgets('shows the recapture button with the scope', (tester) async {
      repository.messages = [capturedMessage()];

      await pumpScreen(tester, scopes: ['view', 'recapture']);

      expect(find.text('Reprocessar'), findsOneWidget);
    });

    // O aviso sobre o destino do boleto só aparece quando há boleto — dizer
    // isso sempre treinaria a pessoa a ignorar o diálogo. O texto espelha a
    // regra do servidor: pendente é cancelado e recriado, decidido bloqueia.
    testWidgets('warns about the existing bill before recapturing',
        (tester) async {
      repository.messages = [
        capturedMessage(outcome: ArtifactOutcomes.promoted, billId: 'bill-7'),
      ];
      await pumpScreen(tester, scopes: ['view', 'recapture']);

      await tester.tap(find.text('Reprocessar'));
      await tester.pumpAndSettle();

      expect(
        find.textContaining('cancelado e recriado pela nova leitura'),
        findsOneWidget,
      );
      expect(
        find.textContaining('aprovado, agendado ou pago'),
        findsOneWidget,
      );
    });

    // Sem permissão de gestão o prazo continua à vista, só não editável.
    testWidgets('shows the retention window without the manage scope',
        (tester) async {
      repository.messages = [capturedMessage()];

      await pumpScreen(tester, scopes: ['view']);

      expect(find.textContaining('não é purgado'), findsOneWidget);
      expect(find.byType(Switch), findsNothing);
    });

    testWidgets('offers the retention control with the manage scope',
        (tester) async {
      repository.messages = [capturedMessage()];

      await pumpScreen(tester, scopes: ['view', 'manage']);

      expect(find.byType(Switch), findsOneWidget);
      expect(find.text('90 d'), findsOneWidget);
    });

    testWidgets('an empty log explains that it fills itself', (tester) async {
      await pumpScreen(tester, scopes: ['view']);

      expect(find.textContaining('se enche sozinho'), findsOneWidget);
    });
  });
}
