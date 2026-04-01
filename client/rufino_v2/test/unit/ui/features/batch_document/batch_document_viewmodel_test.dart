import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/domain/entities/batch_document_unit.dart';
import 'package:rufino_v2/domain/entities/document_group_with_templates.dart';
import 'package:rufino_v2/ui/features/batch_document/viewmodel/batch_document_viewmodel.dart';

import '../../../../testing/mocks/mocks.dart';

void main() {
  late MockBatchDocumentRepository mockBatchRepo;
  late MockDocumentGroupRepository mockGroupRepo;
  late BatchDocumentViewModel viewModel;

  setUp(() {
    mockBatchRepo = MockBatchDocumentRepository();
    mockGroupRepo = MockDocumentGroupRepository();
    viewModel = BatchDocumentViewModel(
      batchDocumentRepository: mockBatchRepo,
      documentGroupRepository: mockGroupRepo,
      companyId: 'company-1',
    );
  });

  group('BatchDocumentViewModel', () {
    group('loadGroupsAndTemplates', () {
      test('sets loaded status and populates groups on success', () async {
        when(() => mockGroupRepo.getDocumentGroupsWithTemplates('company-1'))
            .thenAnswer((_) async => const Result.success([
                  DocumentGroupWithTemplates(
                    id: 'g1',
                    name: 'Admissão',
                    description: '',
                    templates: [
                      DocumentTemplateSummary(id: 't1', name: 'T1', description: ''),
                    ],
                  ),
                ]));

        await viewModel.loadGroupsAndTemplates();

        expect(viewModel.status, BatchDocumentStatus.loaded);
        expect(viewModel.groups.length, 1);
        expect(viewModel.groups.first.id, 'g1');
      });

      test('sets error status when repository returns error', () async {
        when(() => mockGroupRepo.getDocumentGroupsWithTemplates('company-1'))
            .thenAnswer((_) async => const Result.error('fail'));

        await viewModel.loadGroupsAndTemplates();

        expect(viewModel.status, BatchDocumentStatus.error);
        expect(viewModel.errorMessage, isNotNull);
      });
    });

    group('selectGroup', () {
      test('populates templates from the selected group', () async {
        when(() => mockGroupRepo.getDocumentGroupsWithTemplates('company-1'))
            .thenAnswer((_) async => const Result.success([
                  DocumentGroupWithTemplates(
                    id: 'g1', name: 'Admissão', description: '',
                    templates: [
                      DocumentTemplateSummary(id: 't1', name: 'T1', description: ''),
                    ],
                  ),
                  DocumentGroupWithTemplates(
                    id: 'g2', name: 'Periódicos', description: '',
                    templates: [
                      DocumentTemplateSummary(id: 't2', name: 'T2', description: ''),
                    ],
                  ),
                ]));

        await viewModel.loadGroupsAndTemplates();
        viewModel.selectGroup('g1');

        expect(viewModel.selectedGroupId, 'g1');
        expect(viewModel.templates.length, 1);
        expect(viewModel.templates.first.id, 't1');
        expect(viewModel.selectedTemplateId, isNull);
      });
    });

    /// Helper: loads groups and selects group g1 with template t1.
    Future<void> setupGroupAndTemplate() async {
      when(() => mockGroupRepo.getDocumentGroupsWithTemplates('company-1'))
          .thenAnswer((_) async => const Result.success([
                DocumentGroupWithTemplates(
                  id: 'g1', name: 'Admissão', description: '',
                  templates: [
                    DocumentTemplateSummary(id: 't1', name: 'T1', description: ''),
                  ],
                ),
              ]));
      when(() => mockBatchRepo.getPendingDocumentUnits(
            any(), any(),
            employeeStatusId: any(named: 'employeeStatusId'),
            employeeName: any(named: 'employeeName'),
            periodTypeId: any(named: 'periodTypeId'),
            periodYear: any(named: 'periodYear'),
            periodMonth: any(named: 'periodMonth'),
            periodDay: any(named: 'periodDay'),
            periodWeek: any(named: 'periodWeek'),
            pageSize: any(named: 'pageSize'),
            pageNumber: any(named: 'pageNumber'),
          )).thenAnswer((_) async => const Result.success(
            BatchDocumentUnitsPage(items: [], totalCount: 0),
          ));
      await viewModel.loadGroupsAndTemplates();
      viewModel.selectGroup('g1');
    }

    group('selectTemplate', () {
      test('sets selectedTemplateId and loads pending units', () async {
        await setupGroupAndTemplate();
        await viewModel.selectTemplate('t1');

        expect(viewModel.selectedTemplateId, 't1');
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
            )).called(1);
      });
    });

    group('staging files', () {
      test('stageFile adds item and hasStaged returns true', () {
        viewModel.stageFile(
            'unit-1', 'doc-1', 'emp-1', Uint8List(10), 'test.pdf');

        expect(viewModel.hasStaged('unit-1'), true);
        expect(viewModel.stagedFileCount, 1);
        expect(viewModel.stagedFileName('unit-1'), 'test.pdf');
      });

      test('unstageFile removes item', () {
        viewModel.stageFile(
            'unit-1', 'doc-1', 'emp-1', Uint8List(10), 'test.pdf');
        viewModel.unstageFile('unit-1');

        expect(viewModel.hasStaged('unit-1'), false);
        expect(viewModel.stagedFileCount, 0);
      });
    });

    group('selection', () {
      setUp(() async {
        when(() => mockGroupRepo.getDocumentGroupsWithTemplates('company-1'))
            .thenAnswer((_) async => const Result.success([
                  DocumentGroupWithTemplates(
                    id: 'g1', name: 'G1', description: '',
                    templates: [DocumentTemplateSummary(id: 't1', name: 'T1', description: '')],
                  ),
                ]));
        when(() => mockBatchRepo.getPendingDocumentUnits(
              any(), any(),
              employeeStatusId: any(named: 'employeeStatusId'),
              employeeName: any(named: 'employeeName'),
              periodTypeId: any(named: 'periodTypeId'),
              periodYear: any(named: 'periodYear'),
              periodMonth: any(named: 'periodMonth'),
              periodDay: any(named: 'periodDay'),
              periodWeek: any(named: 'periodWeek'),
              pageSize: any(named: 'pageSize'),
              pageNumber: any(named: 'pageNumber'),
            )).thenAnswer((_) async => const Result.success(
              BatchDocumentUnitsPage(
                items: [
                  BatchDocumentUnitItem(
                    documentUnitId: 'u1',
                    documentId: 'd1',
                    employeeId: 'e1',
                    employeeName: 'A',
                    employeeStatusId: '2',
                    employeeStatusName: 'Active',
                    date: '15/03/2026',
                    statusId: '1',
                    statusName: 'Pending',
                    isSignable: false,
                    canGenerateDocument: false,
                  ),
                ],
                totalCount: 1,
              ),
            ));
        await viewModel.loadGroupsAndTemplates();
        viewModel.selectGroup('g1');
        await viewModel.selectTemplate('t1');
      });

      test('toggleSelection adds and removes unit ids', () {
        viewModel.toggleSelection('u1');
        expect(viewModel.selectedUnitIds.contains('u1'), true);

        viewModel.toggleSelection('u1');
        expect(viewModel.selectedUnitIds.contains('u1'), false);
      });

      test('selectAll selects all pending units', () {
        viewModel.selectAll();
        expect(viewModel.selectedUnitIds.length, 1);
        expect(viewModel.selectedUnitIds.contains('u1'), true);
      });

      test('clearSelection empties the set', () {
        viewModel.selectAll();
        viewModel.clearSelection();
        expect(viewModel.selectedUnitIds, isEmpty);
      });
    });

    group('uploadAllStaged', () {
      test('sends staged files and sets uploadComplete on success', () async {
        await setupGroupAndTemplate();

        when(() => mockBatchRepo.uploadDocumentRange(
              'company-1',
              any(),
            )).thenAnswer((_) async => const Result.success([
              BatchUploadResult(documentUnitId: 'u1', success: true),
            ]));

        await viewModel.selectTemplate('t1');

        viewModel.stageFile(
            'u1', 'd1', 'e1', Uint8List(10), 'test.pdf');

        await viewModel.uploadAllStaged();

        expect(viewModel.uploadResults.length, 1);
        expect(viewModel.uploadResults.first.success, true);
        expect(viewModel.stagedFileCount, 0);
      });
    });

    group('batchUpdateDate', () {
      test('clears selection and reloads on success', () async {
        when(() => mockGroupRepo.getDocumentGroupsWithTemplates('company-1'))
            .thenAnswer((_) async => const Result.success([
                  DocumentGroupWithTemplates(
                    id: 'g1', name: 'G1', description: '',
                    templates: [DocumentTemplateSummary(id: 't1', name: 'T1', description: '')],
                  ),
                ]));
        when(() => mockBatchRepo.getPendingDocumentUnits(
              any(), any(),
              employeeStatusId: any(named: 'employeeStatusId'),
              employeeName: any(named: 'employeeName'),
              periodTypeId: any(named: 'periodTypeId'),
              periodYear: any(named: 'periodYear'),
              periodMonth: any(named: 'periodMonth'),
              periodDay: any(named: 'periodDay'),
              periodWeek: any(named: 'periodWeek'),
              pageSize: any(named: 'pageSize'),
              pageNumber: any(named: 'pageNumber'),
            )).thenAnswer((_) async => const Result.success(
              BatchDocumentUnitsPage(
                items: [
                  BatchDocumentUnitItem(
                    documentUnitId: 'u1',
                    documentId: 'd1',
                    employeeId: 'e1',
                    employeeName: 'A',
                    employeeStatusId: '2',
                    employeeStatusName: 'Active',
                    date: '15/03/2026',
                    statusId: '1',
                    statusName: 'Pending',
                    isSignable: false,
                    canGenerateDocument: false,
                  ),
                ],
                totalCount: 1,
              ),
            ));

        when(() => mockBatchRepo.batchUpdateDate(
              'company-1',
              any(),
              '2026-04-01',
            )).thenAnswer((_) async => const Result.success(1));

        await viewModel.loadGroupsAndTemplates();
        viewModel.selectGroup('g1');
        await viewModel.selectTemplate('t1');
        viewModel.toggleSelection('u1');

        await viewModel.batchUpdateDate('2026-04-01');

        expect(viewModel.selectedUnitIds, isEmpty);
        verify(() => mockBatchRepo.batchUpdateDate(
              'company-1',
              any(),
              '2026-04-01',
            )).called(1);
      });
    });

    group('signature settings', () {
      test('setGlobalSignDeadline updates the value', () {
        viewModel.setGlobalSignDeadline('2026-04-15T00:00:00.000Z');
        expect(
            viewModel.globalSignDeadline, '2026-04-15T00:00:00.000Z');
      });

      test('setGlobalReminderDays updates the value', () {
        viewModel.setGlobalReminderDays(14);
        expect(viewModel.globalReminderDays, 14);
      });
    });
  });
}
