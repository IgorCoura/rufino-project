import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:people_management/people_management.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:people_management/src/ui/employee/viewmodel/employee_profile_viewmodel.dart';
import 'package:people_management/src/ui/employee/widgets/components/documents_section.dart';

import '../../fakes/fake_cep_repository.dart';
import '../../fakes/fake_company_repository.dart';
import '../../fakes/fake_department_repository.dart';
import '../../fakes/fake_document_group_repository.dart';
import '../../fakes/fake_employee_repository.dart';
import '../../fakes/fake_permission_repository.dart';
import '../../fakes/fake_workplace_repository.dart';

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
  late FakeDocumentGroupRepository documentGroupRepository;
  late PermissionNotifier permissionNotifier;
  late EmployeeProfileViewModel viewModel;

  /// Replaces the document the section renders, keeping the group around it.
  void useDocument(EmployeeDocument document) {
    employeeRepository.setDocumentsList([document]);
    documentGroupRepository.setGroupsWithDocuments([
      DocumentGroupWithDocuments(
        id: _fakeGroup.id,
        name: _fakeGroup.name,
        description: _fakeGroup.description,
        statusId: _fakeGroup.statusId,
        statusName: _fakeGroup.statusName,
        documents: [document],
      ),
    ]);
  }

  setUp(() async {
    employeeRepository = FakeEmployeeRepository()
      ..setDocumentsList(const [_signableDocument]);
    documentGroupRepository = FakeDocumentGroupRepository()
      ..setGroupsWithDocuments(const [_fakeGroup]);
    viewModel = EmployeeProfileViewModel(
      companyRepository: FakeCompanyRepository()
        ..setSelectedCompany(_fakeCompany),
      employeeRepository: employeeRepository,
      departmentRepository: FakeDepartmentRepository(),
      workplaceRepository: FakeWorkplaceRepository(),
      documentGroupRepository: documentGroupRepository,
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

  group('DocumentsSection schedule-signature-send', () {
    testWidgets('offers scheduling alongside sending now in the generate dialog',
        (tester) async {
      await pumpExpandedSection(tester, signingOptionId: '2');

      await tester.tap(find.byTooltip('Gerar'));
      await tester.pumpAndSettle();

      expect(find.byKey(const ValueKey('generate-dialog-schedule-sign')),
          findsOneWidget);
    });

    testWidgets('hides scheduling when the employee signs physically',
        (tester) async {
      await pumpExpandedSection(tester, signingOptionId: '1');

      await tester.tap(find.byTooltip('Gerar'));
      await tester.pumpAndSettle();

      expect(find.byKey(const ValueKey('generate-dialog-schedule-sign')),
          findsNothing);
    });

    // O caso comum — renovar exatamente no dia em que o documento atual vence —
    // fica a uma confirmação de distância.
    testWidgets('prefills the send date with the suggestion from the server',
        (tester) async {
      final suggestion = _dateInDays(30);
      useDocument(_documentSuggesting(suggestion));
      await pumpExpandedSection(tester, signingOptionId: '2');

      await tester.tap(find.byTooltip('Gerar'));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const ValueKey('generate-dialog-schedule-sign')));
      await tester.pumpAndSettle();

      final field = tester.widget<TextFormField>(
          find.byKey(const ValueKey('schedule-send-on-field')));
      expect(field.controller?.text, suggestion);
    });

    testWidgets('leaves the send date empty when there is nothing to suggest',
        (tester) async {
      await pumpExpandedSection(tester, signingOptionId: '2');

      await tester.tap(find.byTooltip('Gerar'));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const ValueKey('generate-dialog-schedule-sign')));
      await tester.pumpAndSettle();

      final field = tester.widget<TextFormField>(
          find.byKey(const ValueKey('schedule-send-on-field')));
      expect(field.controller?.text, isEmpty);
    });

    // O prazo é contado a partir do envio; anterior a ele nasceria vencido.
    testWidgets('refuses a deadline that is not after the send date',
        (tester) async {
      useDocument(_documentSuggesting(_dateInDays(30)));
      await pumpExpandedSection(tester, signingOptionId: '2');

      await tester.tap(find.byTooltip('Gerar'));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const ValueKey('generate-dialog-schedule-sign')));
      await tester.pumpAndSettle();

      await tester.enterText(
          find.byKey(const ValueKey('schedule-deadline-field')), _dateInDays(29));
      await tester.tap(find.text('Agendar'));
      await tester.pumpAndSettle();

      expect(find.text('A data limite precisa ser posterior à data do envio.'),
          findsOneWidget);
      expect(employeeRepository.lastScheduledSend, isNull);
    });

    testWidgets('schedules the send with both dates when the form is valid',
        (tester) async {
      final sendOn = _dateInDays(30);
      final deadline = _dateInDays(35);
      useDocument(_documentSuggesting(sendOn));
      await pumpExpandedSection(tester, signingOptionId: '2');

      await tester.tap(find.byTooltip('Gerar'));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const ValueKey('generate-dialog-schedule-sign')));
      await tester.pumpAndSettle();

      await tester.enterText(
          find.byKey(const ValueKey('schedule-deadline-field')), deadline);
      await tester.tap(find.text('Agendar'));
      await tester.pumpAndSettle();

      expect(employeeRepository.lastScheduledSend?.sendOn, sendOn);
      expect(employeeRepository.lastScheduledSend?.dateLimitToSign, deadline);
    });

    testWidgets('shows the scheduled date on the unit row', (tester) async {
      final sendOn = _dateInDays(30);
      useDocument(_documentScheduledOn(sendOn));
      await pumpExpandedSection(tester, signingOptionId: '2');

      expect(find.text('Envio agendado: $sendOn'), findsOneWidget);
    });

    testWidgets('cancels the scheduled send after confirmation',
        (tester) async {
      useDocument(_documentScheduledOn(_dateInDays(30)));
      await pumpExpandedSection(tester, signingOptionId: '2');

      await tester.tap(find.byTooltip('Cancelar agendamento'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Cancelar agendamento').last);
      await tester.pumpAndSettle();

      expect(employeeRepository.cancelScheduledSendCalled, isTrue);
    });

    testWidgets('hides the cancel action when nothing is scheduled',
        (tester) async {
      await pumpExpandedSection(tester, signingOptionId: '2');

      expect(find.byTooltip('Cancelar agendamento'), findsNothing);
    });
  });

  group('DocumentsSection add unit by competency', () {
    /// Swaps the granted permissions, keeping a single notifier alive for the
    /// tearDown to dispose.
    Future<void> grantOnly(List<String> scopes) async {
      final previous = permissionNotifier;
      final repository = FakePermissionRepository()
        ..setPermissions([Permission(resource: 'document', scopes: scopes)]);
      permissionNotifier = PermissionNotifier(permissionRepository: repository);
      await permissionNotifier.loadPermissions();
      previous.dispose();
    }

    testWidgets('offers adding a unit on a document by competência',
        (tester) async {
      useDocument(_monthlyDocument());
      await pumpExpandedSection(tester, signingOptionId: '2');

      expect(find.byKey(const ValueKey('document-add-unit')), findsOneWidget);
    });

    // Fora da competência não existe segunda unidade em vigência: a próxima
    // nasce de renovar ou de depreciar/invalidar a atual.
    testWidgets('hides adding on a document without competência',
        (tester) async {
      await pumpExpandedSection(tester, signingOptionId: '2');

      expect(find.byKey(const ValueKey('document-add-unit')), findsNothing);
    });

    testWidgets('hides adding without the create permission', (tester) async {
      await grantOnly(['view', 'edit']);
      useDocument(_monthlyDocument());
      await pumpExpandedSection(tester, signingOptionId: '2');

      expect(find.byKey(const ValueKey('document-add-unit')), findsNothing);
    });

    testWidgets('names the granularity in the dialog', (tester) async {
      useDocument(_monthlyDocument());
      await pumpExpandedSection(tester, signingOptionId: '2');

      await tester.tap(find.byKey(const ValueKey('document-add-unit')));
      await tester.pumpAndSettle();

      expect(find.textContaining('competência mensal'), findsOneWidget);
    });

    testWidgets('refuses an empty date', (tester) async {
      useDocument(_monthlyDocument());
      await pumpExpandedSection(tester, signingOptionId: '2');

      await tester.tap(find.byKey(const ValueKey('document-add-unit')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const ValueKey('add-unit-confirm')));
      await tester.pumpAndSettle();

      expect(find.text('A data não pode ser vazia.'), findsOneWidget);
      expect(employeeRepository.lastCreatedDocumentUnitDate, isNull);
    });

    testWidgets('creates the unit with the informed date', (tester) async {
      useDocument(_monthlyDocument());
      await pumpExpandedSection(tester, signingOptionId: '2');

      await tester.tap(find.byKey(const ValueKey('document-add-unit')));
      await tester.pumpAndSettle();
      await tester.enterText(
          find.byKey(const ValueKey('add-unit-date-field')), '15/03/2026');
      await tester.tap(find.byKey(const ValueKey('add-unit-confirm')));
      await tester.pumpAndSettle();

      expect(employeeRepository.lastCreatedDocumentUnitDate, '15/03/2026');
    });

    testWidgets('offers adding even when the filter returns no unit',
        (tester) async {
      useDocument(_monthlyDocument(units: const []));
      await pumpExpandedSection(tester, signingOptionId: '2');

      expect(find.text('Nenhuma unidade encontrada para o filtro.'),
          findsOneWidget);
      expect(find.byKey(const ValueKey('document-add-unit')), findsOneWidget);
    });
  });
}

