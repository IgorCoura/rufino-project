import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_v2/domain/entities/batch_download.dart';
import 'package:rufino_v2/domain/entities/permission.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/permission_notifier.dart';
import 'package:rufino_v2/ui/features/batch_download/viewmodel/batch_download_viewmodel.dart';
import 'package:rufino_v2/ui/features/batch_download/widgets/batch_download_screen.dart';

import '../../../testing/fakes/fake_batch_download_repository.dart';
import '../../../testing/fakes/fake_document_group_repository.dart';
import '../../../testing/fakes/fake_permission_repository.dart';
import '../../../testing/fakes/fake_workplace_repository.dart';

void main() {
  late FakeBatchDownloadRepository fakeBatchDownloadRepo;
  late FakeDocumentGroupRepository fakeDocGroupRepo;
  late FakeWorkplaceRepository fakeWorkplaceRepo;
  late FakePermissionRepository fakePermissionRepo;
  late PermissionNotifier permissionNotifier;
  late BatchDownloadViewModel viewModel;

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

  setUp(() async {
    fakeBatchDownloadRepo = FakeBatchDownloadRepository();
    fakeDocGroupRepo = FakeDocumentGroupRepository();
    fakeWorkplaceRepo = FakeWorkplaceRepository();
    fakePermissionRepo = FakePermissionRepository()
      ..setPermissions([
        const Permission(
          resource: 'document',
          scopes: ['create', 'view', 'edit', 'download'],
        ),
      ]);

    permissionNotifier = PermissionNotifier(
      permissionRepository: fakePermissionRepo,
    );
    await permissionNotifier.loadPermissions();

    fakeBatchDownloadRepo.employeesPage = BatchDownloadEmployeesPage(
      items: testEmployees,
      totalCount: testEmployees.length,
    );

    viewModel = BatchDownloadViewModel(
      batchDownloadRepository: fakeBatchDownloadRepo,
      documentGroupRepository: fakeDocGroupRepo,
      workplaceRepository: fakeWorkplaceRepo,
      companyId: 'test-company',
    );
  });

  tearDown(() {
    viewModel.dispose();
    permissionNotifier.dispose();
  });

  void setupLargeScreen(WidgetTester tester) {
    tester.view.physicalSize = const Size(1400, 1000);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    // Suppress overflow errors in widget tests — dropdowns with small
    // intrinsic widths can overflow in the test environment.
    final originalOnError = FlutterError.onError;
    FlutterError.onError = (details) {
      if (details.toString().contains('overflowed')) return;
      originalOnError?.call(details);
    };
    addTearDown(() => FlutterError.onError = originalOnError);
  }

  Widget buildTestWidget() {
    return ChangeNotifierProvider<PermissionNotifier>.value(
      value: permissionNotifier,
      child: MaterialApp(
        home: BatchDownloadScreen(viewModel: viewModel),
      ),
    );
  }

  group('BatchDownloadScreen', () {
    testWidgets('renders step 1 with employee list on launch',
        (tester) async {
      setupLargeScreen(tester);
      await tester.pumpWidget(buildTestWidget());
      await tester.pumpAndSettle();

      expect(find.text('Download em Lote'), findsOneWidget);
      expect(find.text('1. Funcionarios'), findsOneWidget);
      expect(find.text('2. Documentos'), findsOneWidget);
      expect(find.text('3. Revisar'), findsOneWidget);
      expect(find.text('Alice Silva'), findsOneWidget);
      expect(find.text('Bob Santos'), findsOneWidget);
    });

    testWidgets('no employees selected means zero count displayed',
        (tester) async {
      setupLargeScreen(tester);
      await tester.pumpWidget(buildTestWidget());
      await tester.pumpAndSettle();

      expect(find.text('0 selecionado(s)'), findsOneWidget);
    });

    testWidgets('selecting an employee updates selected count',
        (tester) async {
      setupLargeScreen(tester);
      await tester.pumpWidget(buildTestWidget());
      await tester.pumpAndSettle();

      await tester.tap(find.text('Alice Silva'));
      await tester.pumpAndSettle();

      expect(find.text('1 selecionado(s)'), findsOneWidget);
    });

    testWidgets('Selecionar Todos selects all employees on page',
        (tester) async {
      setupLargeScreen(tester);
      await tester.pumpWidget(buildTestWidget());
      await tester.pumpAndSettle();

      await tester.tap(find.text('Selecionar Todos'));
      await tester.pumpAndSettle();

      expect(find.text('2 selecionado(s)'), findsOneWidget);
    });

    testWidgets('displays filter controls in step 1', (tester) async {
      setupLargeScreen(tester);
      await tester.pumpWidget(buildTestWidget());
      await tester.pumpAndSettle();

      expect(find.text('Nome'), findsOneWidget);
      expect(find.text('Status'), findsOneWidget);
    });
  });
}
