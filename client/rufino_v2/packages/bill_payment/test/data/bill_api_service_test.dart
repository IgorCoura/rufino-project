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

  group('previewSchedule', () {
    test('asks the schedule-preview route with the date-only query and '
        'parses the four fields', () async {
      final service = serviceReturning(jsonEncode({
        'requestedDate': '2026-09-10',
        'effectiveDate': '2026-09-11',
        'slid': true,
        'immediate': false,
        'afterDueDate': true,
      }));

      final preview =
          await service.previewSchedule('bill-1', DateTime(2026, 9, 10));

      final request = sent.single;
      expect(request.method, 'GET');
      expect(request.url.path, '/api/v1/$tenant/bills/bill-1/schedule-preview');
      expect(request.url.queryParameters['date'], '2026-09-10');
      expect(preview.requestedDate, DateTime(2026, 9, 10));
      expect(preview.effectiveDate, DateTime(2026, 9, 11));
      expect(preview.slid, isTrue);
      expect(preview.immediate, isFalse);
      expect(preview.afterDueDate, isTrue);
    });

    test('an immediate preview carries the flag', () async {
      final service = serviceReturning(jsonEncode({
        'requestedDate': '2026-09-01',
        'effectiveDate': '2026-09-01',
        'slid': false,
        'immediate': true,
      }));

      final preview =
          await service.previewSchedule('bill-1', DateTime(2026, 9, 1));

      expect(preview.immediate, isTrue);
      expect(preview.slid, isFalse);
    });

    test('surfaces the rule refusal the server sent', () async {
      final service = serviceReturning(
        jsonEncode({'id': 'BLP.BIL01', 'message': 'nao achei'}),
        status: 404,
      );

      await expectLater(
        service.previewSchedule('bill-1', DateTime(2026, 9, 10)),
        throwsA(isA<HttpException>()),
      );
    });
  });

  group('getScheduleOptions', () {
    test('asks the schedule-options route and parses each suggestion',
        () async {
      final service = serviceReturning(jsonEncode([
        {
          'option': 'Today',
          'date': null,
          'preview': null,
          'available': false,
          'unavailableReason': 'outside_window',
        },
        {
          'option': 'OnDueDate',
          'date': '2026-09-30',
          'preview': {
            'requestedDate': '2026-09-30',
            'effectiveDate': '2026-09-30',
            'slid': false,
            'immediate': false,
            'afterDueDate': false,
          },
          'available': true,
          'unavailableReason': null,
        },
      ]));

      final options = await service.getScheduleOptions('bill-1');

      final request = sent.single;
      expect(request.method, 'GET');
      expect(request.url.path, '/api/v1/$tenant/bills/bill-1/schedule-options');

      expect(options, hasLength(2));
      expect(options.first.kind, ScheduleOptionKind.today);
      expect(options.first.available, isFalse);
      expect(options.first.date, isNull);
      expect(options.first.preview, isNull);
      expect(
        options.first.unavailableReason,
        ScheduleUnavailableReasons.outsideWindow,
      );

      expect(options.last.kind, ScheduleOptionKind.onDueDate);
      expect(options.last.date, DateTime(2026, 9, 30));
      expect(options.last.preview!.effectiveDate, DateTime(2026, 9, 30));
    });

    // Um servidor que ganhar uma quinta sugestão não pode derrubar um cliente
    // que ainda não a conhece: a opção desconhecida vira `unknown` e a folha
    // simplesmente não desenha um tile para ela.
    test('an unknown option degrades instead of throwing', () async {
      final service = serviceReturning(jsonEncode([
        {'option': 'SomethingNew', 'available': true, 'date': '2026-09-30'},
      ]));

      final options = await service.getScheduleOptions('bill-1');

      expect(options.single.kind, ScheduleOptionKind.unknown);
    });

    test('surfaces the rule refusal the server sent', () async {
      final service = serviceReturning(
        jsonEncode({'id': 'BLP.BIL01', 'message': 'nao achei'}),
        status: 404,
      );

      await expectLater(
        service.getScheduleOptions('bill-1'),
        throwsA(isA<HttpException>()),
      );
    });
  });
}
