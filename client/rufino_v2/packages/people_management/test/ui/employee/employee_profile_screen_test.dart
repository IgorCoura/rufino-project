import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:people_management/people_management.dart';
import 'package:people_management/src/ui/employee/viewmodel/employee_profile_viewmodel.dart';
import 'package:people_management/src/ui/employee/widgets/employee_profile_screen.dart';
import 'package:people_management/src/ui/employee/widgets/components/contact_section.dart';
import 'package:people_management/src/ui/employee/widgets/components/id_card_section.dart';
import 'package:people_management/src/ui/employee/widgets/components/military_document_section.dart';
import 'package:people_management/src/ui/employee/widgets/components/medical_exam_section.dart';
import 'package:people_management/src/ui/employee/widgets/components/personal_info_section.dart';
import 'package:people_management/src/ui/employee/widgets/components/role_info_section.dart';
import 'package:people_management/src/ui/employee/widgets/components/vote_id_section.dart';

import '../../fakes/fake_company_repository.dart';
import '../../fakes/fake_department_repository.dart';
import '../../fakes/fake_document_group_repository.dart';
import '../../fakes/fake_employee_repository.dart';
import '../../fakes/fake_permission_repository.dart';
import '../../fakes/fake_cep_repository.dart';
import '../../fakes/fake_workplace_repository.dart';

const _fakeCompany = Company(
  id: 'company-1',
  corporateName: 'Acme Corp',
  fantasyName: 'Acme',
  cnpj: '00000000000000',
);

const _fakeProfile = EmployeeProfile(
  id: 'emp-1',
  name: 'Ana Lima',
  registration: 'R001',
  status: EmployeeStatus.active,
  roleId: 'role-1',
  workplaceId: 'wp-1',
);

const _fakeRole = Role(
  id: 'role-1',
  name: 'Analista',
  description: 'Analista financeira',
  cbo: '123456',
  remuneration: Remuneration(
    paymentUnit: PaymentUnit(id: '5', name: 'Por Mês'),
    baseSalary: BaseSalary(
      type: SalaryType(id: '1', name: 'BRL'),
      value: '3500.00',
    ),
    description: 'Salário mensal',
  ),
);

