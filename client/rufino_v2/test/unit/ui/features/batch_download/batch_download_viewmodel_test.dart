import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/batch_download.dart';
import 'package:rufino_v2/domain/entities/address.dart';
import 'package:rufino_v2/domain/entities/workplace.dart';
import 'package:rufino_v2/ui/features/batch_download/viewmodel/batch_download_viewmodel.dart';

import '../../../../testing/fakes/fake_batch_download_repository.dart';
import '../../../../testing/fakes/fake_document_group_repository.dart';
import '../../../../testing/fakes/fake_workplace_repository.dart';

void main() {
  late FakeBatchDownloadRepository fakeBatchDownloadRepo;
  late FakeDocumentGroupRepository fakeDocGroupRepo;
  late FakeWorkplaceRepository fakeWorkplaceRepo;
  late BatchDownloadViewModel viewModel;

  const testCompanyId = 'test-company-id';

  final testEmployees = [
    const BatchDownloadEmployee(
      id: 'emp-1',
      name: 'Alice Silva',
      statusId: 2,
      statusName: 'Active',
      roleName: 'Developer',
      workplaceName: 'Office A',
    ),
    const BatchDownloadEmployee(
      id: 'emp-2',
      name: 'Bob Santos',
      statusId: 2,
      statusName: 'Active',
      roleName: 'Designer',
      workplaceName: 'Office B',
    ),
  ];

  final testUnits = [
    const BatchDownloadUnit(
      documentUnitId: 'unit-1',
      documentId: 'doc-1',
      employeeId: 'emp-1',
      employeeName: 'Alice Silva',
      documentTemplateName: 'Holerite',
      documentGroupName: 'RH',
      date: '28/02/2026',
      statusId: 2,
      statusName: 'OK',
      hasFile: true,
    ),
    const BatchDownloadUnit(
      documentUnitId: 'unit-2',
      documentId: 'doc-2',
      employeeId: 'emp-2',
      employeeName: 'Bob Santos',
      documentTemplateName: 'Holerite',
      documentGroupName: 'RH',
      date: '28/02/2026',
      statusId: 1,
      statusName: 'Pending',
      hasFile: false,
    ),
  ];

  setUp(() {
    fakeBatchDownloadRepo = FakeBatchDownloadRepository();
    fakeDocGroupRepo = FakeDocumentGroupRepository();
    fakeWorkplaceRepo = FakeWorkplaceRepository();

    fakeBatchDownloadRepo.employeesPage = BatchDownloadEmployeesPage(
      items: testEmployees,
      totalCount: testEmployees.length,
    );
    fakeBatchDownloadRepo.unitsPage = BatchDownloadUnitsPage(
      items: testUnits,
      totalCount: testUnits.length,
    );

    fakeWorkplaceRepo.setWorkplaces(const [
      Workplace(
        id: 'wp-1',
        name: 'Office A',
        address: Address(
          zipCode: '00000-000',
          street: 'Rua A',
          number: '1',
          complement: '',
          neighborhood: 'Centro',
          city: 'SP',
          state: 'SP',
          country: 'BR',
        ),
      ),
    ]);

    viewModel = BatchDownloadViewModel(
      batchDownloadRepository: fakeBatchDownloadRepo,
      documentGroupRepository: fakeDocGroupRepo,
      workplaceRepository: fakeWorkplaceRepo,
      companyId: testCompanyId,
    );
  });

  tearDown(() => viewModel.dispose());

  group('BatchDownloadViewModel', () {
    test('starts on selectEmployees step with idle status', () {
      expect(viewModel.currentStep, BatchDownloadStep.selectEmployees);
      expect(viewModel.status, BatchDownloadStatus.idle);
    });

    test('initialize loads employees and workplaces', () async {
      await viewModel.initialize();

      expect(viewModel.employees.length, 2);
      expect(viewModel.employeesTotalCount, 2);
      expect(viewModel.workplaces.length, 1);
      expect(viewModel.status, BatchDownloadStatus.loaded);
    });

    test('toggleEmployeeSelection adds and removes employee IDs', () async {
      await viewModel.initialize();

      viewModel.toggleEmployeeSelection('emp-1');
      expect(viewModel.selectedEmployeeIds, contains('emp-1'));
      expect(viewModel.selectedEmployeeCount, 1);

      viewModel.toggleEmployeeSelection('emp-1');
      expect(viewModel.selectedEmployeeIds, isNot(contains('emp-1')));
      expect(viewModel.selectedEmployeeCount, 0);
    });

    test('selectAllEmployeesOnPage selects all current page employees',
        () async {
      await viewModel.initialize();

      viewModel.selectAllEmployeesOnPage();
      expect(viewModel.selectedEmployeeCount, 2);
      expect(viewModel.selectedEmployeeIds, contains('emp-1'));
      expect(viewModel.selectedEmployeeIds, contains('emp-2'));
    });

    test('clearEmployeeSelection clears all selections', () async {
      await viewModel.initialize();
      viewModel.selectAllEmployeesOnPage();

      viewModel.clearEmployeeSelection();
      expect(viewModel.selectedEmployeeCount, 0);
    });

    test('proceedToUnitSelection moves to step 2 and loads units', () async {
      await viewModel.initialize();
      viewModel.toggleEmployeeSelection('emp-1');

      await viewModel.proceedToUnitSelection();

      expect(viewModel.currentStep, BatchDownloadStep.selectUnits);
      expect(viewModel.units.length, 2);
      expect(viewModel.unitsTotalCount, 2);
    });

    test('proceedToUnitSelection does nothing without selected employees',
        () async {
      await viewModel.initialize();

      await viewModel.proceedToUnitSelection();
      expect(viewModel.currentStep, BatchDownloadStep.selectEmployees);
    });

    test('toggleUnitSelection adds and removes unit keys', () async {
      await viewModel.initialize();
      viewModel.toggleEmployeeSelection('emp-1');
      await viewModel.proceedToUnitSelection();

      viewModel.toggleUnitSelection('doc-1', 'unit-1');
      expect(viewModel.selectedUnitKeys, contains('doc-1:unit-1'));
      expect(viewModel.selectedUnitCount, 1);

      viewModel.toggleUnitSelection('doc-1', 'unit-1');
      expect(viewModel.selectedUnitKeys, isNot(contains('doc-1:unit-1')));
    });

    test('selectAllUnitsOnPage only selects units with hasFile=true',
        () async {
      await viewModel.initialize();
      viewModel.toggleEmployeeSelection('emp-1');
      await viewModel.proceedToUnitSelection();

      viewModel.selectAllUnitsOnPage();
      expect(viewModel.selectedUnitCount, 1);
      expect(viewModel.selectedUnitKeys, contains('doc-1:unit-1'));
      expect(
          viewModel.selectedUnitKeys, isNot(contains('doc-2:unit-2')));
    });

    test('proceedToReview moves to review step', () async {
      await viewModel.initialize();
      viewModel.toggleEmployeeSelection('emp-1');
      await viewModel.proceedToUnitSelection();
      viewModel.toggleUnitSelection('doc-1', 'unit-1');

      viewModel.proceedToReview();
      expect(viewModel.currentStep, BatchDownloadStep.review);
    });

    test('proceedToReview does nothing without selected units', () async {
      await viewModel.initialize();
      viewModel.toggleEmployeeSelection('emp-1');
      await viewModel.proceedToUnitSelection();

      viewModel.proceedToReview();
      expect(viewModel.currentStep, BatchDownloadStep.selectUnits);
    });

    test('goBack navigates back through steps', () async {
      await viewModel.initialize();
      viewModel.toggleEmployeeSelection('emp-1');
      await viewModel.proceedToUnitSelection();
      viewModel.toggleUnitSelection('doc-1', 'unit-1');
      viewModel.proceedToReview();

      expect(viewModel.currentStep, BatchDownloadStep.review);
      viewModel.goBack();
      expect(viewModel.currentStep, BatchDownloadStep.selectUnits);
      viewModel.goBack();
      expect(viewModel.currentStep, BatchDownloadStep.selectEmployees);
      viewModel.goBack(); // no-op
      expect(viewModel.currentStep, BatchDownloadStep.selectEmployees);
    });

    test('downloadSelected returns ZIP bytes on success', () async {
      final fakeZipBytes = Uint8List.fromList([0x50, 0x4B, 0x03, 0x04]);
      fakeBatchDownloadRepo.downloadBytes = fakeZipBytes;

      await viewModel.initialize();
      viewModel.toggleEmployeeSelection('emp-1');
      await viewModel.proceedToUnitSelection();
      viewModel.toggleUnitSelection('doc-1', 'unit-1');
      viewModel.proceedToReview();

      final bytes = await viewModel.downloadSelected();

      expect(bytes, isNotNull);
      expect(bytes, fakeZipBytes);
      expect(viewModel.status, BatchDownloadStatus.downloadComplete);
      expect(fakeBatchDownloadRepo.lastDownloadItems, isNotNull);
      expect(fakeBatchDownloadRepo.lastDownloadItems!.length, 1);
      expect(
          fakeBatchDownloadRepo.lastDownloadItems!.first.documentId, 'doc-1');
    });

    test('downloadSelected returns null on error', () async {
      fakeBatchDownloadRepo.errorToThrow = Exception('download failed');

      await viewModel.initialize();
      viewModel.toggleEmployeeSelection('emp-1');
      await viewModel.proceedToUnitSelection();
      viewModel.toggleUnitSelection('doc-1', 'unit-1');
      viewModel.proceedToReview();

      final bytes = await viewModel.downloadSelected();

      expect(bytes, isNull);
      expect(viewModel.status, BatchDownloadStatus.error);
      expect(viewModel.errorMessage, isNotNull);
    });

    test('applyEmployeeFilters resets to page 1 and reloads', () async {
      await viewModel.initialize();

      viewModel.setEmployeeNameFilter('Alice');
      viewModel.setEmployeeStatusFilter(2);
      await viewModel.applyEmployeeFilters();

      expect(fakeBatchDownloadRepo.lastEmployeeFilters?['name'], 'Alice');
      expect(fakeBatchDownloadRepo.lastEmployeeFilters?['statusId'], 2);
      expect(fakeBatchDownloadRepo.lastEmployeeFilters?['pageNumber'], 1);
    });

    test('clearEmployeeFilters resets all filters and reloads', () async {
      await viewModel.initialize();
      viewModel.setEmployeeNameFilter('Alice');
      await viewModel.clearEmployeeFilters();

      expect(fakeBatchDownloadRepo.lastEmployeeFilters?['name'], isNull);
      expect(fakeBatchDownloadRepo.lastEmployeeFilters?['statusId'], isNull);
    });

    test('setEmployeePage changes page number and reloads', () async {
      await viewModel.initialize();

      await viewModel.setEmployeePage(2);
      expect(fakeBatchDownloadRepo.lastEmployeeFilters?['pageNumber'], 2);
      expect(viewModel.employeePageNumber, 2);
    });

    test('applyUnitFilters passes filters to repository', () async {
      await viewModel.initialize();
      viewModel.toggleEmployeeSelection('emp-1');
      await viewModel.proceedToUnitSelection();

      viewModel.setUnitStatusFilter(2);
      viewModel.setPeriodFilter(typeId: 3, year: 2026, month: 2);
      await viewModel.applyUnitFilters();

      expect(fakeBatchDownloadRepo.lastUnitFilters?['unitStatusId'], 2);
      expect(fakeBatchDownloadRepo.lastUnitFilters?['periodYear'], 2026);
      expect(fakeBatchDownloadRepo.lastUnitFilters?['periodMonth'], 2);
    });

    test('selectedUnitsByEmployee groups units by employee name', () async {
      await viewModel.initialize();
      viewModel.toggleEmployeeSelection('emp-1');
      viewModel.toggleEmployeeSelection('emp-2');
      await viewModel.proceedToUnitSelection();
      viewModel.toggleUnitSelection('doc-1', 'unit-1');
      viewModel.proceedToReview();

      final grouped = viewModel.selectedUnitsByEmployee;
      expect(grouped.keys, contains('Alice Silva'));
      expect(grouped['Alice Silva']?.length, 1);
    });

    test('loadEmployees sets error status when API fails', () async {
      await viewModel.initialize();

      fakeBatchDownloadRepo.errorToThrow = Exception('network error');
      await viewModel.loadEmployees();

      expect(viewModel.status, BatchDownloadStatus.error);
      expect(viewModel.errorMessage, isNotNull);
    });
  });

  group('Combination mode', () {
    Future<void> navigateToStep2() async {
      await viewModel.initialize();
      viewModel.toggleEmployeeSelection('emp-1');
      viewModel.toggleEmployeeSelection('emp-2');
      await viewModel.proceedToUnitSelection();
    }

    test('addToCombination creates a group and clears selection', () async {
      await navigateToStep2();
      viewModel.toggleUnitSelection('doc-1', 'unit-1');
      expect(viewModel.selectedUnitCount, 1);

      viewModel.addToCombination();

      expect(viewModel.combinationGroupCount, 1);
      expect(viewModel.combinationGroups.first.groupNumber, 1);
      expect(viewModel.combinationGroups.first.units.length, 1);
      expect(viewModel.selectedUnitCount, 0);
    });

    test('addToCombination does nothing when no units selected', () async {
      await navigateToStep2();

      viewModel.addToCombination();

      expect(viewModel.combinationGroupCount, 0);
      expect(viewModel.isCombineMode, false);
    });

    test('isCombineMode returns true after adding a group', () async {
      await navigateToStep2();
      expect(viewModel.isCombineMode, false);

      viewModel.toggleUnitSelection('doc-1', 'unit-1');
      viewModel.addToCombination();

      expect(viewModel.isCombineMode, true);
    });

    test('multiple groups maintain order and numbering', () async {
      // Use units that have hasFile=true for both
      fakeBatchDownloadRepo.unitsPage = BatchDownloadUnitsPage(
        items: [
          testUnits[0], // unit-1, hasFile=true
          const BatchDownloadUnit(
            documentUnitId: 'unit-3',
            documentId: 'doc-3',
            employeeId: 'emp-1',
            employeeName: 'Alice Silva',
            documentTemplateName: 'Contrato',
            documentGroupName: 'RH',
            date: '01/03/2026',
            statusId: 2,
            statusName: 'OK',
            hasFile: true,
          ),
        ],
        totalCount: 2,
      );
      await navigateToStep2();

      viewModel.toggleUnitSelection('doc-1', 'unit-1');
      viewModel.addToCombination();

      viewModel.toggleUnitSelection('doc-3', 'unit-3');
      viewModel.addToCombination();

      expect(viewModel.combinationGroupCount, 2);
      expect(viewModel.combinationGroups[0].groupNumber, 1);
      expect(viewModel.combinationGroups[1].groupNumber, 2);
      expect(viewModel.combinedTotalUnitCount, 2);
    });

    test('proceedToReview auto-adds current selection in combine mode',
        () async {
      await navigateToStep2();
      viewModel.toggleUnitSelection('doc-1', 'unit-1');
      viewModel.addToCombination();

      // Simulate loading new units
      fakeBatchDownloadRepo.unitsPage = const BatchDownloadUnitsPage(
        items: [
          BatchDownloadUnit(
            documentUnitId: 'unit-4',
            documentId: 'doc-4',
            employeeId: 'emp-1',
            employeeName: 'Alice Silva',
            documentTemplateName: 'ASO',
            documentGroupName: 'SST',
            date: '10/03/2026',
            statusId: 2,
            statusName: 'OK',
            hasFile: true,
          ),
        ],
        totalCount: 1,
      );
      await viewModel.loadDocumentUnits();

      viewModel.toggleUnitSelection('doc-4', 'unit-4');
      viewModel.proceedToReview();

      expect(viewModel.combinationGroupCount, 2);
      expect(viewModel.currentStep, BatchDownloadStep.review);
    });

    test('proceedToReview in normal mode works unchanged', () async {
      await navigateToStep2();
      viewModel.toggleUnitSelection('doc-1', 'unit-1');

      viewModel.proceedToReview();

      expect(viewModel.isCombineMode, false);
      expect(viewModel.currentStep, BatchDownloadStep.review);
    });

    test('removeCombinationGroup removes and renumbers', () async {
      await navigateToStep2();

      // Add 3 groups
      viewModel.toggleUnitSelection('doc-1', 'unit-1');
      viewModel.addToCombination();
      viewModel.toggleUnitSelection('doc-1', 'unit-1');
      viewModel.addToCombination();
      viewModel.toggleUnitSelection('doc-1', 'unit-1');
      viewModel.addToCombination();
      expect(viewModel.combinationGroupCount, 3);

      viewModel.removeCombinationGroup(2);

      expect(viewModel.combinationGroupCount, 2);
      expect(viewModel.combinationGroups[0].groupNumber, 1);
      expect(viewModel.combinationGroups[1].groupNumber, 2);
    });

    test('goBack from combine review preserves groups', () async {
      await navigateToStep2();
      viewModel.toggleUnitSelection('doc-1', 'unit-1');
      viewModel.addToCombination();
      viewModel.proceedToReview();

      expect(viewModel.currentStep, BatchDownloadStep.review);
      viewModel.goBack();

      expect(viewModel.currentStep, BatchDownloadStep.selectUnits);
      expect(viewModel.isCombineMode, true);
      expect(viewModel.combinationGroupCount, 1);
    });

    test('proceedToUnitSelection resets combination state', () async {
      await navigateToStep2();
      viewModel.toggleUnitSelection('doc-1', 'unit-1');
      viewModel.addToCombination();
      expect(viewModel.isCombineMode, true);

      viewModel.goBack(); // back to step 1
      await viewModel.proceedToUnitSelection();

      expect(viewModel.isCombineMode, false);
      expect(viewModel.combinationGroupCount, 0);
    });

    test('combinedUnitsByEmployee groups units by employee', () async {
      fakeBatchDownloadRepo.unitsPage = BatchDownloadUnitsPage(
        items: [
          testUnits[0], // emp-1 Alice
          const BatchDownloadUnit(
            documentUnitId: 'unit-5',
            documentId: 'doc-5',
            employeeId: 'emp-2',
            employeeName: 'Bob Santos',
            documentTemplateName: 'Holerite',
            documentGroupName: 'RH',
            date: '28/02/2026',
            statusId: 2,
            statusName: 'OK',
            hasFile: true,
          ),
        ],
        totalCount: 2,
      );
      await navigateToStep2();

      viewModel.toggleUnitSelection('doc-1', 'unit-1');
      viewModel.toggleUnitSelection('doc-5', 'unit-5');
      viewModel.addToCombination();

      final byEmployee = viewModel.combinedUnitsByEmployee;
      expect(byEmployee.keys, containsAll(['Alice Silva', 'Bob Santos']));
      expect(byEmployee['Alice Silva']?.first.units.length, 1);
      expect(byEmployee['Bob Santos']?.first.units.length, 1);
    });

    test('downloadCombined calls individual download for each unit', () async {
      fakeBatchDownloadRepo.documentUnitBytes = {
        'doc-1:unit-1': Uint8List.fromList([0x25, 0x50, 0x44, 0x46]),
      };
      await navigateToStep2();
      viewModel.toggleUnitSelection('doc-1', 'unit-1');
      viewModel.addToCombination();
      viewModel.proceedToReview();

      await viewModel.downloadCombined();

      expect(
        fakeBatchDownloadRepo.downloadedUnitKeys,
        contains('doc-1:unit-1'),
      );
    });

    test('downloadCombined sets error status on download failure', () async {
      await navigateToStep2();
      viewModel.toggleUnitSelection('doc-1', 'unit-1');
      viewModel.addToCombination();
      viewModel.proceedToReview();

      fakeBatchDownloadRepo.errorToThrow = Exception('download failed');
      final result = await viewModel.downloadCombined();

      expect(result, isNull);
      expect(viewModel.status, BatchDownloadStatus.error);
      expect(viewModel.errorMessage, isNotNull);
    });

    test('combinedFileName uses first unit of first group', () async {
      await navigateToStep2();
      viewModel.toggleUnitSelection('doc-1', 'unit-1');
      viewModel.addToCombination();

      expect(viewModel.combinedFileName, contains('ALICE_SILVA'));
      expect(viewModel.combinedFileName, endsWith('.PDF'));
    });
  });
}
