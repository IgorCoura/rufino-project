import 'dart:convert';

import 'package:bill_payment/bill_payment.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:rufino_core/rufino_core.dart';

/// A requisição do download em lote: rota, corpo e o arquivo que volta.
void main() {
  const tenant = '229b0dc9-5b8c-4aad-8774-dfc3f14e1a6b';

  late http.Request sent;

  BillApiService serviceAnswering(http.Response response) => BillApiService(
        client: MockClient((request) async {
          sent = request;
          return response;
        }),
        baseUrl: 'http://localhost:8100',
        getAuthHeader: () async => 'Bearer token',
        getTenantId: () => tenant,
      );

  // Os ids viajam na ordem da seleção, com as três escolhas, e sem
  // x-requestid — é leitura, só usa POST porque a lista não cabe na URL.
  test('posts the selection in order with the three options', () async {
    final service = serviceAnswering(http.Response.bytes(
      utf8.encode('%PDF-1.4'),
      200,
      headers: {
        'content-type': 'application/pdf',
        'content-disposition':
            'attachment; filename=boletos-2026-09-14.pdf; filename*=UTF-8\'\'boletos-2026-09-14.pdf',
      },
    ));

    final file = await service.exportDocuments(
      billIds: ['b2', 'b1'],
      pages: BillDocumentPages.firstPage,
      includeReceipts: true,
      packaging: BillDocumentPackagings.singlePdf,
    );

    expect(sent.method, 'POST');
    expect(sent.url.path, '/api/v1/$tenant/bills/documents/export');
    expect(sent.headers.containsKey('x-requestid'), isFalse);
    expect(jsonDecode(sent.body), {
      'billIds': ['b2', 'b1'],
      'pages': 'FirstPage',
      'includeReceipts': true,
      'packaging': 'SinglePdf',
    });
    expect(file.contentType, 'application/pdf');
    expect(file.fileName, 'boletos-2026-09-14.pdf');
  });

  // O .zip de um PDF por boleto chega com o tipo e o nome que o servidor deu.
  test('reads the zip the per-bill packaging returns', () async {
    final service = serviceAnswering(http.Response.bytes(
      const [0x50, 0x4B, 0x03, 0x04],
      200,
      headers: {
        'content-type': 'application/zip',
        'content-disposition': 'attachment; filename=boletos-2026-09-14.zip',
      },
    ));

    final file = await service.exportDocuments(
      billIds: ['b1', 'b2'],
      pages: BillDocumentPages.all,
      includeReceipts: false,
      packaging: BillDocumentPackagings.pdfPerBill,
    );

    expect(file.contentType, 'application/zip');
    expect(file.fileName, 'boletos-2026-09-14.zip');
  });

  // A recusa do servidor (teto de boletos) sobe como HttpException com o id
  // do domínio, para a tela mostrar a mensagem da regra.
  test('surfaces the rule refusal the server sent', () async {
    final service = serviceAnswering(http.Response(
      jsonEncode({
        'id': 'BLP.BIL43',
        'message': 'É possível baixar os documentos de no máximo 50 boletos por vez.',
      }),
      400,
    ));

    await expectLater(
      service.exportDocuments(
        billIds: ['b1'],
        pages: BillDocumentPages.all,
        includeReceipts: false,
        packaging: BillDocumentPackagings.singlePdf,
      ),
      throwsA(isA<HttpException>()),
    );
  });
}
