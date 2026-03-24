import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/require_document.dart';
import 'package:rufino_v2/ui/features/require_document/viewmodel/require_document_list_viewmodel.dart';

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
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeRequireDocumentRepository requireDocumentRepository;
  late RequireDocumentListViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    requireDocumentRepository = FakeRequireDocumentRepository()
      ..setRequireDocuments([_fakeRequireDocument]);
    viewModel = RequireDocumentListViewModel(
      companyRepository: companyRepository,
      requireDocumentRepository: requireDocumentRepository,
    );
  });

  group('RequireDocumentListViewModel', () {
    test('starts with empty list and not loading', () {
      expect(viewModel.requireDocuments, isEmpty);
      expect(viewModel.isLoading, isFalse);
      expect(viewModel.hasError, isFalse);
    });

    test('loadRequireDocuments populates the list on success', () async {
      await viewModel.loadRequireDocuments();

      expect(viewModel.requireDocuments.length, 1);
      expect(viewModel.requireDocuments.first.name, 'Admissão CLT');
      expect(viewModel.isLoading, isFalse);
      expect(viewModel.hasError, isFalse);
    });

    test('loadRequireDocuments sets error state on failure', () async {
      requireDocumentRepository.setShouldFail(true);

      await viewModel.loadRequireDocuments();

      expect(viewModel.requireDocuments, isEmpty);
      expect(viewModel.hasError, isTrue);
      expect(viewModel.errorMessage, isNotNull);
    });

    test('loadRequireDocuments transitions through loading state', () async {
      expect(viewModel.isLoading, isFalse);

      final future = viewModel.loadRequireDocuments();
      expect(viewModel.isLoading, isTrue);

      await future;
      expect(viewModel.isLoading, isFalse);
    });
  });
}
