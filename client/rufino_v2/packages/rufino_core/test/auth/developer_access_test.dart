import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';

/// Monta um JWT de mentira com o payload pedido. A assinatura é lixo de
/// propósito: o [DeveloperAccess] não a valida, e não deve — quem valida token
/// é o servidor, a cada requisição.
String _token(Map<String, dynamic> claims) {
  String segment(Map<String, dynamic> map) =>
      base64Url.encode(utf8.encode(json.encode(map))).replaceAll('=', '');

  return '${segment({'alg': 'RS256'})}.${segment(claims)}.assinatura-nao-conferida';
}

void main() {
  group('DeveloperAccess', () {
    // O papel de realm no token libera as ferramentas — sem nenhuma chamada de rede.
    test('reconhece o papel developer em realm_access.roles', () async {
      final access = DeveloperAccess(
        getAccessToken: () async => _token({
          'realm_access': {
            'roles': ['offline_access', 'developer'],
          },
        }),
      );

      await access.load();

      expect(access.isDeveloper, isTrue);
    });

    // Sem o papel, a ferramenta não aparece — é o caso de toda pessoa comum.
    test('nega quando o papel não está no token', () async {
      final access = DeveloperAccess(
        getAccessToken: () async => _token({
          'realm_access': {
            'roles': ['offline_access', 'uma_authorization'],
          },
        }),
      );

      await access.load();

      expect(access.isDeveloper, isFalse);
    });

    // TESTE-ÂNCORA do desacoplamento: papel de CLIENT com o mesmo nome não vale.
    // Era assim que a ferramenta funcionava antes (recurso `debug` dentro do
    // people-management-api), e é exatamente o vínculo que esta mudança desfez.
    test('papel de client não libera — só realm_access conta', () async {
      final access = DeveloperAccess(
        getAccessToken: () async => _token({
          'resource_access': {
            'people-management-api': {
              'roles': ['developer'],
            },
          },
        }),
      );

      await access.load();

      expect(access.isDeveloper, isFalse);
    });

    // Token sem realm_access nenhum não estoura: nasce fechado.
    test('token sem realm_access nega em vez de estourar', () async {
      final access = DeveloperAccess(
        getAccessToken: () async => _token({'sub': 'alguem'}),
      );

      await access.load();

      expect(access.isDeveloper, isFalse);
    });

    // Token ilegível (não é JWT) nega em vez de estourar — errar para o lado
    // fechado é o lado certo quando o que está em jogo é mostrar o AppConfig.
    test('token malformado nega em vez de estourar', () async {
      final access = DeveloperAccess(getAccessToken: () async => 'nao-e-um-jwt');

      await access.load();

      expect(access.isDeveloper, isFalse);
    });

    // Sessão morta é problema do fluxo de autenticação, não desta classe: aqui
    // a exceção vira "sem ferramenta", sem derrubar a home.
    test('falha ao obter o token nega em vez de propagar', () async {
      final access = DeveloperAccess(
        getAccessToken: () async => throw StateError('sessão expirada'),
      );

      await access.load();

      expect(access.isDeveloper, isFalse);
    });

    // Notifica só quando o valor MUDA: recarregar a cada renovação de token não
    // pode redesenhar a home à toa.
    test('notifica apenas na mudança de valor', () async {
      var notifications = 0;
      final access = DeveloperAccess(
        getAccessToken: () async => _token({
          'realm_access': {
            'roles': ['developer'],
          },
        }),
      )..addListener(() => notifications++);

      await access.load();
      await access.load();

      expect(access.isDeveloper, isTrue);
      expect(notifications, 1);
    });

    // O logout tem que apagar o estado: sem isso a ferramenta sobrevive à troca
    // de usuário dentro da mesma execução do app.
    test('clear fecha a ferramenta e notifica', () async {
      var notifications = 0;
      final access = DeveloperAccess(
        getAccessToken: () async => _token({
          'realm_access': {
            'roles': ['developer'],
          },
        }),
      );

      await access.load();
      access.addListener(() => notifications++);
      access.clear();

      expect(access.isDeveloper, isFalse);
      expect(notifications, 1);
    });
  });
}