const _fakeWorkplace = Workplace(
  id: 'wp-1',
  name: 'Sede Principal',
  address: Address(
    zipCode: '01310100',
    street: 'Av. Paulista',
    number: '1000',
    complement: '',
    neighborhood: 'Bela Vista',
    city: 'São Paulo',
    state: 'SP',
    country: 'Brasil',
  ),
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeEmployeeRepository employeeRepository;
  late FakeDepartmentRepository departmentRepository;
  late FakeWorkplaceRepository workplaceRepository;
  late FakeDocumentGroupRepository documentGroupRepository;
  late PermissionNotifier permissionNotifier;
  late EmployeeProfileViewModel viewModel;

  setUp(() async {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    employeeRepository = FakeEmployeeRepository()
      ..setEmployeeProfile(_fakeProfile);
    departmentRepository = FakeDepartmentRepository()
      ..setRole(_fakeRole)
      ..setPaymentUnits(const [
        PaymentUnit(id: '5', name: 'Por Mês'),
      ])
      ..setSalaryTypes(const [
        SalaryType(id: '1', name: 'BRL'),
      ])
      ..setDepartments(const [
        Department(
          id: 'dept-1',
          name: 'Financeiro',
          description: 'Setor financeiro',
          positions: [
            Position(
              id: 'pos-1',
              name: 'Analista',
              description: 'Analista financeiro',
              cbo: '251210',
              roles: [_fakeRole],
            ),
          ],
        ),
      ]);
    workplaceRepository = FakeWorkplaceRepository()
      ..setWorkplace(_fakeWorkplace)
      ..setWorkplaces([_fakeWorkplace]);
    documentGroupRepository = FakeDocumentGroupRepository()
      ..setGroupsWithDocuments(const [
        DocumentGroupWithDocuments(
          id: 'grp-1',
          name: 'Grupo Contratual',
          description: 'Documentos contratuais',
          statusId: '0',
          statusName: 'Okay',
          documents: [
            EmployeeDocument(
              id: 'doc-1',
              name: 'Contrato de Trabalho',
              description: 'Contrato CLT',
              statusId: '3',
              statusName: 'OK',
              isSignable: false,
              canGenerateDocument: true,
              usePreviousPeriod: false,
              totalUnitsCount: 1,
              units: [
                DocumentUnit(
                  id: 'unit-1',
                  statusId: '2',
                  statusName: 'OK',
                  date: '01/01/2026',
                  validity: '',
                  createdAt: '01/01/2026',
                  hasFile: true,
                  name: 'contrato.pdf',
                ),
              ],
            ),
          ],
        ),
      ]);
    viewModel = EmployeeProfileViewModel(
      companyRepository: companyRepository,
      employeeRepository: employeeRepository,
      departmentRepository: departmentRepository,
      workplaceRepository: workplaceRepository,
      documentGroupRepository: documentGroupRepository,
      cepRepository: FakeCepRepository(),
    );
    final fakePermRepo = FakePermissionRepository()
      ..setPermissions(const [
        Permission(resource: 'employee', scopes: ['create', 'view', 'edit', 'upload', 'download']),
        Permission(resource: 'document', scopes: [
          'create',
          'view',
          'edit',
          'upload',
          'download',
          'deprecate',
          'reject',
          'mark-not-applicable',
        ]),
      ]);
    permissionNotifier = PermissionNotifier(permissionRepository: fakePermRepo);
    await permissionNotifier.loadPermissions();
  });

  tearDown(() {
    viewModel.dispose();
    permissionNotifier.dispose();
  });

  Widget buildSubject({
    EmployeeProfileTab initialTab = EmployeeProfileTab.personalData,
  }) =>
      ChangeNotifierProvider<PermissionNotifier>.value(
        value: permissionNotifier,
        child: MaterialApp.router(
          routerConfig: GoRouter(
            routes: [
              GoRoute(
                path: '/',
                builder: (_, __) => EmployeeProfileScreen(
                  viewModel: viewModel,
                  employeeId: 'emp-1',
                  initialTab: initialTab,
                ),
              ),
            ],
          ),
        ),
      );

  /// Finds the 'Editar' button inside a specific section widget type.
  Finder findEditIn<T extends Widget>() => find.descendant(
        of: find.byType(T),
        matching: find.text('Editar'),
      );

  group('EmployeeProfileScreen', () {
    group('per-tab loading', () {
      testWidgets(
          "loads only the visible tab's sections when the profile opens",
          (tester) async {
        await tester.pumpWidget(buildSubject());
        await tester.pumpAndSettle();

        expect(employeeRepository.getEmployeeContactCallCount, 1);
        expect(employeeRepository.getEmployeeAddressCallCount, 1);
        expect(documentGroupRepository.getGroupsWithDocumentsCallCount, 0);
        expect(employeeRepository.getDocumentSigningOptionsCallCount, 0);
        expect(employeeRepository.getContractsCallCount, 0);
        expect(employeeRepository.getMedicalExamCallCount, 0);
      });

      testWidgets(
          'lands straight on the documents tab when initialTab requests it',
          (tester) async {
        await tester.pumpWidget(
            buildSubject(initialTab: EmployeeProfileTab.documents));
        await tester.pumpAndSettle();

        expect(documentGroupRepository.getGroupsWithDocumentsCallCount, 1);
        expect(employeeRepository.getDocumentSigningOptionsCallCount, 1);
        expect(employeeRepository.getEmployeeContactCallCount, 0);
        expect(find.text('Grupo Contratual'), findsOneWidget);
      });

      testWidgets("reloads a tab's data when the user returns to it",
          (tester) async {
        await tester.pumpWidget(buildSubject());
        await tester.pumpAndSettle();

        await tester.tap(find.widgetWithText(Tab, 'Documentos'));
        await tester.pumpAndSettle();
        expect(documentGroupRepository.getGroupsWithDocumentsCallCount, 1);

        await tester.tap(find.widgetWithText(Tab, 'Dados Pessoais'));
        await tester.pumpAndSettle();
        expect(employeeRepository.getEmployeeContactCallCount, 2);

        await tester.tap(find.widgetWithText(Tab, 'Documentos'));
        await tester.pumpAndSettle();
        expect(documentGroupRepository.getGroupsWithDocumentsCallCount, 2);
      });

      testWidgets(
          'does not load the intermediate tab when jumping across tabs',
          (tester) async {
        await tester.pumpWidget(buildSubject());
        await tester.pumpAndSettle();

        await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
        await tester.pumpAndSettle();

        expect(employeeRepository.getContractsCallCount, 1);
        expect(employeeRepository.getMedicalExamCallCount, 1);
        expect(documentGroupRepository.getGroupsWithDocumentsCallCount, 0);
        expect(employeeRepository.getDocumentSigningOptionsCallCount, 0);
      });

      testWidgets(
          'discards an open edit form when the user returns to the tab',
          (tester) async {
        await tester.pumpWidget(buildSubject());
        await tester.pumpAndSettle();

        await tester.scrollUntilVisible(
          findEditIn<ContactSection>(),
          100,
          scrollable: find.byType(Scrollable).first,
        );
        await tester.tap(findEditIn<ContactSection>());
        await tester.pumpAndSettle();
        expect(
          find.descendant(
            of: find.byType(ContactSection),
            matching: find.text('Salvar'),
          ),
          findsOneWidget,
        );

        await tester.tap(find.widgetWithText(Tab, 'Documentos'));
        await tester.pumpAndSettle();
        await tester.tap(find.widgetWithText(Tab, 'Dados Pessoais'));
        await tester.pumpAndSettle();

        expect(
          find.descendant(
            of: find.byType(ContactSection),
            matching: find.text('Salvar'),
          ),
          findsNothing,
        );
        expect(findEditIn<ContactSection>(), findsOneWidget);
      });
    });

    testWidgets('shows loading indicator while fetching the profile',
        (tester) async {
      await tester.pumpWidget(buildSubject());

      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      await tester.pumpAndSettle();
    });

    testWidgets('shows employee information after loading', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Perfil do Funcionário'), findsOneWidget);
      expect(find.text('Ana Lima'), findsWidgets);
      expect(find.text('Registro R001'), findsOneWidget);
      expect(find.text('Ativo'), findsWidgets);
    });

    testWidgets('shows retry state when profile loading fails', (tester) async {
      employeeRepository.setShouldFail(true);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Não foi possível carregar o perfil.'), findsOneWidget);
      expect(find.text('Tentar novamente'), findsOneWidget);
    });

    testWidgets('marks the employee as inactive after confirmation',
        (tester) async {
      // Only a pending employee (no contracts yet) can be marked as inactive,
      // mirroring the backend rule (PMD.EMP17).
      employeeRepository
        ..setEmployeeProfile(
          _fakeProfile.copyWith(status: EmployeeStatus.pending),
        )
        ..setContracts(const []);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Contratos tab.
      await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Marcar como inativo'),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Marcar como inativo'));
      await tester.pumpAndSettle();

      expect(find.text('Confirmar ação'), findsOneWidget);

      await tester.tap(find.text('Confirmar'));
      await tester.pumpAndSettle();

      expect(find.text('Inativo'), findsWidgets);
      expect(
        find.text('Funcionário marcado como inativo com sucesso.'),
        findsOneWidget,
      );
    });

    testWidgets(
        'does not show the inactive action for active employees with a '
        'finished contract', (tester) async {
      employeeRepository.setContracts(const [
        EmployeeContractInfo(
          initDate: '01/01/2025',
          finalDate: '31/12/2025',
          typeId: '1',
          typeName: 'CLT',
        ),
      ]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
      await tester.pumpAndSettle();

      expect(find.text('Marcar como inativo'), findsNothing);
    });

    testWidgets('does not show the inactive action for inactive employees',
        (tester) async {
      employeeRepository.setEmployeeProfile(
        const EmployeeProfile(
          id: 'emp-1',
          name: 'Ana Lima',
          registration: 'R001',
          status: EmployeeStatus.inactive,
          roleId: 'role-1',
          workplaceId: 'wp-1',
        ),
      );

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Marcar como inativo'), findsNothing);
    });

    testWidgets('shows the avatar upload camera icon on the profile card',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.byIcon(Icons.camera_alt), findsOneWidget);
    });

    testWidgets('shows the name edit button on the name card', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Editar').first, findsOneWidget);
    });

    testWidgets('shows the name text field when the edit button is tapped',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.text('Editar').first);
      await tester.pumpAndSettle();

      expect(find.byType(TextField), findsOneWidget);
      expect(find.text('Salvar'), findsOneWidget);
      expect(find.text('Cancelar'), findsOneWidget);
    });

    testWidgets('saves the new name after editing and tapping save',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.text('Editar').first);
      await tester.pumpAndSettle();

      await tester.enterText(find.byType(TextField), 'Ana Souza');
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(find.text('Ana Souza'), findsWidgets);
      expect(
        find.text('Nome atualizado com sucesso.'),
        findsOneWidget,
      );
    });

    testWidgets('cancels name editing without saving when cancel is tapped',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.text('Editar').first);
      await tester.pumpAndSettle();

      await tester.enterText(find.byType(TextField), 'Ana Souza');
      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();

      expect(find.text('Ana Lima'), findsWidgets);
      expect(find.byType(TextField), findsNothing);
    });

    testWidgets('shows the Contato section title', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Contato'), findsOneWidget);
    });

    testWidgets('shows the Endereço section title', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Endereço'), findsOneWidget);
    });

    testWidgets('shows the Informações Pessoais section title', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Informações Pessoais'), findsOneWidget);
    });

    testWidgets('shows the Documento (Identidade) section title',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Documento (Identidade)'), findsOneWidget);
    });

    testWidgets('shows the Título de Eleitor section title', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Título de Eleitor'), findsOneWidget);
    });

    testWidgets(
        'expands the Contato section and shows loading then contact data',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Phone is formatted as "+55 DDD NNNNN-NNNN".
      expect(find.text('+55 11 99999-0000'), findsOneWidget);
      expect(find.text('test@example.com'), findsOneWidget);
    });

    testWidgets(
        'expands the Título de Eleitor section and shows the vote ID number',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('1234.5678.0698'), findsOneWidget);
    });

    testWidgets(
        'expands the Informações Pessoais section and shows personal data',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Homem'), findsOneWidget);
      expect(find.text('Casado(a)'), findsOneWidget);
      expect(find.text('Pardo'), findsOneWidget);
      expect(find.text('Ensino Superior Completo'), findsOneWidget);
    });

    testWidgets(
        'shows edit form with dropdowns when Editar is tapped on Informações Pessoais',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<PersonalInfoSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<PersonalInfoSection>());
      await tester.pumpAndSettle();

      // Dropdown labels and the disability add button should be visible.
      expect(find.text('Gênero'), findsOneWidget);
      expect(find.text('Estado Civil'), findsOneWidget);
      expect(find.text('Etnia'), findsOneWidget);
      expect(find.text('Escolaridade'), findsOneWidget);
      expect(find.text('Adicionar Deficiência'), findsOneWidget);
    });

    testWidgets(
        'adds a disability via the dialog in Informações Pessoais edit mode',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<PersonalInfoSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<PersonalInfoSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Adicionar Deficiência'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Adicionar Deficiência'));
      await tester.pumpAndSettle();

      // Dialog should be open with title and Adicionar button.
      expect(find.text('Adicionar Deficiência'), findsWidgets);
      expect(find.text('Adicionar'), findsOneWidget);

      await tester.tap(find.text('Adicionar'));
      await tester.pumpAndSettle();

      // The first available disability (Física) should now appear in the list.
      expect(find.text('Física'), findsOneWidget);
      // Observation field should now be visible.
      expect(find.text('Observações sobre a deficiência'), findsOneWidget);
    });

    testWidgets(
        'removes a disability with the close button in Informações Pessoais edit mode',
        (tester) async {
      employeeRepository.setPersonalInfo(
        const EmployeePersonalInfo(
          genderId: '1',
          maritalStatusId: '1',
          ethnicityId: '1',
          educationLevelId: '1',
          disabilityIds: ['1'],
          disabilityObservation: '',
        ),
      );

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<PersonalInfoSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<PersonalInfoSection>());
      await tester.pumpAndSettle();

      // Física should appear with a remove button.
      expect(find.text('Física'), findsOneWidget);

      await tester.ensureVisible(find.byTooltip('Remover deficiência'));
      await tester.pumpAndSettle();
      await tester.tap(find.byTooltip('Remover deficiência'));
      await tester.pumpAndSettle();

      // Física is gone; the observation field should be hidden.
      expect(find.text('Física'), findsNothing);
      expect(find.text('Observações sobre a deficiência'), findsNothing);
    });

    testWidgets(
        'expands the Documento (Identidade) section and shows identity data',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('111.444.777-35'), findsWidgets);
      expect(find.text('01/01/1990'), findsWidgets);
      expect(find.text('Maria'), findsWidgets);
      expect(find.text('João'), findsWidgets);
    });

    testWidgets(
        'shows edit form with masked fields when Editar is tapped on Documento (Identidade)',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<IdCardSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<IdCardSection>());
      await tester.pumpAndSettle();

      expect(find.text('CPF'), findsWidgets);
      expect(find.text('Data de nascimento'), findsWidgets);
      expect(find.text('Nome da mãe'), findsWidgets);
      expect(find.text('Salvar'), findsOneWidget);
      expect(find.text('Cancelar'), findsOneWidget);
    });

    testWidgets('saves ID card data and shows success snackbar',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<IdCardSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<IdCardSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(
        find.text('Dados do documento atualizados com sucesso.'),
        findsOneWidget,
      );
    });

    testWidgets(
        'keeps the ID card edit form visible and shows the server message '
        'when the server rejects the save with a validation error',
        (tester) async {
      // Simulates the backend rejecting the payload (blank father name) with
      // the domain validation error PMD18 on the Name field.
      const serverErrorBody = '{"errors":{"Name":[{"code":"PMD18",'
          '"message":"O campo Name, está em um formato invalido.",'
          '"properties":{"NameField":"Name"}}]}}';
      employeeRepository.setEditIdCardError(
        const HttpException(
          statusCode: 400,
          message: 'HTTP 400: Bad Request',
          serverMessages: ['O campo Name, está em um formato invalido.'],
          responseBody: serverErrorBody,
        ),
      );

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<IdCardSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<IdCardSection>());
      await tester.pumpAndSettle();

      // Clear the (optional) father name so the payload reaches the server.
      await tester.scrollUntilVisible(
        find.widgetWithText(TextFormField, 'Nome do pai'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Nome do pai'),
        '',
      );

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      // The server message is surfaced to the user.
      expect(
        find.text('O campo Name, está em um formato invalido.'),
        findsOneWidget,
      );
      // The form stays available so the user can fix the field and retry.
      expect(find.text('Salvar'), findsOneWidget);
      // The load-error placeholder must not replace the form.
      expect(
        find.text('Não foi possível carregar os dados do documento.'),
        findsNothing,
      );
    });

    testWidgets('cancels ID card editing without saving when Cancelar is tapped',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<IdCardSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<IdCardSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Cancelar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();

      // Should return to view mode showing the data.
      expect(find.text('111.444.777-35'), findsWidgets);
      expect(find.text('Salvar'), findsNothing);
    });

    testWidgets(
        'shows CPF required error when CPF is cleared before saving',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<IdCardSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<IdCardSection>());
      await tester.pumpAndSettle();

      // Clear the CPF field.
      await tester.scrollUntilVisible(
        find.widgetWithText(TextFormField, 'CPF'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.enterText(find.widgetWithText(TextFormField, 'CPF'), '');

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(find.text('O CPF não pode ser vazio.'), findsOneWidget);
      // No success snack when validation fails.
      expect(
        find.text('Dados do documento atualizados com sucesso.'),
        findsNothing,
      );
    });

    testWidgets(
        'shows CPF invalid error when the CPF algorithm check fails',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<IdCardSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<IdCardSection>());
      await tester.pumpAndSettle();

      // Enter a CPF that passes the digit count but fails the algorithm.
      await tester.scrollUntilVisible(
        find.widgetWithText(TextFormField, 'CPF'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'CPF'),
        '12345678901',
      );

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(find.text('O CPF não é válido.'), findsOneWidget);
    });

    testWidgets(
        'shows date of birth required error when the date is cleared before saving',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<IdCardSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<IdCardSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.widgetWithText(TextFormField, 'Data de nascimento'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Data de nascimento'),
        '',
      );

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(
        find.text('A Data de nascimento não pode ser vazia.'),
        findsOneWidget,
      );
    });

    testWidgets(
        'shows date of birth invalid error when a future date is entered',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<IdCardSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<IdCardSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.widgetWithText(TextFormField, 'Data de nascimento'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Data de nascimento'),
        '01012099',
      );

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(find.text('A Data de nascimento é inválida.'), findsOneWidget);
    });

    testWidgets(
        'shows mother name required error when mother name is cleared before saving',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<IdCardSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<IdCardSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.widgetWithText(TextFormField, 'Nome da mãe'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Nome da mãe'),
        '',
      );

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(find.text('O Nome da mãe não pode ser vazio.'), findsOneWidget);
    });

    testWidgets(
        'shows birth city required error when birth city is cleared before saving',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<IdCardSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<IdCardSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.widgetWithText(TextFormField, 'Município de nascimento'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Município de nascimento'),
        '',
      );

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(
        find.text('A Cidade de nascimento não pode ser vazia.'),
        findsOneWidget,
      );
    });

    testWidgets(
        'shows nationality required error when nationality is cleared before saving',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<IdCardSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<IdCardSection>());
      await tester.pumpAndSettle();

      // Use ensureVisible to reliably bring the Nacionalidade field into view
      // even on longer pages where scrollUntilVisible may overshoot.
      await tester.ensureVisible(
        find.widgetWithText(TextFormField, 'Nacionalidade'),
      );
      await tester.pumpAndSettle();
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Nacionalidade'),
        '',
      );

      await tester.ensureVisible(find.text('Salvar'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(find.text('A Nacionalidade não pode ser vazia.'), findsOneWidget);
    });

    testWidgets(
        'shows edit form with masked field when Editar is tapped on Título de Eleitor',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<VoteIdSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<VoteIdSection>());
      await tester.pumpAndSettle();

      expect(find.text('Número do título'), findsOneWidget);
      expect(find.text('Salvar'), findsOneWidget);
      expect(find.text('Cancelar'), findsOneWidget);
    });

    testWidgets('saves vote ID and shows success snackbar', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<VoteIdSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<VoteIdSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(
        find.text('Título de eleitor atualizado com sucesso.'),
        findsOneWidget,
      );
    });

    testWidgets(
        'cancels vote ID editing without saving when Cancelar is tapped',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<VoteIdSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<VoteIdSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Cancelar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();

      expect(find.text('1234.5678.0698'), findsOneWidget);
      expect(find.text('Salvar'), findsNothing);
    });

    testWidgets(
        'shows vote ID required error when the number field is cleared before saving',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<VoteIdSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<VoteIdSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.widgetWithText(TextFormField, 'Número do título'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Número do título'),
        '',
      );

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(
        find.text('O Número do título não pode ser vazio.'),
        findsOneWidget,
      );
      expect(
        find.text('Título de eleitor atualizado com sucesso.'),
        findsNothing,
      );
    });

    testWidgets(
        'shows vote ID invalid error when the algorithm check fails',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<VoteIdSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<VoteIdSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.widgetWithText(TextFormField, 'Número do título'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      // 12-digit number that passes length check but fails algorithm.
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Número do título'),
        '123456789012',
      );

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(find.text('O Número do título não é válido.'), findsOneWidget);
    });

    testWidgets('saves personal info and shows success snackbar',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<PersonalInfoSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<PersonalInfoSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(
        find.text('Informações pessoais atualizadas com sucesso.'),
        findsOneWidget,
      );
    });

    testWidgets('shows the Documento Militar section title', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Documento Militar'), findsOneWidget);
    });

    testWidgets(
        'expands the Documento Militar section and shows document data',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('RM-12345'), findsOneWidget);
      expect(find.text('Reservista'), findsOneWidget);
    });

    testWidgets(
        'shows not applicable message when military document is not required',
        (tester) async {
      employeeRepository.setMilitaryDocument(
        const EmployeeMilitaryDocument(
          number: '',
          type: '',
          isRequired: false,
        ),
      );

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(
        find.text('Não se aplica a este funcionário.'),
        findsOneWidget,
      );
    });

    testWidgets(
        'shows edit form when Editar is tapped on Documento Militar',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<MilitaryDocumentSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<MilitaryDocumentSection>());
      await tester.pumpAndSettle();

      expect(find.text('Número do documento'), findsOneWidget);
      expect(find.text('Tipo de documento'), findsOneWidget);
      expect(find.text('Salvar'), findsOneWidget);
      expect(find.text('Cancelar'), findsOneWidget);
    });

    testWidgets('saves military document and shows success snackbar',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<MilitaryDocumentSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<MilitaryDocumentSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(
        find.text('Documento militar atualizado com sucesso.'),
        findsOneWidget,
      );
    });

    testWidgets(
        'cancels military document editing without saving when Cancelar is tapped',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<MilitaryDocumentSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<MilitaryDocumentSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Cancelar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();

      expect(find.text('RM-12345'), findsOneWidget);
      expect(find.text('Salvar'), findsNothing);
    });

    testWidgets(
        'shows document number required error when number is cleared before saving',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<MilitaryDocumentSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<MilitaryDocumentSection>());
      await tester.pumpAndSettle();

      await tester.ensureVisible(
        find.widgetWithText(TextFormField, 'Número do documento'),
      );
      await tester.pumpAndSettle();
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Número do documento'),
        '',
      );

      await tester.ensureVisible(find.text('Salvar'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(
        find.text('O Número do documento não pode ser vazio.'),
        findsOneWidget,
      );
      expect(
        find.text('Documento militar atualizado com sucesso.'),
        findsNothing,
      );
    });

    testWidgets(
        'shows document type required error when type is cleared before saving',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<MilitaryDocumentSection>(),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(findEditIn<MilitaryDocumentSection>());
      await tester.pumpAndSettle();

      await tester.ensureVisible(
        find.widgetWithText(TextFormField, 'Tipo de documento'),
      );
      await tester.pumpAndSettle();
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Tipo de documento'),
        '',
      );

      await tester.ensureVisible(find.text('Salvar'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(
        find.text('O Tipo de documento não pode ser vazio.'),
        findsOneWidget,
      );
    });

    testWidgets('shows the Exame Médico Admissional section title',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Vínculo Empregatício tab.
      await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
      await tester.pumpAndSettle();

      expect(find.text('Exame Médico Admissional'), findsOneWidget);
    });

    testWidgets(
        'expands the Exame Médico Admissional section and shows exam data',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Vínculo Empregatício tab.
      await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
      await tester.pumpAndSettle();

      expect(find.text('15/01/2026'), findsOneWidget);
      expect(find.text('15/01/2027'), findsOneWidget);
    });

    testWidgets(
        'shows edit form with masked date fields when Editar is tapped on Exame Médico Admissional',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Vínculo Empregatício tab.
      await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<MedicalExamSection>(),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(findEditIn<MedicalExamSection>());
      await tester.pumpAndSettle();

      expect(find.text('Data do exame'), findsOneWidget);
      expect(find.text('Validade do exame'), findsOneWidget);
      expect(find.text('Salvar'), findsOneWidget);
      expect(find.text('Cancelar'), findsOneWidget);
    });

    testWidgets('saves medical exam and shows success snackbar',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Vínculo Empregatício tab.
      await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<MedicalExamSection>(),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(findEditIn<MedicalExamSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(
        find.text('Exame médico admissional atualizado com sucesso.'),
        findsOneWidget,
      );
    });

    testWidgets(
        'cancels medical exam editing without saving when Cancelar is tapped',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Vínculo Empregatício tab.
      await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<MedicalExamSection>(),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(findEditIn<MedicalExamSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Cancelar'),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();

      expect(find.text('15/01/2026'), findsOneWidget);
      expect(find.text('Salvar'), findsNothing);
    });

    testWidgets(
        'shows exam date required error when date is cleared before saving',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Vínculo Empregatício tab.
      await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<MedicalExamSection>(),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(findEditIn<MedicalExamSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.widgetWithText(TextFormField, 'Data do exame'),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Data do exame'),
        '',
      );

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(
        find.text('A Data do exame não pode ser vazia.'),
        findsOneWidget,
      );
    });

    testWidgets(
        'shows validity required error when validity is cleared before saving',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Vínculo Empregatício tab.
      await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<MedicalExamSection>(),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(findEditIn<MedicalExamSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.widgetWithText(TextFormField, 'Validade do exame'),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Validade do exame'),
        '',
      );

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(
        find.text('A Validade do exame não pode ser vazia.'),
        findsOneWidget,
      );
    });

    testWidgets(
        'shows exam date invalid error when a past date older than one year is entered',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Vínculo Empregatício tab.
      await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<MedicalExamSection>(),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(findEditIn<MedicalExamSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.widgetWithText(TextFormField, 'Data do exame'),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      // Date older than 1 year ago.
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Data do exame'),
        '01012020',
      );

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(find.text('A Data do exame é inválida.'), findsOneWidget);
    });

    testWidgets(
        'shows validity invalid error when a past date is entered',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Vínculo Empregatício tab.
      await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<MedicalExamSection>(),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(findEditIn<MedicalExamSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.widgetWithText(TextFormField, 'Validade do exame'),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      // Past date — validity must be future.
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Validade do exame'),
        '01012020',
      );

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Salvar'));
      await tester.pumpAndSettle();

      expect(find.text('A Validade do exame é inválida.'), findsOneWidget);
    });

    testWidgets('shows the Informações de Função section title',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Contratos tab.
      await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
      await tester.pumpAndSettle();

      expect(find.text('Informações de Função'), findsOneWidget);
    });

    testWidgets(
        'expands the Informações de Função section and shows role details',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Contratos tab.
      await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
      await tester.pumpAndSettle();

      expect(find.text('Financeiro'), findsOneWidget);
      expect(find.text('Analista'), findsWidgets);
    });

    testWidgets(
        'shows cascading dropdown edit form when Editar is tapped on Informações de Função',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Contratos tab.
      await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<RoleInfoSection>(),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(findEditIn<RoleInfoSection>());
      await tester.pumpAndSettle();

      expect(find.text('Setor'), findsWidgets);
      expect(find.text('Cargo'), findsWidgets);
      expect(find.text('Função'), findsWidgets);
      expect(find.text('Salvar'), findsOneWidget);
      expect(find.text('Cancelar'), findsOneWidget);
    });

    testWidgets(
        'cancels role info editing without saving when Cancelar is tapped',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Contratos tab.
      await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        findEditIn<RoleInfoSection>(),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(findEditIn<RoleInfoSection>());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Cancelar'),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();

      expect(find.text('Financeiro'), findsOneWidget);
      expect(find.text('Salvar'), findsNothing);
    });

    testWidgets('shows the Dependentes section title', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Dependentes'), findsOneWidget);
    });

    testWidgets(
        'expands the Dependentes section and shows dependent data',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      expect(find.text('Maria Silva'), findsOneWidget);
    });

    testWidgets('shows the Local de Trabalho section title', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Contratos tab.
      await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
      await tester.pumpAndSettle();

      expect(find.text('Local de Trabalho'), findsOneWidget);
    });

    testWidgets(
        'expands the Local de Trabalho section and shows workplace data',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Contratos tab.
      await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
      await tester.pumpAndSettle();

      expect(find.text('Sede Principal'), findsWidgets);
    });

    testWidgets('shows the Contratos section title', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Contratos tab.
      await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
      await tester.pumpAndSettle();

      expect(find.text('Contratos'), findsOneWidget);
    });

    testWidgets('expands the Contratos section and shows contract data',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Contratos tab.
      await tester.tap(find.widgetWithText(Tab, 'Vínculo Empregatício'));
      await tester.pumpAndSettle();

      expect(find.text('CLT'), findsOneWidget);
      expect(find.text('01/01/2026'), findsOneWidget);
    });

    testWidgets(
        'shows the Opções de Assinatura de Documentos section title',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Documentos tab.
      await tester.tap(find.widgetWithText(Tab, 'Documentos'));
      await tester.pumpAndSettle();

      expect(
          find.text('Opções de Assinatura de Documentos'), findsOneWidget);
    });

    testWidgets(
        'expands the signing options section and shows current option',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Documentos tab.
      await tester.tap(find.widgetWithText(Tab, 'Documentos'));
      await tester.pumpAndSettle();

      // No signing option set on profile — shows "Não informado".
      expect(find.text('Não informado'), findsWidgets);
    });

    testWidgets('shows the Documentos section title', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Documentos tab.
      await tester.tap(find.widgetWithText(Tab, 'Documentos'));
      await tester.pumpAndSettle();

      // Tab label + section title both say 'Documentos'.
      expect(find.text('Documentos'), findsNWidgets(2));
    });

    testWidgets('expands the Documentos section and shows document groups',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Documentos tab.
      await tester.tap(find.widgetWithText(Tab, 'Documentos'));
      await tester.pumpAndSettle();

      expect(find.text('Grupo Contratual'), findsOneWidget);
    });

    testWidgets(
        'shows item count without page buttons when document has only one page',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Documentos tab.
      await tester.tap(find.widgetWithText(Tab, 'Documentos'));
      await tester.pumpAndSettle();

      // Scroll to group and expand it.
      await tester.scrollUntilVisible(
        find.text('Grupo Contratual'),
        200,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Grupo Contratual'));
      await tester.pumpAndSettle();

      // Scroll to document and expand it.
      await tester.scrollUntilVisible(
        find.text('Contrato de Trabalho'),
        200,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Contrato de Trabalho'));
      await tester.pumpAndSettle();

      // Scroll down to reveal the item count.
      for (var i = 0; i < 10; i++) {
        await tester.drag(
            find.byType(Scrollable).last, const Offset(0, -200));
        await tester.pumpAndSettle();
      }

      // Shows item count but no "Página X de Y" text.
      expect(find.text('1 item'), findsOneWidget);
      expect(find.textContaining('Página'), findsNothing);
    });

    // A unidade do fixture está OK, então depreciar e invalidar aparecem e "não aplicável" não.
    // O documento do fixture não é por competência, então criar unidade à mão também não aparece:
    // ali duas unidades não podem cobrir ao mesmo tempo.
    testWidgets(
        'offers deprecate and invalidate on a delivered unit, and no add button',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.widgetWithText(Tab, 'Documentos'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Grupo Contratual'),
        200,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Grupo Contratual'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Contrato de Trabalho'),
        200,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Contrato de Trabalho'));
      await tester.pumpAndSettle();

      expect(find.byKey(const ValueKey('unit-deprecate')), findsOneWidget);
      expect(find.byKey(const ValueKey('unit-invalidate')), findsOneWidget);
      expect(find.byKey(const ValueKey('unit-renew')), findsOneWidget);
      expect(find.byKey(const ValueKey('unit-not-applicable')), findsNothing);
      expect(find.byKey(const ValueKey('document-add-unit')), findsNothing);
    });

    // Renovar é a saída de uma unidade vencida: sem ela o documento fica sem
    // nenhuma ação possível, já que vencida não é depreciável nem invalidável e
    // o vencimento não cria mais a substituta sozinho.
    testWidgets('offers renew on an expired unit and marks the replacement',
        (tester) async {
      const expiredDocument = EmployeeDocument(
        id: 'doc-1',
        name: 'Contrato de Trabalho',
        description: 'Contrato CLT',
        statusId: '7',
        statusName: 'Expired',
        isSignable: false,
        canGenerateDocument: true,
        usePreviousPeriod: false,
        totalUnitsCount: 2,
        units: [
          DocumentUnit(
            id: 'unit-1',
            statusId: '9',
            statusName: 'Expired',
            date: '01/01/2026',
            validity: '01/02/2026',
            createdAt: '01/01/2026',
            hasFile: true,
            name: 'contrato.pdf',
          ),
          DocumentUnit(
            id: 'unit-2',
            statusId: '1',
            statusName: 'Pending',
            date: '',
            validity: '',
            createdAt: '01/02/2026',
            hasFile: false,
            name: '',
            replacesDocumentUnitId: 'unit-1',
          ),
        ],
      );

      employeeRepository.setDocumentsList(const [expiredDocument]);
      documentGroupRepository.setGroupsWithDocuments(const [
        DocumentGroupWithDocuments(
          id: 'grp-1',
          name: 'Grupo Contratual',
          description: 'Documentos contratuais',
          statusId: '2',
          statusName: 'RequiresAttention',
          documents: [expiredDocument],
        ),
      ]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.widgetWithText(Tab, 'Documentos'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Grupo Contratual'),
        200,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Grupo Contratual'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Contrato de Trabalho'),
        200,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Contrato de Trabalho'));
      await tester.pumpAndSettle();

      // Renovar é a única ação da vencida; depreciar não aparece em nenhuma das
      // duas linhas. O invalidar que sobra é o da pendente substituta, não o da
      // vencida — vencida é a prova do período coberto e a API recusa.
      expect(find.byKey(const ValueKey('unit-renew')), findsOneWidget);
      expect(find.byKey(const ValueKey('unit-deprecate')), findsNothing);
      expect(find.byKey(const ValueKey('unit-invalidate')), findsOneWidget);

      // A substituta se identifica na lista — sem isso ela é indistinguível de
      // uma pendência qualquer.
      expect(find.byKey(const ValueKey('unit-renewal-badge')), findsOneWidget);

      await tester.ensureVisible(find.byKey(const ValueKey('unit-renew')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const ValueKey('unit-renew')));
      await tester.pumpAndSettle();

      expect(find.text('Renovar documento'), findsOneWidget);
      expect(find.byKey(const ValueKey('unit-renew-confirm')), findsOneWidget);

      await tester.tap(find.byKey(const ValueKey('unit-renew-confirm')));
      await tester.pumpAndSettle();

      expect(employeeRepository.renewedDocumentUnitIds, ['unit-1']);
    });

    // O servidor manda o nome do smart enum em inglês; a tela precisa rotular
    // pelo id. Fixture com o nome cru para que traduzir de volta a statusName
    // volte a quebrar o teste.
    testWidgets(
        'labels a not applicable unit in Portuguese and offers to require it '
        'again', (tester) async {
      const notApplicableDocument = EmployeeDocument(
        id: 'doc-1',
        name: 'Contrato de Trabalho',
        description: 'Contrato CLT',
        statusId: '3',
        statusName: 'OK',
        isSignable: false,
        canGenerateDocument: true,
        usePreviousPeriod: false,
        totalUnitsCount: 1,
        units: [
          DocumentUnit(
            id: 'unit-1',
            statusId: '6',
            statusName: 'NotApplicable',
            date: '01/01/2026',
            validity: '',
            createdAt: '01/01/2026',
            hasFile: false,
            name: '',
          ),
        ],
      );

      // A lista de unidades vem do employeeRepository (expandir o documento
      // recarrega a página); o grupo só desenha o tile.
      employeeRepository.setDocumentsList(const [notApplicableDocument]);
      documentGroupRepository.setGroupsWithDocuments(const [
        DocumentGroupWithDocuments(
          id: 'grp-1',
          name: 'Grupo Contratual',
          description: 'Documentos contratuais',
          statusId: '0',
          statusName: 'Okay',
          documents: [notApplicableDocument],
        ),
      ]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.widgetWithText(Tab, 'Documentos'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Grupo Contratual'),
        200,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Grupo Contratual'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Contrato de Trabalho'),
        200,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Contrato de Trabalho'));
      await tester.pumpAndSettle();

      expect(find.text('Não Aplicável'), findsOneWidget);
      expect(find.text('NotApplicable'), findsNothing);

      // Invalidar é a única saída dessa unidade — sem ela o documento fica sem
      // nenhuma ação possível na tela.
      expect(find.byKey(const ValueKey('unit-invalidate')), findsOneWidget);
      expect(find.byKey(const ValueKey('unit-deprecate')), findsNothing);
      expect(find.byKey(const ValueKey('unit-not-applicable')), findsNothing);

      await tester.ensureVisible(find.byKey(const ValueKey('unit-invalidate')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const ValueKey('unit-invalidate')));
      await tester.pumpAndSettle();

      expect(find.text('Voltar a exigir documento'), findsOneWidget);
      expect(
          find.byKey(const ValueKey('unit-invalidate-confirm')), findsOneWidget);
    });

    // Blindagem contra a origem do inglês na tela: quando o id não bate (status
    // novo no servidor, id vazio, formato inesperado), o rótulo cai no nome do
    // smart enum. Traduzir também pelo nome mantém a tela em português.
    testWidgets(
        'labels document and unit in Portuguese when only the English enum '
        'name matches', (tester) async {
      const unmatchedIdDocument = EmployeeDocument(
        id: 'doc-1',
        name: 'Contrato de Trabalho',
        description: 'Contrato CLT',
        statusId: '',
        statusName: 'AwaitingSignature',
        isSignable: false,
        canGenerateDocument: true,
        usePreviousPeriod: false,
        totalUnitsCount: 1,
        units: [
          DocumentUnit(
            id: 'unit-1',
            statusId: '',
            statusName: 'Pending',
            date: '01/01/2026',
            validity: '',
            createdAt: '01/01/2026',
            hasFile: false,
            name: '',
          ),
        ],
      );

      employeeRepository.setDocumentsList(const [unmatchedIdDocument]);
      documentGroupRepository.setGroupsWithDocuments(const [
        DocumentGroupWithDocuments(
          id: 'grp-1',
          name: 'Grupo Contratual',
          description: 'Documentos contratuais',
          statusId: '',
          statusName: 'Okay',
          documents: [unmatchedIdDocument],
        ),
      ]);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.widgetWithText(Tab, 'Documentos'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Grupo Contratual'),
        200,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Grupo Contratual'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Contrato de Trabalho'),
        200,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Contrato de Trabalho'));
      await tester.pumpAndSettle();

      expect(find.text('Aguardando Assinatura'), findsOneWidget);
      expect(find.text('Pendente'), findsWidgets);
      expect(find.text('AwaitingSignature'), findsNothing);
      expect(find.text('Pending'), findsNothing);
      expect(find.text('Okay'), findsNothing);
    });

    testWidgets('asks for confirmation before deprecating a unit',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.widgetWithText(Tab, 'Documentos'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Grupo Contratual'),
        200,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Grupo Contratual'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Contrato de Trabalho'),
        200,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Contrato de Trabalho'));
      await tester.pumpAndSettle();

      await tester.ensureVisible(find.byKey(const ValueKey('unit-deprecate')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const ValueKey('unit-deprecate')));
      await tester.pumpAndSettle();

      expect(find.text('Depreciar documento'), findsOneWidget);
      expect(
          find.byKey(const ValueKey('unit-deprecate-confirm')), findsOneWidget);
    });

    testWidgets(
        'expands the Documentos section in mobile viewport without overflow',
        (tester) async {
      tester.view.physicalSize = const Size(360, 640);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      // Suppress overflow errors from unrelated layouts so the test reflects
      // the documents-section redesign in isolation.
      final originalOnError = FlutterError.onError;
      FlutterError.onError = (details) {
        if (details.toString().contains('overflowed')) return;
        originalOnError?.call(details);
      };
      addTearDown(() => FlutterError.onError = originalOnError);

      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.widgetWithText(Tab, 'Documentos'));
      await tester.pumpAndSettle();

      expect(find.text('Grupo Contratual'), findsOneWidget);
    });
  });
}
