import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/document_group.dart';
import 'package:rufino_v2/ui/features/document_group/viewmodel/document_group_form_viewmodel.dart';

import '../../../../testing/fakes/fake_company_repository.dart';
import '../../../../testing/fakes/fake_document_group_repository.dart';

const _fakeCompany = Company(
  id: 'company-1',
  corporateName: 'Acme Corp',
  fantasyName: 'Acme',
  cnpj: '00000000000000',
);

const _fakeGroup = DocumentGroup(
  id: 'grp-1',
  name: 'Admissão',
  description: 'Documentos de admissão do funcionário',
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeDocumentGroupRepository groupRepository;
  late DocumentGroupFormViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    groupRepository = FakeDocumentGroupRepository()
      ..setGroups([_fakeGroup]);
    viewModel = DocumentGroupFormViewModel(
      companyRepository: companyRepository,
      documentGroupRepository: groupRepository,
    );
  });

  tearDown(() => viewModel.dispose());

  group('DocumentGroupFormViewModel', () {
    test('starts with idle status and new group', () {
      expect(viewModel.status, DocumentGroupFormStatus.idle);
      expect(viewModel.isNew, isTrue);
    });

    test('loadGroup populates controllers from repository', () async {
      await viewModel.loadGroup('grp-1');

      expect(viewModel.status, DocumentGroupFormStatus.idle);
      expect(viewModel.isNew, isFalse);
      expect(viewModel.nameController.text, 'Admissão');
      expect(viewModel.descriptionController.text,
          'Documentos de admissão do funcionário');
    });

    test('loadGroup sets error when group is not found', () async {
      await viewModel.loadGroup('nonexistent');

      expect(viewModel.status, DocumentGroupFormStatus.error);
      expect(viewModel.errorMessage, isNotNull);
    });

    test('loadGroup sets error when repository fails', () async {
      groupRepository.setShouldFail(true);

      await viewModel.loadGroup('grp-1');

      expect(viewModel.status, DocumentGroupFormStatus.error);
    });

    test('save for new group transitions to saved', () async {
      viewModel.nameController.text = 'Periódicos';
      viewModel.descriptionController.text = 'Documentos periódicos';

      await viewModel.save();

      expect(viewModel.status, DocumentGroupFormStatus.saved);
      expect(groupRepository.lastCreatedGroupName, 'Periódicos');
    });

    test('save for existing group transitions to saved', () async {
      await viewModel.loadGroup('grp-1');

      await viewModel.save();

      expect(viewModel.status, DocumentGroupFormStatus.saved);
      expect(groupRepository.lastUpdatedGroupId, 'grp-1');
    });

    test('save transitions to error when repository fails', () async {
      viewModel.nameController.text = 'Test';
      viewModel.descriptionController.text = 'Test desc';
      groupRepository.setShouldFail(true);

      await viewModel.save();

      expect(viewModel.status, DocumentGroupFormStatus.error);
    });

    test('validateName returns error for empty name', () {
      expect(viewModel.validateName(''), isNotNull);
      expect(viewModel.validateName(null), isNotNull);
      expect(viewModel.validateName('   '), isNotNull);
    });

    test('validateName returns null for valid name', () {
      expect(viewModel.validateName('Admissão'), isNull);
    });

    test('validateName returns error for name exceeding 100 characters', () {
      final longName = 'A' * 101;
      expect(viewModel.validateName(longName), isNotNull);
    });

    test('validateDescription returns error for empty description', () {
      expect(viewModel.validateDescription(''), isNotNull);
      expect(viewModel.validateDescription(null), isNotNull);
      expect(viewModel.validateDescription('   '), isNotNull);
    });

    test('validateDescription returns null for valid description', () {
      expect(viewModel.validateDescription('Documentos de admissão'), isNull);
    });

    test('validateDescription returns error for description exceeding 1000 characters',
        () {
      final longDesc = 'A' * 1001;
      expect(viewModel.validateDescription(longDesc), isNotNull);
    });

    test('isSaving returns true while saving', () async {
      viewModel.nameController.text = 'Test';
      viewModel.descriptionController.text = 'Desc';

      bool wasSaving = false;
      viewModel.addListener(() {
        if (viewModel.isSaving) wasSaving = true;
      });

      await viewModel.save();

      expect(wasSaving, isTrue);
    });
  });
}
