import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/core/errors/document_scanner_exception.dart';
import 'package:rufino_v2/ui/core/widgets/scanner_error_handler.dart';

void main() {
  group('presentScannerError', () {
    testWidgets(
      'shows a snackbar with the localized message for a plugin failure',
      (tester) async {
        await tester.pumpWidget(
          _harness(const ScannerPluginFailureException('boom')),
        );

        await tester.tap(find.text('trigger'));
        await tester.pump();

        expect(
          find.text('Não foi possível abrir o digitalizador. Tente novamente.'),
          findsOneWidget,
        );
      },
    );

    testWidgets(
      'shows a snackbar for a denied (non-permanent) permission failure',
      (tester) async {
        await tester.pumpWidget(
          _harness(const ScannerPermissionDeniedException()),
        );

        await tester.tap(find.text('trigger'));
        await tester.pump();

        expect(
          find.text('Permita o acesso à câmera para digitalizar documentos.'),
          findsOneWidget,
        );
      },
    );

    testWidgets(
      'shows a snackbar with the localized path-aware message for a file '
      'read failure',
      (tester) async {
        await tester.pumpWidget(
          _harness(
            const ScannerFileReadException('/tmp/page.jpg', 'sandbox error'),
          ),
        );

        await tester.tap(find.text('trigger'));
        await tester.pump();

        expect(
          find.text('Falha ao ler a imagem digitalizada. Tente novamente.'),
          findsOneWidget,
        );
      },
    );

    testWidgets(
      'shows an open-settings dialog instead of a snackbar when the '
      'permission was permanently denied',
      (tester) async {
        await tester.pumpWidget(
          _harness(const ScannerPermissionPermanentlyDeniedException()),
        );

        await tester.tap(find.text('trigger'));
        await tester.pumpAndSettle();

        expect(find.byType(AlertDialog), findsOneWidget);
        expect(find.text('Permissão de câmera necessária'), findsOneWidget);
        expect(find.text('Abrir Ajustes'), findsOneWidget);
        expect(find.text('Cancelar'), findsOneWidget);

        await tester.tap(find.text('Cancelar'));
        await tester.pumpAndSettle();

        expect(find.byType(AlertDialog), findsNothing);
      },
    );
  });
}

Widget _harness(DocumentScannerException exception) {
  return MaterialApp(
    home: Scaffold(
      body: Builder(
        builder: (context) => TextButton(
          onPressed: () => presentScannerError(context, exception),
          child: const Text('trigger'),
        ),
      ),
    ),
  );
}
