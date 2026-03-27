import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/ui/core/widgets/error_dialog.dart';

void main() {
  group('showErrorSnackBar', () {
    testWidgets('shows all messages joined by newline', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Builder(
            builder: (context) => Scaffold(
              body: ElevatedButton(
                onPressed: () => showErrorSnackBar(
                  context,
                  messages: [
                    'O campo Nome é obrigatório.',
                    'O campo Email é inválido.',
                  ],
                ),
                child: const Text('Show'),
              ),
            ),
          ),
        ),
      );

      await tester.tap(find.text('Show'));
      await tester.pump();

      expect(find.byType(SnackBar), findsOneWidget);
      expect(
        find.text('O campo Nome é obrigatório.\nO campo Email é inválido.'),
        findsOneWidget,
      );
    });

    testWidgets('shows fallback message when messages is empty',
        (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Builder(
            builder: (context) => Scaffold(
              body: ElevatedButton(
                onPressed: () => showErrorSnackBar(
                  context,
                  messages: [],
                  fallbackMessage: 'Erro genérico.',
                ),
                child: const Text('Show'),
              ),
            ),
          ),
        ),
      );

      await tester.tap(find.text('Show'));
      await tester.pump();

      expect(find.byType(SnackBar), findsOneWidget);
      expect(find.text('Erro genérico.'), findsOneWidget);
    });

    testWidgets('shows default fallback when no fallback is provided',
        (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Builder(
            builder: (context) => Scaffold(
              body: ElevatedButton(
                onPressed: () => showErrorSnackBar(
                  context,
                  messages: [],
                ),
                child: const Text('Show'),
              ),
            ),
          ),
        ),
      );

      await tester.tap(find.text('Show'));
      await tester.pump();

      expect(find.text('Ocorreu um erro inesperado.'), findsOneWidget);
    });

    testWidgets('replaces current snackbar when called again', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Builder(
            builder: (context) => Scaffold(
              body: Column(
                children: [
                  ElevatedButton(
                    onPressed: () => showErrorSnackBar(
                      context,
                      messages: ['Primeiro erro.'],
                    ),
                    child: const Text('First'),
                  ),
                  ElevatedButton(
                    onPressed: () => showErrorSnackBar(
                      context,
                      messages: ['Segundo erro.'],
                    ),
                    child: const Text('Second'),
                  ),
                ],
              ),
            ),
          ),
        ),
      );

      await tester.tap(find.text('First'));
      await tester.pump();
      expect(find.text('Primeiro erro.'), findsOneWidget);

      await tester.tap(find.text('Second'));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 500));
      expect(find.text('Segundo erro.'), findsOneWidget);
    });
  });
}
