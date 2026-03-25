import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/document_template.dart';
import 'package:rufino_v2/ui/features/document_template/viewmodel/document_template_list_viewmodel.dart';

import '../../../../testing/fakes/fake_company_repository.dart';
import '../../../../testing/fakes/fake_document_template_repository.dart';

const _fakeCompany = Company(
  id: 'company-1',
  corporateName: 'Acme Corp',
  fantasyName: 'Acme',
  cnpj: '00000000000000',
);

const _fakeTemplate = DocumentTemplate(
  id: 'tpl-1',
  name: 'Contrato CLT',
  description: 'Template de contrato CLT padrão',
  validityInDays: 365,
  workload: 44,
  usePreviousPeriod: false,
  acceptsSignature: true,
);

const _fakeTemplate2 = DocumentTemplate(
  id: 'tpl-2',
  name: 'Atestado de Saúde',
  description: 'Template de atestado ocupacional',
  validityInDays: 30,
  workload: null,
  usePreviousPeriod: true,
  acceptsSignature: false,
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeDocumentTemplateRepository templateRepository;
  late DocumentTemplateListViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    templateRepository = FakeDocumentTemplateRepository()
      ..setTemplates([_fakeTemplate]);
    viewModel = DocumentTemplateListViewModel(
      companyRepository: companyRepository,
      documentTemplateRepository: templateRepository,
    );
  });

  tearDown(() => viewModel.dispose());

  group('DocumentTemplateListViewModel', () {
    group('initial state', () {
      test('starts idle with empty list', () {
        expect(viewModel.isLoading, isFalse);
        expect(viewModel.hasError, isFalse);
        expect(viewModel.templates, isEmpty);
        expect(viewModel.errorMessage, isNull);
      });
    });

    group('loadTemplates', () {
      test('populates the template list on success', () async {
        await viewModel.loadTemplates();

        expect(viewModel.isLoading, isFalse);
        expect(viewModel.hasError, isFalse);
        expect(viewModel.templates, hasLength(1));
        expect(viewModel.templates.first.name, 'Contrato CLT');
      });

      test('loads multiple templates', () async {
        templateRepository.setTemplates([_fakeTemplate, _fakeTemplate2]);

        await viewModel.loadTemplates();

        expect(viewModel.templates, hasLength(2));
        expect(viewModel.templates[1].name, 'Atestado de Saúde');
      });

      test('sets error when no company is selected', () async {
        companyRepository.setSelectedCompany(null);

        await viewModel.loadTemplates();

        expect(viewModel.hasError, isTrue);
        expect(viewModel.errorMessage, isNotNull);
        expect(viewModel.templates, isEmpty);
      });

      test('sets error when repository fails', () async {
        templateRepository.setShouldFail(true);

        await viewModel.loadTemplates();

        expect(viewModel.hasError, isTrue);
        expect(viewModel.errorMessage, isNotNull);
        expect(viewModel.templates, isEmpty);
      });

      test('notifies listeners during loading', () async {
        final loadingStates = <bool>[];
        viewModel.addListener(() => loadingStates.add(viewModel.isLoading));

        await viewModel.loadTemplates();

        expect(loadingStates.first, isTrue);
        expect(loadingStates.last, isFalse);
      });

      test('clears previous error on retry', () async {
        templateRepository.setShouldFail(true);
        await viewModel.loadTemplates();
        expect(viewModel.hasError, isTrue);

        templateRepository.setShouldFail(false);
        await viewModel.loadTemplates();

        expect(viewModel.hasError, isFalse);
        expect(viewModel.templates, hasLength(1));
      });
    });
  });
}
