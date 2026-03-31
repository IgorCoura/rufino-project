import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/ui/features/batch_document/widgets/document_scan_dialog.dart';

void main() {
  group('DocumentScanDialog', () {
    testWidgets('renders app bar with title', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(body: SizedBox.shrink()),
        ),
      );

      // Show the dialog as a full-screen route.
      unawaited(
        showDialog<List<Uint8List>>(
          context: tester.element(find.byType(Scaffold)),
          builder: (_) => const DocumentScanDialog(),
        ),
      );
      // Use pump() instead of pumpAndSettle() because camera init
      // never completes in the test environment.
      await tester.pump();
      await tester.pump();

      expect(find.text('Digitalizar Documento'), findsOneWidget);
    });

    testWidgets('renders close button that pops with null', (tester) async {
      List<Uint8List>? result;

      await tester.pumpWidget(
        MaterialApp(
          home: Builder(
            builder: (context) => Scaffold(
              body: Center(
                child: ElevatedButton(
                  onPressed: () async {
                    result = await showDialog<List<Uint8List>>(
                      context: context,
                      builder: (_) => const DocumentScanDialog(),
                    );
                  },
                  child: const Text('Open'),
                ),
              ),
            ),
          ),
        ),
      );

      await tester.tap(find.text('Open'));
      // Pump frames to show dialog (camera init is async and won't settle).
      await tester.pump();
      await tester.pump();

      // Tap the close button.
      await tester.tap(find.byIcon(Icons.close));
      await tester.pump();
      await tester.pump();

      expect(result, isNull);
    });

    testWidgets('shows loading indicator during camera initialization',
        (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(body: SizedBox.shrink()),
        ),
      );

      unawaited(
        showDialog<List<Uint8List>>(
          context: tester.element(find.byType(Scaffold)),
          builder: (_) => const DocumentScanDialog(),
        ),
      );
      // Pump only one frame (camera still initializing).
      await tester.pump();

      expect(find.byType(CircularProgressIndicator), findsWidgets);
    });
  });
}

/// Helper to discard a future (avoids lint warnings).
void unawaited(Future<void> future) {}
