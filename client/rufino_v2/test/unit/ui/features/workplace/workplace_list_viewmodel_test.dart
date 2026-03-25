import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/address.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/workplace.dart';
import 'package:rufino_v2/ui/features/workplace/viewmodel/workplace_list_viewmodel.dart';

import '../../../../testing/fakes/fake_company_repository.dart';
import '../../../../testing/fakes/fake_workplace_repository.dart';

const _fakeCompany = Company(
  id: 'company-1',
  corporateName: 'Acme Corp',
  fantasyName: 'Acme',
  cnpj: '00000000000000',
);

const _fakeAddress = Address(
  zipCode: '01310100',
  street: 'Av. Paulista',
  number: '1000',
  complement: '',
  neighborhood: 'Bela Vista',
  city: 'São Paulo',
  state: 'SP',
  country: 'Brasil',
);

const _fakeWorkplace = Workplace(
  id: 'wp-1',
  name: 'Sede Principal',
  address: _fakeAddress,
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeWorkplaceRepository workplaceRepository;
  late WorkplaceListViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    workplaceRepository = FakeWorkplaceRepository();
    viewModel = WorkplaceListViewModel(
      companyRepository: companyRepository,
      workplaceRepository: workplaceRepository,
    );
  });

  tearDown(() => viewModel.dispose());

  group('WorkplaceListViewModel', () {
    test('initial state has no loading and empty workplaces', () {
      expect(viewModel.isLoading, isFalse);
      expect(viewModel.hasError, isFalse);
      expect(viewModel.workplaces, isEmpty);
    });

    test('transitions through loading state then populates workplaces on success',
        () async {
      workplaceRepository.setWorkplaces([_fakeWorkplace]);

      final future = viewModel.loadWorkplaces();
      expect(viewModel.isLoading, isTrue);

      await future;

      expect(viewModel.isLoading, isFalse);
      expect(viewModel.hasError, isFalse);
      expect(viewModel.workplaces, [_fakeWorkplace]);
    });

    test('sets hasError and clears workplaces when repository fails', () async {
      workplaceRepository.setShouldFail(true);

      await viewModel.loadWorkplaces();

      expect(viewModel.hasError, isTrue);
      expect(viewModel.workplaces, isEmpty);
      expect(viewModel.errorMessage, isNotNull);
    });

    test('sets hasError with company message when no company is selected',
        () async {
      companyRepository.setSelectedCompany(null);

      await viewModel.loadWorkplaces();

      expect(viewModel.hasError, isTrue);
      expect(viewModel.errorMessage, contains('empresa'));
    });

    test('succeeds with empty list when company has no workplaces', () async {
      workplaceRepository.setWorkplaces([]);

      await viewModel.loadWorkplaces();

      expect(viewModel.hasError, isFalse);
      expect(viewModel.workplaces, isEmpty);
    });

    test('notifies listeners on loading start and on completion', () async {
      workplaceRepository.setWorkplaces([_fakeWorkplace]);
      final states = <bool>[];
      viewModel.addListener(() => states.add(viewModel.isLoading));

      await viewModel.loadWorkplaces();

      expect(states, [true, false]);
    });
  });
}
