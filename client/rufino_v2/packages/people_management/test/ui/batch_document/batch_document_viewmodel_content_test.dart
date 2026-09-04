import 'package:rufino_core/rufino_core.dart';
import 'package:people_management/people_management.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import '../../fakes/fake_document_content_repository.dart';
import '../../fakes/mocks.dart';
import 'package:people_management/src/ui/batch_document/viewmodel/batch_document_viewmodel.dart';

/// The batch screen only REPORTS an outdated snapshot — refreshing is done by
/// editing each document. These tests cover the reporting side: which units
/// are asked about, which come back flagged, and the cases where the answer
/// cannot be trusted and therefore must not warn.
void main() {
  late MockBatchDocumentRepository mockBatchRepo;
  late MockDocumentGroupRepository mockGroupRepo;
  late FakeDocumentContentRepository fakeContentRepo;
  late BatchDocumentViewModel viewModel;

  BatchDocumentUnitItem unit({required String id, required String name}) =>
      BatchDocumentUnitItem(
        documentUnitId: id,
        documentId: 'd-$id',
        documentTemplateId: 't1',        documentTemplateName: 'T1',        documentGroupName: 'Grupo',        employeeId: 'e-$id',
        employeeName: name,
        employeeStatusId: '2',
        employeeStatusName: 'Ativo',
        date: '15/03/2026',
        statusId: '1',
        statusName: 'Pendente',
        isSignable: true,
        canGenerateDocument: true,
      );

  setUp(() {
    mockBatchRepo = MockBatchDocumentRepository();
    mockGroupRepo = MockDocumentGroupRepository();
    fakeContentRepo = FakeDocumentContentRepository();
    viewModel = BatchDocumentViewModel(
      batchDocumentRepository: mockBatchRepo,
      documentGroupRepository: mockGroupRepo,
      companyId: 'company-1',
      documentContentRepository: fakeContentRepo,
    );
  });

  tearDown(() => viewModel.dispose());

  /// Loads a single-template group with [units] pending and selects them all.
  Future<void> loadAndSelect(List<BatchDocumentUnitItem> units) async {
    when(() => mockGroupRepo.getDocumentGroupsWithTemplates('company-1'))
        .thenAnswer((_) async => const Result.success([
              DocumentGroupWithTemplates(
                id: 'g1',
                name: 'Grupo',
                description: '',
                templates: [
                  DocumentTemplateSummary(
                      id: 't1', name: 'T1', description: ''),
                ],
              ),
            ]));
    when(() => mockBatchRepo.getPendingDocumentUnits(
          any(),          documentGroupId: any(named: 'documentGroupId'),          documentTemplateId: any(named: 'documentTemplateId'),          employeeId: any(named: 'employeeId'),                    employeeStatusId: any(named: 'employeeStatusId'),
          employeeName: any(named: 'employeeName'),
          periodTypeId: any(named: 'periodTypeId'),
          periodYear: any(named: 'periodYear'),
          periodMonth: any(named: 'periodMonth'),
          periodDay: any(named: 'periodDay'),
          periodWeek: any(named: 'periodWeek'),
          pageSize: any(named: 'pageSize'),
          pageNumber: any(named: 'pageNumber'),
        )).thenAnswer((_) async =>
        Result.success(BatchDocumentUnitsPage(items: units, totalCount: units.length)));

    await viewModel.loadGroupsAndTemplates();
    await viewModel.selectGroup('g1');
    await viewModel.selectTemplate('t1');
    viewModel.selectAll();
  }

  group('BatchDocumentViewModel.checkOutdatedContent', () {
    test('returns only the units whose snapshot diverges', () async {
      await loadAndSelect([
        unit(id: 'u1', name: 'Maria'),
        unit(id: 'u2', name: 'João'),
      ]);
      fakeContentRepo.markOutdated('u2');

      final outdated = await viewModel.checkOutdatedContent();

      expect(outdated, {'u2'});
      expect(fakeContentRepo.checkCallCount, 1);
    });

    test('asks about the selected units only', () async {
      await loadAndSelect([
        unit(id: 'u1', name: 'Maria'),
        unit(id: 'u2', name: 'João'),
      ]);
      viewModel.clearSelection();
      viewModel.toggleSelection('u1');

      await viewModel.checkOutdatedContent();

      expect(
        fakeContentRepo.lastCheckedItems.map((e) => e.documentUnitId),
        ['u1'],
      );
    });

    test('does not warn when the check could not be concluded', () async {
      await loadAndSelect([unit(id: 'u1', name: 'Maria')]);
      fakeContentRepo.markCheckFailed('u1');

      expect(await viewModel.checkOutdatedContent(), isEmpty);
    });

    test('does not warn when the check itself fails', () async {
      await loadAndSelect([unit(id: 'u1', name: 'Maria')]);
      fakeContentRepo.markOutdated('u1');
      fakeContentRepo.setCheckShouldFail(true);

      expect(await viewModel.checkOutdatedContent(), isEmpty);
    });

    test('skips the call entirely when nothing is selected', () async {
      await loadAndSelect([unit(id: 'u1', name: 'Maria')]);
      viewModel.clearSelection();

      expect(await viewModel.checkOutdatedContent(), isEmpty);
      expect(fakeContentRepo.checkCallCount, 0);
    });

    test('reports nothing when no content repository is available', () async {
      final isolated = BatchDocumentViewModel(
        batchDocumentRepository: mockBatchRepo,
        documentGroupRepository: mockGroupRepo,
        companyId: 'company-1',
      );
      addTearDown(isolated.dispose);

      expect(await isolated.checkOutdatedContent(), isEmpty);
    });
  });
}
