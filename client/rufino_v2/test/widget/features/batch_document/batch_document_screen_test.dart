import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mocktail/mocktail.dart';
import 'package:provider/provider.dart';
import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/core/utils/page_rotation_finder.dart';
import 'package:rufino_v2/domain/entities/batch_document_unit.dart';
import 'package:rufino_v2/domain/entities/document_group_with_templates.dart';
import 'package:rufino_v2/domain/entities/employee.dart';
import 'package:rufino_v2/domain/entities/permission.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/permission_notifier.dart';
import 'package:rufino_v2/ui/features/batch_document/viewmodel/batch_document_viewmodel.dart';
import 'package:rufino_v2/ui/features/batch_document/widgets/batch_document_screen.dart';

import '../../../testing/fakes/fake_employee_repository.dart';
import '../../../testing/fakes/fake_permission_repository.dart';
import '../../../testing/mocks/mocks.dart';

void main() {
  late MockBatchDocumentRepository mockBatchRepo;
  late MockDocumentGroupRepository mockGroupRepo;
  late FakeEmployeeRepository fakeEmployeeRepo;
  late BatchDocumentViewModel viewModel;
  late PermissionNotifier permissionNotifier;

  const groups = [
    DocumentGroupWithTemplates(
      id: 'g1',
      name: 'Admissão',
      description: '',
      templates: [
        DocumentTemplateSummary(id: 't1', name: 'Contrato', description: ''),
        DocumentTemplateSummary(id: 't2', name: 'Ficha', description: ''),
      ],
    ),
  ];

  const pendingUnits = [
    BatchDocumentUnitItem(
      documentUnitId: 'du1',
      documentId: 'd1',
      documentTemplateId: 't1',      documentTemplateName: 'T1',      documentGroupName: 'Grupo',      employeeId: 'e1',
      employeeName: 'João Silva',
      employeeStatusId: '2',
      employeeStatusName: 'Ativo',
      date: '15/03/2026',
      statusId: '1',
      statusName: 'Pendente',
      isSignable: true,
      canGenerateDocument: false,
    ),
    BatchDocumentUnitItem(
      documentUnitId: 'du2',
      documentId: 'd2',
      documentTemplateId: 't1',      documentTemplateName: 'T1',      documentGroupName: 'Grupo',      employeeId: 'e2',
      employeeName: 'Maria Santos',
      employeeStatusId: '2',
      employeeStatusName: 'Ativo',
      date: '16/03/2026',
      statusId: '1',
      statusName: 'Pendente',
      isSignable: false,
      canGenerateDocument: false,
    ),
  ];

  setUp(() async {
    mockBatchRepo = MockBatchDocumentRepository();
    mockGroupRepo = MockDocumentGroupRepository();

    when(() => mockGroupRepo.getDocumentGroupsWithTemplates(any()))
        .thenAnswer((_) async => const Result.success(groups));

    when(() => mockBatchRepo.getPendingDocumentUnits(
          any(),          documentGroupId: any(named: 'documentGroupId'),          documentTemplateId: any(named: 'documentTemplateId'),          employeeId: any(named: 'employeeId'),                    employeeStatusId: any(named: 'employeeStatusId'),
          employeeName: any(named: 'employeeName'),
          periodTypeId: any(named: 'periodTypeId'),
          periodYear: any(named: 'periodYear'),
          periodMonth: any(named: 'periodMonth'),
          periodDay: any(named: 'periodDay'),
          periodWeek: any(named: 'periodWeek'),
          pageSize: any(named: 'pageSize'),
          pageNumber: any(named: 'pageNumber'),
        )).thenAnswer((_) async => const Result.success(
          BatchDocumentUnitsPage(items: pendingUnits, totalCount: 2),
        ));

    fakeEmployeeRepo = FakeEmployeeRepository()
      ..setEmployees(const [
        Employee(
          id: 'e1',
          name: 'João Silva',
          registration: '001',
          status: EmployeeStatus.active,
          roleName: 'Pedreiro',
          documentStatus: DocumentStatus.ok,
        ),
      ]);

    viewModel = BatchDocumentViewModel(
      batchDocumentRepository: mockBatchRepo,
      documentGroupRepository: mockGroupRepo,
      employeeRepository: fakeEmployeeRepo,
      companyId: 'company-1',
    );

    final fakePermRepo = FakePermissionRepository()
      ..setPermissions(const [
        Permission(resource: 'document', scopes: [
          'create',
          'edit',
          'view',
          'upload',
          'send2sign',
          'generate',
        ]),
      ]);
    permissionNotifier =
        PermissionNotifier(permissionRepository: fakePermRepo);
    await permissionNotifier.loadPermissions();
  });

  tearDown(() {
    viewModel.dispose();
    permissionNotifier.dispose();
  });

  Widget buildSubject() {
    return ChangeNotifierProvider<PermissionNotifier>.value(
      value: permissionNotifier,
      child: MaterialApp.router(
        routerConfig: GoRouter(
          routes: [
            GoRoute(
              path: '/',
              builder: (_, __) =>
                  BatchDocumentScreen(viewModel: viewModel),
            ),
            GoRoute(
              path: '/home',
              builder: (_, __) =>
                  const Scaffold(body: Text('Home')),
            ),
          ],
        ),
      ),
    );
  }

  group('BatchDocumentScreen', () {
    testWidgets('renders the screen title', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Documentos em Lote'), findsOneWidget);
    });

    testWidgets('renders the three scope fields', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Escopo'), findsOneWidget);
      expect(find.byKey(const ValueKey('employee-scope-field')),
          findsOneWidget);
      expect(find.text('Grupo de Documentos'), findsOneWidget);
      expect(find.text('Documento'), findsOneWidget);
    });

    testWidgets('shows filters, actions and list without choosing a scope',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();
      await tester.drag(find.byType(CustomScrollView), const Offset(0, -300));
      await tester.pumpAndSettle();

      expect(find.text('Busca e filtros'), findsOneWidget);
      expect(find.text('Selecionar Todos'), findsOneWidget);
      expect(find.text('Criar Docs Faltantes'), findsOneWidget);
    });

    testWidgets('disables creating pendings until a document scope is chosen',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();
      await tester.drag(find.byType(CustomScrollView), const Offset(0, -300));
      await tester.pumpAndSettle();

      final button = tester.widget<OutlinedButton>(
        find.ancestor(
          of: find.text('Criar Docs Faltantes'),
          matching: find.byType(OutlinedButton),
        ),
      );
      expect(button.onPressed, isNull);
    });

    testWidgets('scopes the query to the employee picked in the dialog',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const ValueKey('employee-scope-field')));
      await tester.pumpAndSettle();

      expect(find.text('Buscar Funcionário'), findsOneWidget);

      await tester.enterText(
          find.byKey(const ValueKey('employee-picker-search')), 'João');
      await tester.testTextInput.receiveAction(TextInputAction.search);
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const ValueKey('employee-option-e1')));
      await tester.pumpAndSettle();

      expect(viewModel.selectedEmployeeId, 'e1');
      expect(fakeEmployeeRepo.lastNameFilter, 'João');
      verify(() => mockBatchRepo.getPendingDocumentUnits(
            any(),
            documentGroupId: any(named: 'documentGroupId'),
            documentTemplateId: any(named: 'documentTemplateId'),
            employeeId: 'e1',
            employeeStatusId: any(named: 'employeeStatusId'),
            employeeName: any(named: 'employeeName'),
            periodTypeId: any(named: 'periodTypeId'),
            periodYear: any(named: 'periodYear'),
            periodMonth: any(named: 'periodMonth'),
            periodDay: any(named: 'periodDay'),
            periodWeek: any(named: 'periodWeek'),
            pageSize: any(named: 'pageSize'),
            pageNumber: any(named: 'pageNumber'),
          )).called(1);
    });

    testWidgets('clears the employee scope back to all employees',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();
      await viewModel.selectEmployee('e1', 'João Silva');
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const ValueKey('employee-scope-clear')));
      await tester.pumpAndSettle();

      expect(viewModel.selectedEmployeeId, isNull);
      expect(find.text('Todos os funcionários'), findsOneWidget);
    });

    testWidgets('scopes the query to the chosen group', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const ValueKey('group-scope')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Admissão').last);
      await tester.pumpAndSettle();

      verify(() => mockBatchRepo.getPendingDocumentUnits(
            any(),
            documentGroupId: 'g1',
            documentTemplateId: null,
            employeeId: any(named: 'employeeId'),
            employeeStatusId: any(named: 'employeeStatusId'),
            employeeName: any(named: 'employeeName'),
            periodTypeId: any(named: 'periodTypeId'),
            periodYear: any(named: 'periodYear'),
            periodMonth: any(named: 'periodMonth'),
            periodDay: any(named: 'periodDay'),
            periodWeek: any(named: 'periodWeek'),
            pageSize: any(named: 'pageSize'),
            pageNumber: any(named: 'pageNumber'),
          )).called(1);
    });

    testWidgets('shows the group and template of each row', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.drag(
        find.byType(CustomScrollView),
        const Offset(0, -400),
      );
      await tester.pumpAndSettle();

      expect(find.text('Grupo · T1'), findsAtLeastNWidgets(1));
    });

    testWidgets('shows employee names in the document list after loading',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Scroll down to reveal list items below the filter section.
      await tester.drag(
        find.byType(CustomScrollView),
        const Offset(0, -400),
      );
      await tester.pumpAndSettle();

      expect(find.text('João Silva'), findsOneWidget);
      expect(find.text('Maria Santos'), findsOneWidget);
    });

    testWidgets('shows empty state when no pending units exist',
        (tester) async {
      when(() => mockBatchRepo.getPendingDocumentUnits(
            any(),            documentGroupId: any(named: 'documentGroupId'),            documentTemplateId: any(named: 'documentTemplateId'),            employeeId: any(named: 'employeeId'),                        employeeStatusId: any(named: 'employeeStatusId'),
            employeeName: any(named: 'employeeName'),
            periodTypeId: any(named: 'periodTypeId'),
            periodYear: any(named: 'periodYear'),
            periodMonth: any(named: 'periodMonth'),
            periodDay: any(named: 'periodDay'),
            periodWeek: any(named: 'periodWeek'),
            pageSize: any(named: 'pageSize'),
            pageNumber: any(named: 'pageNumber'),
          )).thenAnswer((_) async => const Result.success(
            BatchDocumentUnitsPage(items: [], totalCount: 0),
          ));

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Scroll down to reveal empty state below the filter section.
      await tester.drag(
        find.byType(CustomScrollView),
        const Offset(0, -400),
      );
      await tester.pumpAndSettle();

      expect(find.text('Nenhum documento pendente encontrado.'), findsOneWidget);
    });

    testWidgets('shows Selecionar Todos filter chip', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();
      await tester.drag(find.byType(CustomScrollView), const Offset(0, -300));
      await tester.pumpAndSettle();

      expect(find.byType(FilterChip), findsOneWidget);
    });

    testWidgets('upload button shows staged file count of zero',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();
      await tester.drag(find.byType(CustomScrollView), const Offset(0, -300));
      await tester.pumpAndSettle();

      expect(find.text('Enviar (0)'), findsOneWidget);
    });

    testWidgets('shows status badges for employees and documents',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Scroll down to reveal list items.
      await tester.drag(
        find.byType(CustomScrollView),
        const Offset(0, -400),
      );
      await tester.pumpAndSettle();

      // Employee status badges.
      expect(find.text('Ativo'), findsAtLeastNWidgets(1));
      // Document status badges.
      expect(find.text('Pendente'), findsAtLeastNWidgets(1));
    });

    testWidgets('navigates back to home when back button is pressed',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();

      expect(find.text('Home'), findsOneWidget);
    });
  });

  group('keyboard dismissal in competência filter', () {
    Future<void> openFilterAndPickMensal(WidgetTester tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.text('Competência').first);
      await tester.pumpAndSettle();
      await tester.tap(find.text('Mensal').last);
      await tester.pumpAndSettle();
    }

    testWidgets('period TextFields declare TextInputAction.done',
        (tester) async {
      await openFilterAndPickMensal(tester);

      final yearField = tester.widget<TextField>(
        find.widgetWithText(TextField, 'Ano'),
      );
      final monthField = tester.widget<TextField>(
        find.widgetWithText(TextField, 'Mês'),
      );

      expect(yearField.textInputAction, TextInputAction.done);
      expect(monthField.textInputAction, TextInputAction.done);
    });

    testWidgets('pressing done on Ano releases focus and dismisses keyboard',
        (tester) async {
      await openFilterAndPickMensal(tester);

      await tester.enterText(
        find.widgetWithText(TextField, 'Ano'),
        '2026',
      );

      EditableText anoEditable() => tester.widget<EditableText>(
            find.descendant(
              of: find.widgetWithText(TextField, 'Ano'),
              matching: find.byType(EditableText),
            ),
          );

      expect(anoEditable().focusNode.hasFocus, isTrue);

      await tester.testTextInput.receiveAction(TextInputAction.done);
      await tester.pumpAndSettle();

      expect(
        anoEditable().focusNode.hasFocus,
        isFalse,
        reason: 'Done deve liberar o foco e fechar o teclado.',
      );
    });

    testWidgets(
        'body has a GestureDetector that unfocuses fields when tapped',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Focus the name field.
      await tester
          .tap(find.widgetWithText(TextField, 'Nome do Funcionário'));
      await tester.pumpAndSettle();

      EditableText nameEditable() => tester.widget<EditableText>(
            find.descendant(
              of: find.widgetWithText(TextField, 'Nome do Funcionário'),
              matching: find.byType(EditableText),
            ),
          );

      expect(nameEditable().focusNode.hasFocus, isTrue);

      // The Scaffold body is the GestureDetector built by
      // BatchDocumentScreen specifically to dismiss focus on tap.
      // Invoke its callback to verify the wiring without depending on
      // hit-test geometry or sibling gesture detectors in the tree.
      final scaffold = tester
          .widgetList<Scaffold>(find.byType(Scaffold))
          .firstWhere((s) => s.body is GestureDetector);
      final detector = scaffold.body! as GestureDetector;
      expect(detector.behavior, HitTestBehavior.opaque);
      expect(detector.onTap, isNotNull);
      detector.onTap!.call();
      await tester.pumpAndSettle();

      expect(nameEditable().focusNode.hasFocus, isFalse);
    });
  });

  group('confirmation dialog before sending', () {
    Future<void> selectGroupAndTemplate(WidgetTester tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const ValueKey('group-scope')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Admissão').last);
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(
          const ValueKey('template-scope-g1')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Contrato').last);
      await tester.pumpAndSettle();
    }

    testWidgets(
        'Enviar (N) opens confirmation dialog and uploads on confirm',
        (tester) async {
      when(() => mockBatchRepo.uploadDocumentRange(any(), any()))
          .thenAnswer((_) async => const Result.success([]));

      await selectGroupAndTemplate(tester);

      // Stage a file for João Silva so the Enviar button enables.
      viewModel.stageFile(
        'du1',
        'd1',
        'e1',
        Uint8List.fromList(const [1, 2, 3]),
        'contrato.pdf',
      );
      await tester.pumpAndSettle();

      // The action bar may render below the viewport; ensure it's
      // scrolled into view before tapping.
      await tester.drag(find.byType(CustomScrollView), const Offset(0, -300));
      await tester.pumpAndSettle();

      await tester.ensureVisible(find.text('Enviar (1)'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Enviar (1)'));
      await tester.pumpAndSettle();

      // Dialog visible with the staged employee's name.
      expect(find.text('Confirmar Envio'), findsOneWidget);
      expect(find.text('João Silva'), findsWidgets);

      // Now there are two "Enviar (1)": the action bar (covered by dialog)
      // and the dialog's confirm — the latter is the topmost, last finder.
      await tester.tap(find.text('Enviar (1)').last);
      await tester.pumpAndSettle();

      verify(() => mockBatchRepo.uploadDocumentRange(
            'company-1',
            any(that: isA<List<BatchUploadItem>>()),
          )).called(1);
    });

    testWidgets('cancelling the confirmation does not call the repository',
        (tester) async {
      await selectGroupAndTemplate(tester);

      viewModel.stageFile(
        'du1',
        'd1',
        'e1',
        Uint8List.fromList(const [1, 2, 3]),
        'contrato.pdf',
      );
      await tester.pumpAndSettle();

      await tester.drag(find.byType(CustomScrollView), const Offset(0, -300));
      await tester.pumpAndSettle();

      await tester.ensureVisible(find.text('Enviar (1)'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Enviar (1)'));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();

      verifyNever(() => mockBatchRepo.uploadDocumentRange(any(), any()));
    });

    testWidgets(
        'Gerar e Assinar shows confirmation after date-limit and omits Data do Documento',
        (tester) async {
      when(() => mockBatchRepo.generateAndSignRange(any(), any(), any(), any()))
          .thenAnswer((_) async => const Result.success(null));

      // The pendingUnits fixture has canGenerateDocument=false; override
      // the response so the button enables.
      when(() => mockBatchRepo.getPendingDocumentUnits(
            any(),            documentGroupId: any(named: 'documentGroupId'),            documentTemplateId: any(named: 'documentTemplateId'),            employeeId: any(named: 'employeeId'),                        employeeStatusId: any(named: 'employeeStatusId'),
            employeeName: any(named: 'employeeName'),
            periodTypeId: any(named: 'periodTypeId'),
            periodYear: any(named: 'periodYear'),
            periodMonth: any(named: 'periodMonth'),
            periodDay: any(named: 'periodDay'),
            periodWeek: any(named: 'periodWeek'),
            pageSize: any(named: 'pageSize'),
            pageNumber: any(named: 'pageNumber'),
          )).thenAnswer((_) async => const Result.success(
            BatchDocumentUnitsPage(
              items: [
                BatchDocumentUnitItem(
                  documentUnitId: 'du1',
                  documentId: 'd1',
                  documentTemplateId: 't1',                  documentTemplateName: 'T1',                  documentGroupName: 'Grupo',                  employeeId: 'e1',
                  employeeName: 'João Silva',
                  employeeStatusId: '2',
                  employeeStatusName: 'Ativo',
                  date: '15/03/2026',
                  statusId: '1',
                  statusName: 'Pendente',
                  isSignable: true,
                  canGenerateDocument: true,
                ),
              ],
              totalCount: 1,
            ),
          ));

      await selectGroupAndTemplate(tester);
      viewModel.toggleSelection('du1');
      await tester.pumpAndSettle();

      await tester.drag(find.byType(CustomScrollView), const Offset(0, -300));
      await tester.pumpAndSettle();

      await tester.ensureVisible(find.text('Gerar e Assinar'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Gerar e Assinar'));
      await tester.pumpAndSettle();

      // Step 1: date-limit dialog appears.
      expect(find.text('Gerar e Enviar para Assinar'), findsOneWidget);
      await tester.enterText(find.byType(TextFormField), '20/04/2026');
      await tester.tap(find.text('Confirmar'));
      await tester.pumpAndSettle();

      // Step 2: confirmation dialog appears WITHOUT the Data do Documento
      // column (no staged attachments for generate flow).
      expect(find.text('Confirmar Geração e Assinatura'), findsOneWidget);
      expect(find.text('Data do Documento'), findsNothing);

      await tester.tap(find.text('Gerar e Assinar (1)'));
      await tester.pumpAndSettle();

      verify(() => mockBatchRepo.generateAndSignRange(
            'company-1',
            any(that: isA<List<BatchDocumentUnitItem>>()),
            any(),
            any(),
          )).called(1);
    });
  });

  group('BatchDocumentScreen scanning session', () {
    late MockDocumentScannerRepository mockScanner;

    /// Rotation stub that never rotates, so the real image decoder and the
    /// compute isolate stay out of the widget test.
    Future<RotationCandidate?> noRotation({
      required Uint8List originalBytes,
      required String originalText,
      required Future<String> Function(Uint8List) recognizeText,
    }) async =>
        null;

    List<Uint8List> pages(int count) => [
          for (var i = 0; i < count; i++) Uint8List.fromList([0xFF, 0xD8, i]),
        ];

    setUp(() {
      registerFallbackValue(Uint8List(0));
      registerFallbackValue(<Uint8List>[]);

      mockScanner = MockDocumentScannerRepository();
      when(() => mockScanner.isPlatformSupported).thenReturn(true);
      when(() => mockScanner.recognizeText(any())).thenAnswer((_) async => '');
      when(() => mockScanner.imagesToPdf(any()))
          .thenAnswer((_) async => Uint8List.fromList([0x25, 0x50, 0x44]));

      viewModel.dispose();
      viewModel = BatchDocumentViewModel(
        batchDocumentRepository: mockBatchRepo,
        documentGroupRepository: mockGroupRepo,
        employeeRepository: fakeEmployeeRepo,
        companyId: 'company-1',
        scannerRepository: mockScanner,
        pageRotationFinder: noRotation,
        textExtractor: (_) async => '',
      );
    });

    /// Opens the screen, scans one stack of [pageCount] pages and stops at
    /// the session dialog.
    Future<void> scanOnce(WidgetTester tester, int pageCount) async {
      when(() => mockScanner.scanPages())
          .thenAnswer((_) async => Result.success(pages(pageCount)));

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();
      await tester.drag(find.byType(CustomScrollView), const Offset(0, -300));
      await tester.pumpAndSettle();

      await tester.ensureVisible(find.text('Digitalizar'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Digitalizar'));
      await tester.pumpAndSettle();
    }

    testWidgets('offers splitting a lone scan of several pages',
        (tester) async {
      await scanOnce(tester, 4);

      expect(find.text('1 documento digitalizado'), findsOneWidget);
      expect(find.byKey(const Key('scan-session-split')), findsOneWidget);
    });

    testWidgets('does not offer splitting a single page', (tester) async {
      await scanOnce(tester, 1);

      expect(find.byKey(const Key('scan-session-split')), findsNothing);
      expect(find.byKey(const Key('scan-session-scan-more')), findsOneWidget);
    });

    testWidgets('does not offer splitting once a second document is scanned',
        (tester) async {
      await scanOnce(tester, 4);

      await tester.tap(find.byKey(const Key('scan-session-scan-more')));
      await tester.pumpAndSettle();

      expect(find.text('2 documentos digitalizados'), findsOneWidget);
      expect(find.byKey(const Key('scan-session-split')), findsNothing);
    });

    testWidgets('processes one document per part after a split',
        (tester) async {
      await scanOnce(tester, 4);

      await tester.tap(find.byKey(const Key('scan-session-split')));
      await tester.pumpAndSettle();

      expect(find.text('4 páginas capturadas.'), findsOneWidget);
      await tester.enterText(find.byKey(const Key('split-pages-field')), '2');
      await tester.tap(find.byKey(const Key('split-scan-confirm')));
      await tester.pumpAndSettle();

      expect(find.text('2 documentos'), findsOneWidget);
      await tester.tap(find.byKey(const Key('split-result-process')));
      await tester.pumpAndSettle();

      // One PDF per part: the four scanned pages became two documents.
      verify(() => mockScanner.imagesToPdf(any())).called(2);
    });

    testWidgets('returns to the session dialog when the split is cancelled',
        (tester) async {
      await scanOnce(tester, 4);

      await tester.tap(find.byKey(const Key('scan-session-split')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('split-scan-cancel')));
      await tester.pumpAndSettle();

      expect(find.text('1 documento digitalizado'), findsOneWidget);
      expect(find.byKey(const Key('scan-session-split')), findsOneWidget);
      verifyNever(() => mockScanner.imagesToPdf(any()));
    });

    testWidgets('processes nothing when the split result is discarded',
        (tester) async {
      await scanOnce(tester, 4);

      await tester.tap(find.byKey(const Key('scan-session-split')));
      await tester.pumpAndSettle();
      await tester.enterText(find.byKey(const Key('split-pages-field')), '2');
      await tester.tap(find.byKey(const Key('split-scan-confirm')));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('split-result-discard')));
      await tester.pumpAndSettle();

      verifyNever(() => mockScanner.imagesToPdf(any()));
      expect(find.text('2 documentos'), findsNothing);
    });
  });
}
