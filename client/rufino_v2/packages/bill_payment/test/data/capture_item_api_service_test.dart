import 'package:bill_payment/bill_payment.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

/// O endereço que o serviço monta — o nível que os testes de repositório não veem.
///
/// Os testes existentes trocam o `CaptureItemApiService` por um mock, então nada exercitava a
/// construção da URL. Foi por aí que passou o defeito de 2026-08-27: o `$` da interpolação saiu
/// **escapado** no fonte (`'/capture-items/\$id/artifact'`), o app chamava a rota com o texto
/// literal `$id`, e o servidor respondia 404 sem nunca registrar a tentativa — a análise estática
/// não vê problema nenhum numa string válida.
void main() {
  const tenant = '229b0dc9-5b8c-4aad-8774-dfc3f14e1a6b';
  const itemId = '01a04130-e42f-7122-b902-635375be0adb';

  late List<Uri> requested;

  CaptureItemApiService serviceReturning(String body) {
    requested = [];

    return CaptureItemApiService(
      client: MockClient((request) async {
        requested.add(request.url);
        return http.Response(body, 200);
      }),
      baseUrl: 'http://localhost:8100',
      getAuthHeader: () async => 'Bearer token',
      getTenantId: () => tenant,
    );
  }

  // TESTE DE REGRESSÃO: o id do item entra na rota INTERPOLADO, não como texto literal.
  test('dismissItem hits the item route with the interpolated id', () async {
    final service = serviceReturning('{}');

    await service.dismissItem(itemId, note: 'nao reconheco');

    expect(
      requested.single.path,
      '/api/v1/$tenant/capture-items/$itemId/dismiss',
    );
  });

  // O mesmo para a anexação, que é onde o defeito apareceu para quem usava a tela.
  test('attachArtifact hits the item route with the interpolated id', () async {
    final service = serviceReturning('{}');

    await service.attachArtifact(
      itemId,
      [1, 2, 3, 4],
      fileName: 'boleto.pdf',
      contentType: 'application/pdf',
    );

    expect(
      requested.single.path,
      '/api/v1/$tenant/capture-items/$itemId/artifact',
    );
  });

  // A contraprova que dá sentido às duas de cima: nenhuma rota deste serviço pode conter um
  // `$` literal. Sem ela, os testes acima só cobrem os dois métodos que já sabemos que quebraram.
  test('no route carries a literal dollar sign', () async {
    final service = serviceReturning('{"items":[],"nextCursor":null}');

    await service.listItems();
    await service.reprocessItem(itemId);
    await service.dismissItem(itemId);

    expect(requested.map((uri) => uri.path), everyElement(isNot(contains(r'$'))));
  });
}
