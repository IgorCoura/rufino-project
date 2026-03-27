import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:rufino_v2/domain/entities/address.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/employee.dart';
import 'package:rufino_v2/domain/entities/department.dart';
import 'package:rufino_v2/domain/entities/employee_contract.dart';
import 'package:rufino_v2/domain/entities/document_group_with_documents.dart';
import 'package:rufino_v2/domain/entities/employee_document.dart';
import 'package:rufino_v2/domain/entities/position.dart';
import 'package:rufino_v2/domain/entities/remuneration.dart';
import 'package:rufino_v2/domain/entities/employee_military_document.dart';
import 'package:rufino_v2/domain/entities/employee_personal_info.dart';
import 'package:rufino_v2/domain/entities/employee_profile.dart';
import 'package:rufino_v2/domain/entities/role.dart';
import 'package:rufino_v2/domain/entities/workplace.dart';
import 'package:rufino_v2/ui/features/employee/viewmodel/employee_profile_viewmodel.dart';
import 'package:rufino_v2/ui/features/employee/widgets/employee_profile_screen.dart';

import '../../../testing/fakes/fake_company_repository.dart';
import '../../../testing/fakes/fake_department_repository.dart';
import '../../../testing/fakes/fake_document_group_repository.dart';
import '../../../testing/fakes/fake_employee_repository.dart';
import '../../../testing/fakes/fake_workplace_repository.dart';

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
  late EmployeeProfileViewModel viewModel;

  setUp(() {
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
          statusId: '1',
          statusName: 'OK',
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
    );
  });

  tearDown(() => viewModel.dispose());

  Widget buildSubject() => MaterialApp.router(
        routerConfig: GoRouter(
          routes: [
            GoRoute(
              path: '/',
              builder: (_, __) => EmployeeProfileScreen(
                viewModel: viewModel,
                employeeId: 'emp-1',
              ),
            ),
          ],
        ),
      );

  group('EmployeeProfileScreen', () {
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
      // Use a finished contract so the "Marcar como inativo" button appears.
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

      // Navigate to the Contratos tab.
      await tester.tap(find.widgetWithText(Tab, 'Contratos'));
      await tester.pumpAndSettle();

      // Expand the Contratos section where the button now lives.
      await tester.tap(find.text('Contratos').last);
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

      expect(find.text('Editar'), findsOneWidget);
    });

    testWidgets('shows the name text field when the edit button is tapped',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.text('Editar'));
      await tester.pumpAndSettle();

      expect(find.byType(TextField), findsOneWidget);
      expect(find.text('Salvar'), findsOneWidget);
      expect(find.text('Cancelar'), findsOneWidget);
    });

    testWidgets('saves the new name after editing and tapping save',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.tap(find.text('Editar'));
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

      await tester.tap(find.text('Editar'));
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

      await tester.scrollUntilVisible(
        find.text('Contato'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Contato'));
      await tester.pump();

      // After settle, the contact data should be displayed formatted.
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

      await tester.scrollUntilVisible(
        find.text('Título de Eleitor'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Título de Eleitor'));
      await tester.pumpAndSettle();

      expect(find.text('1234.5678.0698'), findsOneWidget);
    });

    testWidgets(
        'expands the Informações Pessoais section and shows personal data',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Informações Pessoais'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Informações Pessoais'));
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
        find.text('Informações Pessoais'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Informações Pessoais'));
      await tester.pumpAndSettle();

      // The name section also has "Editar" — use scrollUntilVisible with
      // .last to reach and tap the personal info section's button below it.
      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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
        find.text('Informações Pessoais'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Informações Pessoais'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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
        find.text('Informações Pessoais'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Informações Pessoais'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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

      await tester.scrollUntilVisible(
        find.text('Documento (Identidade)'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Documento (Identidade)'));
      await tester.pumpAndSettle();

      expect(find.text('111.444.777-35'), findsOneWidget);
      expect(find.text('01/01/1990'), findsOneWidget);
      expect(find.text('Maria'), findsOneWidget);
      expect(find.text('João'), findsOneWidget);
    });

    testWidgets(
        'shows edit form with masked fields when Editar is tapped on Documento (Identidade)',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Documento (Identidade)'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Documento (Identidade)'));
      await tester.pumpAndSettle();

      // The name section also has "Editar" above — use .last to reach the
      // ID card section's button.
      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
      await tester.pumpAndSettle();

      expect(find.text('CPF'), findsOneWidget);
      expect(find.text('Data de nascimento'), findsOneWidget);
      expect(find.text('Nome da mãe'), findsOneWidget);
      expect(find.text('Salvar'), findsOneWidget);
      expect(find.text('Cancelar'), findsOneWidget);
    });

    testWidgets('saves ID card data and shows success snackbar',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Documento (Identidade)'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Documento (Identidade)'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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

    testWidgets('cancels ID card editing without saving when Cancelar is tapped',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Documento (Identidade)'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Documento (Identidade)'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Cancelar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();

      // Should return to view mode showing the data.
      expect(find.text('111.444.777-35'), findsOneWidget);
      expect(find.text('Salvar'), findsNothing);
    });

    testWidgets(
        'shows CPF required error when CPF is cleared before saving',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Documento (Identidade)'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Documento (Identidade)'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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
        find.text('Documento (Identidade)'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Documento (Identidade)'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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
        find.text('Documento (Identidade)'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Documento (Identidade)'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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
        find.text('Documento (Identidade)'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Documento (Identidade)'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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
        find.text('Documento (Identidade)'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Documento (Identidade)'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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
        find.text('Documento (Identidade)'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Documento (Identidade)'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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
        find.text('Documento (Identidade)'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Documento (Identidade)'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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
        find.text('Título de Eleitor'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Título de Eleitor'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
      await tester.pumpAndSettle();

      expect(find.text('Número do título'), findsOneWidget);
      expect(find.text('Salvar'), findsOneWidget);
      expect(find.text('Cancelar'), findsOneWidget);
    });

    testWidgets('saves vote ID and shows success snackbar', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Título de Eleitor'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Título de Eleitor'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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
        find.text('Título de Eleitor'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Título de Eleitor'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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
        find.text('Título de Eleitor'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Título de Eleitor'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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
        find.text('Título de Eleitor'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Título de Eleitor'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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
        find.text('Informações Pessoais'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Informações Pessoais'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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

      await tester.scrollUntilVisible(
        find.text('Documento Militar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Documento Militar'));
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

      await tester.scrollUntilVisible(
        find.text('Documento Militar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Documento Militar'));
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
        find.text('Documento Militar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Documento Militar'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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
        find.text('Documento Militar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Documento Militar'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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
        find.text('Documento Militar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Documento Militar'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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
        find.text('Documento Militar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Documento Militar'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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
        find.text('Documento Militar'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Documento Militar'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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

      expect(find.text('Exame Médico Admissional'), findsOneWidget);
    });

    testWidgets(
        'expands the Exame Médico Admissional section and shows exam data',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Exame Médico Admissional'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Exame Médico Admissional'));
      await tester.pumpAndSettle();

      expect(find.text('15/01/2026'), findsOneWidget);
      expect(find.text('15/01/2027'), findsOneWidget);
    });

    testWidgets(
        'shows edit form with masked date fields when Editar is tapped on Exame Médico Admissional',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Exame Médico Admissional'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Exame Médico Admissional'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
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

      await tester.scrollUntilVisible(
        find.text('Exame Médico Admissional'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Exame Médico Admissional'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).first,
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

      await tester.scrollUntilVisible(
        find.text('Exame Médico Admissional'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Exame Médico Admissional'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Cancelar'),
        100,
        scrollable: find.byType(Scrollable).first,
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

      await tester.scrollUntilVisible(
        find.text('Exame Médico Admissional'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Exame Médico Admissional'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.widgetWithText(TextFormField, 'Data do exame'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Data do exame'),
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
        find.text('A Data do exame não pode ser vazia.'),
        findsOneWidget,
      );
    });

    testWidgets(
        'shows validity required error when validity is cleared before saving',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Exame Médico Admissional'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Exame Médico Admissional'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.widgetWithText(TextFormField, 'Validade do exame'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Validade do exame'),
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
        find.text('A Validade do exame não pode ser vazia.'),
        findsOneWidget,
      );
    });

    testWidgets(
        'shows exam date invalid error when a past date older than one year is entered',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Exame Médico Admissional'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Exame Médico Admissional'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.widgetWithText(TextFormField, 'Data do exame'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      // Date older than 1 year ago.
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Data do exame'),
        '01012020',
      );

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).first,
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

      await tester.scrollUntilVisible(
        find.text('Exame Médico Admissional'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Exame Médico Admissional'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Editar').last);
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.widgetWithText(TextFormField, 'Validade do exame'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      // Past date — validity must be future.
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Validade do exame'),
        '01012020',
      );

      await tester.scrollUntilVisible(
        find.text('Salvar'),
        100,
        scrollable: find.byType(Scrollable).first,
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
      await tester.tap(find.widgetWithText(Tab, 'Contratos'));
      await tester.pumpAndSettle();

      expect(find.text('Informações de Função'), findsOneWidget);
    });

    testWidgets(
        'expands the Informações de Função section and shows role details',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Contratos tab.
      await tester.tap(find.widgetWithText(Tab, 'Contratos'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Informações de Função'),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Informações de Função'));
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
      await tester.tap(find.widgetWithText(Tab, 'Contratos'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Informações de Função'),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Informações de Função'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Editar').last);
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
      await tester.tap(find.widgetWithText(Tab, 'Contratos'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Informações de Função'),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Informações de Função'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Editar').last,
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Editar').last);
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

      await tester.scrollUntilVisible(
        find.text('Dependentes'),
        100,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Dependentes'));
      await tester.pumpAndSettle();

      expect(find.text('Maria Silva'), findsOneWidget);
    });

    testWidgets('shows the Local de Trabalho section title', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Contratos tab.
      await tester.tap(find.widgetWithText(Tab, 'Contratos'));
      await tester.pumpAndSettle();

      expect(find.text('Local de Trabalho'), findsOneWidget);
    });

    testWidgets(
        'expands the Local de Trabalho section and shows workplace data',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Contratos tab.
      await tester.tap(find.widgetWithText(Tab, 'Contratos'));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Local de Trabalho'),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Local de Trabalho'));
      await tester.pumpAndSettle();

      expect(find.text('Sede Principal'), findsWidgets);
    });

    testWidgets('shows the Contratos section title', (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Contratos tab.
      await tester.tap(find.widgetWithText(Tab, 'Contratos'));
      await tester.pumpAndSettle();

      // Tab label + section title both say 'Contratos'.
      expect(find.text('Contratos'), findsNWidgets(2));
    });

    testWidgets('expands the Contratos section and shows contract data',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pumpAndSettle();

      // Navigate to the Contratos tab.
      await tester.tap(find.widgetWithText(Tab, 'Contratos'));
      await tester.pumpAndSettle();

      // Tap the section title (last match — first is the tab label).
      await tester.tap(find.text('Contratos').last);
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

      await tester.scrollUntilVisible(
        find.text('Opções de Assinatura de Documentos'),
        100,
        scrollable: find.byType(Scrollable).last,
      );
      await tester.tap(find.text('Opções de Assinatura de Documentos'));
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

      // Tap the section title (last match — first is the tab label).
      await tester.tap(find.text('Documentos').last);
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

      // Tap the section title (last match — first is the tab label).
      await tester.tap(find.text('Documentos').last);
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
  });
}
