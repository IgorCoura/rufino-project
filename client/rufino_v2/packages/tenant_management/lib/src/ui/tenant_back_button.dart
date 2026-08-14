import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Botão de voltar que funciona tanto para tela empilhada quanto para tela
/// alcançada por substituição.
///
/// As telas deste módulo chegam pelos dois caminhos: pelo seletor elas são
/// empilhadas (`push`), pelo menu do Home elas substituem a pilha (`go`). Um
/// `pop` cru falha no segundo caso — e é assim que uma tela fica sem saída.
/// Aqui: volta se houver para onde, e senão vai para [fallback].
class TenantBackButton extends StatelessWidget {
  /// Cria o botão, com [fallback] para quando não há pilha.
  const TenantBackButton({super.key, required this.fallback});

  /// Rota usada quando não há nada para desempilhar.
  final String fallback;

  @override
  Widget build(BuildContext context) {
    return IconButton(
      icon: const Icon(Icons.arrow_back),
      tooltip: 'Voltar',
      onPressed: () {
        if (context.canPop()) {
          context.pop();
        } else {
          context.go(fallback);
        }
      },
    );
  }
}
