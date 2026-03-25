import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/document_template.dart';
import 'package:rufino_v2/ui/features/document_template/viewmodel/document_template_form_viewmodel.dart';

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
  bodyFileName: 'corpo.html',
  headerFileName: 'header.html',
  footerFileName: 'footer.html',
  documentGroupId: 'grp-1',
  recoverDataTypeIds: [1, 3],
  placeSignatures: [
    PlaceSignatureData(
      typeSignatureId: '1',
      page: '1',
      positionBottom: '10',
      positionLeft: '20',
      sizeX: '30',
      sizeY: '40',
    ),
  ],
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeDocumentTemplateRepository templateRepository;
  late DocumentTemplateFormViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    templateRepository = FakeDocumentTemplateRepository()
      ..setTemplate(_fakeTemplate);
    viewModel = DocumentTemplateFormViewModel(
      companyRepository: companyRepository,
      documentTemplateRepository: templateRepository,
    );
  });

  tearDown(() => viewModel.dispose());

  group('DocumentTemplateFormViewModel', () {
    group('initial state', () {
      test('starts with idle status and new template', () {
        expect(viewModel.status, DocumentTemplateFormStatus.idle);
        expect(viewModel.isNew, isTrue);
        expect(viewModel.isLoading, isFalse);
        expect(viewModel.isSaving, isFalse);
        expect(viewModel.errorMessage, isNull);
        expect(viewModel.usePreviousPeriod, isFalse);
        expect(viewModel.acceptsSignature, isFalse);
        expect(viewModel.selectedDocumentGroupId, isEmpty);
        expect(viewModel.selectedRecoverDataTypeIds, isEmpty);
        expect(viewModel.placeSignatures, isEmpty);
        expect(viewModel.hasFile, isFalse);
      });
    });

    group('loadTemplate', () {
      test('populates controllers from repository', () async {
        await viewModel.loadTemplate('tpl-1');

        expect(viewModel.status, DocumentTemplateFormStatus.idle);
        expect(viewModel.isNew, isFalse);
        expect(viewModel.nameController.text, 'Contrato CLT');
        expect(viewModel.descriptionController.text,
            'Template de contrato CLT padrão');
        expect(viewModel.validityController.text, '365');
        expect(viewModel.workloadController.text, '44');
        expect(viewModel.usePreviousPeriod, isFalse);
        expect(viewModel.acceptsSignature, isTrue);
        expect(viewModel.bodyFileNameController.text, 'corpo.html');
        expect(viewModel.headerFileNameController.text, 'header.html');
        expect(viewModel.footerFileNameController.text, 'footer.html');
        expect(viewModel.selectedDocumentGroupId, 'grp-1');
        expect(viewModel.selectedRecoverDataTypeIds, [1, 3]);
        expect(viewModel.placeSignatures, hasLength(1));
      });

      test('sets error when repository fails', () async {
        templateRepository.setShouldFail(true);

        await viewModel.loadTemplate('tpl-1');

        expect(viewModel.status, DocumentTemplateFormStatus.error);
        expect(viewModel.errorMessage, isNotNull);
      });

      test('does nothing when templateId is empty', () async {
        await viewModel.loadTemplate('');

        expect(viewModel.status, DocumentTemplateFormStatus.idle);
        expect(viewModel.isNew, isTrue);
      });

      test('populates lookup data (groups, types, signatures)', () async {
        await viewModel.loadTemplate('tpl-1');

        expect(viewModel.documentGroups, hasLength(2));
        expect(viewModel.recoverDataTypes, hasLength(2));
        expect(viewModel.typeSignatures, hasLength(2));
      });
    });

    group('loadOptions', () {
      test('loads lookup data for new template', () async {
        await viewModel.loadOptions();

        expect(viewModel.status, DocumentTemplateFormStatus.idle);
        expect(viewModel.documentGroups, hasLength(2));
        expect(viewModel.recoverDataTypes, hasLength(2));
        expect(viewModel.typeSignatures, hasLength(2));
      });

      test('loads empty lists when repository fails', () async {
        templateRepository.setShouldFail(true);

        await viewModel.loadOptions();

        expect(viewModel.status, DocumentTemplateFormStatus.idle);
        expect(viewModel.documentGroups, isEmpty);
        expect(viewModel.recoverDataTypes, isEmpty);
        expect(viewModel.typeSignatures, isEmpty);
      });
    });

    group('save', () {
      test('creates new template and transitions to saved', () async {
        viewModel.nameController.text = 'Novo Template';
        viewModel.descriptionController.text = 'Descrição do novo template';

        await viewModel.save();

        expect(viewModel.status, DocumentTemplateFormStatus.saved);
        expect(templateRepository.lastCreatedTemplateName, 'Novo Template');
      });

      test('updates existing template and transitions to saved', () async {
        await viewModel.loadTemplate('tpl-1');

        await viewModel.save();

        expect(viewModel.status, DocumentTemplateFormStatus.saved);
        expect(templateRepository.lastUpdatedTemplateId, 'tpl-1');
      });

      test('transitions to error when repository fails', () async {
        viewModel.nameController.text = 'Test';
        viewModel.descriptionController.text = 'Test desc';
        templateRepository.setShouldFail(true);

        await viewModel.save();

        expect(viewModel.status, DocumentTemplateFormStatus.error);
        expect(viewModel.errorMessage, isNotNull);
      });

      test('notifies listeners during save', () async {
        final statuses = <DocumentTemplateFormStatus>[];
        viewModel.addListener(() => statuses.add(viewModel.status));
        viewModel.nameController.text = 'Test';
        viewModel.descriptionController.text = 'Desc';

        await viewModel.save();

        expect(statuses, contains(DocumentTemplateFormStatus.saving));
        expect(statuses.last, DocumentTemplateFormStatus.saved);
      });
    });

    group('setters', () {
      test('setUsePreviousPeriod updates flag and notifies', () {
        var notified = false;
        viewModel.addListener(() => notified = true);

        viewModel.setUsePreviousPeriod(true);

        expect(viewModel.usePreviousPeriod, isTrue);
        expect(notified, isTrue);
      });

      test('setAcceptsSignature updates flag and notifies', () {
        var notified = false;
        viewModel.addListener(() => notified = true);

        viewModel.setAcceptsSignature(true);

        expect(viewModel.acceptsSignature, isTrue);
        expect(notified, isTrue);
      });

      test('setDocumentGroupId updates selection and notifies', () {
        var notified = false;
        viewModel.addListener(() => notified = true);

        viewModel.setDocumentGroupId('grp-2');

        expect(viewModel.selectedDocumentGroupId, 'grp-2');
        expect(notified, isTrue);
      });

      test('setDocumentGroupId handles null by clearing', () {
        viewModel.setDocumentGroupId('grp-1');
        viewModel.setDocumentGroupId(null);

        expect(viewModel.selectedDocumentGroupId, isEmpty);
      });
    });

    group('toggleRecoverDataType', () {
      test('adds type id when not present', () {
        viewModel.toggleRecoverDataType(1);

        expect(viewModel.selectedRecoverDataTypeIds, [1]);
      });

      test('removes type id when already present', () {
        viewModel.toggleRecoverDataType(1);
        viewModel.toggleRecoverDataType(1);

        expect(viewModel.selectedRecoverDataTypeIds, isEmpty);
      });

      test('handles multiple toggles correctly', () {
        viewModel.toggleRecoverDataType(1);
        viewModel.toggleRecoverDataType(3);
        viewModel.toggleRecoverDataType(5);
        viewModel.toggleRecoverDataType(3);

        expect(viewModel.selectedRecoverDataTypeIds, [1, 5]);
      });
    });

    group('replaceRecoverDataType', () {
      test('replaces type at valid index', () {
        viewModel.toggleRecoverDataType(1);
        viewModel.toggleRecoverDataType(3);

        viewModel.replaceRecoverDataType(0, 7);

        expect(viewModel.selectedRecoverDataTypeIds, [7, 3]);
      });

      test('does nothing for invalid index', () {
        viewModel.toggleRecoverDataType(1);

        viewModel.replaceRecoverDataType(-1, 7);
        viewModel.replaceRecoverDataType(5, 7);

        expect(viewModel.selectedRecoverDataTypeIds, [1]);
      });
    });

    group('signature management', () {
      test('addPlaceSignature appends empty signature', () {
        viewModel.addPlaceSignature();

        expect(viewModel.placeSignatures, hasLength(1));
        expect(viewModel.placeSignatures.first.typeSignatureId, isEmpty);
      });

      test('removePlaceSignature removes at index', () {
        viewModel.addPlaceSignature();
        viewModel.addPlaceSignature();

        viewModel.removePlaceSignature(0);

        expect(viewModel.placeSignatures, hasLength(1));
      });

      test('removePlaceSignature does nothing for invalid index', () {
        viewModel.addPlaceSignature();

        viewModel.removePlaceSignature(-1);
        viewModel.removePlaceSignature(5);

        expect(viewModel.placeSignatures, hasLength(1));
      });

      test('updatePlaceSignature updates without notifying', () {
        viewModel.addPlaceSignature();
        var notified = false;
        viewModel.addListener(() => notified = true);

        viewModel.updatePlaceSignature(
          0,
          const PlaceSignatureData(page: '5'),
        );

        expect(viewModel.placeSignatures.first.page, '5');
        expect(notified, isFalse);
      });

      test('updatePlaceSignatureAndNotify updates and notifies', () {
        viewModel.addPlaceSignature();
        var notified = false;
        viewModel.addListener(() => notified = true);

        viewModel.updatePlaceSignatureAndNotify(
          0,
          const PlaceSignatureData(typeSignatureId: '2'),
        );

        expect(viewModel.placeSignatures.first.typeSignatureId, '2');
        expect(notified, isTrue);
      });

      test('updatePlaceSignature does nothing for invalid index', () {
        viewModel.addPlaceSignature();

        viewModel.updatePlaceSignature(
          5,
          const PlaceSignatureData(page: '99'),
        );

        expect(viewModel.placeSignatures.first.page, isEmpty);
      });
    });

    group('validateName', () {
      test('returns error for empty name', () {
        expect(viewModel.validateName(''), isNotNull);
        expect(viewModel.validateName(null), isNotNull);
      });

      test('returns error for whitespace-only name', () {
        expect(viewModel.validateName('   '), isNotNull);
      });

      test('returns null for valid name', () {
        expect(viewModel.validateName('Template A'), isNull);
      });

      test('returns error for name exceeding 100 characters', () {
        final longName = 'a' * 101;
        expect(viewModel.validateName(longName), isNotNull);
      });

      test('returns null for name with exactly 100 characters', () {
        final name = 'a' * 100;
        expect(viewModel.validateName(name), isNull);
      });
    });

    group('validateDescription', () {
      test('returns error for empty description', () {
        expect(viewModel.validateDescription(''), isNotNull);
        expect(viewModel.validateDescription(null), isNotNull);
      });

      test('returns null for valid description', () {
        expect(viewModel.validateDescription('Some description'), isNull);
      });

      test('returns error for description exceeding 500 characters', () {
        final longDesc = 'a' * 501;
        expect(viewModel.validateDescription(longDesc), isNotNull);
      });
    });

    group('validateValidity', () {
      test('returns null for empty value (optional field)', () {
        expect(viewModel.validateValidity(''), isNull);
        expect(viewModel.validateValidity(null), isNull);
        expect(viewModel.validateValidity('  '), isNull);
      });

      test('returns null for valid values in range', () {
        expect(viewModel.validateValidity('0'), isNull);
        expect(viewModel.validateValidity('30'), isNull);
        expect(viewModel.validateValidity('999'), isNull);
      });

      test('returns error for invalid number', () {
        expect(viewModel.validateValidity('abc'), isNotNull);
      });

      test('returns error for out of range values', () {
        expect(viewModel.validateValidity('-1'), isNotNull);
        expect(viewModel.validateValidity('1000'), isNotNull);
      });
    });

    group('validateWorkload', () {
      test('returns null for empty value (optional field)', () {
        expect(viewModel.validateWorkload(''), isNull);
        expect(viewModel.validateWorkload(null), isNull);
      });

      test('returns null for valid values in range', () {
        expect(viewModel.validateWorkload('0'), isNull);
        expect(viewModel.validateWorkload('8'), isNull);
        expect(viewModel.validateWorkload('999'), isNull);
      });

      test('returns error for out of range values', () {
        expect(viewModel.validateWorkload('-1'), isNotNull);
        expect(viewModel.validateWorkload('1000'), isNotNull);
      });
    });

    group('validateSignatureNumber', () {
      test('returns error for empty value', () {
        expect(viewModel.validateSignatureNumber('', 'Página'), isNotNull);
        expect(viewModel.validateSignatureNumber(null, 'Página'), isNotNull);
      });

      test('returns null for valid values in range', () {
        expect(viewModel.validateSignatureNumber('0', 'Página'), isNull);
        expect(viewModel.validateSignatureNumber('50', 'Página'), isNull);
        expect(viewModel.validateSignatureNumber('100', 'Página'), isNull);
        expect(viewModel.validateSignatureNumber('33.5', 'Página'), isNull);
      });

      test('returns error for out of range values', () {
        expect(viewModel.validateSignatureNumber('-1', 'Página'), isNotNull);
        expect(viewModel.validateSignatureNumber('101', 'Página'), isNotNull);
      });

      test('includes label in error message', () {
        final error = viewModel.validateSignatureNumber('', 'Página');
        expect(error, contains('Página'));
      });
    });

    group('validateFileName', () {
      test('returns null for empty value (optional field)', () {
        expect(viewModel.validateFileName(''), isNull);
        expect(viewModel.validateFileName(null), isNull);
        expect(viewModel.validateFileName('  '), isNull);
      });

      test('returns null for valid html file name', () {
        expect(viewModel.validateFileName('corpo.html'), isNull);
        expect(viewModel.validateFileName('header.HTML'), isNull);
      });

      test('returns error for non-html extension', () {
        expect(viewModel.validateFileName('file.txt'), isNotNull);
        expect(viewModel.validateFileName('file.pdf'), isNotNull);
      });

      test('returns error for file name exceeding 20 characters', () {
        final longName = '${'a' * 16}.html';
        expect(viewModel.validateFileName(longName), isNotNull);
      });

      test('returns null for file name with exactly 20 characters', () {
        // 15 chars + .html = 20
        final name = '${'a' * 15}.html';
        expect(viewModel.validateFileName(name), isNull);
      });
    });

    group('loadRecoverDataModels', () {
      test('loads JSON data from repository', () async {
        await viewModel.loadRecoverDataModels();

        expect(viewModel.recoverDataModels,
            '{"company":{"name":"string"}}');
      });

      test('does nothing when no company selected', () async {
        companyRepository.setSelectedCompany(null);

        await viewModel.loadRecoverDataModels();

        expect(viewModel.recoverDataModels, '{}');
      });
    });
  });
}
