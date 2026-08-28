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
            onOpenBill: (_) {},
            onOpenCaptureItem: (_) {},
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

    // O aviso de que o boleto não é apagado só aparece quando há boleto — dizer
    // isso sempre treinaria a pessoa a ignorar o diálogo.
    testWidgets('warns about the existing bill before recapturing',
        (tester) async {
      repository.messages = [
        capturedMessage(outcome: ArtifactOutcomes.promoted, billId: 'bill-7'),
      ];
      await pumpScreen(tester, scopes: ['view', 'recapture']);

      await tester.tap(find.text('Reprocessar'));
      await tester.pumpAndSettle();

      expect(find.textContaining('boleto NÃO é apagado'), findsOneWidget);
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
