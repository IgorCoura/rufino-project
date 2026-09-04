import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/data/payer_profile_api_models.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:rufino_core/rufino_core.dart';

import '../fakes/fakes.dart';

class MockPayerProfileApiService extends Mock
    implements PayerProfileApiService {}

void main() {
  late MockPayerProfileApiService apiService;
  late FakeErrorReporter reporter;
  late PayerProfileRepositoryImpl repository;

  setUp(() {
    apiService = MockPayerProfileApiService();
    reporter = FakeErrorReporter();
    repository =
        PayerProfileRepositoryImpl(apiService: apiService, reporter: reporter);
  });

  group('PayerProfileRepositoryImpl', () {
    test('a missing profile resolves to null — the onboarding state, not an '
        'error', () async {
      when(() => apiService.getProfile()).thenAnswer((_) async => null);

      final result = await repository.getProfile();

      result.fold(
        onSuccess: (profile) => expect(profile, isNull),
        onError: (error, _) => fail('404 is onboarding, not failure: $error'),
      );
      expect(reporter.capturedErrors, isEmpty);
    });

    test('a duplicate registration surfaces the domain message without '
        'reporting', () async {
      when(
        () => apiService.registerProfile(
          kind: any(named: 'kind'),
          legalName: any(named: 'legalName'),
          primaryTaxId: any(named: 'primaryTaxId'),
        ),
      ).thenThrow(
        const HttpException(
          statusCode: 409,
          message: 'HTTP 409',
          serverMessages: ['Perfil já cadastrado.'],
          domainErrorId: 'BLP.PRF02',
        ),
      );

      final result = await repository.registerProfile(
        kind: PayerKinds.company,
        legalName: 'Padaria do Zé LTDA',
        primaryTaxId: '11.222.333/0001-81',
      );

      result.fold(
        onSuccess: (_) => fail('should have failed'),
        onError: (error, _) {
          expect((error as BillPaymentRuleException).code, 'BLP.PRF02');
        },
      );
      expect(reporter.capturedErrors, isEmpty);
    });

    test('linking the provider account resolves to the scheduling flag',
        () async {
      when(() => apiService.linkAsaasAccount(any()))
          .thenAnswer((_) async => true);

      final result = await repository.linkAsaasAccount('acct-ref');

      result.fold(
        onSuccess: (can) => expect(can, isTrue),
        onError: (error, _) => fail('should have succeeded: $error'),
      );
    });

    test('an outage is wrapped as a network failure and reported', () async {
      when(() => apiService.getProfile())
          .thenThrow(const HttpException(statusCode: 503, message: 'HTTP 503'));

      final result = await repository.getProfile();

      result.fold(
        onSuccess: (_) => fail('should have failed'),
        onError: (error, _) =>
            expect(error, isA<BillPaymentNetworkException>()),
      );
      expect(reporter.capturedErrors, hasLength(1));
    });
  });

  group('PayerProfileMapper', () {
    test('maps the full read model including additional documents', () {
      final profile = PayerProfileMapper.fromJson({
        'id': 'profile-1',
        'kind': 'Company',
        'legalName': 'Padaria do Zé LTDA',
        'primaryTaxId': '11.222.333/0001-81',
        'primaryTaxIdKind': 'CNPJ',
        'additionalTaxIds': [
          {'value': '111.444.777-35', 'kind': 'CPF'},
        ],
        'matchByCnpjRoot': true,
        'canSchedulePayments': false,
      });

      expect(profile.kind, 'Company');
      expect(profile.additionalTaxIds, hasLength(1));
      expect(profile.additionalTaxIds.first.kind, 'CPF');
      expect(profile.matchByCnpjRoot, isTrue);
      expect(profile.supportsCnpjRootMatching, isTrue);
    });
  });
}
