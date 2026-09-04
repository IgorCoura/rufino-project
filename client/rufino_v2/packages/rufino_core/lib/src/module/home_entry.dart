import 'package:flutter/widgets.dart';

/// Uma entrada do menu do Home: para onde ela leva e o que precisa ser
/// verdade para ela aparecer.
///
/// Quem declara é o módulo dono da rota, não a casca — foi para isso que ela
/// existe. Enquanto as entradas viviam escritas à mão no `home_screen.dart`, o
/// menu era o único lugar do app que conhecia os três produtos por dentro, e
/// acrescentar uma tela significava editar um arquivo da casca.
@immutable
class HomeEntry {
  /// Cria a entrada.
  const HomeEntry({
    required this.icon,
    required this.label,
    required this.route,
    required this.resource,
    this.scope,
  });

  /// Cria uma entrada de FERRAMENTA do aplicativo — não de um produto.
  ///
  /// A diferença é de quem decide se ela aparece: a entrada de produto é
  /// filtrada por permissão de API ([resource] + [scope]); a de ferramenta é
  /// filtrada pela casca, por papel de realm lido do token. Por isso [resource]
  /// fica vazio aqui, e nenhum módulo lê esta lista.
  const HomeEntry.tool({
    required this.icon,
    required this.label,
    required this.route,
  })  : resource = '',
        scope = null;

  /// Ícone do cartão.
  final IconData icon;

  /// O que a pessoa lê no cartão.
  final String label;

  /// Rota para onde o cartão leva.
  final String route;

  /// O recurso do Keycloak que precisa conceder algo.
  final String resource;

  /// Um escopo específico, quando "qualquer escopo" não basta.
  ///
  /// Nulo significa que basta a pessoa ter alguma permissão sobre [resource] —
  /// é o caso da maioria dos cartões, que só abrem uma listagem.
  final String? scope;
}
