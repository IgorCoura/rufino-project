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
      expect(viewModel.corporateNameController.text, 'Acme Corp S.A.');
      expect(viewModel.fantasyNameController.text, 'Acme');
    });

    test('save for new company transitions to saved', () async {
      viewModel.corporateNameController.text = 'Nova Empresa';
      viewModel.fantasyNameController.text = 'Nova';
      viewModel.cnpjController.text = '12345678000190';
      viewModel.emailController.text = 'teste@email.com';
      viewModel.phoneController.text = '11999999999';
      viewModel.zipCodeController.text = '01310100';
      viewModel.streetController.text = 'Rua Teste';
      viewModel.numberController.text = '100';
      viewModel.complementController.text = '';
      viewModel.neighborhoodController.text = 'Centro';
      viewModel.cityController.text = 'São Paulo';
      viewModel.stateController.text = 'SP';
      viewModel.countryController.text = 'Brasil';

      await viewModel.save();

      expect(viewModel.status, CompanyFormStatus.saved);
    });
  });
}
