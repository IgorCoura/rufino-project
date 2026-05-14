import 'dart:async';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/batch_document_unit.dart';
import 'package:rufino_v2/ui/features/batch_document/widgets/confirm_document_dates_dialog.dart';

BatchDocumentUnitItem _unit({
  required String id,
  required String name,
  String date = '15/03/2026',
  String statusId = '1',
  String statusName = 'Pendente',
  Period? period,
}) {
  return BatchDocumentUnitItem(
    documentUnitId: id,
    documentId: 'd-$id',
    employeeId: 'e-$id',
    employeeName: name,
    employeeStatusId: '2',
    employeeStatusName: 'Ativo',
    date: date,
    statusId: statusId,
    statusName: statusName,
    period: period,
    isSignable: true,
    canGenerateDocument: false,
  );
}

AttachedDocumentBytes _attached({String fileName = 'doc.pdf'}) =>
    AttachedDocumentBytes(
      bytes: Uint8List.fromList(const [1, 2, 3]),
      fileName: fileName,
    );

void main() {
  testWidgets('renders title, confirm label and one row per item',
      (tester) async {
    final items = [
      _unit(id: '1', name: 'João Silva'),
      _unit(id: '2', name: 'Maria Santos'),
      _unit(id: '3', name: 'Carlos Lima'),
    ];

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) => ElevatedButton(
              onPressed: () => showConfirmDocumentDatesDialog(
                context,
                title: 'Confirmar Envio',
                confirmLabel: 'Enviar (3)',
                icon: Icons.cloud_upload_outlined,
                items: items,
              ),
              child: const Text('open'),
            ),
          ),
        ),
      ),
    );
    await tester.tap(find.text('open'));
    await tester.pumpAndSettle();

    expect(find.text('Confirmar Envio'), findsOneWidget);
    expect(find.text('Enviar (3)'), findsOneWidget);
    expect(find.text('João Silva'), findsOneWidget);
    expect(find.text('Maria Santos'), findsOneWidget);
    expect(find.text('Carlos Lima'), findsOneWidget);
  });

  testWidgets('renders 5 columns when attachments are provided',
      (tester) async {
    final items = [_unit(id: '1', name: 'João Silva')];
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) => ElevatedButton(
              onPressed: () => showConfirmDocumentDatesDialog(
                context,
                title: 'Confirmar Envio',
                confirmLabel: 'Enviar (1)',
                icon: Icons.cloud_upload_outlined,
                items: items,
                attachments: {'1': _attached()},
                extractor: ({required bytes, required fileName}) async => null,
              ),
              child: const Text('open'),
            ),
          ),
        ),
      ),
    );
    await tester.tap(find.text('open'));
    await tester.pumpAndSettle();

    expect(find.text('Funcionário'), findsOneWidget);
    expect(find.text('Competência'), findsOneWidget);
    expect(find.text('Data'), findsOneWidget);
    expect(find.text('Data do Documento'), findsOneWidget);
    expect(find.text('Status'), findsOneWidget);
  });

  testWidgets('renders 4 columns when attachments are omitted',
      (tester) async {
    final items = [_unit(id: '1', name: 'João Silva')];
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) => ElevatedButton(
              onPressed: () => showConfirmDocumentDatesDialog(
                context,
                title: 'Confirmar Geração',
                confirmLabel: 'Gerar (1)',
                icon: Icons.history_edu_outlined,
                items: items,
              ),
              child: const Text('open'),
            ),
          ),
        ),
      ),
    );
    await tester.tap(find.text('open'));
    await tester.pumpAndSettle();

    expect(find.text('Funcionário'), findsOneWidget);
    expect(find.text('Competência'), findsOneWidget);
    expect(find.text('Data'), findsOneWidget);
    expect(find.text('Data do Documento'), findsNothing);
    expect(find.text('Status'), findsOneWidget);
  });

  testWidgets('cancel returns false', (tester) async {
    final items = [_unit(id: '1', name: 'João Silva')];
    Future<bool>? future;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) => ElevatedButton(
              onPressed: () {
                future = showConfirmDocumentDatesDialog(
                  context,
                  title: 'Confirmar',
                  confirmLabel: 'Enviar (1)',
                  icon: Icons.cloud_upload_outlined,
                  items: items,
                );
              },
              child: const Text('open'),
            ),
          ),
        ),
      ),
    );
    await tester.tap(find.text('open'));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Cancelar'));
    await tester.pumpAndSettle();

    expect(await future, isFalse);
  });

  testWidgets('confirm returns true', (tester) async {
    final items = [_unit(id: '1', name: 'João Silva')];
    Future<bool>? future;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) => ElevatedButton(
              onPressed: () {
                future = showConfirmDocumentDatesDialog(
                  context,
                  title: 'Confirmar',
                  confirmLabel: 'Enviar (1)',
                  icon: Icons.cloud_upload_outlined,
                  items: items,
                );
              },
              child: const Text('open'),
            ),
          ),
        ),
      ),
    );
    await tester.tap(find.text('open'));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Enviar (1)'));
    await tester.pumpAndSettle();

    expect(await future, isTrue);
  });

  testWidgets('invalid date shows warning row and footer banner; confirm stays enabled',
      (tester) async {
    final items = [
      _unit(id: '1', name: 'Carlos Lima', date: '32/13/2026'),
    ];
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) => ElevatedButton(
              onPressed: () => showConfirmDocumentDatesDialog(
                context,
                title: 'Confirmar',
                confirmLabel: 'Enviar (1)',
                icon: Icons.cloud_upload_outlined,
                items: items,
              ),
              child: const Text('open'),
            ),
          ),
        ),
      ),
    );
    await tester.tap(find.text('open'));
    await tester.pumpAndSettle();

    expect(find.byIcon(Icons.warning_amber_rounded), findsWidgets);
    expect(
      find.textContaining('1 documento(s) com data inválida'),
      findsOneWidget,
    );

    final confirmButtons = tester
        .widgetList<FilledButton>(
          find.byWidgetPredicate((w) => w is FilledButton),
        )
        .toList();
    expect(confirmButtons, isNotEmpty);
    expect(
      confirmButtons.any((b) => b.onPressed != null),
      isTrue,
      reason: 'Confirmar deve continuar habilitado mesmo com data inválida.',
    );
  });

  testWidgets('divergent extracted date shows priority icon and footer counter',
      (tester) async {
    final items = [
      _unit(id: '1', name: 'João Silva', date: '15/03/2026'),
    ];
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) => ElevatedButton(
              onPressed: () => showConfirmDocumentDatesDialog(
                context,
                title: 'Confirmar',
                confirmLabel: 'Enviar (1)',
                icon: Icons.cloud_upload_outlined,
                items: items,
                attachments: {'1': _attached()},
                extractor: ({required bytes, required fileName}) async =>
                    '12/03/2026',
              ),
              child: const Text('open'),
            ),
          ),
        ),
      ),
    );
    await tester.tap(find.text('open'));
    await tester.pumpAndSettle();

    expect(find.byIcon(Icons.priority_high), findsOneWidget);
    expect(
      find.textContaining('1 documento(s) com data divergente'),
      findsOneWidget,
    );
  });

  testWidgets('shows per-row spinner while extraction is in flight',
      (tester) async {
    final items = [_unit(id: '1', name: 'João Silva')];
    final completer = Completer<String?>();
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) => ElevatedButton(
              onPressed: () => showConfirmDocumentDatesDialog(
                context,
                title: 'Confirmar',
                confirmLabel: 'Enviar (1)',
                icon: Icons.cloud_upload_outlined,
                items: items,
                attachments: {'1': _attached()},
                extractor: ({required bytes, required fileName}) =>
                    completer.future,
              ),
              child: const Text('open'),
            ),
          ),
        ),
      ),
    );
    await tester.tap(find.text('open'));
    await tester.pump();
    await tester.pump();

    expect(find.byType(CircularProgressIndicator), findsWidgets);

    completer.complete('15/03/2026');
    await tester.pumpAndSettle();

    expect(find.byType(CircularProgressIndicator), findsNothing);
  });
}
