import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/ui/features/company/viewmodel/company_form_viewmodel.dart';

import '../../../../testing/fakes/fake_company_repository.dart';

void main() {
  late FakeCompanyRepository companyRepository;
  late CompanyFormViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository();
    viewModel = CompanyFormViewModel(companyRepository: companyRepository);
  });

  tearDown(() => viewModel.dispose());

  group('CompanyFormViewModel', () {
    test('initial state is new company (empty id)', () {
      expect(viewModel.isNew, true);
      expect(viewModel.status, CompanyFormStatus.idle);
    });

    test('loadCompany populates fields', () async {
      await viewModel.loadCompany('company-1');

      expect(viewModel.status, CompanyFormStatus.idle);
      expect(viewModel.corporateName, 'Acme Corp S.A.');
      expect(viewModel.fantasyName, 'Acme');
    });

    test('save for new company transitions to saved', () async {
      viewModel.setCorporateName('Nova Empresa');
      viewModel.setFantasyName('Nova');
      viewModel.setCnpj('12345678000190');
      viewModel.setEmail('teste@email.com');
      viewModel.setPhone('11999999999');
      viewModel.setZipCode('01310100');
      viewModel.setStreet('Rua Teste');
      viewModel.setNumber('100');
      viewModel.setComplement('');
      viewModel.setNeighborhood('Centro');
      viewModel.setCity('São Paulo');
      viewModel.setState('SP');
      viewModel.setCountry('Brasil');

      await viewModel.save();

      expect(viewModel.status, CompanyFormStatus.saved);
    });
  });
}
