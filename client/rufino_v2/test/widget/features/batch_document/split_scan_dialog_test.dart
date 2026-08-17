import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/ui/features/batch_document/widgets/split_scan_dialog.dart';

void main() {
  /// Pumps a host screen whose button opens [open] and stores its result.
  Future<void> pumpHost<T>(
    WidgetTester tester,
    Future<T> Function(BuildContext context) open,
    void Function(T result) onResult,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Builder(
          builder: (context) => Scaffold(
            body: Center(
              child: ElevatedButton(
                onPressed: () async => onResult(await open(context)),
                child: const Text('Abrir'),
              ),
            ),
          ),
        ),
      ),
    );
    await tester.tap(find.text('Abrir'));
    await tester.pumpAndSettle();
  }

  group('showSplitScanDialog', () {
    testWidgets('states how many pages were captured', (tester) async {
      await pumpHost<int?>(
        tester,
        (context) => showSplitScanDialog(context, totalPages: 10),
        (_) {},
      );

      expect(find.text('Dividir digitalização'), findsOneWidget);
      expect(find.text('10 páginas capturadas.'), findsOneWidget);
    });

    testWidgets('previews the resulting documents for a valid value',
        (tester) async {
      await pumpHost<int?>(
        tester,
        (context) => showSplitScanDialog(context, totalPages: 10),
        (_) {},
      );

      await tester.enterText(find.byKey(const Key('split-pages-field')), '2');
      await tester.pump();

      expect(find.text('5 documentos de 2 páginas'), findsOneWidget);
    });

    testWidgets('uses the singular form when the split yields one document',
        (tester) async {
      await pumpHost<int?>(
        tester,
        (context) => showSplitScanDialog(context, totalPages: 4),
        (_) {},
      );

      await tester.enterText(find.byKey(const Key('split-pages-field')), '4');
      await tester.pump();

      expect(find.text('1 documento de 4 páginas'), findsOneWidget);
    });

    testWidgets('shows an error and keeps the dialog open when pages are '
        'left over', (tester) async {
      int? result;
      var completed = false;

      await pumpHost<int?>(
        tester,
        (context) => showSplitScanDialog(context, totalPages: 10),
        (value) {
          result = value;
          completed = true;
        },
      );

      await tester.enterText(find.byKey(const Key('split-pages-field')), '3');
      await tester.tap(find.byKey(const Key('split-scan-confirm')));
      await tester.pumpAndSettle();

      expect(
        find.text('10 páginas não podem ser divididas a cada 3 (sobram 1).'),
        findsOneWidget,
      );
      expect(find.text('Dividir digitalização'), findsOneWidget);
      expect(completed, isFalse);
      expect(result, isNull);
    });

    testWidgets('does not close when confirming without a value',
        (tester) async {
      var completed = false;

      await pumpHost<int?>(
        tester,
        (context) => showSplitScanDialog(context, totalPages: 10),
        (_) => completed = true,
      );

      await tester.tap(find.byKey(const Key('split-scan-confirm')));
      await tester.pumpAndSettle();

      expect(find.text('Informe quantas páginas por documento.'),
          findsOneWidget);
      expect(completed, isFalse);
    });

    testWidgets('returns the chosen page count when confirmed',
        (tester) async {
      int? result;

      await pumpHost<int?>(
        tester,
        (context) => showSplitScanDialog(context, totalPages: 10),
        (value) => result = value,
      );

      await tester.enterText(find.byKey(const Key('split-pages-field')), '2');
      await tester.tap(find.byKey(const Key('split-scan-confirm')));
      await tester.pumpAndSettle();

      expect(result, 2);
    });

    testWidgets('returns null when cancelled', (tester) async {
      int? result;
      var completed = false;

      await pumpHost<int?>(
        tester,
        (context) => showSplitScanDialog(context, totalPages: 10),
        (value) {
          result = value;
          completed = true;
        },
      );

      await tester.enterText(find.byKey(const Key('split-pages-field')), '2');
      await tester.tap(find.byKey(const Key('split-scan-cancel')));
      await tester.pumpAndSettle();

      expect(completed, isTrue);
      expect(result, isNull);
    });
  });

  group('showSplitResultDialog', () {
    testWidgets('describes the split it is confirming', (tester) async {
      await pumpHost<bool>(
        tester,
        (context) => showSplitResultDialog(
          context,
          documentCount: 5,
          pagesPerDocument: 2,
        ),
        (_) {},
      );

      expect(find.text('5 documentos'), findsOneWidget);
      expect(
        find.text(
          'A digitalização de 10 páginas foi dividida em 5 documentos '
          'de 2 páginas.',
        ),
        findsOneWidget,
      );
    });

    testWidgets('returns true when the user processes the documents',
        (tester) async {
      bool? result;

      await pumpHost<bool>(
        tester,
        (context) => showSplitResultDialog(
          context,
          documentCount: 5,
          pagesPerDocument: 2,
        ),
        (value) => result = value,
      );

      await tester.tap(find.byKey(const Key('split-result-process')));
      await tester.pumpAndSettle();

      expect(result, isTrue);
    });

    testWidgets('returns false when the user discards the scan',
        (tester) async {
      bool? result;

      await pumpHost<bool>(
        tester,
        (context) => showSplitResultDialog(
          context,
          documentCount: 5,
          pagesPerDocument: 2,
        ),
        (value) => result = value,
      );

      await tester.tap(find.byKey(const Key('split-result-discard')));
      await tester.pumpAndSettle();

      expect(result, isFalse);
    });
  });
}
