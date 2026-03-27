import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:rufino_v2/domain/entities/address.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/permission.dart';
import 'package:rufino_v2/domain/entities/workplace.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/permission_notifier.dart';
import 'package:rufino_v2/ui/features/workplace/viewmodel/workplace_list_viewmodel.dart';
import 'package:rufino_v2/ui/features/workplace/widgets/workplace_list_screen.dart';

import '../../../testing/fakes/fake_company_repository.dart';
import '../../../testing/fakes/fake_permission_repository.dart';
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
  late PermissionNotifier permissionNotifier;
  late WorkplaceListViewModel viewModel;

  setUp(() async {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    workplaceRepository = FakeWorkplaceRepository();
    viewModel = WorkplaceListViewModel(
      companyRepository: companyRepository,
      workplaceRepository: workplaceRepository,
    );
    final fakePermRepo = FakePermissionRepository()
      ..setPermissions([
        const Permission(resource: 'workplace', scopes: ['create', 'view', 'edit']),
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
                    WorkplaceListScreen(viewModel: viewModel),
              ),
              GoRoute(
                path: '/home',
                builder: (_, __) => const Scaffold(body: Text('home')),
              ),
              GoRoute(
                path: '/workplace/create',
                builder: (_, __) => const Scaffold(body: Text('create')),
              ),
              GoRoute(
                path: '/workplace/edit/:id',
                builder: (_, __) => const Scaffold(body: Text('edit')),
              ),
            ],
          ),
        ),
      );

  group('WorkplaceListScreen', () {
    testWidgets('shows loading indicator while fetching data', (tester) async {
      workplaceRepository.setWorkplaces([_fakeWorkplace]);

      await tester.pumpWidget(buildSubject());
      expect(find.byType(CircularProgressIndicator), findsOneWidget);

      await tester.pumpAndSettle();
    });

    testWidgets('shows workplace name and address after loading', (tester) async {
      workplaceRepository.setWorkplaces([_fakeWorkplace]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Sede Principal'), findsOneWidget);
      expect(find.text('Bela Vista, São Paulo - SP'), findsOneWidget);
    });

    testWidgets('shows empty state message when there are no workplaces',
        (tester) async {
      workplaceRepository.setWorkplaces([]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Nenhum local de trabalho cadastrado.'), findsOneWidget);
    });

    testWidgets('shows error state when loading fails', (tester) async {
      workplaceRepository.setShouldFail(true);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Tentar novamente'), findsOneWidget);
    });

    testWidgets('shows FAB to add a new workplace', (tester) async {
      workplaceRepository.setWorkplaces([]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.byType(FloatingActionButton), findsOneWidget);
    });

    testWidgets('navigates to create screen when FAB is tapped', (tester) async {
      workplaceRepository.setWorkplaces([]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.byType(FloatingActionButton));
      await tester.pumpAndSettle();

      expect(find.text('create'), findsOneWidget);
    });

    testWidgets('shows back button in AppBar', (tester) async {
      workplaceRepository.setWorkplaces([]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.byType(IconButton), findsOneWidget);
    });

    testWidgets('navigates to home when back button is tapped', (tester) async {
      workplaceRepository.setWorkplaces([]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.byType(IconButton));
      await tester.pumpAndSettle();

      expect(find.text('home'), findsOneWidget);
    });

    testWidgets('reloads workplaces after returning from create screen',
        (tester) async {
      workplaceRepository.setWorkplaces([]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to create
      await tester.tap(find.byType(FloatingActionButton));
      await tester.pumpAndSettle();
      expect(find.text('create'), findsOneWidget);

      // Add a workplace so the list is non-empty on reload
      workplaceRepository.setWorkplaces([_fakeWorkplace]);

      // Pop back to the list
      final NavigatorState navigator =
          tester.state(find.byType(Navigator).last);
      navigator.pop();
      await tester.pumpAndSettle();

      // After pop, loadWorkplaces is called via .then() — list should be visible
      expect(find.text('Sede Principal'), findsOneWidget);
    });
  });
}
