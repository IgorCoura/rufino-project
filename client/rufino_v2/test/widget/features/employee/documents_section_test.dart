import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/document_group_with_documents.dart';
import 'package:rufino_v2/domain/entities/employee.dart';
import 'package:rufino_v2/domain/entities/employee_document.dart';
import 'package:rufino_v2/domain/entities/employee_profile.dart';
import 'package:rufino_v2/domain/entities/permission.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/permission_notifier.dart';
import 'package:rufino_v2/ui/features/employee/viewmodel/employee_profile_viewmodel.dart';
import 'package:rufino_v2/ui/features/employee/widgets/components/documents_section.dart';

import '../../../testing/fakes/fake_cep_repository.dart';
import '../../../testing/fakes/fake_company_repository.dart';
import '../../../testing/fakes/fake_department_repository.dart';
import '../../../testing/fakes/fake_document_group_repository.dart';
import '../../../testing/fakes/fake_employee_repository.dart';
import '../../../testing/fakes/fake_permission_repository.dart';
import '../../../testing/fakes/fake_workplace_repository.dart';

const _fakeCompany = Company(
  id: 'company-1',
  corporateName: 'Acme Corp',
  fantasyName: 'Acme',
  cnpj: '00000000000000',
);

const _signableDocument = EmployeeDocument(
  id: 'doc-1',
  name: 'Contrato de Trabalho',
  description: 'Contrato CLT',
  statusId: '1',
  statusName: 'Pendente',
  isSignable: true,
  canGenerateDocument: true,
  usePreviousPeriod: false,
  totalUnitsCount: 1,
  units: [
    DocumentUnit(
      id: 'unit-1',
      statusId: '1',
      statusName: 'Pendente',
      date: '01/01/2026',
      validity: '',
      createdAt: '01/01/2026',
      hasFile: false,
      name: '',
    ),
  ],
);

const _fakeGroup = DocumentGroupWithDocuments(
  id: 'grp-1',
  name: 'Grupo Contratual',
  description: 'Documentos contratuais',
  statusId: '2',
  statusName: 'Pendente',
  documents: [_signableDocument],
);

void main() {
  late FakeEmployeeRepository employeeRepository;
  late PermissionNotifier permissionNotifier;
  late EmployeeProfileViewModel viewModel;

  setUp(() async {
    employeeRepository = FakeEmployeeRepository()
      ..setDocumentsList(const [_signableDocument]);
    viewModel = EmployeeProfileViewModel(
      companyRepository: FakeCompanyRepository()
        ..setSelectedCompany(_fakeCompany),
      employeeRepository: employeeRepository,
      departmentRepository: FakeDepartmentRepository(),
      workplaceRepository: FakeWorkplaceRepository(),
      documentGroupRepository: FakeDocumentGroupRepository()
        ..setGroupsWithDocuments(const [_fakeGroup]),
      cepRepository: FakeCepRepository(),
    );
    final fakePermRepo = FakePermissionRepository()
      ..setPermissions(const [
        Permission(
          resource: 'document',
          scopes: [
            'create',
            'view',
            'edit',
            'upload',
            'download',
            'generate',
            'send2sign',
            'mark-not-applicable',
          ],
        ),
      ]);
    permissionNotifier = PermissionNotifier(permissionRepository: fakePermRepo);
    await permissionNotifier.loadPermissions();
  });

  tearDown(() {
    viewModel.dispose();
    permissionNotifier.dispose();
  });

  /// Loads the profile with [signingOptionId], pumps the documents section
  /// and expands the group and document tiles down to the unit row.
  Future<void> pumpExpandedSection(
    WidgetTester tester, {
    required String signingOptionId,
  }) async {
    tester.view.physicalSize = const Size(1200, 2400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.reset);

    employeeRepository.setEmployeeProfile(EmployeeProfile(
      id: 'emp-1',
      name: 'Ana Lima',
      registration: 'R001',
      status: EmployeeStatus.active,
      roleId: 'role-1',
      workplaceId: 'wp-1',
      documentSigningOptionsId: signingOptionId,
    ));
    await viewModel.load('emp-1');
    await viewModel.openTab(EmployeeProfileTab.documents);

    await tester.pumpWidget(
      ChangeNotifierProvider<PermissionNotifier>.value(
        value: permissionNotifier,
        child: MaterialApp(
          home: Scaffold(
            body: SingleChildScrollView(
              child: DocumentsSection(viewModel: viewModel),
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Grupo Contratual'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Contrato de Trabalho'));
    await tester.pumpAndSettle();
  }

  group('DocumentsSection send-to-signature visibility', () {
    testWidgets(
        'shows the send-to-signature button in the send dialog when the '
        'employee has a digital signing option', (tester) async {
      await pumpExpandedSection(tester, signingOptionId: '2');

      await tester.tap(find.byTooltip('Enviar'));
      await tester.pumpAndSettle();

      expect(find.text('Enviar para assinatura'), findsOneWidget);
      expect(find.text('Enviar arquivo'), findsOneWidget);
    });

    testWidgets(
        'shows the generate-and-send button in the generate dialog when the '
        'employee has a digital signing option', (tester) async {
      await pumpExpandedSection(tester, signingOptionId: '2');

      await tester.tap(find.byTooltip('Gerar'));
      await tester.pumpAndSettle();

      expect(find.text('Gerar e enviar para assinatura'), findsOneWidget);
      expect(find.text('Gerar arquivo'), findsOneWidget);
    });

    testWidgets(
        'hides the send-to-signature button in the send dialog when the '
        'employee has a physical signing option', (tester) async {
      await pumpExpandedSection(tester, signingOptionId: '1');

      await tester.tap(find.byTooltip('Enviar'));
      await tester.pumpAndSettle();

      expect(find.text('Enviar para assinatura'), findsNothing);
      expect(find.text('Enviar arquivo'), findsOneWidget);
    });

    testWidgets(
        'hides the generate-and-send button in the generate dialog when the '
        'employee has a physical signing option', (tester) async {
      await pumpExpandedSection(tester, signingOptionId: '1');

      await tester.tap(find.byTooltip('Gerar'));
      await tester.pumpAndSettle();

      expect(find.text('Gerar e enviar para assinatura'), findsNothing);
      expect(find.text('Gerar arquivo'), findsOneWidget);
    });

    testWidgets(
        'hides both signature buttons when the employee has no signing '
        'option configured', (tester) async {
      await pumpExpandedSection(tester, signingOptionId: '');

      await tester.tap(find.byTooltip('Enviar'));
      await tester.pumpAndSettle();
      expect(find.text('Enviar para assinatura'), findsNothing);

      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();

      await tester.tap(find.byTooltip('Gerar'));
      await tester.pumpAndSettle();
      expect(find.text('Gerar e enviar para assinatura'), findsNothing);
    });
  });
}
