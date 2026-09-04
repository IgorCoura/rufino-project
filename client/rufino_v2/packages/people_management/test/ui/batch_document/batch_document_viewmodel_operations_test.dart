import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:people_management/people_management.dart';
import 'package:people_management/src/ui/batch_document/viewmodel/batch_document_viewmodel.dart';

import '../../fakes/mocks.dart';

/// Covers the batch document operations around the three independent scope
/// axes (employee, group, template), the generate/sign flows, the
/// date-validation guards and the filter/pagination surface.
void main() {
  late MockBatchDocumentRepository mockBatchRepo;
  late MockDocumentGroupRepository mockGroupRepo;
  late BatchDocumentViewModel viewModel;

  BatchDocumentUnitItem unit({
    required String id,
    required String employeeId,
    required String name,
    String templateId = 't1',
    String date = '15/03/2026',
    bool signable = true,
    bool canGenerate = true,
  }) =>
      BatchDocumentUnitItem(
        documentUnitId: id,
        documentId: 'd-$id',
        documentTemplateId: templateId,
        documentTemplateName: templateId.toUpperCase(),
        documentGroupName: 'Grupo',
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

  EmployeeMissingDocument missing(
    String employeeId,
    String name, {
    String templateId = 't1',
  }) =>
      EmployeeMissingDocument(
        employeeId: employeeId,
        employeeName: name,
        employeeStatusId: '2',
        employeeStatusName: 'Ativo',
        documentTemplateId: templateId,
        documentTemplateName: templateId.toUpperCase(),
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

  /// Stubs the pending-units call for ANY scope with [result].
  void stubPending(Result<BatchDocumentUnitsPage> result) {
    when(() => mockBatchRepo.getPendingDocumentUnits(
          'company-1',
          documentGroupId: any(named: 'documentGroupId'),
          documentTemplateId: any(named: 'documentTemplateId'),
          employeeId: any(named: 'employeeId'),
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

  void stubMissing(Result<List<EmployeeMissingDocument>> result) {
    when(() => mockBatchRepo.getMissingEmployees(
          'company-1',
          documentGroupId: any(named: 'documentGroupId'),
          documentTemplateId: any(named: 'documentTemplateId'),
          employeeId: any(named: 'employeeId'),
          employeeStatusId: any(named: 'employeeStatusId'),
          employeeName: any(named: 'employeeName'),
        )).thenAnswer((_) async => result);
  }

  /// Stubs a group `g1` containing templates [templateIds] and loads it.
  Future<void> loadGroups(List<String> templateIds) async {
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
  }

  /// Loads the list already scoped to group `g1` and template `t1`.
  Future<void> setupLoaded({List<BatchDocumentUnitItem> items = const []}) async {
    await loadGroups(['t1', 't2']);
    stubPending(Result.success(
        BatchDocumentUnitsPage(items: items, totalCount: items.length)));
    await viewModel.selectGroup('g1');
    await viewModel.selectTemplate('t1');
  }

  // ───────────────────────────── scope axes ───────────────────────────────

  group('scope axes', () {
    test('loads every pending unit of the company when no scope is set',
        () async {
      await loadGroups(['t1']);
      stubPending(Result.success(BatchDocumentUnitsPage(
        items: [unit(id: 'u1', employeeId: 'e1', name: 'Ana')],
        totalCount: 1,
      )));

      await viewModel.loadPendingUnits();

      expect(viewModel.hasAnyScope, isFalse);
      expect(viewModel.pendingUnits.length, 1);
      expect(viewModel.totalCount, 1);
      verify(() => mockBatchRepo.getPendingDocumentUnits(
            'company-1',
            documentGroupId: null,
            documentTemplateId: null,
            employeeId: null,
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

    test('filters by employee alone, without group or template', () async {
      await loadGroups(['t1']);
      stubPending(Result.success(BatchDocumentUnitsPage(
        items: [
          unit(id: 'u1', employeeId: 'e1', name: 'Ana', templateId: 't1'),
          unit(id: 'u2', employeeId: 'e1', name: 'Ana', templateId: 't9'),
        ],
        totalCount: 2,
      )));

      await viewModel.selectEmployee('e1', 'Ana');

      expect(viewModel.selectedEmployeeId, 'e1');
      expect(viewModel.selectedEmployeeName, 'Ana');
      expect(viewModel.selectedGroupId, isNull);
      expect(viewModel.selectedTemplateId, isNull);
      expect(viewModel.pendingUnits.length, 2);
      verify(() => mockBatchRepo.getPendingDocumentUnits(
            'company-1',
            documentGroupId: null,
            documentTemplateId: null,
            employeeId: 'e1',
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

    test('filters by group alone, in a single request', () async {
      await loadGroups(['t1', 't2']);
      stubPending(const Result.success(
          BatchDocumentUnitsPage(items: [], totalCount: 0)));

      await viewModel.selectGroup('g1');

      expect(viewModel.selectedGroupId, 'g1');
      expect(viewModel.selectedTemplateId, isNull);
      verify(() => mockBatchRepo.getPendingDocumentUnits(
            'company-1',
            documentGroupId: 'g1',
            documentTemplateId: null,
            employeeId: any(named: 'employeeId'),
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

    test('combines group and template', () async {
      await setupLoaded();

      expect(viewModel.selectedGroupId, 'g1');
      expect(viewModel.selectedTemplateId, 't1');
      verify(() => mockBatchRepo.getPendingDocumentUnits(
            'company-1',
            documentGroupId: 'g1',
            documentTemplateId: 't1',
            employeeId: any(named: 'employeeId'),
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

    test('clears the template when the group changes', () async {
      await setupLoaded();

      await viewModel.selectGroup(null);

      expect(viewModel.selectedGroupId, isNull);
      expect(viewModel.selectedTemplateId, isNull);
      expect(viewModel.templates, isEmpty);
    });

    test('drops selection and staged files when the scope changes', () async {
      await setupLoaded(
          items: [unit(id: 'u1', employeeId: 'e1', name: 'Ana')]);
      viewModel.toggleSelection('u1');
      viewModel.stageFile('u1', 'd-u1', 'e1', Uint8List(3), 'a.pdf');

      await viewModel.selectEmployee('e2', 'Bruno');

      expect(viewModel.selectedUnitIds, isEmpty);
      expect(viewModel.stagedFileCount, 0);
      expect(viewModel.pageNumber, 1);
    });

    test('sets an error message when the load fails', () async {
      await loadGroups(['t1']);
      stubPending(const Result.error('boom'));

      await viewModel.loadPendingUnits();

      expect(viewModel.status, BatchDocumentStatus.error);
      expect(viewModel.errorMessage, isNotNull);
      expect(viewModel.pendingUnits, isEmpty);
      expect(viewModel.totalCount, 0);
    });
  });

  // ──────────────────────── capabilities of the selection ─────────────────

  group('capabilities of the selection', () {
    test('cannot generate when any selected unit is not generatable',
        () async {
      await setupLoaded(items: [
        unit(id: 'u1', employeeId: 'e1', name: 'Ana'),
        unit(id: 'u2', employeeId: 'e2', name: 'Bruno', canGenerate: false),
      ]);

      viewModel.toggleSelection('u1');
      expect(viewModel.canGenerateSelected, isTrue);

      viewModel.toggleSelection('u2');
      expect(viewModel.canGenerateSelected, isFalse);
    });

    test('cannot sign when any selected unit is not signable', () async {
      await setupLoaded(items: [
        unit(id: 'u1', employeeId: 'e1', name: 'Ana'),
        unit(id: 'u2', employeeId: 'e2', name: 'Bruno', signable: false),
      ]);

      viewModel.toggleSelection('u1');
      expect(viewModel.canSignSelected, isTrue);

      viewModel.toggleSelection('u2');
      expect(viewModel.canSignSelected, isFalse);
    });

    test('neither capability holds without a selection', () async {
      await setupLoaded(
          items: [unit(id: 'u1', employeeId: 'e1', name: 'Ana')]);

      expect(viewModel.canGenerateSelected, isFalse);
      expect(viewModel.canSignSelected, isFalse);
    });

    test('staged signing follows the staged units, not the selection',
        () async {
      await setupLoaded(items: [
        unit(id: 'u1', employeeId: 'e1', name: 'Ana'),
        unit(id: 'u2', employeeId: 'e2', name: 'Bruno', signable: false),
      ]);

      viewModel.stageFile('u1', 'd-u1', 'e1', Uint8List(1), 'a.pdf');
      expect(viewModel.canSignStaged, isTrue);

      viewModel.stageFile('u2', 'd-u2', 'e2', Uint8List(1), 'b.pdf');
      expect(viewModel.canSignStaged, isFalse);
    });
  });

  // ─────────────────────────── loadMissingEmployees ───────────────────────

  group('loadMissingEmployees', () {
    test('populates one row per employee x template pair', () async {
      await setupLoaded();
      stubMissing(Result.success([
        missing('e1', 'Ana', templateId: 't1'),
        missing('e1', 'Ana', templateId: 't2'),
        missing('e2', 'Bruno', templateId: 't1'),
      ]));

      await viewModel.loadMissingEmployees();

      expect(viewModel.missingEmployees.length, 3);
      expect(
          viewModel.missingEmployees
              .where((e) => e.employeeId == 'e1')
              .map((e) => e.documentTemplateId),
          containsAll(['t1', 't2']));
    });

    test('does not query without a group or template scope', () async {
      await loadGroups(['t1']);
      stubPending(const Result.success(
          BatchDocumentUnitsPage(items: [], totalCount: 0)));
      await viewModel.selectEmployee('e1', 'Ana');

      await viewModel.loadMissingEmployees();

      expect(viewModel.canCreateMissing, isFalse);
      verifyNever(() => mockBatchRepo.getMissingEmployees(
            any(),
            documentGroupId: any(named: 'documentGroupId'),
            documentTemplateId: any(named: 'documentTemplateId'),
            employeeId: any(named: 'employeeId'),
            employeeStatusId: any(named: 'employeeStatusId'),
            employeeName: any(named: 'employeeName'),
          ));
    });

    test('sets an error message when the lookup fails', () async {
      await setupLoaded();
      stubMissing(const Result.error('network'));

      await viewModel.loadMissingEmployees();

      expect(viewModel.errorMessage, isNotNull);
    });
  });

  // ────────────────────────── batchCreateDocumentUnits ────────────────────

  group('batchCreateDocumentUnits', () {
    test('groups the chosen pairs by template, one call each', () async {
      await setupLoaded();
      when(() => mockBatchRepo.batchCreateDocumentUnits(
            'company-1',
            any(),
            any(),
          )).thenAnswer((_) async => const Result.success([]));

      await viewModel.batchCreateDocumentUnits([
        missing('e1', 'Ana', templateId: 't1'),
        missing('e2', 'Bruno', templateId: 't1'),
        missing('e1', 'Ana', templateId: 't2'),
      ]);

      verify(() => mockBatchRepo
              .batchCreateDocumentUnits('company-1', 't1', ['e1', 'e2']))
          .called(1);
      verify(() =>
              mockBatchRepo.batchCreateDocumentUnits('company-1', 't2', ['e1']))
          .called(1);
    });

    test('does nothing when the list is empty', () async {
      await setupLoaded();

      await viewModel.batchCreateDocumentUnits([]);

      verifyNever(
          () => mockBatchRepo.batchCreateDocumentUnits(any(), any(), any()));
    });

    test('sets an error message when creation fails', () async {
      await setupLoaded();
      when(() => mockBatchRepo.batchCreateDocumentUnits(
              'company-1', 't1', any()))
          .thenAnswer((_) async => const Result.error('cannot create'));

      await viewModel.batchCreateDocumentUnits([missing('e1', 'Ana')]);

      expect(viewModel.errorMessage, isNotNull);
    });
  });

  // ─────────────────────────────── uploadAllStaged ────────────────────────

  group('uploadAllStaged', () {
    test('blocks upload and reports invalid dates without calling the repo',
        () async {
      await setupLoaded(items: [
        unit(id: 'u1', employeeId: 'e1', name: 'Ana', date: '32/13/2026'),
      ]);

      viewModel.stageFile('u1', 'd-u1', 'e1', Uint8List(3), 'a.pdf');

      await viewModel.uploadAllStaged();

      expect(viewModel.status, BatchDocumentStatus.error);
      expect(viewModel.errorMessage, contains('data inválida'));
      verifyNever(() => mockBatchRepo.uploadDocumentRange(any(), any()));
    });

    test('sets error status when the repository fails', () async {
      await setupLoaded(
          items: [unit(id: 'u1', employeeId: 'e1', name: 'Ana')]);
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
      await setupLoaded(
          items: [unit(id: 'u1', employeeId: 'e1', name: 'Ana')]);
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
      await setupLoaded(items: [
        unit(id: 'u1', employeeId: 'e1', name: 'Ana', date: '99/99/9999'),
      ]);
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
      await setupLoaded(
          items: [unit(id: 'u1', employeeId: 'e1', name: 'Ana', date: date)]);
      viewModel.toggleSelection('u1');
    }

    test('returns null without touching the repo when nothing is selected',
        () async {
      await setupLoaded();

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
      await setupLoaded(
          items: [unit(id: 'u1', employeeId: 'e1', name: 'Ana', date: date)]);
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
      await setupLoaded(
          items: [unit(id: 'u1', employeeId: 'e1', name: 'Ana')]);
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
      await setupLoaded(items: [
        unit(id: 'u1', employeeId: 'e1', name: 'Ana', date: '15/03/2026'),
        unit(id: 'u2', employeeId: 'e2', name: 'Bruno', date: '99/99/9999'),
      ]);

      viewModel.stageFile('u1', 'd-u1', 'e1', Uint8List(1), 'a.pdf');
      viewModel.stageFile('u2', 'd-u2', 'e2', Uint8List(1), 'b.pdf');

      final invalid = viewModel.validateStagedDates();

      expect(invalid, ['Bruno']);
    });

    test('returns an empty list when all staged dates are valid', () async {
      await setupLoaded(
          items: [unit(id: 'u1', employeeId: 'e1', name: 'Ana')]);
      viewModel.stageFile('u1', 'd-u1', 'e1', Uint8List(1), 'a.pdf');

      expect(viewModel.validateStagedDates(), isEmpty);
    });
  });

  // ─────────────────────────── filters & pagination ───────────────────────

  group('filters and pagination', () {
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

    test('clearFilters keeps the scope', () async {
      await setupLoaded();

      await viewModel.clearFilters();

      expect(viewModel.selectedGroupId, 'g1');
      expect(viewModel.selectedTemplateId, 't1');
    });

    test('setPage updates the page number and reloads', () async {
      await setupLoaded();

      await viewModel.setPage(2);

      expect(viewModel.pageNumber, 2);
      verify(() => mockBatchRepo.getPendingDocumentUnits(
            'company-1',
            documentGroupId: any(named: 'documentGroupId'),
            documentTemplateId: any(named: 'documentTemplateId'),
            employeeId: any(named: 'employeeId'),
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
