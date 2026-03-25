import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/document_group.dart';
import 'package:rufino_v2/ui/features/document_group/viewmodel/document_group_list_viewmodel.dart';

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
  late DocumentGroupListViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    groupRepository = FakeDocumentGroupRepository()
      ..setGroups([_fakeGroup]);
    viewModel = DocumentGroupListViewModel(
      companyRepository: companyRepository,
      documentGroupRepository: groupRepository,
    );
  });

  tearDown(() => viewModel.dispose());

  group('DocumentGroupListViewModel', () {
    test('starts idle with empty list', () {
      expect(viewModel.isLoading, isFalse);
      expect(viewModel.hasError, isFalse);
      expect(viewModel.groups, isEmpty);
    });

    test('loadGroups populates the group list', () async {
      await viewModel.loadGroups();

      expect(viewModel.isLoading, isFalse);
      expect(viewModel.groups, hasLength(1));
      expect(viewModel.groups.first.name, 'Admissão');
    });

    test('loadGroups sets error when no company is selected', () async {
      companyRepository.setSelectedCompany(null);

      await viewModel.loadGroups();

      expect(viewModel.hasError, isTrue);
      expect(viewModel.errorMessage, isNotNull);
    });

    test('loadGroups sets error when repository fails', () async {
      groupRepository.setShouldFail(true);

      await viewModel.loadGroups();

      expect(viewModel.hasError, isTrue);
      expect(viewModel.errorMessage, isNotNull);
    });

    test('loadGroups clears previous error on success', () async {
      groupRepository.setShouldFail(true);
      await viewModel.loadGroups();
      expect(viewModel.hasError, isTrue);

      groupRepository.setShouldFail(false);
      await viewModel.loadGroups();

      expect(viewModel.hasError, isFalse);
      expect(viewModel.groups, hasLength(1));
    });
  });
}
