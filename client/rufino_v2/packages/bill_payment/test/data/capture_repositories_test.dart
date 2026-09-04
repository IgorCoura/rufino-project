import 'package:bill_payment/bill_payment.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:rufino_core/rufino_core.dart';

import '../fakes/fakes.dart';

class MockCaptureSourceApiService extends Mock
    implements CaptureSourceApiService {}

class MockCaptureItemApiService extends Mock implements CaptureItemApiService {}

void main() {
  const credential = GraphCredentialInput(
    directoryId: 'dir-1',
    clientId: 'client-1',
    clientSecret: 'secret',
  );

  setUpAll(() {
    registerFallbackValue(credential);
  });

  group('CaptureSourceRepositoryImpl', () {
    late MockCaptureSourceApiService apiService;
    late FakeErrorReporter reporter;
    late CaptureSourceRepositoryImpl repository;

    setUp(() {
      apiService = MockCaptureSourceApiService();
      reporter = FakeErrorReporter();
      repository = CaptureSourceRepositoryImpl(
        apiService: apiService,
        reporter: reporter,
      );
    });

    test('a refused access proof surfaces the domain rule without reporting',
        () async {
      when(
        () => apiService.connectSource(
          displayName: any(named: 'displayName'),
          address: any(named: 'address'),
          credential: any(named: 'credential'),
          folderPath: any(named: 'folderPath'),
        ),
      ).thenThrow(
        const HttpException(
          statusCode: 409,
          message: 'HTTP 409',
          serverMessages: ['A caixa não respondeu à prova de acesso.'],
          domainErrorId: 'BLP.CPS14',
        ),
      );

      final result = await repository.connectSource(
        displayName: 'Contas',
        address: 'contas@empresa.com.br',
        credential: credential,
      );

      result.fold(
        onSuccess: (_) => fail('should have failed'),
        onError: (error, _) =>
            expect((error as BillPaymentRuleException).code, 'BLP.CPS14'),
      );
      expect(reporter.capturedErrors, isEmpty);
    });

    test('a successful connect resolves to the new source id and nothing '
        'else — the server no longer discloses other accounts', () async {
      when(
        () => apiService.connectSource(
          displayName: any(named: 'displayName'),
          address: any(named: 'address'),
          credential: any(named: 'credential'),
          folderPath: any(named: 'folderPath'),
        ),
      ).thenAnswer((_) async => const ConnectOutcome(id: 'src-1'));

      final result = await repository.connectSource(
        displayName: 'Contas',
        address: 'contas@empresa.com.br',
        credential: credential,
      );

      result.fold(
        onSuccess: (outcome) => expect(outcome.id, 'src-1'),
        onError: (error, _) => fail('should have succeeded: $error'),
      );
    });

    test('the credential travels as one opaque JSON string', () {
      final encoded = CaptureSourceApiService.encodeCredential(credential);

      expect(encoded, contains('"directoryId":"dir-1"'));
      expect(encoded, contains('"clientId":"client-1"'));
      expect(encoded, contains('"clientSecret":"secret"'));
    });
  });

  group('CaptureItemRepositoryImpl', () {
    late MockCaptureItemApiService apiService;
    late FakeErrorReporter reporter;
    late CaptureItemRepositoryImpl repository;

    setUp(() {
      apiService = MockCaptureItemApiService();
      reporter = FakeErrorReporter();
      repository = CaptureItemRepositoryImpl(
        apiService: apiService,
        reporter: reporter,
      );
    });

    test('a contradicted claim surfaces BLP.CPI04 without reporting',
        () async {
      when(() => apiService.claimItem(any())).thenThrow(
        const HttpException(
          statusCode: 409,
          message: 'HTTP 409',
          serverMessages: ['O pagador identificado não é este cliente.'],
          domainErrorId: 'BLP.CPI04',
        ),
      );

      final result = await repository.claimItem('item-1');

      result.fold(
        onSuccess: (_) => fail('should have failed'),
        onError: (error, _) =>
            expect((error as BillPaymentRuleException).code, 'BLP.CPI04'),
      );
      expect(reporter.capturedErrors, isEmpty);
    });

    test('a successful claim resolves to the new bill id', () async {
      when(() => apiService.claimItem(any())).thenAnswer(
        (_) async => const ClaimOutcome(
          id: 'item-1',
          billId: 'bill-7',
          status: 'Promoted',
        ),
      );

      final result = await repository.claimItem('item-1');

      result.fold(
        onSuccess: (outcome) => expect(outcome.billId, 'bill-7'),
        onError: (error, _) => fail('should have succeeded: $error'),
      );
    });

    test('the quarantine mapper renders absent financial fields as null', () {
      final item = CaptureItemMapper.fromJson({
        'id': 'item-1',
        'sourceId': 'src-1',
        'sender': 'contador@escritorio.com.br',
        'subject': 'Boleto sindicato',
        'receivedAt': '2026-08-01T09:00:00Z',
        'status': 'Unrecognized',
        'reason': 'no_instrument_in_document',
      });

      expect(item.hasArtifact, isFalse);
      expect(item.sourceUrl, isNull);
      expect(item.billId, isNull);
      expect(item.acceptsReprocess, isTrue);
      expect(item.acceptsClaim, isFalse);
    });
  });
}
