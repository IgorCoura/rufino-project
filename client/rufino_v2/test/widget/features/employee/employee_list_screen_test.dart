import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/employee.dart';
import 'package:rufino_v2/domain/entities/permission.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/permission_notifier.dart';
import 'package:rufino_v2/ui/features/employee/viewmodel/employee_list_viewmodel.dart';
import 'package:rufino_v2/ui/features/employee/widgets/employee_list_screen.dart';

import '../../../testing/fakes/fake_company_repository.dart';
import '../../../testing/fakes/fake_employee_repository.dart';
import '../../../testing/fakes/fake_permission_repository.dart';

const _fakeCompany = Company(
  id: 'company-1',
  corporateName: 'Acme Corp',
  fantasyName: 'Acme',
  cnpj: '00000000000000',
);

const _fakeEmployee = Employee(
  id: 'emp-1',
  name: 'Ana Lima',
  registration: 'R001',
  status: EmployeeStatus.active,
  roleName: 'Analista',
  documentStatus: DocumentStatus.ok,
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeEmployeeRepository employeeRepository;
  late PermissionNotifier permissionNotifier;
  late EmployeeListViewModel viewModel;

  setUp(() async {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    employeeRepository = FakeEmployeeRepository();
    viewModel = EmployeeListViewModel(
      companyRepository: companyRepository,
      employeeRepository: employeeRepository,
    );
    final fakePermRepo = FakePermissionRepository()
      ..setPermissions([
        const Permission(resource: 'employee', scopes: ['create', 'view', 'edit']),
      ]);
    permissionNotifier = PermissionNotifier(permissionRepository: fakePermRepo);
    await permissionNotifier.loadPermissions();
  });

  tearDown(() {
    viewModel.dispose();
    permissionNotifier.dispose();
  });

  Widget buildSubject() => ChangeNotifierProvider<PermissionNotifier>.value(
        value: permissionNotifier,
        child: MaterialApp.router(
          routerConfig: GoRouter(
            routes: [
              GoRoute(
                path: '/',
                builder: (_, __) =>
                    EmployeeListScreen(viewModel: viewModel),
              ),
              GoRoute(
                path: '/home',
                builder: (_, __) => const Scaffold(body: Text('home')),
              ),
              GoRoute(
                path: '/employee/create',
                builder: (_, __) => const Scaffold(body: Text('create')),
              ),
              GoRoute(
                path: '/employee/:id',
                builder: (_, __) => const Scaffold(body: Text('detail')),
              ),
            ],
          ),
        ),
      );

  group('EmployeeListScreen', () {
    testWidgets('shows loading indicator while fetching data', (tester) async {
      employeeRepository.setEmployees([_fakeEmployee]);

      await tester.pumpWidget(buildSubject());
      expect(find.byType(CircularProgressIndicator), findsOneWidget);

      await tester.pumpAndSettle();
    });

    testWidgets('shows employee name and role after loading', (tester) async {
      employeeRepository.setEmployees([_fakeEmployee]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Ana Lima'), findsOneWidget);
      expect(find.text('Função: Analista'), findsOneWidget);
    });

    testWidgets('shows document status and employee status columns',
        (tester) async {
      employeeRepository.setEmployees([_fakeEmployee]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Documentos'), findsWidgets);
      expect(find.text('Status'), findsWidgets);
      expect(find.text('OK'), findsWidgets);
      expect(find.text('Ativo'), findsOneWidget);
    });

    testWidgets('shows empty state message when there are no employees',
        (tester) async {
      employeeRepository.setEmployees([]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Nenhum funcionário encontrado.'), findsOneWidget);
      expect(
        find.text('Ajuste os filtros ou cadastre um novo funcionário.'),
        findsOneWidget,
      );
    });

    testWidgets('shows error state when loading fails', (tester) async {
      employeeRepository.setShouldFail(true);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Tentar novamente'), findsOneWidget);
    });

    testWidgets('shows FAB to add a new employee', (tester) async {
      employeeRepository.setEmployees([]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.byType(FloatingActionButton), findsOneWidget);
    });

    testWidgets('navigates to create screen when FAB is tapped',
        (tester) async {
      employeeRepository.setEmployees([]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.byType(FloatingActionButton));
      await tester.pumpAndSettle();

      expect(find.text('create'), findsOneWidget);
    });

    testWidgets('shows search bar, search-param dropdown, and filter dropdowns',
        (tester) async {
      employeeRepository.setEmployees([]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.byType(TextField), findsOneWidget);
      expect(
        find.byType(DropdownButtonFormField<SearchParam>),
        findsOneWidget,
      );
      expect(
        find.byType(DropdownButtonFormField<EmployeeStatus>),
        findsOneWidget,
      );
      expect(
        find.byType(DropdownButtonFormField<DocumentStatus>),
        findsOneWidget,
      );
      expect(find.text('Busca e filtros'), findsOneWidget);
    });

    testWidgets('shows column legend and filter labels', (tester) async {
      employeeRepository.setEmployees([_fakeEmployee]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Nomes'), findsOneWidget);
      expect(find.text('Buscar por'), findsOneWidget);
      expect(find.text('Documentos'), findsWidgets);
    });

    testWidgets('navigates to home when back button is tapped', (tester) async {
      employeeRepository.setEmployees([]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.byTooltip('Voltar para início'));
      await tester.pumpAndSettle();

      expect(find.text('home'), findsOneWidget);
    });

    testWidgets('reloads employees after returning from create screen',
        (tester) async {
      employeeRepository.setEmployees([]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.byType(FloatingActionButton));
      await tester.pumpAndSettle();
      expect(find.text('create'), findsOneWidget);

      employeeRepository.setEmployees([_fakeEmployee]);

      final NavigatorState navigator =
          tester.state(find.byType(Navigator).last);
      navigator.pop();
      await tester.pumpAndSettle();

      expect(find.text('Ana Lima'), findsOneWidget);
    });
  });
}
