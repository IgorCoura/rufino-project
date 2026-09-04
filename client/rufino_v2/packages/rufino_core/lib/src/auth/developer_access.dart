import 'dart:convert';

import 'package:flutter/foundation.dart';

/// Se a pessoa logada carrega o papel de realm que libera as ferramentas de
/// diagnóstico do aplicativo.
///
/// **Lê o papel direto do token, sem nenhuma chamada de rede.** É a diferença
/// para o [PermissionNotifier], que pergunta ao Keycloak quais recursos e
/// escopos a pessoa alcança numa API: aqui não há API envolvida. A tela de
/// diagnóstico é do aplicativo — verifica o Sentry, mostra as permissões já
/// carregadas e exibe o `AppConfig` —, e nenhuma dessas três coisas chama o
/// servidor.
///
/// **É REALM ROLE, não client role, e isso é o ponto.** Até 2026-09-04 a
/// ferramenta era liberada por um recurso `debug` dentro do
/// `people-management-api`, com uma policy que cravava um nome de usuário. Duas
/// consequências: o diagnóstico do app quebrava sempre que aquele BC era
/// mexido, e a policy morria em silêncio no dia em que o usuário fosse
/// renomeado. `realm_access.roles` viaja em todo token, independente de
/// audiência, e descreve a PESSOA — que é o que a pergunta realmente é.
class DeveloperAccess extends ChangeNotifier {
  /// Cria o leitor sobre [getAccessToken], a mesma closure que o resto do app
  /// usa para obter o token corrente (ela já cuida dos dois fluxos de login e
  /// da renovação).
  DeveloperAccess({required Future<String> Function() getAccessToken})
      : _getAccessToken = getAccessToken;

  /// Nome do papel de realm. Não é `debug`: o papel descreve quem a pessoa é,
  /// e a tela de diagnóstico é uma das coisas que isso libera — não a única
  /// para sempre.
  static const String developerRole = 'developer';

  final Future<String> Function() _getAccessToken;

  bool _isDeveloper = false;

  /// Falso até o token ser lido. Errar para o lado fechado é o lado certo: o
  /// custo de esconder a ferramenta por um instante é nada, e o de mostrá-la
  /// para quem não deveria é expor configuração do app.
  bool get isDeveloper => _isDeveloper;

  /// Relê o papel do token corrente. Barato o bastante para ser chamado no
  /// arranque e depois de trocar de sessão.
  Future<void> load() async {
    late final bool result;
    try {
      result = _hasDeveloperRole(await _getAccessToken());
    } on Object {
      // Token indisponível (sessão expirada, storage vazio) não é erro desta
      // classe: quem trata sessão morta é o fluxo de autenticação. Aqui só
      // significa "sem ferramenta".
      result = false;
    }

    if (result == _isDeveloper) return;

    _isDeveloper = result;
    notifyListeners();
  }

  /// Limpa o estado — chamar no logout, senão a ferramenta sobrevive à troca
  /// de usuário dentro da mesma execução do app.
  void clear() {
    if (!_isDeveloper) return;
    _isDeveloper = false;
    notifyListeners();
  }

  static bool _hasDeveloperRole(String accessToken) {
    final claims = _decodePayload(accessToken);
    if (claims == null) return false;

    final realmAccess = claims['realm_access'];
    if (realmAccess is! Map<String, dynamic>) return false;

    final roles = realmAccess['roles'];
    if (roles is! List) return false;

    return roles.any((role) => role == developerRole);
  }

  /// Decodifica o payload do JWT. **Não valida assinatura, e não precisa**: o
  /// token veio do fluxo de login e é o mesmo que o app manda para as APIs —
  /// quem o valida é o servidor, a cada requisição. O que se lê aqui decide o
  /// que MOSTRAR, nunca o que o servidor aceita.
  static Map<String, dynamic>? _decodePayload(String token) {
    final parts = token.split('.');
    if (parts.length != 3) return null;

    try {
      final normalized = base64Url.normalize(parts[1]);
      final decoded = utf8.decode(base64Url.decode(normalized));
      final claims = json.decode(decoded);
      return claims is Map<String, dynamic> ? claims : null;
    } on Object {
      return null;
    }
  }
}
