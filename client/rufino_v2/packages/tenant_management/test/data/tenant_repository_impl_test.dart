import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart' as http_testing;
import 'package:rufino_core/rufino_core.dart';
import 'package:tenant_management/tenant_management.dart';

import '../fakes/fake_error_reporter.dart';

const _tenantJson = {
  'id': '11111111-1111-1111-1111-111111111111',
  'kind': 'Company',
  'legalName': 'Padaria do Zé LTDA',
  'tradeName': 'Pão Quente',
  'primaryTaxId': '11222333000181',
  'primaryTaxIdKind': 'CNPJ',
  'status': 'Active',
  'suspensionReason': '',
  'accessProvisioning': 'Failed',
  'contact': {'email': 'contato@paoquente.com.br', 'phone': '31999990000'},
  'address': {
    'zipCode': '30110000',
    'street': 'RUA DAS FLORES',
    'number': '100',
    'complement': '',
    'neighborhood': 'CENTRO',
    'city': 'BELO HORIZONTE',
    'state': 'MG',
    'country': 'BRASIL',
  },
  'products': [
    {
      'product': 'PeopleManagement',
      'isActive': true,
      'activatedAt': '2026-01-01T00:00:00',
      'deactivatedAt': null,
    },
  ],
  'memberships': [
    {
      'email': 'dono@paoquente.com.br',
      'role': 'Owner',
      'isActive': true,
      'provisioning': 'Failed',
      'userId': null,
    },
  ],
  'createdAt': '2026-01-01T00:00:00',
  'updatedAt': '2026-01-02T00:00:00',
};

TenantRepositoryImpl _repository(
  http.Client client,
  FakeErrorReporter reporter,
) {
  return TenantRepositoryImpl(
    apiService: TenantApiService(
      client: client,
      baseUrl: 'tenants.example.com',
      getAuthHeader: () async => 'Bearer token',
    ),
    reporter: reporter,
  );
}

