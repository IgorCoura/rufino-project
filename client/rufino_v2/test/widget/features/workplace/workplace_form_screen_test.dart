import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:rufino_v2/domain/entities/address.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/workplace.dart';
import 'package:rufino_v2/ui/features/workplace/viewmodel/workplace_form_viewmodel.dart';
import 'package:rufino_v2/ui/features/workplace/widgets/workplace_form_screen.dart';

import '../../../testing/fakes/fake_company_repository.dart';
import '../../../testing/fakes/fake_workplace_repository.dart';

const _fakeCompany = Company(
  id: 'company-1',
  corporateName: 'Acme Corp',
  fantasyName: 'Acme',
  cnpj: '00000000000000',
);

const _fakeAddress = Address(
  zipCode: '01310100',
  street: 'Av. Paulista',
  number: '1000',
  complement: '',
  neighborhood: 'Bela Vista',
  city: 'São Paulo',
  state: 'SP',
  country: 'Brasil',
);

const _fakeWorkplace = Workplace(
  id: 'wp-1',
  name: 'Sede Principal',
  address: _fakeAddress,
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeWorkplaceRepository workplaceRepository;
  late WorkplaceFormViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    workplaceRepository = FakeWorkplaceRepository();
    viewModel = WorkplaceFormViewModel(
      companyRepository: companyRepository,
      workplaceRepository: workplaceRepository,
    );
  });

  tearDown(() => viewModel.dispose());

  /// Builds the subject with a parent `/workplace` route so [context.pop()]
  /// returns to the `workplace list` stub.
  Widget buildSubject({String? workplaceId}) {
    final initialLocation = workplaceId != null
        ? '/workplace/edit/$workplaceId'
        : '/workplace/create';

    return MaterialApp.router(
      routerConfig: GoRouter(
        initialLocation: initialLocation,
        routes: [
          GoRoute(
            path: '/workplace',
            builder: (_, __) =>
                const Scaffold(body: Text('workplace list')),
            routes: [
              GoRoute(
                path: 'create',
                builder: (_, __) => WorkplaceFormScreen(viewModel: viewModel),
              ),
              GoRoute(
                path: 'edit/:id',
                builder: (context, state) => WorkplaceFormScreen(
                  viewModel: viewModel,
                  workplaceId: state.pathParameters['id'],
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  group('WorkplaceFormScreen', () {
    testWidgets('shows Criar Local de Trabalho title when creating a new workplace',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Criar Local de Trabalho'), findsOneWidget);
    });

    testWidgets('shows Editar Local de Trabalho title when editing an existing workplace',
        (tester) async {
      workplaceRepository.setWorkplace(_fakeWorkplace);

      await tester.pumpWidget(buildSubject(workplaceId: 'wp-1'));
      await tester.pumpAndSettle();

      expect(find.text('Editar Local de Trabalho'), findsOneWidget);
    });

    testWidgets('shows loading indicator while loading existing workplace',
        (tester) async {
      workplaceRepository.setWorkplace(_fakeWorkplace);

      await tester.pumpWidget(buildSubject(workplaceId: 'wp-1'));
      expect(find.byType(CircularProgressIndicator), findsOneWidget);

      await tester.pumpAndSettle();
    });

    testWidgets('shows required form fields', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.widgetWithText(TextFormField, 'Nome'), findsOneWidget);
      expect(find.widgetWithText(TextFormField, 'CEP'), findsOneWidget);
      expect(find.widgetWithText(TextFormField, 'Rua'), findsOneWidget);
    });

    testWidgets('shows Salvar and Cancelar buttons', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.widgetWithText(FilledButton, 'Salvar'), findsOneWidget);
      expect(find.widgetWithText(TextButton, 'Cancelar'), findsOneWidget);
    });

    testWidgets('shows validation errors when saving with empty required fields',
        (tester) async {
      // Use 1:1 pixel ratio and tall viewport so all fields are visible
      tester.view.devicePixelRatio = 1.0;
      tester.view.physicalSize = const Size(1080, 2400);
      addTearDown(() {
        tester.view.resetDevicePixelRatio();
        tester.view.resetPhysicalSize();
      });

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.widgetWithText(FilledButton, 'Salvar'));
      await tester.pumpAndSettle();

      expect(find.text('Não pode ser vazio.'), findsWidgets);
    });

    testWidgets('saves successfully and navigates back to workplace list',
        (tester) async {
      // Use 1:1 pixel ratio and tall viewport so all fields are visible
      tester.view.devicePixelRatio = 1.0;
      tester.view.physicalSize = const Size(1080, 2400);
      addTearDown(() {
        tester.view.resetDevicePixelRatio();
        tester.view.resetPhysicalSize();
      });

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.enterText(
          find.widgetWithText(TextFormField, 'Nome'), 'Filial SP');
      await tester.enterText(
          find.widgetWithText(TextFormField, 'CEP'), '01310100');
      await tester.enterText(
          find.widgetWithText(TextFormField, 'Rua'), 'Av. Paulista');
      await tester.enterText(
          find.widgetWithText(TextFormField, 'Número'), '1000');
      await tester.enterText(
          find.widgetWithText(TextFormField, 'Bairro'), 'Bela Vista');
      await tester.enterText(
          find.widgetWithText(TextFormField, 'Cidade'), 'São Paulo');
      await tester.enterText(
          find.widgetWithText(TextFormField, 'Estado'), 'SP');
      await tester.enterText(
          find.widgetWithText(TextFormField, 'País'), 'Brasil');

      await tester.tap(find.widgetWithText(FilledButton, 'Salvar'));
      await tester.pumpAndSettle();

      expect(find.text('workplace list'), findsOneWidget);
    });

    testWidgets('navigates back to workplace list when Cancelar is tapped',
        (tester) async {
      // Use 1:1 pixel ratio and tall viewport so the buttons are visible
      tester.view.devicePixelRatio = 1.0;
      tester.view.physicalSize = const Size(1080, 2400);
      addTearDown(() {
        tester.view.resetDevicePixelRatio();
        tester.view.resetPhysicalSize();
      });

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.widgetWithText(TextButton, 'Cancelar'));
      await tester.pumpAndSettle();

      expect(find.text('workplace list'), findsOneWidget);
    });

    testWidgets('pre-fills form fields when editing an existing workplace',
        (tester) async {
      workplaceRepository.setWorkplace(_fakeWorkplace);

      await tester.pumpWidget(buildSubject(workplaceId: 'wp-1'));
      await tester.pumpAndSettle();

      expect(find.widgetWithText(TextFormField, 'Sede Principal'), findsOneWidget);
      expect(find.widgetWithText(TextFormField, '01310100'), findsOneWidget);
      expect(find.widgetWithText(TextFormField, 'São Paulo'), findsOneWidget);
    });
  });
}
