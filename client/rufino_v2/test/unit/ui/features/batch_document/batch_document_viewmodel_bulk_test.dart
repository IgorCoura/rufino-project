import 'dart:convert';
import 'dart:typed_data';

import 'package:file_picker/file_picker.dart';
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

  /// Fake text extractor that decodes file bytes as UTF-8 text.
  String fakeTextExtractor(Uint8List bytes) => utf8.decode(bytes);

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

  /// Creates a PlatformFile with the given name and text content as bytes.
  /// Note: These are plain text bytes, not actual PDF bytes. The PDF
  /// extractor will return empty string, but the fuzzy matcher will still
  /// attempt to match using the fallback (full text as a single fragment).
  PlatformFile makePlainTextFile(String name, String content) {
    final bytes = Uint8List.fromList(utf8.encode(content));
    return PlatformFile(
      name: name,
      size: bytes.length,
      bytes: bytes,
    );
  }

  group('BatchDocumentViewModel bulk upload', () {
    test('processBulkFiles populates bulkMatches with correct count',
        () async {
      await loadPendingUnits([pendingUnit1, pendingUnit2, pendingUnit3]);

      final files = [
        makePlainTextFile('file1.pdf', 'Nome: João Silva Santos'),
        makePlainTextFile('file2.pdf', 'Nome: Maria Aparecida de Souza'),
      ];

      await viewModel.processBulkFiles(files);

      expect(viewModel.bulkMatches.length, equals(2));
      expect(viewModel.isBulkProcessing, isFalse);
      expect(viewModel.hasBulkMatches, isTrue);
    });

    test('processBulkFiles matches files to correct employees', () async {
      await loadPendingUnits([pendingUnit1, pendingUnit2, pendingUnit3]);

      final files = [
        makePlainTextFile('file1.pdf', 'Nome: João Silva Santos'),
      ];

      await viewModel.processBulkFiles(files);

      final match = viewModel.bulkMatches.first;
      expect(match.matchedEmployeeId, equals('e1'));
      expect(match.matchedEmployeeName, equals('João Silva Santos'));
      expect(match.isMatched, isTrue);
    });

    test('processBulkFiles excludes already-staged units from candidates',
        () async {
      await loadPendingUnits([pendingUnit1, pendingUnit2]);

      // Stage a file for unit 1 first.
      viewModel.stageFile(
        'u1',
        'd1',
        'e1',
        Uint8List.fromList([1, 2, 3]),
        'existing.pdf',
      );

      // Now bulk upload a file with João's name — should not match u1.
      final files = [
        makePlainTextFile('file1.pdf', 'Nome: João Silva Santos'),
      ];

      await viewModel.processBulkFiles(files);

      final match = viewModel.bulkMatches.first;
      // Should NOT match u1 since it is already staged.
      expect(match.matchedDocumentUnitId, isNot(equals('u1')));
    });

    test(
        'processBulkFiles does not assign same unit to multiple files',
        () async {
      await loadPendingUnits([pendingUnit1]);

      // Two files both containing João's name — only first should match.
      final files = [
        makePlainTextFile('file1.pdf', 'Nome: João Silva Santos'),
        makePlainTextFile('file2.pdf', 'Nome: João Silva Santos'),
      ];

      await viewModel.processBulkFiles(files);

      final assignedUnits = viewModel.bulkMatches
          .where((m) => m.isMatched)
          .map((m) => m.matchedDocumentUnitId)
          .toList();

      // Only one file should be assigned to u1.
      expect(assignedUnits.where((id) => id == 'u1').length, equals(1));
    });

    test('reassignBulkMatch updates the match at given index', () async {
      await loadPendingUnits([pendingUnit1, pendingUnit2]);

      final files = [
        makePlainTextFile('file1.pdf', 'some random text'),
      ];

      await viewModel.processBulkFiles(files);

      // Find the index of the file (should be 0).
      viewModel.reassignBulkMatch(0, pendingUnit2);

      expect(viewModel.bulkMatches[0].matchedEmployeeId, equals('e2'));
      expect(viewModel.bulkMatches[0].matchedEmployeeName,
          equals('Maria Aparecida de Souza'));
      expect(viewModel.bulkMatches[0].confidence, equals(1.0));
      expect(viewModel.bulkMatches[0].confidenceLevel,
          equals(MatchConfidenceLevel.high));
    });

    test('removeBulkMatch removes entry and decrements count', () async {
      await loadPendingUnits([pendingUnit1, pendingUnit2]);

      final files = [
        makePlainTextFile('file1.pdf', 'Nome: João Silva Santos'),
        makePlainTextFile('file2.pdf', 'Nome: Maria Aparecida de Souza'),
      ];

      await viewModel.processBulkFiles(files);
      expect(viewModel.bulkMatches.length, equals(2));

      viewModel.removeBulkMatch(0);
      expect(viewModel.bulkMatches.length, equals(1));
    });

    test('confirmBulkMatches stages matched files and clears bulk list',
        () async {
      await loadPendingUnits([pendingUnit1, pendingUnit2]);

      final files = [
        makePlainTextFile('file1.pdf', 'Nome: João Silva Santos'),
      ];

      await viewModel.processBulkFiles(files);

      // Manually assign to make sure it's matched.
      viewModel.reassignBulkMatch(0, pendingUnit1);

      viewModel.confirmBulkMatches();

      expect(viewModel.hasBulkMatches, isFalse);
      expect(viewModel.bulkMatches, isEmpty);
      expect(viewModel.hasStaged('u1'), isTrue);
      expect(viewModel.stagedFileName('u1'), equals('file1.pdf'));
    });

    test('cancelBulkMatches clears without staging', () async {
      await loadPendingUnits([pendingUnit1]);

      final files = [
        makePlainTextFile('file1.pdf', 'Nome: João Silva Santos'),
      ];

      await viewModel.processBulkFiles(files);
      expect(viewModel.hasBulkMatches, isTrue);

      viewModel.cancelBulkMatches();

      expect(viewModel.hasBulkMatches, isFalse);
      expect(viewModel.hasStaged('u1'), isFalse);
    });

    test('isBulkProcessing is true during processing and false after',
        () async {
      await loadPendingUnits([pendingUnit1]);

      var wasTrueDuringProcessing = false;
      viewModel.addListener(() {
        if (viewModel.isBulkProcessing) {
          wasTrueDuringProcessing = true;
        }
      });

      final files = [
        makePlainTextFile('file1.pdf', 'Nome: João Silva Santos'),
      ];

      await viewModel.processBulkFiles(files);

      expect(wasTrueDuringProcessing, isTrue);
      expect(viewModel.isBulkProcessing, isFalse);
    });

    test('bulkMatchedCount and bulkUnmatchedCount reflect current state',
        () async {
      await loadPendingUnits([pendingUnit1, pendingUnit2]);

      final files = [
        makePlainTextFile('file1.pdf', 'Nome: João Silva Santos'),
        makePlainTextFile('file2.pdf', 'completely unrelated content xyz'),
      ];

      await viewModel.processBulkFiles(files);

      // At least the first file should be matched.
      expect(viewModel.bulkMatchedCount, greaterThanOrEqualTo(1));
      expect(
        viewModel.bulkMatchedCount + viewModel.bulkUnmatchedCount,
        equals(2),
      );
    });

    test('processBulkFiles sorts matches worst-first', () async {
      await loadPendingUnits([pendingUnit1, pendingUnit2]);

      final files = [
        makePlainTextFile('matched.pdf', 'Nome: João Silva Santos'),
        makePlainTextFile('unmatched.pdf', 'XYZXYZ no name here XYZXYZ'),
      ];

      await viewModel.processBulkFiles(files);

      if (viewModel.bulkMatches.length == 2) {
        // Unmatched/low confidence should come before high confidence.
        // The sort order is: none, low, medium, high — so the first
        // item should have lower or equal confidence than the last.
        expect(
          viewModel.bulkMatches.first.confidence,
          lessThanOrEqualTo(viewModel.bulkMatches.last.confidence),
        );
      }
    });
  });
}