void main() {
  late FakeErrorReporter reporter;

  setUp(() => reporter = FakeErrorReporter());

  group('TenantRepositoryImpl reading', () {
    test('turns the read model into a tenant with its access state', () async {
      final client = http_testing.MockClient(
        (_) async => http.Response(jsonEncode(_tenantJson), 200),
      );

      final result = await _repository(client, reporter).getTenant('x');
      final tenant = result.valueOrNull!;

      expect(tenant.displayName, 'Pão Quente');
      expect(tenant.formattedTaxId, '11.222.333/0001-81');
      expect(tenant.hasFailedProvisioning, isTrue);
      expect(tenant.memberships.single.hasFailed, isTrue);
      expect(tenant.address.city, 'BELO HORIZONTE');
      expect(tenant.contact.formattedPhone, '(31) 99999-0000');
    });

    test('reads a listing page and its cursor', () async {
      final client = http_testing.MockClient(
        (_) async => http.Response(
          jsonEncode({
            'items': [
              {
                'id': 'a',
                'kind': 'Individual',
                'legalName': 'José da Silva',
                'tradeName': '',
                'primaryTaxId': '52998224725',
                'status': 'Active',
                'accessProvisioning': 'Done',
                'contactEmail': 'jose@exemplo.com',
                'activeProducts': ['BillPayment'],
                'createdAt': '2026-01-01T00:00:00',
              },
            ],
            'nextCursor': 'abc',
          }),
          200,
        ),
      );

      final page = (await _repository(client, reporter).listTenants())
          .valueOrNull!;

      expect(page.items.single.displayName, 'José da Silva');
      expect(page.items.single.formattedTaxId, '529.982.247-25');
      expect(page.hasMore, isTrue);
      expect(page.nextCursor, 'abc');
    });

    test('a page without a cursor is the last one', () async {
      final client = http_testing.MockClient(
        (_) async => http.Response(
          jsonEncode({'items': <dynamic>[], 'nextCursor': null}),
          200,
        ),
      );

      final page = (await _repository(client, reporter).listTenants())
          .valueOrNull!;

      expect(page.items, isEmpty);
      expect(page.hasMore, isFalse);
    });
  });

  group('TenantRepositoryImpl failure classification', () {
    test('a refused rule carries the server message and is not reported',
        () async {
      final client = http_testing.MockClient(
        (_) async => http.Response(
          jsonEncode({
            'id': 'TNM.TNT20',
            'message': 'O último responsável não pode perder o acesso.',
          }),
          400,
        ),
      );

      final result = await _repository(client, reporter)
          .revokeMembership('x', 'dono@paoquente.com.br');

      expect(result.isError, isTrue);
      result.fold(
        onSuccess: (_) => fail('expected a failure'),
        onError: (error, _) {
          expect(error, isA<TenantRuleException>());
          expect((error as TenantRuleException).code, 'TNM.TNT20');
          expect(
            tenantErrorMessage(error, fallback: 'x'),
            'O último responsável não pode perder o acesso.',
          );
        },
      );
      // Regra de negócio recusando é o sistema funcionando: não vai para o
      // monitoramento de erro.
      expect(reporter.capturedErrors, isEmpty);
    });

    test('a server fault is reported as a network failure', () async {
      final client = http_testing.MockClient(
        (_) async => http.Response('boom', 500),
      );

      final result = await _repository(client, reporter).getTenant('x');

      expect(result.isError, isTrue);
      expect(reporter.capturedErrors, hasLength(1));
      expect(
        reporter.capturedErrors.single.error,
        isA<TenantNetworkException>(),
      );
    });

    test('403 becomes access denied and is never reported', () async {
      final client = http_testing.MockClient(
        (_) async => http.Response('', 403),
      );

      final result = await _repository(client, reporter).getTenant('x');

      expect(result.isError, isTrue);
      result.fold(
        onSuccess: (_) => fail('expected a failure'),
        onError: (error, _) {
          expect(error, isA<TenantNetworkException>());
          expect(
            (error as TenantNetworkException).cause,
            isA<AccessDeniedException>(),
          );
          expect(
            tenantErrorMessage(error, fallback: 'x'),
            'Você não tem permissão para esta ação.',
          );
        },
      );
      // 403 é ExpectedFailure embrulhado: negar permissão não é bug.
      expect(reporter.capturedErrors, isEmpty);
    });

    test('401 becomes a session expiry and is never reported', () async {
      final client = http_testing.MockClient(
        (_) async => http.Response('', 401),
      );

      final result = await _repository(client, reporter).getMyTenants();

      expect(result.isError, isTrue);
      expect(reporter.capturedErrors, isEmpty);
    });
  });

  group('TenantApiService requests', () {
    test('every write carries an idempotency id, reads do not', () async {
      final headers = <String, Map<String, String>>{};
      final client = http_testing.MockClient((request) async {
        headers[request.method] = request.headers;
        return http.Response(jsonEncode({'id': 'new-id'}), 200);
      });

      final repo = _repository(client, reporter);
      await repo.getMyTenants();
      await repo.reprovisionAccess('x');

      expect(headers['GET']!.containsKey('x-requestid'), isFalse);
      expect(headers['POST']!['x-requestid'], isNotEmpty);
    });

    test('the revoked e-mail travels in the query string, not the path',
        () async {
      Uri? seen;
      final client = http_testing.MockClient((request) async {
        seen = request.url;
        return http.Response('', 200);
      });

      await _repository(client, reporter)
          .revokeMembership('abc', 'dono@paoquente.com.br');

      expect(seen!.path, '/api/v1/tenants/abc/members');
      expect(seen!.queryParameters['email'], 'dono@paoquente.com.br');
    });

    test('an individual sends no trade name at all', () async {
      Map<String, dynamic>? body;
      final client = http_testing.MockClient((request) async {
        body = jsonDecode(request.body) as Map<String, dynamic>;
        return http.Response(jsonEncode({'id': 'new-id'}), 200);
      });

      await _repository(client, reporter).registerTenant(
        const RegisterTenantInput(
          kind: TenantKinds.individual,
          legalName: 'José da Silva',
          tradeName: '',
          primaryTaxId: '52998224725',
          contactEmail: 'jose@exemplo.com',
          contactPhone: '',
          address: TenantAddress(
            zipCode: '30110000',
            street: 'Rua das Flores',
            number: '100',
            complement: '',
            neighborhood: 'Centro',
            city: 'Belo Horizonte',
            state: 'MG',
          ),
          ownerEmail: 'jose@exemplo.com',
          products: ['BillPayment'],
        ),
      );

      expect(body!['tradeName'], isNull);
      expect(body!['contactPhone'], isNull);
      expect(body!['kind'], 'Individual');
      expect(body!.containsKey('id'), isFalse);
    });

    test('an informed id is sent so a migrated cadastro keeps its identity',
        () async {
      Map<String, dynamic>? body;
      final client = http_testing.MockClient((request) async {
        body = jsonDecode(request.body) as Map<String, dynamic>;
        return http.Response(jsonEncode({'id': 'kept'}), 200);
      });

      await _repository(client, reporter).registerTenant(
        const RegisterTenantInput(
          kind: TenantKinds.company,
          legalName: 'Padaria do Zé LTDA',
          tradeName: 'Pão Quente',
          primaryTaxId: '11222333000181',
          contactEmail: 'contato@paoquente.com.br',
          contactPhone: '31999990000',
          address: TenantAddress(
            zipCode: '30110000',
            street: 'Rua das Flores',
            number: '100',
            complement: '',
            neighborhood: 'Centro',
            city: 'Belo Horizonte',
            state: 'MG',
          ),
          ownerEmail: 'dono@paoquente.com.br',
          products: [],
          id: 'kept',
        ),
      );

      expect(body!['id'], 'kept');
    });
  });
}
