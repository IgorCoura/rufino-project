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
}
