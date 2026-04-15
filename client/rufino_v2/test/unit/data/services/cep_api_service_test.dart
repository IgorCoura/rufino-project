import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart' as http_testing;
import 'package:rufino_v2/core/errors/cep_exception.dart';
import 'package:rufino_v2/data/services/cep_api_service.dart';

void main() {
  group('CepApiService', () {
    test('lookup strips formatting and hits the ViaCEP JSON endpoint',
        () async {
      late Uri calledUri;
      final client = http_testing.MockClient((request) async {
        calledUri = request.url;
        return http.Response(
          jsonEncode({
            'cep': '01310-100',
            'logradouro': 'Avenida Paulista',
            'complemento': 'de 612 a 1510 - lado par',
            'bairro': 'Bela Vista',
            'localidade': 'São Paulo',
            'uf': 'SP',
          }),
          200,
          headers: {'content-type': 'application/json; charset=utf-8'},
        );
      });
      final service = CepApiService(client: client);

      final dto = await service.lookup('01310-100');

      expect(calledUri.toString(), 'https://viacep.com.br/ws/01310100/json');
      expect(dto.logradouro, 'Avenida Paulista');
      expect(dto.bairro, 'Bela Vista');
      expect(dto.localidade, 'São Paulo');
      expect(dto.uf, 'SP');
    });

    test('lookup throws CepNotFoundException when body contains erro=true',
        () async {
      final client = http_testing.MockClient((request) async {
        return http.Response(jsonEncode({'erro': true}), 200);
      });
      final service = CepApiService(client: client);

      expect(
        () => service.lookup('00000000'),
        throwsA(isA<CepNotFoundException>()),
      );
    });

    test('lookup throws CepLookupException on non-2xx status', () async {
      final client = http_testing.MockClient((request) async {
        return http.Response('boom', 500);
      });
      final service = CepApiService(client: client);

      expect(
        () => service.lookup('12345678'),
        throwsA(isA<CepLookupException>()),
      );
    });

    test('lookup throws CepLookupException on malformed body', () async {
      final client = http_testing.MockClient((request) async {
        return http.Response('not-json', 200);
      });
      final service = CepApiService(client: client);

      expect(
        () => service.lookup('12345678'),
        throwsA(isA<CepLookupException>()),
      );
    });
  });
}
