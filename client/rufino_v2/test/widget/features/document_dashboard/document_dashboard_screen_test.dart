import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:rufino_v2/domain/entities/document_dashboard.dart';
import 'package:rufino_v2/domain/entities/document_group_with_templates.dart';
import 'package:rufino_v2/ui/features/document_dashboard/viewmodel/document_dashboard_viewmodel.dart';
import 'package:rufino_v2/ui/features/document_dashboard/widgets/document_dashboard_screen.dart';

import '../../../testing/fakes/fake_document_dashboard_repository.dart';
import '../../../testing/fakes/fake_document_group_repository.dart';

void main() {
  late FakeDocumentDashboardRepository dashboardRepository;
  late FakeDocumentGroupRepository groupRepository;
  late DocumentDashboardViewModel viewModel;
  String? pushedLocation;

  DashboardUnitItem unit({
    String unitId = 'unit-1',
    String employeeId = 'emp-1',
    String employeeName = 'Maria da Silva',
    String validity = '15/06/2026',
  }) =>
      DashboardUnitItem(
        documentUnitId: unitId,
        documentId: 'doc-1',
        employeeId: employeeId,
        employeeName: employeeName,
        employeeStatusId: '2',
        employeeStatusName: 'Active',
        documentTemplateName: 'ASO',
        documentGroupName: 'Saúde',
        date: '01/03/2026',
        validity: validity,
        statusId: '2',
        statusName: 'OK',
        hasFile: true,
      );

  setUp(() {
    pushedLocation = null;
    dashboardRepository = FakeDocumentDashboardRepository();
    groupRepository = FakeDocumentGroupRepository()
      ..setGroupsWithTemplates([
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
      expired: 3,
      expiring: 2,
      pending: 5,
      awaitingSignature: 1,
      requiresValidation: 0,
    ));
    dashboardRepository.setUnitsPage(
      DashboardUnitsPage(items: [unit()], totalCount: 1),
    );
    viewModel = DocumentDashboardViewModel(
      dashboardRepository: dashboardRepository,
      documentGroupRepository: groupRepository,
      companyId: 'company-1',
      nowProvider: () => DateTime(2026, 6, 10),
    );
  });

  tearDown(() {
    viewModel.dispose();
  });

  Widget buildSubject() => MaterialApp.router(
        routerConfig: GoRouter(
          routes: [
            GoRoute(
              path: '/',
              builder: (_, __) =>
                  DocumentDashboardScreen(viewModel: viewModel),
            ),
            GoRoute(
              path: '/employee/:id',
              builder: (_, state) {
                pushedLocation = state.uri.toString();
                return const Scaffold(body: Text('profile-stub'));
              },
            ),
            GoRoute(
              path: '/home',
              builder: (_, __) => const Scaffold(body: Text('home-stub')),
            ),
          ],
        ),
      );

  Future<void> pumpLoaded(WidgetTester tester) async {
    await viewModel.loadDashboard();
    await tester.pumpWidget(buildSubject());
    await tester.pumpAndSettle();
  }

  group('DocumentDashboardScreen', () {
    testWidgets('shows one KPI card per bucket with its count',
        (tester) async {
      await pumpLoaded(tester);

      expect(find.byKey(const ValueKey('bucket-card-expired')), findsOneWidget);
      expect(
          find.byKey(const ValueKey('bucket-card-expiring')), findsOneWidget);
      expect(find.byKey(const ValueKey('bucket-card-pending')), findsOneWidget);
      expect(find.text('Vencidos'), findsOneWidget);
      expect(find.text('3'), findsOneWidget);
      expect(find.text('5'), findsOneWidget);
    });

    testWidgets('tapping a KPI card loads that bucket from the repository',
        (tester) async {
      await pumpLoaded(tester);

      await tester.tap(find.byKey(const ValueKey('bucket-card-pending')));
      await tester.pumpAndSettle();

      expect(dashboardRepository.lastBucket, DashboardBucket.pending);
    });

    testWidgets('renders a unit row with employee, template and urgency chip',
        (tester) async {
      await pumpLoaded(tester);

      expect(find.text('Maria da Silva — ASO'), findsOneWidget);
      expect(find.text('Vence em 5 dias'), findsOneWidget);
    });

    testWidgets(
        'tapping a unit row opens the employee profile on the documents tab',
        (tester) async {
      await pumpLoaded(tester);

      await tester.tap(find.text('Maria da Silva — ASO'));
      await tester.pumpAndSettle();

      expect(find.text('profile-stub'), findsOneWidget);
      expect(pushedLocation, '/employee/emp-1?tab=documents');
    });

    testWidgets('groups the rows by employee when the toggle is switched',
        (tester) async {
      dashboardRepository.setUnitsPage(DashboardUnitsPage(
        items: [
          unit(unitId: 'unit-1', employeeId: 'emp-1'),
          unit(unitId: 'unit-2', employeeId: 'emp-1'),
          unit(
            unitId: 'unit-3',
            employeeId: 'emp-2',
            employeeName: 'João Souza',
          ),
        ],
        totalCount: 3,
      ));
      await pumpLoaded(tester);

      await tester.tap(find.text('Por funcionário'));
      await tester.pumpAndSettle();

      expect(
          find.byKey(const ValueKey('employee-group-emp-1')), findsOneWidget);
      expect(
          find.byKey(const ValueKey('employee-group-emp-2')), findsOneWidget);
      expect(find.text('Maria da Silva'), findsOneWidget);
      expect(find.text('João Souza'), findsOneWidget);
    });

    testWidgets(
        'filters by employees still in admission when Pendentes is selected',
        (tester) async {
      await pumpLoaded(tester);

      await tester.tap(find.byKey(const ValueKey('employee-status-filter')));
      await tester.pumpAndSettle();
      await tester
          .tap(find.byKey(const ValueKey('employee-status-option-1')).last);
      await tester.pumpAndSettle();

      expect(dashboardRepository.lastEmployeeStatusId, 1);
    });

    testWidgets('shows the empty state when the bucket has no units',
        (tester) async {
      dashboardRepository.setUnitsPage(
        const DashboardUnitsPage(items: [], totalCount: 0),
      );
      await pumpLoaded(tester);

      expect(find.text('Nenhum documento neste filtro.'), findsOneWidget);
    });

    testWidgets('shows the error state with a retry button when loading fails',
        (tester) async {
      dashboardRepository.setShouldFail(true);
      await pumpLoaded(tester);

      expect(find.text('Tentar novamente'), findsOneWidget);

      dashboardRepository.setShouldFail(false);
      await tester.tap(find.text('Tentar novamente'));
      await tester.pumpAndSettle();

      expect(find.text('Maria da Silva — ASO'), findsOneWidget);
    });
  });
}
