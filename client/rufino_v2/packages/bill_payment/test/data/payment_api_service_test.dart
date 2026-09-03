import 'dart:convert';

import 'package:bill_payment/bill_payment.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:rufino_core/rufino_core.dart';

/// The payment endpoints at the request level (phase 3): the routes hit, the
/// idempotency header on mutations, and the two answers of `by-bill` — an
/// order, or the normal 404 while the outbox has not created one yet.
void main() {
  const tenant = '229b0dc9-5b8c-4aad-8774-dfc3f14e1a6b';

  const orderJson = '{'
      '"id":"order-1","billId":"bill-1","rail":"Boleto","status":"Pending",'
      '"hold":"None","requestedScheduleDate":"2026-09-10",'
      '"effectiveScheduleDate":"2026-09-11","amount":615.07,"fee":1.99,'
      '"paidAt":null,"failReasons":[],"lastError":null,'
      '"submissionAttempts":1,"requiresConfirmation":false,'
      '"hasReceipt":true,"createdAt":"2026-09-02T12:00:00Z"}';

  late List<http.BaseRequest> sent;

  PaymentApiService serviceReturning(String body, {int status = 200}) {
    sent = [];
    return PaymentApiService(
      client: MockClient((request) async {
        sent.add(request);
        return http.Response(body, status);
      }),
      baseUrl: 'http://localhost:8100',
      getAuthHeader: () async => 'Bearer token',
      getTenantId: () => tenant,
    );
  }

  group('getByBill', () {
    test('parses the full order projection', () async {
      final service = serviceReturning(orderJson);

      final order = await service.getByBill('bill-1');

      expect(sent.single.url.path, '/api/v1/$tenant/payments/by-bill/bill-1');
      expect(order!.id, 'order-1');
      expect(order.status, PaymentOrderStatuses.pending);
      expect(order.hold, PaymentOrderHolds.none);
      expect(order.effectiveScheduleDate, DateTime.parse('2026-09-11'));
      expect(order.amount, 615.07);
      expect(order.fee, 1.99);
      expect(order.hasReceipt, isTrue);
      expect(order.canCancel, isTrue);
    });

    // 404 é resposta normal: a aprovação cria a ordem pelo outbox, e há uma
    // janela observável antes de ela existir.
    test('returns null on 404 instead of failing', () async {
      final service = serviceReturning('', status: 404);

      expect(await service.getByBill('bill-1'), isNull);
    });
  });

  group('mutations', () {
    // Toda mutação leva o x-requestid — é a idempotência de escrita do BC.
    test('cancel and confirm hit their routes with an idempotency key',
        () async {
      final service = serviceReturning('{}');

      await service.cancel('order-1');
      await service.confirmImmediate('order-1');

      expect(sent[0].url.path, '/api/v1/$tenant/payments/order-1/cancel');
      expect(
        sent[1].url.path,
        '/api/v1/$tenant/payments/order-1/confirm-immediate',
      );
      for (final request in sent) {
        expect(request.headers['x-requestid'], isNotEmpty);
      }
    });
  });

  group('getReceipt', () {
    test('downloads the stored receipt with its media type and name',
        () async {
      sent = [];
      final service = PaymentApiService(
        client: MockClient((request) async {
          sent.add(request);
          return http.Response(
            '%PDF-1.4 comprovante',
            200,
            headers: {
              'content-type': 'application/pdf; charset=utf-8',
              'content-disposition': 'attachment; filename="comprovante.pdf"',
            },
          );
        }),
        baseUrl: 'http://localhost:8100',
        getAuthHeader: () async => 'Bearer token',
        getTenantId: () => tenant,
      );

      final receipt = await service.getReceipt('order-1');

      expect(sent.single.url.path, '/api/v1/$tenant/payments/order-1/receipt');
      expect(receipt.contentType, 'application/pdf');
      expect(receipt.fileName, 'comprovante.pdf');
      expect(receipt.bytes, isNotEmpty);
    });

    // Sem comprovante o servidor recusa com regra — o serviço lança a
    // HttpException com {id, message} e a camada acima classifica.
    test('a rule refusal surfaces as an HttpException with the domain id',
        () async {
      final service = serviceReturning(
        '{"id":"BLP.PMO16","message":"Sem comprovante ainda."}',
        status: 404,
      );

      await expectLater(
        service.getReceipt('order-1'),
        throwsA(
          isA<HttpException>()
              .having((e) => e.statusCode, 'statusCode', 404)
              .having((e) => e.domainErrorId, 'domainErrorId', 'BLP.PMO16'),
        ),
      );
    });
  });

  group('approveBill (phase 3 field)', () {
    // O aceite de execução imediata do vencido viaja no corpo (ADR-017 do BC).
    test('carries the immediate-execution acknowledgement in the body',
        () async {
      sent = [];
      final bodies = <String>[];
      final service = BillApiService(
        client: MockClient((request) async {
          sent.add(request);
          bodies.add(request.body);
          return http.Response('{}', 200);
        }),
        baseUrl: 'http://localhost:8100',
        getAuthHeader: () async => 'Bearer token',
        getTenantId: () => tenant,
      );

      await service.approveBill(
        'bill-1',
        scheduleFor: DateTime(2026, 9, 10),
        acknowledgeImmediateExecution: true,
      );

      final body = jsonDecode(bodies.single) as Map<String, dynamic>;
      expect(body['acknowledgeImmediateExecution'], isTrue);
      expect(body['acknowledgeRisk'], isFalse);
    });
  });
}
