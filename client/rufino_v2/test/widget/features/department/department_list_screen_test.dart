import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/department.dart';
import 'package:rufino_v2/domain/entities/position.dart';
import 'package:rufino_v2/domain/entities/remuneration.dart';
import 'package:rufino_v2/domain/entities/role.dart';
import 'package:rufino_v2/ui/features/department/viewmodel/department_list_viewmodel.dart';
import 'package:rufino_v2/ui/features/department/widgets/department_list_screen.dart';

import '../../../testing/fakes/fake_company_repository.dart';
import '../../../testing/fakes/fake_department_repository.dart';

const _fakeCompany = Company(
  id: 'company-1',
  corporateName: 'Acme Corp',
  fantasyName: 'Acme',
  cnpj: '00000000000000',
);

const _fakeRole = Role(
  id: 'role-1',
  name: 'Analista Jr',
  description: 'Analista júnior',
  cbo: '123456',
  remuneration: Remuneration(
    paymentUnit: PaymentUnit(id: '5', name: 'Por Mês'),
    baseSalary: BaseSalary(type: SalaryType(id: '1', name: 'BRL'), value: '3500.00'),
    description: 'Salário mensal',
  ),
);

const _fakePosition = Position(
  id: 'pos-1',
  name: 'Analista',
  description: 'Analista financeiro',
  cbo: '123456',
  roles: [_fakeRole],
);

const _fakeDepartment = Department(
  id: 'dept-1',
  name: 'Financeiro',
  description: 'Departamento financeiro',
  positions: [_fakePosition],
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeDepartmentRepository departmentRepository;
  late DepartmentListViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    departmentRepository = FakeDepartmentRepository();
    viewModel = DepartmentListViewModel(
      companyRepository: companyRepository,
      departmentRepository: departmentRepository,
    );
  });

  tearDown(() => viewModel.dispose());

  Widget buildSubject() => MaterialApp.router(
        routerConfig: GoRouter(
          routes: [
            GoRoute(
              path: '/',
              builder: (_, __) =>
                  DepartmentListScreen(viewModel: viewModel),
            ),
            GoRoute(
              path: '/home',
              builder: (_, __) => const Scaffold(body: Text('home')),
            ),
            GoRoute(
              path: '/department/create',
              builder: (_, __) => const Scaffold(body: Text('create')),
            ),
            GoRoute(
              path: '/department/edit/:id',
              builder: (_, __) => const Scaffold(body: Text('edit dept')),
            ),
            GoRoute(
              path: '/department/position/create/:departmentId',
              builder: (_, __) => const Scaffold(body: Text('create pos')),
            ),
            GoRoute(
              path: '/department/position/edit/:departmentId/:id',
              builder: (_, __) => const Scaffold(body: Text('edit pos')),
            ),
            GoRoute(
              path: '/department/role/create/:positionId',
              builder: (_, __) => const Scaffold(body: Text('create role')),
            ),
            GoRoute(
              path: '/department/role/edit/:positionId/:id',
              builder: (_, __) => const Scaffold(body: Text('edit role')),
            ),
          ],
        ),
      );

  group('DepartmentListScreen', () {
    testWidgets('shows loading indicator while fetching data', (tester) async {
      // Load is not awaited — triggers loading state
      departmentRepository.setDepartments([_fakeDepartment]);

      await tester.pumpWidget(buildSubject());
      // First frame: loading
      expect(find.byType(CircularProgressIndicator), findsOneWidget);

      await tester.pumpAndSettle();
    });

    testWidgets('shows department name and description after loading', (tester) async {
      departmentRepository.setDepartments([_fakeDepartment]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Financeiro'), findsOneWidget);
      expect(find.text('Departamento financeiro'), findsOneWidget);
    });

    testWidgets('shows empty state message when there are no departments', (tester) async {
      departmentRepository.setDepartments([]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Nenhum setor cadastrado.'), findsOneWidget);
    });

    testWidgets('shows error state when loading fails', (tester) async {
      departmentRepository.setShouldFail(true);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Tentar novamente'), findsOneWidget);
    });

    testWidgets('shows FAB to add a new department', (tester) async {
      departmentRepository.setDepartments([]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.byType(FloatingActionButton), findsOneWidget);
    });

    testWidgets('shows position name inside expanded department tile', (tester) async {
      departmentRepository.setDepartments([_fakeDepartment]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.text('Financeiro'));
      await tester.pumpAndSettle();

      expect(find.text('Analista'), findsOneWidget);
    });

    testWidgets('navigates to create screen when FAB is tapped', (tester) async {
      departmentRepository.setDepartments([]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.byType(FloatingActionButton));
      await tester.pumpAndSettle();

      expect(find.text('create'), findsOneWidget);
    });

    testWidgets('shows back button in AppBar', (tester) async {
      departmentRepository.setDepartments([]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.byType(IconButton), findsOneWidget);
    });

    testWidgets('navigates to home when back button is tapped', (tester) async {
      departmentRepository.setDepartments([]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.byType(IconButton));
      await tester.pumpAndSettle();

      expect(find.text('home'), findsOneWidget);
    });

    testWidgets('reloads departments after returning from create screen', (tester) async {
      departmentRepository.setDepartments([]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to create
      await tester.tap(find.byType(FloatingActionButton));
      await tester.pumpAndSettle();
      expect(find.text('create'), findsOneWidget);

      // Add a department so the list is non-empty on reload
      departmentRepository.setDepartments([_fakeDepartment]);

      // Pop back to the list
      final NavigatorState navigator = tester.state(find.byType(Navigator).last);
      navigator.pop();
      await tester.pumpAndSettle();

      // After pop, loadDepartments is called via .then() — list should be visible
      expect(find.text('Financeiro'), findsOneWidget);
    });
  });
}
