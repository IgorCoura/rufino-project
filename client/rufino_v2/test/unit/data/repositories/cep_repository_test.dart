import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart' as http_testing;
import 'package:rufino_v2/core/errors/cep_exception.dart';
import 'package:rufino_v2/data/repositories/cep_repository_impl.dart';
import 'package:rufino_v2/data/services/cep_api_service.dart';

void main() {
  group('CepRepositoryImpl', () {
    test('returns Result.success with mapped Address on success', () async {
      final client = http_testing.MockClient((_) async {
        return http.Response(
          jsonEncode({
            'cep': '01310-100',
            'logradouro': 'Avenida Paulista',
            'complemento': '',
            'bairro': 'Bela Vista',
            'localidade': 'São Paulo',
            'uf': 'SP',
          }),
          200,
        );
      });
      final repo = CepRepositoryImpl(apiService: CepApiService(client: client));

      final result = await repo.lookupCep('01310100');

      expect(result.isSuccess, isTrue);
      final address = result.valueOrNull!;
      expect(address.city, 'São Paulo');
      expect(address.country, 'Brasil');
    });

    test('returns Result.error with CepNotFoundException when CEP is unknown',
        () async {
      final client = http_testing.MockClient((_) async {
        return http.Response(jsonEncode({'erro': true}), 200);
      });
      final repo = CepRepositoryImpl(apiService: CepApiService(client: client));

      final result = await repo.lookupCep('00000000');

      expect(result.isError, isTrue);
      expect(result.errorOrNull, isA<CepNotFoundException>());
    });

    test('returns Result.error with CepLookupException on HTTP failure',
        () async {
      final client = http_testing.MockClient((_) async {
        return http.Response('boom', 500);
      });
      final repo = CepRepositoryImpl(apiService: CepApiService(client: client));

      final result = await repo.lookupCep('12345678');

      expect(result.isError, isTrue);
      expect(result.errorOrNull, isA<CepLookupException>());
    });
  });
}
