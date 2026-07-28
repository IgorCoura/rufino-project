import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:rufino_v2/data/services/file_save_service.dart';
import 'package:rufino_v2/data/services/spreadsheet_service.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/employee.dart';
import 'package:rufino_v2/domain/entities/permission.dart';
import 'package:rufino_v2/domain/repositories/company_repository.dart';
import 'package:rufino_v2/domain/repositories/department_repository.dart';
import 'package:rufino_v2/domain/repositories/employee_repository.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/permission_notifier.dart';
import 'package:rufino_v2/ui/features/employee/viewmodel/employee_list_viewmodel.dart';
import 'package:rufino_v2/ui/features/employee/widgets/employee_list_screen.dart';

import '../../../testing/fakes/fake_company_repository.dart';
import '../../../testing/fakes/fake_department_repository.dart';
import '../../../testing/fakes/fake_employee_repository.dart';
import '../../../testing/fakes/fake_permission_repository.dart';
import '../../../testing/fakes/recording_file_save_service.dart';
import '../../../testing/fakes/recording_spreadsheet_service.dart';

const _fakeCompany = Company(
  id: 'company-1',
  corporateName: 'Acme Corp',
  fantasyName: 'Acme',
  cnpj: '00000000000000',
);

// Pending status so 'Ativo' only ever appears inside the filter dropdown,
// keeping the menu-item finder unambiguous.
const _fakeEmployee = Employee(
  id: 'emp-1',
  name: 'Ana Lima',
  registration: 'R001',
  status: EmployeeStatus.pending,
  roleName: 'Analista',
  documentStatus: DocumentStatus.ok,
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeEmployeeRepository employeeRepository;
  late PermissionNotifier permissionNotifier;

  setUp(() async {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    employeeRepository = FakeEmployeeRepository()
      ..setEmployees([_fakeEmployee]);
    final fakePermRepo = FakePermissionRepository()
      ..setPermissions([
        const Permission(
          resource: 'employee',
          scopes: ['create', 'view', 'edit', 'download'],
        ),
      ]);
    permissionNotifier = PermissionNotifier(permissionRepository: fakePermRepo);
    await permissionNotifier.loadPermissions();
  });

  tearDown(() {
    permissionNotifier.dispose();
  });

  Widget buildSubject() => MultiProvider(
        providers: [
          Provider<CompanyRepository>.value(value: companyRepository),
          Provider<EmployeeRepository>.value(value: employeeRepository),
          Provider<DepartmentRepository>.value(
            value: FakeDepartmentRepository(),
          ),
          Provider<SpreadsheetService>.value(
            value: RecordingSpreadsheetService(),
          ),
          Provider<FileSaveService>.value(value: RecordingFileSaveService()),
          ChangeNotifierProvider<PermissionNotifier>.value(
            value: permissionNotifier,
          ),
        ],
        child: MaterialApp.router(
          routerConfig: GoRouter(
            initialLocation: '/employee',
            routes: [
              GoRoute(
                path: '/employee',
                builder: (_, __) => const EmployeeListPage(),
              ),
              GoRoute(
                path: '/employee/:id',
                builder: (_, __) => const Scaffold(body: Text('detail')),
              ),
              GoRoute(
                path: '/home',
                builder: (_, __) => const Scaffold(body: Text('home')),
              ),
            ],
          ),
        ),
      );

  EmployeeListViewModel viewModelOf(WidgetTester tester) => tester
      .widget<EmployeeListScreen>(find.byType(EmployeeListScreen))
      .viewModel;

  Future<void> applySearchAndStatusFilter(WidgetTester tester) async {
    await tester.enterText(find.byType(TextField), 'Ana');
    await tester.testTextInput.receiveAction(TextInputAction.search);
    await tester.pumpAndSettle();

    await tester.tap(find.byType(DropdownButtonFormField<EmployeeStatus>));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Ativo').last);
    await tester.pumpAndSettle();
  }

  Future<void> openProfileAndReturn(WidgetTester tester) async {
    await tester.tap(find.text('Ana Lima'));
    await tester.pumpAndSettle();
    expect(find.text('detail'), findsOneWidget);

    tester.state<NavigatorState>(find.byType(Navigator).last).pop();
    await tester.pumpAndSettle();
  }

  group('EmployeeListPage', () {
    testWidgets('keeps the same view model instance after navigating to the '
        'profile and back', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      final viewModelBefore = viewModelOf(tester);
      await openProfileAndReturn(tester);

      expect(identical(viewModelOf(tester), viewModelBefore), isTrue);
    });

    testWidgets('preserves search query and filters after navigating to the '
        'profile and back', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await applySearchAndStatusFilter(tester);
      expect(employeeRepository.lastNameFilter, 'Ana');

      await openProfileAndReturn(tester);

      final viewModel = viewModelOf(tester);
      expect(viewModel.searchQuery, 'Ana');
      expect(viewModel.selectedStatus, EmployeeStatus.active);
      expect(
        tester.widget<TextField>(find.byType(TextField)).controller?.text,
        'Ana',
      );
      expect(find.text('Ativo'), findsOneWidget);
    });

    testWidgets('does not reload the list when returning from the profile',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      final callsBefore = employeeRepository.getEmployeesCallCount;
      await openProfileAndReturn(tester);

      expect(employeeRepository.getEmployeesCallCount, callsBefore);
      expect(find.text('Ana Lima'), findsOneWidget);
    });
  });
}
