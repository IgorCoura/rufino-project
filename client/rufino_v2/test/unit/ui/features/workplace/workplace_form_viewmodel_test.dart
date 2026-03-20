import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/address.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/workplace.dart';
import 'package:rufino_v2/ui/features/workplace/viewmodel/workplace_form_viewmodel.dart';

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
  late WorkplaceFormViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    workplaceRepository = FakeWorkplaceRepository();
    viewModel = WorkplaceFormViewModel(
      companyRepository: companyRepository,
      workplaceRepository: workplaceRepository,
    );
  });

  tearDown(() => viewModel.dispose());

  group('WorkplaceFormViewModel', () {
    test('initial state is new workplace with idle status', () {
      expect(viewModel.isNew, isTrue);
      expect(viewModel.isLoading, isFalse);
      expect(viewModel.isSaving, isFalse);
      expect(viewModel.name, '');
    });

    test('loadWorkplace populates all fields from repository', () async {
      workplaceRepository.setWorkplace(_fakeWorkplace);

      await viewModel.loadWorkplace('wp-1');

      expect(viewModel.isNew, isFalse);
      expect(viewModel.name, 'Sede Principal');
      expect(viewModel.zipCode, '01310100');
      expect(viewModel.street, 'Av. Paulista');
      expect(viewModel.number, '1000');
      expect(viewModel.neighborhood, 'Bela Vista');
      expect(viewModel.city, 'São Paulo');
      expect(viewModel.state, 'SP');
      expect(viewModel.country, 'Brasil');
    });

    test('loadWorkplace transitions to error when repository fails', () async {
      workplaceRepository.setShouldFail(true);

      await viewModel.loadWorkplace('wp-1');

      expect(viewModel.status, WorkplaceFormStatus.error);
      expect(viewModel.errorMessage, isNotNull);
    });

    test('loadWorkplace does nothing when workplaceId is empty', () async {
      await viewModel.loadWorkplace('');

      expect(viewModel.isNew, isTrue);
      expect(viewModel.status, WorkplaceFormStatus.idle);
    });

    test('save for new workplace transitions to saved and calls createWorkplace',
        () async {
      viewModel
        ..setName('Filial SP')
        ..setZipCode('01310100')
        ..setStreet('Av. Paulista')
        ..setNumber('1000')
        ..setComplement('')
        ..setNeighborhood('Bela Vista')
        ..setCity('São Paulo')
        ..setState('SP')
        ..setCountry('Brasil');

      await viewModel.save();

      expect(viewModel.status, WorkplaceFormStatus.saved);
      expect(workplaceRepository.lastCreatedWorkplaceName, 'Filial SP');
    });

    test('save for existing workplace transitions to saved and calls updateWorkplace',
        () async {
      workplaceRepository.setWorkplace(_fakeWorkplace);
      await viewModel.loadWorkplace('wp-1');

      viewModel.setName('Sede Atualizada');
      await viewModel.save();

      expect(viewModel.status, WorkplaceFormStatus.saved);
      expect(workplaceRepository.lastUpdatedWorkplaceId, 'wp-1');
    });

    test('save transitions to error when repository fails', () async {
      workplaceRepository.setWorkplace(_fakeWorkplace);
      await viewModel.loadWorkplace('wp-1');

      workplaceRepository.setShouldFail(true);
      await viewModel.save();

      expect(viewModel.status, WorkplaceFormStatus.error);
      expect(viewModel.errorMessage, isNotNull);
    });

    test('save transitions through saving state before completing', () async {
      final states = <WorkplaceFormStatus>[];
      viewModel.addListener(() => states.add(viewModel.status));

      await viewModel.save();

      expect(states, containsAllInOrder([
        WorkplaceFormStatus.saving,
        WorkplaceFormStatus.saved,
      ]));
    });
  });
}