EmployeeDocument _monthlyDocument({List<DocumentUnit>? units}) =>
    EmployeeDocument(
      id: _signableDocument.id,
      name: _signableDocument.name,
      description: _signableDocument.description,
      statusId: _signableDocument.statusId,
      statusName: _signableDocument.statusName,
      isSignable: true,
      canGenerateDocument: true,
      usePreviousPeriod: false,
      periodTypeId: 3,
      totalUnitsCount: units?.length ?? _signableDocument.units.length,
      units: units ?? _signableDocument.units,
    );

/// Formats today + [days] as `dd/MM/yyyy`.
///
/// Relativa a hoje de propósito: o campo de envio recusa data no passado, então
/// uma data fixa faria o teste passar hoje e falhar quando ela vencesse.
String _dateInDays(int days) {
  final target = DateTime.now().add(Duration(days: days));
  final d = target.day.toString().padLeft(2, '0');
  final m = target.month.toString().padLeft(2, '0');
  return '$d/$m/${target.year}';
}

EmployeeDocument _documentSuggesting(String suggestedDate) => EmployeeDocument(
      id: _signableDocument.id,
      name: _signableDocument.name,
      description: _signableDocument.description,
      statusId: _signableDocument.statusId,
      statusName: _signableDocument.statusName,
      isSignable: true,
      canGenerateDocument: true,
      usePreviousPeriod: false,
      totalUnitsCount: 1,
      units: _signableDocument.units,
      suggestedSignatureScheduleDate: suggestedDate,
    );

EmployeeDocument _documentScheduledOn(String sendOn) => EmployeeDocument(
      id: _signableDocument.id,
      name: _signableDocument.name,
      description: _signableDocument.description,
      statusId: _signableDocument.statusId,
      statusName: _signableDocument.statusName,
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
          scheduledSignatureSendOn: sendOn,
        ),
      ],
    );
