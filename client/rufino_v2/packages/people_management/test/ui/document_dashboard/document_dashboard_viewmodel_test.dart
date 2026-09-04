import 'package:flutter_test/flutter_test.dart';
import 'package:people_management/people_management.dart';

import '../../fakes/fake_document_dashboard_repository.dart';
import '../../fakes/fake_document_group_repository.dart';
import 'package:people_management/src/ui/document_dashboard/viewmodel/document_dashboard_viewmodel.dart';

void main() {
  late FakeDocumentDashboardRepository dashboardRepository;
  late FakeDocumentGroupRepository groupRepository;

  DashboardUnitItem unit({
    String unitId = 'unit-1',
    String employeeId = 'emp-1',
  }) =>
      DashboardUnitItem(
        documentUnitId: unitId,
        documentId: 'doc-1',
        employeeId: employeeId,
        employeeName: 'Maria da Silva',
        employeeStatusId: '2',
        employeeStatusName: 'Active',
        documentTemplateName: 'ASO',
        documentGroupName: 'Saúde',
        date: '01/03/2026',
        validity: '15/06/2026',
        statusId: '2',
        statusName: 'OK',
        hasFile: true,
      );

  DocumentDashboardViewModel buildViewModel() {
    dashboardRepository = FakeDocumentDashboardRepository();
    groupRepository = FakeDocumentGroupRepository();
    return DocumentDashboardViewModel(
      dashboardRepository: dashboardRepository,
      documentGroupRepository: groupRepository,
      companyId: 'company-1',
      nowProvider: () => DateTime(2026, 3, 10),
    );
  }

  group('DocumentDashboardViewModel.loadDashboard', () {
    test('loads groups, summary and units and ends in loaded status',
        () async {
      final viewModel = buildViewModel();
      groupRepository.setGroupsWithTemplates([
        const DocumentGroupWithTemplates(
          id: 'group-1',
          name: 'Saúde',
          description: '',
          templates: [
            DocumentTemplateSummary(id: 'tpl-1', name: 'ASO', description: ''),
          ],
        ),
      ]);
      dashboardRepository.setSummary(const DashboardSummary(
        expired: 1,
        expiring: 2,
        pending: 3,
        awaitingSignature: 0,
        requiresValidation: 0,
      ));
      dashboardRepository.setUnitsPage(
        DashboardUnitsPage(items: [unit()], totalCount: 1),
      );

      await viewModel.loadDashboard();

      expect(viewModel.status, DocumentDashboardStatus.loaded);
      expect(viewModel.groups, hasLength(1));
      expect(viewModel.summary.expired, 1);
      expect(viewModel.units, hasLength(1));
      expect(viewModel.totalCount, 1);
    });

    test('starts filtering by active employees and the expired bucket',
        () async {
      final viewModel = buildViewModel();

      await viewModel.loadDashboard();

      expect(viewModel.selectedBucket, DashboardBucket.expired);
      expect(dashboardRepository.lastBucket, DashboardBucket.expired);
      expect(dashboardRepository.lastEmployeeStatusId,
          DocumentDashboardViewModel.activeEmployeeStatusId);
      expect(dashboardRepository.lastExpiringInDays, 30);
    });

    test('emits error status with a message when the summary fails',
        () async {
      final viewModel = buildViewModel();
      dashboardRepository.setShouldFail(true);

      await viewModel.loadDashboard();

      expect(viewModel.status, DocumentDashboardStatus.error);
      expect(viewModel.errorMessage, isNotNull);
      expect(dashboardRepository.unitsCallCount, 0);
    });

    test('emits error status when loading the groups fails', () async {
      final viewModel = buildViewModel();
      groupRepository.setShouldFail(true);

      await viewModel.loadDashboard();

      expect(viewModel.status, DocumentDashboardStatus.error);
      expect(dashboardRepository.summaryCallCount, 0);
    });
  });

  group('DocumentDashboardViewModel.selectBucket', () {
    test('reloads only the units keeping the summary untouched', () async {
      final viewModel = buildViewModel();
      await viewModel.loadDashboard();
      final summaryCallsAfterLoad = dashboardRepository.summaryCallCount;

      await viewModel.selectBucket(DashboardBucket.pending);

      expect(viewModel.selectedBucket, DashboardBucket.pending);
      expect(dashboardRepository.lastBucket, DashboardBucket.pending);
      expect(dashboardRepository.summaryCallCount, summaryCallsAfterLoad);
    });

    test('resets pagination to the first page', () async {
      final viewModel = buildViewModel();
      dashboardRepository.setUnitsPage(
        DashboardUnitsPage(items: [unit()], totalCount: 120),
      );
      await viewModel.loadDashboard();
      await viewModel.setPage(2);

      await viewModel.selectBucket(DashboardBucket.expiring);

      expect(dashboardRepository.lastPageNumber, 1);
      expect(viewModel.pageNumber, 1);
    });

    test('does nothing when the bucket is already selected', () async {
      final viewModel = buildViewModel();
      await viewModel.loadDashboard();
      final callsAfterLoad = dashboardRepository.unitsCallCount;

      await viewModel.selectBucket(DashboardBucket.expired);

      expect(dashboardRepository.unitsCallCount, callsAfterLoad);
    });
  });

  group('DocumentDashboardViewModel filters', () {
    test('setHorizon reloads summary and units with the new horizon',
        () async {
      final viewModel = buildViewModel();
      await viewModel.loadDashboard();

      await viewModel.setHorizon(60);

      expect(viewModel.expiringInDays, 60);
      expect(dashboardRepository.lastExpiringInDays, 60);
      expect(dashboardRepository.summaryCallCount, 2);
      expect(dashboardRepository.unitsCallCount, 2);
    });

    test('selectGroup clears the template filter and reloads', () async {
      final viewModel = buildViewModel();
      await viewModel.loadDashboard();
      await viewModel.selectTemplate('tpl-1');

      await viewModel.selectGroup('group-1');

      expect(viewModel.selectedGroupId, 'group-1');
      expect(viewModel.selectedTemplateId, isNull);
      expect(dashboardRepository.lastDocumentGroupId, 'group-1');
      expect(dashboardRepository.lastDocumentTemplateId, isNull);
    });

    test('setEmployeeNameFilter trims and nullifies an empty search',
        () async {
      final viewModel = buildViewModel();
      await viewModel.loadDashboard();

      await viewModel.setEmployeeNameFilter('  Maria  ');
      expect(dashboardRepository.lastEmployeeName, 'Maria');

      await viewModel.setEmployeeNameFilter('   ');
      expect(dashboardRepository.lastEmployeeName, isNull);
    });

    test('setEmployeeStatusFilter forwards null to query all statuses',
        () async {
      final viewModel = buildViewModel();
      await viewModel.loadDashboard();

      await viewModel.setEmployeeStatusFilter(null);

      expect(dashboardRepository.lastEmployeeStatusId, isNull);
    });
  });

  group('DocumentDashboardViewModel pagination', () {
    test('setPage loads the requested page within bounds', () async {
      final viewModel = buildViewModel();
      dashboardRepository.setUnitsPage(
        DashboardUnitsPage(items: [unit()], totalCount: 120),
      );
      await viewModel.loadDashboard();

      await viewModel.setPage(3);

      expect(viewModel.pageNumber, 3);
      expect(dashboardRepository.lastPageNumber, 3);
    });

    test('setPage ignores out-of-bounds pages', () async {
      final viewModel = buildViewModel();
      dashboardRepository.setUnitsPage(
        DashboardUnitsPage(items: [unit()], totalCount: 10),
      );
      await viewModel.loadDashboard();
      final callsAfterLoad = dashboardRepository.unitsCallCount;

      await viewModel.setPage(0);
      await viewModel.setPage(2);

      expect(dashboardRepository.unitsCallCount, callsAfterLoad);
      expect(viewModel.pageNumber, 1);
    });

    test('pageCount derives from totalCount and pageSize', () async {
      final viewModel = buildViewModel();
      dashboardRepository.setUnitsPage(
        DashboardUnitsPage(items: [unit()], totalCount: 101),
      );
      await viewModel.loadDashboard();

      expect(viewModel.pageCount, 3);
    });

    test('setPageSize resets to the first page', () async {
      final viewModel = buildViewModel();
      dashboardRepository.setUnitsPage(
        DashboardUnitsPage(items: [unit()], totalCount: 120),
      );
      await viewModel.loadDashboard();
      await viewModel.setPage(2);

      await viewModel.setPageSize(20);

      expect(dashboardRepository.lastPageSize, 20);
      expect(dashboardRepository.lastPageNumber, 1);
    });
  });

  group('DocumentDashboardViewModel grouping', () {
    test('unitsByEmployee groups the current page preserving order',
        () async {
      final viewModel = buildViewModel();
      dashboardRepository.setUnitsPage(DashboardUnitsPage(
        items: [
          unit(unitId: 'unit-1', employeeId: 'emp-1'),
          unit(unitId: 'unit-2', employeeId: 'emp-2'),
          unit(unitId: 'unit-3', employeeId: 'emp-1'),
        ],
        totalCount: 3,
      ));
      await viewModel.loadDashboard();

      final grouped = viewModel.unitsByEmployee;

      expect(grouped.keys, ['emp-1', 'emp-2']);
      expect(grouped['emp-1']!.map((u) => u.documentUnitId),
          ['unit-1', 'unit-3']);
    });

    test('setGrouping only notifies when the grouping changes', () async {
      final viewModel = buildViewModel();
      var notifications = 0;
      viewModel.addListener(() => notifications++);

      viewModel.setGrouping(DashboardGrouping.byEmployee);
      viewModel.setGrouping(DashboardGrouping.byEmployee);

      expect(viewModel.grouping, DashboardGrouping.byEmployee);
      expect(notifications, 1);
    });
  });
}
