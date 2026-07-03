import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/domain/entities/batch_document_unit.dart';
import 'package:rufino_v2/domain/entities/document_group_with_templates.dart';
import 'package:rufino_v2/ui/features/batch_document/viewmodel/batch_document_viewmodel.dart';

import '../../../../testing/mocks/mocks.dart';

/// Characterization tests for the batch document operations that were
/// previously uncovered: the "Todos" multi-template fan-out
/// ([loadPendingUnits], [loadMissingEmployees], [batchCreateDocumentUnits]),
/// the generate/sign flows, the date-validation guards, and the
/// filter/pagination surface.
///
/// These lock in the CURRENT (serial) behavior so the upcoming parallelism
/// refactor can be verified against a known-good baseline.
void main() {
  late MockBatchDocumentRepository mockBatchRepo;
  late MockDocumentGroupRepository mockGroupRepo;
  late BatchDocumentViewModel viewModel;

  const allId = BatchDocumentViewModel.allTemplatesId;

  BatchDocumentUnitItem unit({
    required String id,
    required String employeeId,
    required String name,
    String date = '15/03/2026',
    bool signable = true,
    bool canGenerate = true,
  }) =>
      BatchDocumentUnitItem(
        documentUnitId: id,
        documentId: 'd-$id',
        employeeId: employeeId,
        employeeName: name,
        employeeStatusId: '2',
        employeeStatusName: 'Ativo',
        date: date,
        statusId: '1',
        statusName: 'Pendente',
        isSignable: signable,
        canGenerateDocument: canGenerate,
      );

  setUpAll(() {
    registerFallbackValue(<BatchDocumentUnitItem>[]);
    registerFallbackValue(<BatchUploadItem>[]);
    registerFallbackValue(<String>[]);
  });

  setUp(() {
    mockBatchRepo = MockBatchDocumentRepository();
    mockGroupRepo = MockDocumentGroupRepository();
    viewModel = BatchDocumentViewModel(
      batchDocumentRepository: mockBatchRepo,
      documentGroupRepository: mockGroupRepo,
      companyId: 'company-1',
    );
  });

  tearDown(() => viewModel.dispose());

  /// Stubs a group `g1` containing templates [templateIds] and loads it.
  Future<void> loadGroup(List<String> templateIds) async {
    when(() => mockGroupRepo.getDocumentGroupsWithTemplates('company-1'))
        .thenAnswer((_) async => Result.success([
              DocumentGroupWithTemplates(
                id: 'g1',
                name: 'Grupo',
                description: '',
                templates: [
                  for (final id in templateIds)
                    DocumentTemplateSummary(
                        id: id, name: id.toUpperCase(), description: ''),
                ],
              ),
            ]));
    await viewModel.loadGroupsAndTemplates();
    viewModel.selectGroup('g1');
  }

  /// Convenience matcher for the fully-named getPendingDocumentUnits stub.
  void stubPending(String templateId, Result<BatchDocumentUnitsPage> result) {
    when(() => mockBatchRepo.getPendingDocumentUnits(
          'company-1',
          templateId,
          employeeStatusId: any(named: 'employeeStatusId'),
          employeeName: any(named: 'employeeName'),
          periodTypeId: any(named: 'periodTypeId'),
          periodYear: any(named: 'periodYear'),
          periodMonth: any(named: 'periodMonth'),
          periodDay: any(named: 'periodDay'),
          periodWeek: any(named: 'periodWeek'),
          pageSize: any(named: 'pageSize'),
          pageNumber: any(named: 'pageNumber'),
        )).thenAnswer((_) async => result);
  }

  // ───────────────────────── loadPendingUnits "Todos" ─────────────────────

  group('loadPendingUnits with "Todos" (multi-template)', () {
    test('queries every template in the group and merges the results',
        () async {
      await loadGroup(['t1', 't2']);
      stubPending(
        't1',
        Result.success(BatchDocumentUnitsPage(
          items: [unit(id: 'u1', employeeId: 'e1', name: 'Ana')],
          totalCount: 1,
        )),
      );
      stubPending(
        't2',
        Result.success(BatchDocumentUnitsPage(
          items: [unit(id: 'u2', employeeId: 'e2', name: 'Bruno')],
          totalCount: 3,
        )),
      );

      await viewModel.selectTemplate(allId);

      expect(viewModel.selectedTemplateId, allId);
      expect(viewModel.isAllTemplatesSelected, isTrue);
      expect(viewModel.pendingUnits.map((u) => u.documentUnitId),
          containsAll(['u1', 'u2']));
      expect(viewModel.pendingUnits.length, 2);
      // Total is the SUM of each template's page total.
      expect(viewModel.totalCount, 4);
      expect(viewModel.status, BatchDocumentStatus.loaded);
    });

    test('fails the whole load and discards units when any template fails',
        () async {
      await loadGroup(['t1', 't2']);
      stubPending('t1', const Result.error('boom'));
      stubPending(
        't2',
        Result.success(BatchDocumentUnitsPage(
          items: [unit(id: 'u2', employeeId: 'e2', name: 'Bruno')],
          totalCount: 1,
        )),
      );

      await viewModel.selectTemplate(allId);

      // Fail-all policy: end-state is error with NO units, even though t2
      // succeeded. Templates are now queried concurrently (both requested).
      expect(viewModel.status, BatchDocumentStatus.error);
      expect(viewModel.errorMessage, isNotNull);
      expect(viewModel.pendingUnits, isEmpty);
      expect(viewModel.totalCount, 0);
      verify(() => mockBatchRepo.getPendingDocumentUnits(
            'company-1',
            't2',
            employeeStatusId: any(named: 'employeeStatusId'),
            employeeName: any(named: 'employeeName'),
            periodTypeId: any(named: 'periodTypeId'),
            periodYear: any(named: 'periodYear'),
            periodMonth: any(named: 'periodMonth'),
            periodDay: any(named: 'periodDay'),
            periodWeek: any(named: 'periodWeek'),
            pageSize: any(named: 'pageSize'),
            pageNumber: any(named: 'pageNumber'),
          )).called(1);
    });
  });

  // ─────────────────────────── loadMissingEmployees ───────────────────────

  group('loadMissingEmployees', () {
    void stubMissing(
      String templateId,
      Result<List<EmployeeMissingDocument>> result,
    ) {
      when(() => mockBatchRepo.getMissingEmployees(
            'company-1',
            templateId,
            employeeStatusId: any(named: 'employeeStatusId'),
            employeeName: any(named: 'employeeName'),
          )).thenAnswer((_) async => result);
    }

    EmployeeMissingDocument missing(String id, String name) =>
        EmployeeMissingDocument(
          employeeId: id,
          employeeName: name,
          employeeStatusId: '2',
          employeeStatusName: 'Ativo',
        );

    test('populates missingEmployees for a single template', () async {
      await loadGroup(['t1']);
      stubPending('t1',
          const Result.success(BatchDocumentUnitsPage(items: [], totalCount: 0)));
      await viewModel.selectTemplate('t1');

      stubMissing('t1', Result.success([missing('e1', 'Ana'), missing('e2', 'Bruno')]));

      await viewModel.loadMissingEmployees();

      expect(viewModel.missingEmployees.map((e) => e.employeeId),
          containsAll(['e1', 'e2']));
      expect(viewModel.missingEmployees.length, 2);
    });

    test('deduplicates employees across templates in "Todos" mode', () async {
      await loadGroup(['t1', 't2']);
      stubPending('t1',
          const Result.success(BatchDocumentUnitsPage(items: [], totalCount: 0)));
      stubPending('t2',
          const Result.success(BatchDocumentUnitsPage(items: [], totalCount: 0)));
      await viewModel.selectTemplate(allId);

      // e1 appears in BOTH templates and must be counted once.
      stubMissing('t1', Result.success([missing('e1', 'Ana'), missing('e2', 'Bruno')]));
      stubMissing('t2', Result.success([missing('e1', 'Ana'), missing('e3', 'Célia')]));

      await viewModel.loadMissingEmployees();

      final ids = viewModel.missingEmployees.map((e) => e.employeeId).toList();
      expect(ids, containsAll(['e1', 'e2', 'e3']));
      expect(ids.where((id) => id == 'e1').length, 1);
      expect(viewModel.missingEmployees.length, 3);
    });

    test('sets an error message when a template lookup fails', () async {
      await loadGroup(['t1']);
      stubPending('t1',
          const Result.success(BatchDocumentUnitsPage(items: [], totalCount: 0)));
      await viewModel.selectTemplate('t1');

      stubMissing('t1', const Result.error('network'));

      await viewModel.loadMissingEmployees();

      expect(viewModel.errorMessage, isNotNull);
    });
  });

  // ────────────────────────── batchCreateDocumentUnits ────────────────────

  group('batchCreateDocumentUnits', () {
    test('creates per template then reloads pending units', () async {
      await loadGroup(['t1', 't2']);
      stubPending('t1',
          const Result.success(BatchDocumentUnitsPage(items: [], totalCount: 0)));
      stubPending('t2',
          const Result.success(BatchDocumentUnitsPage(items: [], totalCount: 0)));
      await viewModel.selectTemplate(allId);

      when(() => mockBatchRepo.batchCreateDocumentUnits(
            'company-1',
            any(),
            any(),
          )).thenAnswer((_) async => const Result.success([]));

      await viewModel.batchCreateDocumentUnits(['e1', 'e2']);

      verify(() => mockBatchRepo.batchCreateDocumentUnits('company-1', 't1', ['e1', 'e2']))
          .called(1);
      verify(() => mockBatchRepo.batchCreateDocumentUnits('company-1', 't2', ['e1', 'e2']))
          .called(1);
      // Reload happens after creation (t1 + t2 initial + t1 + t2 reload).
      verify(() => mockBatchRepo.getPendingDocumentUnits(
            'company-1',
            't1',
            employeeStatusId: any(named: 'employeeStatusId'),
            employeeName: any(named: 'employeeName'),
            periodTypeId: any(named: 'periodTypeId'),
            periodYear: any(named: 'periodYear'),
            periodMonth: any(named: 'periodMonth'),
            periodDay: any(named: 'periodDay'),
            periodWeek: any(named: 'periodWeek'),
            pageSize: any(named: 'pageSize'),
            pageNumber: any(named: 'pageNumber'),
          )).called(2);
    });

    test('does nothing when the employee list is empty', () async {
      await loadGroup(['t1']);
      stubPending('t1',
          const Result.success(BatchDocumentUnitsPage(items: [], totalCount: 0)));
      await viewModel.selectTemplate('t1');

      await viewModel.batchCreateDocumentUnits([]);

      verifyNever(() => mockBatchRepo.batchCreateDocumentUnits(any(), any(), any()));
    });

    test('sets an error message when creation fails', () async {
      await loadGroup(['t1']);
      stubPending('t1',
          const Result.success(BatchDocumentUnitsPage(items: [], totalCount: 0)));
      await viewModel.selectTemplate('t1');

      when(() => mockBatchRepo.batchCreateDocumentUnits('company-1', 't1', any()))
          .thenAnswer((_) async => const Result.error('cannot create'));

      await viewModel.batchCreateDocumentUnits(['e1']);

      expect(viewModel.errorMessage, isNotNull);
    });
  });

  // ─────────────────────────────── uploadAllStaged ────────────────────────

  group('uploadAllStaged', () {
    test('blocks upload and reports invalid dates without calling the repo',
        () async {
      await loadGroup(['t1']);
      stubPending(
        't1',
        Result.success(BatchDocumentUnitsPage(
          items: [unit(id: 'u1', employeeId: 'e1', name: 'Ana', date: '32/13/2026')],
          totalCount: 1,
        )),
      );
      await viewModel.selectTemplate('t1');

      viewModel.stageFile('u1', 'd-u1', 'e1', Uint8List(3), 'a.pdf');

      await viewModel.uploadAllStaged();

      expect(viewModel.status, BatchDocumentStatus.error);
      expect(viewModel.errorMessage, contains('data inválida'));
      verifyNever(() => mockBatchRepo.uploadDocumentRange(any(), any()));
    });

    test('sets error status when the repository fails', () async {
      await loadGroup(['t1']);
      stubPending(
        't1',
        Result.success(BatchDocumentUnitsPage(
          items: [unit(id: 'u1', employeeId: 'e1', name: 'Ana')],
          totalCount: 1,
        )),
      );
      await viewModel.selectTemplate('t1');
      viewModel.stageFile('u1', 'd-u1', 'e1', Uint8List(3), 'a.pdf');

      when(() => mockBatchRepo.uploadDocumentRange('company-1', any()))
          .thenAnswer((_) async => const Result.error('upload failed'));

      await viewModel.uploadAllStaged();

      // NOTE (current behavior): the trailing loadPendingUnits() reload flips
      // status back to `loaded`, masking the transient `error`. The error
      // message from the failed upload is preserved, and files stay staged.
      expect(viewModel.errorMessage, isNotNull);
      expect(viewModel.status, BatchDocumentStatus.loaded);
      expect(viewModel.stagedFileCount, 1);
    });

    test('does nothing when there are no staged files', () async {
      await viewModel.uploadAllStaged();
      verifyNever(() => mockBatchRepo.uploadDocumentRange(any(), any()));
    });
  });

  // ──────────────────────────── uploadAllStagedToSign ─────────────────────

  group('uploadAllStagedToSign', () {
    Future<void> setupWithStagedValidUnit() async {
      await loadGroup(['t1']);
      stubPending(
        't1',
        Result.success(BatchDocumentUnitsPage(
          items: [unit(id: 'u1', employeeId: 'e1', name: 'Ana')],
          totalCount: 1,
        )),
      );
      await viewModel.selectTemplate('t1');
      viewModel.stageFile('u1', 'd-u1', 'e1', Uint8List(3), 'a.pdf');
    }

    test('does nothing when the global deadline is not set', () async {
      await setupWithStagedValidUnit();

      await viewModel.uploadAllStagedToSign();

      verifyNever(() => mockBatchRepo.uploadDocumentRangeToSign(
            any(), any(), any(), any()));
    });

    test('uploads and clears staged files on success', () async {
      await setupWithStagedValidUnit();
      viewModel.setGlobalSignDeadline('2026-04-01T00:00:00.000Z');
      viewModel.setGlobalReminderDays(5);

      when(() => mockBatchRepo.uploadDocumentRangeToSign(
            'company-1',
            any(),
            '2026-04-01T00:00:00.000Z',
            5,
          )).thenAnswer((_) async => const Result.success([
            BatchUploadResult(documentUnitId: 'u1', success: true),
          ]));

      await viewModel.uploadAllStagedToSign();

      // NOTE (current behavior): `uploadComplete` is transient — the trailing
      // reload settles status on `loaded`. Staging is cleared and results kept.
      expect(viewModel.status, BatchDocumentStatus.loaded);
      expect(viewModel.stagedFileCount, 0);
      expect(viewModel.uploadResults.length, 1);
    });

    test('blocks when a staged unit has an invalid date', () async {
      await loadGroup(['t1']);
      stubPending(
        't1',
        Result.success(BatchDocumentUnitsPage(
          items: [unit(id: 'u1', employeeId: 'e1', name: 'Ana', date: '99/99/9999')],
          totalCount: 1,
        )),
      );
      await viewModel.selectTemplate('t1');
      viewModel.stageFile('u1', 'd-u1', 'e1', Uint8List(3), 'a.pdf');
      viewModel.setGlobalSignDeadline('2026-04-01T00:00:00.000Z');

      await viewModel.uploadAllStagedToSign();

      expect(viewModel.status, BatchDocumentStatus.error);
      expect(viewModel.errorMessage, contains('data inválida'));
      verifyNever(() => mockBatchRepo.uploadDocumentRangeToSign(
            any(), any(), any(), any()));
    });
  });

  // ─────────────────────────────── generatePdfRange ───────────────────────

  group('generatePdfRange', () {
    Future<void> setupSelected({String date = '15/03/2026'}) async {
      await loadGroup(['t1']);
      stubPending(
        't1',
        Result.success(BatchDocumentUnitsPage(
          items: [unit(id: 'u1', employeeId: 'e1', name: 'Ana', date: date)],
          totalCount: 1,
        )),
      );
      await viewModel.selectTemplate('t1');
      viewModel.toggleSelection('u1');
    }

    test('returns null without touching the repo when nothing is selected',
        () async {
      await loadGroup(['t1']);
      stubPending('t1',
          const Result.success(BatchDocumentUnitsPage(items: [], totalCount: 0)));
      await viewModel.selectTemplate('t1');

      final bytes = await viewModel.generatePdfRange();

      expect(bytes, isNull);
      verifyNever(() => mockBatchRepo.generatePdfRange(any(), any()));
    });

    test('returns ZIP bytes on success', () async {
      await setupSelected();
      final zip = Uint8List.fromList([1, 2, 3, 4]);
      when(() => mockBatchRepo.generatePdfRange('company-1', any()))
          .thenAnswer((_) async => Result.success(zip));

      final bytes = await viewModel.generatePdfRange();

      expect(bytes, zip);
      expect(viewModel.status, BatchDocumentStatus.loaded);
    });

    test('blocks and reports invalid dates before generating', () async {
      await setupSelected(date: '45/45/2026');

      final bytes = await viewModel.generatePdfRange();

      expect(bytes, isNull);
      expect(viewModel.status, BatchDocumentStatus.error);
      expect(viewModel.errorMessage, contains('data inválida'));
      verifyNever(() => mockBatchRepo.generatePdfRange(any(), any()));
    });

    test('sets error status when generation fails', () async {
      await setupSelected();
      when(() => mockBatchRepo.generatePdfRange('company-1', any()))
          .thenAnswer((_) async => const Result.error('generation failed'));

      final bytes = await viewModel.generatePdfRange();

      expect(bytes, isNull);
      // NOTE (current behavior): error status is masked by the trailing
      // reload; the returned bytes are null and the message persists.
      expect(viewModel.errorMessage, isNotNull);
      expect(viewModel.status, BatchDocumentStatus.loaded);
    });
  });

  // ────────────────────────────── generateAndSignRange ────────────────────

  group('generateAndSignRange', () {
    Future<void> setupSelected({String date = '15/03/2026'}) async {
      await loadGroup(['t1']);
      stubPending(
        't1',
        Result.success(BatchDocumentUnitsPage(
          items: [unit(id: 'u1', employeeId: 'e1', name: 'Ana', date: date)],
          totalCount: 1,
        )),
      );
      await viewModel.selectTemplate('t1');
      viewModel.toggleSelection('u1');
    }

    test('does nothing when the deadline is not set', () async {
      await setupSelected();

      await viewModel.generateAndSignRange();

      verifyNever(() => mockBatchRepo.generateAndSignRange(
            any(), any(), any(), any()));
    });

    test('clears the selection on success', () async {
      await setupSelected();
      viewModel.setGlobalSignDeadline('2026-04-01T00:00:00.000Z');
      viewModel.setGlobalReminderDays(7);

      when(() => mockBatchRepo.generateAndSignRange(
            'company-1',
            any(),
            '2026-04-01T00:00:00.000Z',
            7,
          )).thenAnswer((_) async => const Result.success(null));

      await viewModel.generateAndSignRange();

      expect(viewModel.selectedUnitIds, isEmpty);
      expect(viewModel.status, BatchDocumentStatus.loaded);
    });

    test('blocks on invalid dates', () async {
      await setupSelected(date: '00/00/0000');
      viewModel.setGlobalSignDeadline('2026-04-01T00:00:00.000Z');

      await viewModel.generateAndSignRange();

      expect(viewModel.status, BatchDocumentStatus.error);
      expect(viewModel.errorMessage, contains('data inválida'));
      verifyNever(() => mockBatchRepo.generateAndSignRange(
            any(), any(), any(), any()));
    });

    test('sets error status when the repository fails', () async {
      await setupSelected();
      viewModel.setGlobalSignDeadline('2026-04-01T00:00:00.000Z');
      when(() => mockBatchRepo.generateAndSignRange(
            'company-1', any(), any(), any()))
          .thenAnswer((_) async => const Result.error('sign failed'));

      await viewModel.generateAndSignRange();

      // NOTE (current behavior): error status is masked by the trailing
      // reload; the error message survives.
      expect(viewModel.errorMessage, isNotNull);
      expect(viewModel.status, BatchDocumentStatus.loaded);
    });
  });

  // ──────────────────────────────── batchUpdateDate ───────────────────────

  group('batchUpdateDate', () {
    test('sets an error message when the update fails', () async {
      await loadGroup(['t1']);
      stubPending(
        't1',
        Result.success(BatchDocumentUnitsPage(
          items: [unit(id: 'u1', employeeId: 'e1', name: 'Ana')],
          totalCount: 1,
        )),
      );
      await viewModel.selectTemplate('t1');
      viewModel.toggleSelection('u1');

      when(() => mockBatchRepo.batchUpdateDate('company-1', any(), '2026-04-01'))
          .thenAnswer((_) async => const Result.error('update failed'));

      await viewModel.batchUpdateDate('2026-04-01');

      expect(viewModel.errorMessage, isNotNull);
      // Selection is preserved on failure (only cleared on success).
      expect(viewModel.selectedUnitIds, contains('u1'));
    });

    test('does nothing when no unit is selected', () async {
      await viewModel.batchUpdateDate('2026-04-01');
      verifyNever(() => mockBatchRepo.batchUpdateDate(any(), any(), any()));
    });
  });

  // ─────────────────────────────── validateStagedDates ────────────────────

  group('validateStagedDates', () {
    test('returns the names of staged units with invalid dates only',
        () async {
      await loadGroup(['t1']);
      stubPending(
        't1',
        Result.success(BatchDocumentUnitsPage(
          items: [
            unit(id: 'u1', employeeId: 'e1', name: 'Ana', date: '15/03/2026'),
            unit(id: 'u2', employeeId: 'e2', name: 'Bruno', date: '99/99/9999'),
          ],
          totalCount: 2,
        )),
      );
      await viewModel.selectTemplate('t1');

      viewModel.stageFile('u1', 'd-u1', 'e1', Uint8List(1), 'a.pdf');
      viewModel.stageFile('u2', 'd-u2', 'e2', Uint8List(1), 'b.pdf');

      final invalid = viewModel.validateStagedDates();

      expect(invalid, ['Bruno']);
    });

    test('returns an empty list when all staged dates are valid', () async {
      await loadGroup(['t1']);
      stubPending(
        't1',
        Result.success(BatchDocumentUnitsPage(
          items: [unit(id: 'u1', employeeId: 'e1', name: 'Ana')],
          totalCount: 1,
        )),
      );
      await viewModel.selectTemplate('t1');
      viewModel.stageFile('u1', 'd-u1', 'e1', Uint8List(1), 'a.pdf');

      expect(viewModel.validateStagedDates(), isEmpty);
    });
  });

  // ─────────────────────────── filters & pagination ───────────────────────

  group('filters and pagination', () {
    Future<void> setupLoaded() async {
      await loadGroup(['t1']);
      stubPending('t1',
          const Result.success(BatchDocumentUnitsPage(items: [], totalCount: 0)));
      await viewModel.selectTemplate('t1');
    }

    test('applyFilters resets to page 1 and reloads', () async {
      await setupLoaded();
      await viewModel.setPage(3);
      expect(viewModel.pageNumber, 3);

      await viewModel.applyFilters();

      expect(viewModel.pageNumber, 1);
    });

    test('clearFilters wipes every filter and reloads from page 1', () async {
      await setupLoaded();
      viewModel.setEmployeeStatusFilter(2);
      viewModel.setEmployeeNameFilter('Ana');
      viewModel.setPeriodFilter(typeId: 3, year: 2026, month: 2);
      await viewModel.setPage(4);

      await viewModel.clearFilters();

      expect(viewModel.employeeStatusFilter, isNull);
      expect(viewModel.employeeNameFilter, isNull);
      expect(viewModel.periodTypeFilter, isNull);
      expect(viewModel.periodYearFilter, isNull);
      expect(viewModel.periodMonthFilter, isNull);
      expect(viewModel.pageNumber, 1);
    });

    test('setPage updates the page number and reloads', () async {
      await setupLoaded();

      await viewModel.setPage(2);

      expect(viewModel.pageNumber, 2);
      verify(() => mockBatchRepo.getPendingDocumentUnits(
            'company-1',
            't1',
            employeeStatusId: any(named: 'employeeStatusId'),
            employeeName: any(named: 'employeeName'),
            periodTypeId: any(named: 'periodTypeId'),
            periodYear: any(named: 'periodYear'),
            periodMonth: any(named: 'periodMonth'),
            periodDay: any(named: 'periodDay'),
            periodWeek: any(named: 'periodWeek'),
            pageSize: any(named: 'pageSize'),
            pageNumber: 2,
          )).called(1);
    });

    test('setPageSize resets to page 1 and updates the size', () async {
      await setupLoaded();
      await viewModel.setPage(5);

      await viewModel.setPageSize(100);

      expect(viewModel.pageSize, 100);
      expect(viewModel.pageNumber, 1);
    });

    test('setPeriodFilter stores every period field', () async {
      await setupLoaded();

      viewModel.setPeriodFilter(
          typeId: 2, year: 2026, month: 3, day: 15, week: 4);

      expect(viewModel.periodTypeFilter, 2);
      expect(viewModel.periodYearFilter, 2026);
      expect(viewModel.periodMonthFilter, 3);
      expect(viewModel.periodDayFilter, 15);
      expect(viewModel.periodWeekFilter, 4);
    });
  });
}
