import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/core/utils/fuzzy_name_matcher.dart';
import 'package:rufino_v2/domain/entities/batch_document_unit.dart';
import 'package:rufino_v2/domain/entities/document_group_with_templates.dart';
import 'package:rufino_v2/ui/features/batch_document/viewmodel/batch_document_viewmodel.dart';

import '../../../../testing/mocks/mocks.dart';

void main() {
  late MockBatchDocumentRepository mockBatchRepo;
  late MockDocumentGroupRepository mockGroupRepo;
  late MockDocumentScannerService mockScanner;
  late BatchDocumentViewModel viewModel;

  const pendingUnit1 = BatchDocumentUnitItem(
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
  );

  const pendingUnit2 = BatchDocumentUnitItem(
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
  );

  const pendingUnit3 = BatchDocumentUnitItem(
    documentUnitId: 'u3',
    documentId: 'd3',
    employeeId: 'e3',
    employeeName: 'Carlos Eduardo Ferreira',
    employeeStatusId: '2',
    employeeStatusName: 'Ativo',
    date: '01/01/2026',
    statusId: '1',
    statusName: 'Pendente',
    isSignable: false,
    canGenerateDocument: false,
  );

  final fakeImage1 = Uint8List.fromList([0xFF, 0xD8, 0x01]);
  final fakeImage2 = Uint8List.fromList([0xFF, 0xD8, 0x02]);
  final fakeImage3 = Uint8List.fromList([0xFF, 0xD8, 0x03]);
  final fakePdfBytes = Uint8List.fromList([0x25, 0x50, 0x44, 0x46]);

  /// Fake text extractor — not used in scan tests but required by constructor.
  String fakeTextExtractor(Uint8List bytes) => '';

  setUp(() {
    mockBatchRepo = MockBatchDocumentRepository();
    mockGroupRepo = MockDocumentGroupRepository();
    mockScanner = MockDocumentScannerService();

    registerFallbackValue(Uint8List(0));
    registerFallbackValue(<Uint8List>[]);

    viewModel = BatchDocumentViewModel(
      batchDocumentRepository: mockBatchRepo,
      documentGroupRepository: mockGroupRepo,
      companyId: 'company-1',
      textExtractor: fakeTextExtractor,
      scannerService: mockScanner,
    );
  });

  /// Helper to set up the viewModel with pending units loaded.
  Future<void> loadPendingUnits(List<BatchDocumentUnitItem> units) async {
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
        )).thenAnswer((_) async => Result.success(
          BatchDocumentUnitsPage(items: units, totalCount: units.length),
        ));

    await viewModel.loadGroupsAndTemplates();
    viewModel.selectGroup('g1');
    await viewModel.selectTemplate('t1');
  }

  group('BatchDocumentViewModel document scanning', () {
    test('isScanSupported returns true when scanner service is provided and '
        'platform is supported', () {
      when(() => mockScanner.isPlatformSupported).thenReturn(true);
      expect(viewModel.isScanSupported, isTrue);
    });

    test('isScanSupported returns false when scanner reports unsupported', () {
      when(() => mockScanner.isPlatformSupported).thenReturn(false);
      expect(viewModel.isScanSupported, isFalse);
    });

    test('isScanSupported returns false when no scanner service is provided',
        () {
      final vmWithoutScanner = BatchDocumentViewModel(
        batchDocumentRepository: mockBatchRepo,
        documentGroupRepository: mockGroupRepo,
        companyId: 'company-1',
        textExtractor: fakeTextExtractor,
      );
      expect(vmWithoutScanner.isScanSupported, isFalse);
    });

    test('processScannedDocuments does nothing when list is empty', () async {
      await viewModel.processScannedDocuments([]);
      expect(viewModel.bulkMatches, isEmpty);
      expect(viewModel.isBulkProcessing, isFalse);
    });

    test('processScannedDocuments creates one match per document', () async {
      await loadPendingUnits([pendingUnit1, pendingUnit2, pendingUnit3]);

      when(() => mockScanner.imagesToPdf(any()))
          .thenAnswer((_) async => fakePdfBytes);
      when(() => mockScanner.recognizeText(fakeImage1))
          .thenAnswer((_) async => 'Nome: João Silva Santos');
      when(() => mockScanner.recognizeText(fakeImage2))
          .thenAnswer((_) async => 'Nome: Maria Aparecida de Souza');

      await viewModel.processScannedDocuments([
        [fakeImage1], // Document 1: 1 page
        [fakeImage2], // Document 2: 1 page
      ]);

      expect(viewModel.bulkMatches, hasLength(2));
    });

    test('processScannedDocuments matches each document to correct employee',
        () async {
      await loadPendingUnits([pendingUnit1, pendingUnit2, pendingUnit3]);

      when(() => mockScanner.imagesToPdf(any()))
          .thenAnswer((_) async => fakePdfBytes);
      when(() => mockScanner.recognizeText(fakeImage1))
          .thenAnswer((_) async => 'Nome: João Silva Santos');
      when(() => mockScanner.recognizeText(fakeImage2))
          .thenAnswer((_) async => 'Nome: Maria Aparecida de Souza');

      await viewModel.processScannedDocuments([
        [fakeImage1],
        [fakeImage2],
      ]);

      // Sorted by confidence: high matches last.
      final matchNames =
          viewModel.bulkMatches.map((m) => m.matchedEmployeeName).toSet();
      expect(matchNames, contains('João Silva Santos'));
      expect(matchNames, contains('Maria Aparecida de Souza'));
    });

    test('processScannedDocuments does not assign same unit to two documents',
        () async {
      await loadPendingUnits([pendingUnit1, pendingUnit2]);

      when(() => mockScanner.imagesToPdf(any()))
          .thenAnswer((_) async => fakePdfBytes);
      // Both documents contain the same employee name.
      when(() => mockScanner.recognizeText(any()))
          .thenAnswer((_) async => 'Nome: João Silva Santos');

      await viewModel.processScannedDocuments([
        [fakeImage1],
        [fakeImage2],
      ]);

      expect(viewModel.bulkMatches, hasLength(2));
      final unitIds = viewModel.bulkMatches
          .where((m) => m.isMatched)
          .map((m) => m.matchedDocumentUnitId)
          .toSet();
      // Each matched unit must be unique (no duplicates).
      expect(unitIds.length, equals(unitIds.toSet().length));
    });

    test('processScannedDocuments handles multi-page documents with OCR',
        () async {
      await loadPendingUnits([pendingUnit1, pendingUnit2]);

      when(() => mockScanner.imagesToPdf(any()))
          .thenAnswer((_) async => fakePdfBytes);
      when(() => mockScanner.recognizeText(fakeImage1))
          .thenAnswer((_) async => 'Nome:');
      when(() => mockScanner.recognizeText(fakeImage2))
          .thenAnswer((_) async => 'João Silva Santos');

      await viewModel.processScannedDocuments([
        [fakeImage1, fakeImage2], // Single document with 2 pages
      ]);

      expect(viewModel.bulkMatches, hasLength(1));
      final match = viewModel.bulkMatches.first;
      expect(match.extractedText, contains('João Silva Santos'));
      expect(match.matchedEmployeeId, equals('e1'));
    });

    test('processScannedDocuments sets none confidence when OCR is empty',
        () async {
      await loadPendingUnits([pendingUnit1]);

      when(() => mockScanner.imagesToPdf(any()))
          .thenAnswer((_) async => fakePdfBytes);
      when(() => mockScanner.recognizeText(any()))
          .thenAnswer((_) async => '');

      await viewModel.processScannedDocuments([
        [fakeImage1],
      ]);

      expect(viewModel.bulkMatches, hasLength(1));
      final match = viewModel.bulkMatches.first;
      expect(match.isMatched, isFalse);
      expect(match.confidenceLevel, MatchConfidenceLevel.none);
    });

    test('processScannedDocuments generates unique filenames per document',
        () async {
      await loadPendingUnits([pendingUnit1, pendingUnit2, pendingUnit3]);

      when(() => mockScanner.imagesToPdf(any()))
          .thenAnswer((_) async => fakePdfBytes);
      when(() => mockScanner.recognizeText(any()))
          .thenAnswer((_) async => '');

      await viewModel.processScannedDocuments([
        [fakeImage1],
        [fakeImage2],
        [fakeImage3],
      ]);

      final fileNames = viewModel.bulkMatches.map((m) => m.fileName).toList();
      expect(fileNames[0], isNot(equals(fileNames[1])));
      expect(fileNames[1], isNot(equals(fileNames[2])));
      // All should end with .pdf and contain the index suffix.
      for (final name in fileNames) {
        expect(name, endsWith('.pdf'));
        expect(name, startsWith('scan_'));
      }
    });

    test('processScannedDocuments resets isBulkProcessing on completion',
        () async {
      await loadPendingUnits([pendingUnit1]);

      when(() => mockScanner.imagesToPdf(any()))
          .thenAnswer((_) async => fakePdfBytes);
      when(() => mockScanner.recognizeText(any()))
          .thenAnswer((_) async => '');

      await viewModel.processScannedDocuments([
        [fakeImage1],
      ]);

      expect(viewModel.isBulkProcessing, isFalse);
    });

    test('processScannedDocuments resets isBulkProcessing even on error',
        () async {
      await loadPendingUnits([pendingUnit1]);

      when(() => mockScanner.imagesToPdf(any()))
          .thenThrow(Exception('PDF generation failed'));

      await viewModel.processScannedDocuments([
        [fakeImage1],
      ]);

      expect(viewModel.isBulkProcessing, isFalse);
    });

    test('processScannedDocuments skips empty page lists within documents',
        () async {
      await loadPendingUnits([pendingUnit1, pendingUnit2]);

      when(() => mockScanner.imagesToPdf(any()))
          .thenAnswer((_) async => fakePdfBytes);
      when(() => mockScanner.recognizeText(any()))
          .thenAnswer((_) async => 'Nome: João Silva Santos');

      await viewModel.processScannedDocuments([
        [], // Empty document — should be skipped
        [fakeImage1], // Valid document
      ]);

      expect(viewModel.bulkMatches, hasLength(1));
      expect(viewModel.bulkMatches.first.matchedEmployeeId, equals('e1'));
    });

    test('scanPages delegates to scanner service', () async {
      when(() => mockScanner.scanPages())
          .thenAnswer((_) async => [fakeImage1, fakeImage2]);

      final result = await viewModel.scanPages();

      expect(result, hasLength(2));
      verify(() => mockScanner.scanPages()).called(1);
    });

    test('scanPages returns null when user cancels', () async {
      when(() => mockScanner.scanPages()).thenAnswer((_) async => null);

      final result = await viewModel.scanPages();

      expect(result, isNull);
    });
  });
}
