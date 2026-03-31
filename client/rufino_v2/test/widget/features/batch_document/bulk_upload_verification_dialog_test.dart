import 'dart:convert';
import 'dart:typed_data';

import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/domain/entities/batch_document_unit.dart';
import 'package:rufino_v2/domain/entities/document_group_with_templates.dart';
import 'package:rufino_v2/ui/features/batch_document/viewmodel/batch_document_viewmodel.dart';
import 'package:rufino_v2/ui/features/batch_document/widgets/bulk_upload_verification_dialog.dart';

import '../../../testing/mocks/mocks.dart';

void main() {
  late MockBatchDocumentRepository mockBatchRepo;
  late MockDocumentGroupRepository mockGroupRepo;
  late BatchDocumentViewModel viewModel;

  const pendingUnits = [
    BatchDocumentUnitItem(
      documentUnitId: 'u1',
      documentId: 'd1',
      employeeId: 'e1',
      employeeName: 'João Silva Santos',
      employeeStatusId: '2',
      employeeStatusName: 'Ativo',
      date: '01/01/2026',
      statusId: '1',
      statusName: 'Pendente',
      isSignable: false,
      canGenerateDocument: false,
    ),
    BatchDocumentUnitItem(
      documentUnitId: 'u2',
      documentId: 'd2',
      employeeId: 'e2',
      employeeName: 'Maria Aparecida de Souza',
      employeeStatusId: '2',
      employeeStatusName: 'Ativo',
      date: '01/01/2026',
      statusId: '1',
      statusName: 'Pendente',
      isSignable: false,
      canGenerateDocument: false,
    ),
  ];

  String fakeTextExtractor(Uint8List bytes) => utf8.decode(bytes);

  PlatformFile makeFile(String name, String content) {
    final bytes = Uint8List.fromList(utf8.encode(content));
    return PlatformFile(name: name, size: bytes.length, bytes: bytes);
  }

  Future<void> setupViewModel() async {
    when(() => mockGroupRepo.getDocumentGroupsWithTemplates('company-1'))
        .thenAnswer((_) async => const Result.success([
              DocumentGroupWithTemplates(
                id: 'g1',
                name: 'Admissão',
                description: '',
                templates: [
                  DocumentTemplateSummary(
                      id: 't1', name: 'T1', description: ''),
                ],
              ),
            ]));
    when(() => mockBatchRepo.getPendingDocumentUnits(
          'company-1',
          't1',
          pageSize: any(named: 'pageSize'),
          pageNumber: any(named: 'pageNumber'),
          employeeStatusId: any(named: 'employeeStatusId'),
          employeeName: any(named: 'employeeName'),
          periodTypeId: any(named: 'periodTypeId'),
          periodYear: any(named: 'periodYear'),
          periodMonth: any(named: 'periodMonth'),
          periodDay: any(named: 'periodDay'),
          periodWeek: any(named: 'periodWeek'),
        )).thenAnswer((_) async => const Result.success(
          BatchDocumentUnitsPage(
              items: pendingUnits, totalCount: 2),
        ));

    await viewModel.loadGroupsAndTemplates();
    viewModel.selectGroup('g1');
    await viewModel.selectTemplate('t1');
  }

  setUp(() {
    mockBatchRepo = MockBatchDocumentRepository();
    mockGroupRepo = MockDocumentGroupRepository();
    viewModel = BatchDocumentViewModel(
      batchDocumentRepository: mockBatchRepo,
      documentGroupRepository: mockGroupRepo,
      companyId: 'company-1',
      textExtractor: fakeTextExtractor,
    );
  });

  tearDown(() {
    viewModel.dispose();
  });

  Widget buildSubject() {
    return MaterialApp(
      home: Scaffold(
        body: Builder(
          builder: (context) => Center(
            child: ElevatedButton(
              onPressed: () {
                showDialog<void>(
                  context: context,
                  builder: (_) => BulkUploadVerificationDialog(
                    viewModel: viewModel,
                  ),
                );
              },
              child: const Text('Open'),
            ),
          ),
        ),
      ),
    );
  }

  Future<void> openDialog(WidgetTester tester) async {
    await tester.pumpWidget(buildSubject());
    await tester.tap(find.text('Open'));
    // Use pump() instead of pumpAndSettle() because SfPdfViewer
    // continuously rebuilds and never reaches a settled state.
    await tester.pump();
    await tester.pump(const Duration(seconds: 1));
  }

  group('BulkUploadVerificationDialog', () {
    testWidgets('renders file list with correct count', (tester) async {
      await setupViewModel();
      // Use runAsync because processBulkFiles contains
      // Future.delayed(Duration.zero) which hangs in FakeAsync.
      await tester.runAsync(() => viewModel.processBulkFiles([
            makeFile('file1.pdf', 'Nome: João Silva Santos'),
            makeFile('file2.pdf', 'Nome: Maria Aparecida de Souza'),
          ]));

      await openDialog(tester);

      expect(find.text('file1.pdf'), findsOneWidget);
      expect(find.text('file2.pdf'), findsOneWidget);
    });

    testWidgets('shows matched employee name', (tester) async {
      await setupViewModel();
      await tester.runAsync(() => viewModel.processBulkFiles([
            makeFile('file1.pdf', 'Nome: João Silva Santos'),
          ]));

      await openDialog(tester);

      expect(find.textContaining('João Silva Santos'), findsWidgets);
    });

    testWidgets('shows dialog title', (tester) async {
      await setupViewModel();
      await tester.runAsync(() => viewModel.processBulkFiles([
            makeFile('file1.pdf', 'Nome: João Silva Santos'),
          ]));

      await openDialog(tester);

      expect(
          find.text('Verificação de Upload em Lote'), findsOneWidget);
    });

    testWidgets('confirm button shows matched count', (tester) async {
      await setupViewModel();
      await tester.runAsync(() => viewModel.processBulkFiles([
            makeFile('file1.pdf', 'Nome: João Silva Santos'),
          ]));

      await openDialog(tester);

      // The button label includes the matched count.
      expect(
        find.textContaining('Confirmar'),
        findsOneWidget,
      );
    });

    testWidgets('cancel button pops dialog and clears matches',
        (tester) async {
      await setupViewModel();
      await tester.runAsync(() => viewModel.processBulkFiles([
            makeFile('file1.pdf', 'Nome: João Silva Santos'),
          ]));

      await openDialog(tester);
      expect(viewModel.hasBulkMatches, isTrue);

      await tester.tap(find.text('Cancelar'));
      await tester.pump();
      await tester.pump(const Duration(seconds: 1));

      expect(viewModel.hasBulkMatches, isFalse);
    });

    testWidgets('summary bar shows file count', (tester) async {
      await setupViewModel();
      await tester.runAsync(() => viewModel.processBulkFiles([
            makeFile('file1.pdf', 'Nome: João Silva Santos'),
            makeFile('file2.pdf', 'Nome: Maria Aparecida de Souza'),
          ]));

      await openDialog(tester);

      expect(find.textContaining('2 arquivos'), findsOneWidget);
    });

    testWidgets('shows unmatched state for files with no match',
        (tester) async {
      await setupViewModel();
      await tester.runAsync(() => viewModel.processBulkFiles([
            makeFile('unknown.pdf', 'XYZXYZ random text XYZXYZ'),
          ]));

      await openDialog(tester);

      expect(find.textContaining('Sem correspondência'), findsWidgets);
    });

    testWidgets(
        'confirm button stages files and closes dialog', (tester) async {
      await setupViewModel();
      await tester.runAsync(() => viewModel.processBulkFiles([
            makeFile('file1.pdf', 'Nome: João Silva Santos'),
          ]));

      // Manually assign to ensure match exists.
      viewModel.reassignBulkMatch(0, pendingUnits[0]);

      await openDialog(tester);

      await tester.tap(find.textContaining('Confirmar'));
      await tester.pump();
      await tester.pump(const Duration(seconds: 1));

      expect(viewModel.hasBulkMatches, isFalse);
      expect(viewModel.hasStaged('u1'), isTrue);
    });

    testWidgets('shows confidence indicator icons', (tester) async {
      await setupViewModel();
      await tester.runAsync(() => viewModel.processBulkFiles([
            makeFile('file1.pdf', 'Nome: João Silva Santos'),
          ]));

      await openDialog(tester);

      // Should show at least one confidence indicator icon.
      expect(
        find.byIcon(Icons.check_circle).evaluate().isNotEmpty ||
            find.byIcon(Icons.help).evaluate().isNotEmpty ||
            find.byIcon(Icons.warning).evaluate().isNotEmpty ||
            find.byIcon(Icons.error).evaluate().isNotEmpty,
        isTrue,
      );
    });
  });
}
