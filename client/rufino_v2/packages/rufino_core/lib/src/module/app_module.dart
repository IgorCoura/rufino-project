import 'package:flutter/widgets.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/single_child_widget.dart';

import 'home_entry.dart';

/// Um módulo plugado na casca do app.
///
/// A casca compõe uma lista destes e não conhece mais nada de nenhum produto:
/// as rotas, as dependências e as entradas do menu vêm todas daqui. Ligar ou
/// desligar um produto é acrescentar ou tirar uma linha dessa lista.
///
/// **As duas porteiras ficam dentro do módulo, e é o ponto.** Só ele sabe qual
/// produto do tenant o habilita e qual notifier de permissão é o seu — os três
/// usam tipos diferentes, porque o `provider` resolve por tipo. Enquanto essa
/// decisão morava no `home_screen.dart`, ela era um `if` por produto, escrito à
/// mão, que ninguém lembrava de acrescentar ao criar uma tela.
abstract class AppModule {
  /// Cria o módulo.
  const AppModule();

  /// Título do grupo deste módulo no menu do Home.
  String get menuTitle;

  /// As rotas do módulo, prontas para entrar no roteador da casca.
  List<RouteBase> routes();

  /// As dependências que o módulo publica na árvore.
  ///
  /// O que vem de fora — cliente HTTP, base url, cabeçalho de autorização,
  /// repórter de erro e as capacidades de plataforma — chega pelo construtor
  /// do módulo, porque só a casca sabe montá-las.
  List<SingleChildWidget> providers();

  /// As entradas do menu que **esta** pessoa pode ver **neste** tenant.
  ///
  /// Devolve vazio quando o produto não está habilitado para o cliente ou
  /// quando a pessoa não tem permissão para nenhuma tela — e nesse caso o
  /// grupo inteiro some do menu.
  List<HomeEntry> visibleEntries(BuildContext context);
}
