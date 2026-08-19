import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/payer_profile/payer_profile_viewmodel.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../fakes/fakes.dart';

void main() {
  late FakePayerProfileRepository repository;
  late PayerProfileViewModel viewModel;

  setUp(() {
    repository = FakePayerProfileRepository();
    viewModel = PayerProfileViewModel(repository: repository);
  });

  tearDown(() => viewModel.dispose());

  group('PayerProfileViewModel', () {
    test('an absent profile lands on onboarding, not error', () async {
      await viewModel.load();

      expect(viewModel.status, PayerProfileStatus.onboarding);
      expect(viewModel.errorMessage, isNull);
    });

    test('an existing profile lands on loaded', () async {
      repository.profile = payerProfile();

      await viewModel.load();

      expect(viewModel.status, PayerProfileStatus.loaded);
      expect(viewModel.profile!.legalName, 'Padaria do Zé LTDA');
    });

    test('registering moves the screen from onboarding to loaded', () async {
      await viewModel.load();

      final registered = await viewModel.register(
        kind: PayerKinds.company,
        legalName: 'Padaria do Zé LTDA',
        primaryTaxId: '11.222.333/0001-81',
      );

      expect(registered, isTrue);
      expect(viewModel.status, PayerProfileStatus.loaded);
    });

    test('a refused mutation surfaces the rule message and stays put',
        () async {
      repository.profile = payerProfile();
      await viewModel.load();
      repository.setShouldFail(true);

      final saved = await viewModel.addTaxId('111.444.777-35');

      expect(saved, isFalse);
      expect(viewModel.errorMessage, 'regra disse não');
    });

    test('a blank account reference is sent as null to clear the link',
        () async {
      repository.profile = payerProfile();
      await viewModel.load();

      await viewModel.linkAsaasAccount('   ');

      expect(repository.calls, contains('linkAsaasAccount:-'));
    });

    test('a failed load lands on error with a message', () async {
      repository.setShouldFail(true);

      await viewModel.load();

      expect(viewModel.status, PayerProfileStatus.error);
      expect(viewModel.errorMessage, isNotNull);
    });
  });
}
