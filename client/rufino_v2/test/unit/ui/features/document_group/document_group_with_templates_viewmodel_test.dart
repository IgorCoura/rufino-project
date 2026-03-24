import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/document_group_with_templates.dart';
import 'package:rufino_v2/ui/features/document_group/viewmodel/document_group_with_templates_viewmodel.dart';

import '../../../../testing/fakes/fake_company_repository.dart';
import '../../../../testing/fakes/fake_document_group_repository.dart';

const _fakeCompany = Company(
  id: 'company-1',
  corporateName: 'Acme Corp',
  fantasyName: 'Acme',
  cnpj: '00000000000000',
);

const _fakeGroupWithTemplates = DocumentGroupWithTemplates(
  id: 'grp-1',
  name: 'Admissão',
  description: 'Documentos de admissão do funcionário',
  templates: [
    DocumentTemplateSummary(
      id: 'tpl-1',
      name: 'Contrato de Trabalho',
      description: 'Modelo padrão de contrato',
    ),
    DocumentTemplateSummary(
      id: 'tpl-2',
      name: 'Ficha de Registro',
      description: 'Ficha de registro do funcionário',
    ),
  ],
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeDocumentGroupRepository groupRepository;
  late DocumentGroupWithTemplatesViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    groupRepository = FakeDocumentGroupRepository()
      ..setGroupsWithTemplates([_fakeGroupWithTemplates]);
    viewModel = DocumentGroupWithTemplatesViewModel(
      companyRepository: companyRepository,
      documentGroupRepository: groupRepository,
    );
  });

  tearDown(() => viewModel.dispose());

  group('DocumentGroupWithTemplatesViewModel', () {
    test('starts in idle state with empty groups list', () {
      expect(viewModel.isLoading, isFalse);
      expect(viewModel.hasError, isFalse);
      expect(viewModel.groups, isEmpty);
    });

    test('sets loading then idle state on successful load', () async {
      DocumentGroupWithTemplatesStatus? capturedLoadingStatus;

      viewModel.addListener(() {
        if (viewModel.isLoading) {
          capturedLoadingStatus = DocumentGroupWithTemplatesStatus.loading;
        }
      });

      await viewModel.loadGroups();

      expect(capturedLoadingStatus, DocumentGroupWithTemplatesStatus.loading);
      expect(viewModel.isLoading, isFalse);
    });

    test('populates groups with templates from repository', () async {
      await viewModel.loadGroups();

      expect(viewModel.isLoading, isFalse);
      expect(viewModel.groups, hasLength(1));
      expect(viewModel.groups.first.name, 'Admissão');
      expect(viewModel.groups.first.templates, hasLength(2));
      expect(viewModel.groups.first.templates.first.name, 'Contrato de Trabalho');
      expect(viewModel.groups.first.templates.last.name, 'Ficha de Registro');
    });

    test('sets error state when no company is selected', () async {
      companyRepository.setSelectedCompany(null);

      await viewModel.loadGroups();

      expect(viewModel.hasError, isTrue);
      expect(viewModel.errorMessage, isNotNull);
    });

    test('sets error state when repository returns error', () async {
      groupRepository.setShouldFail(true);

      await viewModel.loadGroups();

      expect(viewModel.hasError, isTrue);
      expect(viewModel.errorMessage, isNotNull);
    });

    test('clears previous groups on error', () async {
      await viewModel.loadGroups();
      expect(viewModel.groups, hasLength(1));

      groupRepository.setShouldFail(true);
      await viewModel.loadGroups();

      expect(viewModel.hasError, isTrue);
      expect(viewModel.groups, isEmpty);
    });
  });
}
