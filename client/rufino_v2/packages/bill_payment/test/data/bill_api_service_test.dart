import 'dart:convert';

import 'package:bill_payment/bill_payment.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

/// A requisição que o serviço monta — o nível que os testes de repositório não veem.
///
/// Os testes de ViewModel trocam o repositório por um fake, então nada exercitava a construção
/// do `multipart/form-data` da importação com anexo: nome das partes, campos obrigatórios e o
/// `Content-Type` que o multipart precisa montar sozinho.
void main() {
  const tenant = '229b0dc9-5b8c-4aad-8774-dfc3f14e1a6b';
  const okBody = '{"id":"bill-1","kind":"BankSlip","rail":"Pix"}';

  late List<http.BaseRequest> sent;
  late List<String> bodies;

  BillApiService serviceReturning(String body, {int status = 200}) {
    sent = [];
    bodies = [];

    return BillApiService(
      client: MockClient((request) async {
        sent.add(request);
        bodies.add(request.body);
        return http.Response(body, status);
      }),
      baseUrl: 'http://localhost:8100',
      getAuthHeader: () async => 'Bearer token',
      getTenantId: () => tenant,
    );
  }

  group('importBill', () {
    test('sends JSON when there is no file', () async {
      final service = serviceReturning(okBody);

      final outcome = await service.importBill(digitableLine: '3419...');

      expect(sent.single.headers['Content-Type'], contains('application/json'));
      expect(outcome.id, 'bill-1');
    });

    test('sends multipart with the file and the required fields', () async {
      final service = serviceReturning(okBody);

      final outcome = await service.importBill(
        documentBytes: const [1, 2, 3],
        documentFileName: 'boleto.pdf',
        documentContentType: 'application/pdf',
      );

      final request = sent.single;
      expect(request.headers['Content-Type'], contains('multipart/form-data'));
      expect(request.url.path, '/api/v1/$tenant/bills/import');

      final body = bodies.single;
      expect(body, contains('name="file"'));
      expect(body, contains('filename="boleto.pdf"'));
      expect(body, contains('name="sourceKind"'));
      expect(body, contains('ManualUpload'));
      expect(body, contains('name="receivedAt"'));

      expect(outcome.id, 'bill-1');
    });

    test('carries the typed digits alongside the file', () async {
      final service = serviceReturning(okBody);

      await service.importBill(
        digitableLine: '34191.23454 67890.123457 67890.123457 3 14880000061507',
        documentBytes: const [1, 2, 3],
        documentFileName: 'boleto.pdf',
        documentContentType: 'application/pdf',
      );

      final body = bodies.single;
      expect(body, contains('name="digitableLine"'));
      expect(body, contains('34191234546789012345767890123457314880000061507'));
    });

    test('surfaces the domain error the server sent', () async {
      final service = serviceReturning(
        jsonEncode({'id': 'BLP.BIL02', 'message': 'ja foi importado'}),
        status: 409,
      );

      await expectLater(
        service.importBill(
          documentBytes: const [1, 2, 3],
          documentFileName: 'boleto.pdf',
          documentContentType: 'application/pdf',
        ),
        throwsA(isA<HttpException>()),
      );
    });
  });
}
