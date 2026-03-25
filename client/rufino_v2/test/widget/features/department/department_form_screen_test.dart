import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/department.dart';
import 'package:rufino_v2/ui/features/department/viewmodel/department_form_viewmodel.dart';
import 'package:rufino_v2/ui/features/department/widgets/department_form_screen.dart';

import '../../../testing/fakes/fake_company_repository.dart';
import '../../../testing/fakes/fake_department_repository.dart';

const _fakeCompany = Company(
  id: 'company-1',
  corporateName: 'Acme Corp',
  fantasyName: 'Acme',
  cnpj: '00000000000000',
);

const _fakeDepartment = Department(
  id: 'dept-1',
  name: 'Financeiro',
  description: 'Departamento financeiro',
  positions: [],
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeDepartmentRepository departmentRepository;
  late DepartmentFormViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    departmentRepository = FakeDepartmentRepository();
    viewModel = DepartmentFormViewModel(
      companyRepository: companyRepository,
      departmentRepository: departmentRepository,
    );
  });

  tearDown(() => viewModel.dispose());

  /// Builds the subject with a parent `/department` route so [context.pop()]
  /// returns to the `department list` stub.
  Widget buildSubject({String? departmentId}) {
    final initialLocation = departmentId != null
        ? '/department/edit/$departmentId'
        : '/department/create';

    return MaterialApp.router(
      routerConfig: GoRouter(
        initialLocation: initialLocation,
        routes: [
          GoRoute(
            path: '/department',
            builder: (_, __) =>
                const Scaffold(body: Text('department list')),
            routes: [
              GoRoute(
                path: 'create',
                builder: (_, __) => DepartmentFormScreen(
                  viewModel: viewModel,
                ),
              ),
              GoRoute(
                path: 'edit/:id',
                builder: (context, state) => DepartmentFormScreen(
                  viewModel: viewModel,
                  departmentId: state.pathParameters['id'],
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  group('DepartmentFormScreen', () {
    testWidgets('shows Criar Setor title when creating a new department', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Criar Setor'), findsOneWidget);
    });

    testWidgets('shows Editar Setor title when editing an existing department', (tester) async {
      departmentRepository.setDepartment(_fakeDepartment);

      await tester.pumpWidget(buildSubject(departmentId: 'dept-1'));
      await tester.pumpAndSettle();

      expect(find.text('Editar Setor'), findsOneWidget);
    });

    testWidgets('shows loading indicator while loading existing department', (tester) async {
      departmentRepository.setDepartment(_fakeDepartment);

      await tester.pumpWidget(buildSubject(departmentId: 'dept-1'));
      // First frame before async completes
      expect(find.byType(CircularProgressIndicator), findsOneWidget);

      await tester.pumpAndSettle();
    });

    testWidgets('shows Nome and Descrição fields', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.widgetWithText(TextFormField, 'Nome'), findsOneWidget);
      expect(find.widgetWithText(TextFormField, 'Descrição'), findsOneWidget);
    });

    testWidgets('shows Salvar and Cancelar buttons', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.widgetWithText(FilledButton, 'Salvar'), findsOneWidget);
      expect(find.widgetWithText(TextButton, 'Cancelar'), findsOneWidget);
    });

    testWidgets('shows validation error when saving with empty name', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.widgetWithText(FilledButton, 'Salvar'));
      await tester.pumpAndSettle();

      expect(find.text('Não pode ser vazio.'), findsWidgets);
    });

    testWidgets('saves successfully and navigates back to department list', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.enterText(
          find.widgetWithText(TextFormField, 'Nome'), 'RH');
      await tester.enterText(
          find.widgetWithText(TextFormField, 'Descrição'), 'Recursos Humanos');

      await tester.tap(find.widgetWithText(FilledButton, 'Salvar'));
      await tester.pumpAndSettle();

      expect(find.text('department list'), findsOneWidget);
    });

    testWidgets('navigates back to department list when Cancelar is tapped', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.widgetWithText(TextButton, 'Cancelar'));
      await tester.pumpAndSettle();

      expect(find.text('department list'), findsOneWidget);
    });

    testWidgets('pre-fills form fields when editing an existing department', (tester) async {
      departmentRepository.setDepartment(_fakeDepartment);

      await tester.pumpWidget(buildSubject(departmentId: 'dept-1'));
      await tester.pumpAndSettle();

      expect(find.widgetWithText(TextFormField, 'Financeiro'), findsOneWidget);
      expect(find.widgetWithText(TextFormField, 'Departamento financeiro'),
          findsOneWidget);
    });
  });
}
