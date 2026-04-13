import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/require_document.dart';
import 'package:rufino_v2/ui/features/require_document/viewmodel/require_document_form_viewmodel.dart';

import '../../../../testing/fakes/fake_company_repository.dart';
import '../../../../testing/fakes/fake_require_document_repository.dart';

const _fakeCompany = Company(
  id: 'company-1',
  corporateName: 'Acme Corp',
  fantasyName: 'Acme',
  cnpj: '00000000000000',
);

const _fakeRequireDocument = RequireDocument(
  id: 'req-1',
  name: 'Admissão CLT',
  description: 'Documentos obrigatórios para admissão CLT',
  associationIds: ['assoc-1'],
  associations: [AssociationItem(id: 'assoc-1', name: 'Desenvolvedor')],
  associationTypeId: 1,
  associationTypeName: 'Função',
  documentTemplates: [
    RequireDocumentTemplate(id: 'tpl-1', name: 'Contrato CLT'),
  ],
  listenEvents: [
    ListenEvent(
      eventId: 1,
      eventName: 'Evento Criado',
      statuses: [EventStatus(id: 1, name: 'Pendente')],
    ),
  ],
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeRequireDocumentRepository requireDocumentRepository;
  late RequireDocumentFormViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    requireDocumentRepository = FakeRequireDocumentRepository()
      ..setRequireDocument(_fakeRequireDocument);
    viewModel = RequireDocumentFormViewModel(
      companyRepository: companyRepository,
      requireDocumentRepository: requireDocumentRepository,
    );
  });

  tearDown(() => viewModel.dispose());

  group('RequireDocumentFormViewModel', () {
    test('starts with idle status and new require document', () {
      expect(viewModel.status, RequireDocumentFormStatus.idle);
      expect(viewModel.isNew, isTrue);
    });

    test('loadRequireDocument populates controllers from repository',
        () async {
      await viewModel.loadRequireDocument('req-1');

      expect(viewModel.status, RequireDocumentFormStatus.idle);
      expect(viewModel.isNew, isFalse);
      expect(viewModel.nameController.text, 'Admissão CLT');
      expect(viewModel.descriptionController.text,
          'Documentos obrigatórios para admissão CLT');
      expect(viewModel.selectedAssociationTypeId, '1');
      expect(viewModel.selectedAssociationIds, ['assoc-1']);
      expect(viewModel.selectedDocumentTemplates.length, 1);
      expect(viewModel.listenEvents.length, 1);
    });

    test('loadRequireDocument sets error when repository fails', () async {
      requireDocumentRepository.setShouldFail(true);

      await viewModel.loadRequireDocument('req-1');

      expect(viewModel.status, RequireDocumentFormStatus.error);
    });

    test('loadOptions transitions to idle on success', () async {
      await viewModel.loadOptions();

      expect(viewModel.status, RequireDocumentFormStatus.idle);
      expect(viewModel.associationTypes.length, 2);
      expect(viewModel.availableEvents.length, 2);
      expect(viewModel.availableStatuses.length, 3);
      expect(viewModel.availableDocumentTemplates.length, 2);
    });

    test('save for new require document transitions to saved', () async {
      await viewModel.loadOptions();
      viewModel.nameController.text = 'Novo Requerimento';
      viewModel.descriptionController.text = 'Descrição do requerimento';

      await viewModel.save();

      expect(viewModel.status, RequireDocumentFormStatus.saved);
      expect(
          requireDocumentRepository.lastCreatedName, 'Novo Requerimento');
    });

    test('save for existing require document transitions to saved', () async {
      await viewModel.loadRequireDocument('req-1');

      await viewModel.save();

      expect(viewModel.status, RequireDocumentFormStatus.saved);
      expect(requireDocumentRepository.lastUpdatedId, 'req-1');
    });

    test('save transitions to error when repository fails', () async {
      viewModel.nameController.text = 'Test';
      viewModel.descriptionController.text = 'Test desc';
      requireDocumentRepository.setShouldFail(true);

      await viewModel.save();

      expect(viewModel.status, RequireDocumentFormStatus.error);
    });

    test('validateName returns error for empty name', () {
      expect(viewModel.validateName(''), isNotNull);
      expect(viewModel.validateName(null), isNotNull);
    });

    test('validateName returns null for valid name', () {
      expect(viewModel.validateName('Requerimento A'), isNull);
    });

    test('validateName returns error for name exceeding 100 characters', () {
      final longName = 'A' * 101;
      expect(viewModel.validateName(longName), isNotNull);
    });

    test('validateDescription returns error for empty description', () {
      expect(viewModel.validateDescription(''), isNotNull);
      expect(viewModel.validateDescription(null), isNotNull);
    });

    test('validateDescription returns null for valid description', () {
      expect(viewModel.validateDescription('Uma descrição válida'), isNull);
    });

    test('validateDescription returns error for description exceeding 500 characters',
        () {
      final longDesc = 'A' * 501;
      expect(viewModel.validateDescription(longDesc), isNotNull);
    });

    test('onAssociationTypeChanged resets association and loads options',
        () async {
      await viewModel.loadOptions();

      await viewModel.onAssociationTypeChanged('1');

      expect(viewModel.selectedAssociationTypeId, '1');
      expect(viewModel.selectedAssociationIds, isEmpty);
      expect(viewModel.associations.length, 2);
      expect(requireDocumentRepository.lastAssociationTypeId, '1');
    });

    test('addDocumentTemplate adds a template to the selected list',
        () async {
      await viewModel.loadOptions();

      viewModel.addDocumentTemplate('tpl-1');

      expect(viewModel.selectedDocumentTemplates.length, 1);
      expect(viewModel.selectedDocumentTemplates.first.name, 'Contrato CLT');
    });

    test('addDocumentTemplate does not add duplicates', () async {
      await viewModel.loadOptions();

      viewModel.addDocumentTemplate('tpl-1');
      viewModel.addDocumentTemplate('tpl-1');

      expect(viewModel.selectedDocumentTemplates.length, 1);
    });

    test('removeDocumentTemplate removes a template from the selected list',
        () async {
      await viewModel.loadOptions();
      viewModel.addDocumentTemplate('tpl-1');

      viewModel.removeDocumentTemplate('tpl-1');

      expect(viewModel.selectedDocumentTemplates, isEmpty);
    });

    test('addListenEvent adds an event to the events list', () async {
      await viewModel.loadOptions();

      viewModel.addListenEvent('1');

      expect(viewModel.listenEvents.length, 1);
      expect(viewModel.listenEvents.first.eventName, 'Evento Criado');
    });

    test('removeListenEvent removes an event from the events list', () async {
      await viewModel.loadOptions();
      viewModel.addListenEvent('1');

      viewModel.removeListenEvent(1);

      expect(viewModel.listenEvents, isEmpty);
    });

    test('addStatusToEvent adds a status to the specified event', () async {
      await viewModel.loadOptions();
      viewModel.addListenEvent('1');

      viewModel.addStatusToEvent(1, '2');

      expect(viewModel.listenEvents.first.statuses.length, 1);
      expect(viewModel.listenEvents.first.statuses.first.name, 'Ativo');
    });

    test('removeStatusFromEvent removes a status from the specified event',
        () async {
      await viewModel.loadOptions();
      viewModel.addListenEvent('1');
      viewModel.addStatusToEvent(1, '2');

      viewModel.removeStatusFromEvent(1, 2);

      expect(viewModel.listenEvents.first.statuses, isEmpty);
    });

    test('generateDocumentUnits transitions to generated on success',
        () async {
      await viewModel.loadRequireDocument('req-1');

      await viewModel.generateDocumentUnits();

      expect(viewModel.status, RequireDocumentFormStatus.generated);
      expect(requireDocumentRepository.lastGeneratedRequireDocumentId,
          'req-1');
    });

    test('generateDocumentUnits transitions to error when repository fails',
        () async {
      await viewModel.loadRequireDocument('req-1');
      requireDocumentRepository.setShouldFail(true);

      await viewModel.generateDocumentUnits();

      expect(viewModel.status, RequireDocumentFormStatus.error);
    });

    test('generateDocumentUnits does nothing when id is empty', () async {
      await viewModel.generateDocumentUnits();

      expect(viewModel.status, RequireDocumentFormStatus.idle);
      expect(requireDocumentRepository.lastGeneratedRequireDocumentId, isNull);
    });
  });
}
