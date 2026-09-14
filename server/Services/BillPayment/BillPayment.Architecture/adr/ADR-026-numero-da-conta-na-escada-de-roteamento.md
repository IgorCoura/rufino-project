# ADR-026 — O número da conta cadastrado roteia a conta de concessionária

**Data:** 2026-09-14
**Status:** Aceito
**Reabre, com outro desenho:** o degrau 2 abandonado do [doc 07](../07-multitenancy-and-routing.md) (`RoutingRule`)
**Estende:** [ADR-014](ADR-014-expectativa-e-lembretes.md) (a expectativa passa a servir também ao roteamento), [ADR-011](ADR-011-llm-propoe-codigo-dispoe.md)

## Contexto

Relatado sobre a fatura da Vivo, e generalizado pelo usuário: *"boletos de concessionária vêm sem
o CPF/CNPJ do pagador, ou seja, todos vão para a quarentena"*. A escada só atribuía um boleto ao
tenant com o documento dele impresso (93,3% medido — num acervo de **cobrança**), com a senha
derivada ou com um beneficiário exclusivo. Arrecadação raramente traz nenhum dos três.

O que toda conta de concessionária traz é **o número da conta do cliente**: Nº da conta na Vivo,
instalação na EDP, matrícula no DAE, RGI na SABESP. Na arrecadação ele costuma estar dentro do
próprio código de barras — na Vivo, `1123004411` está no campo livre.

A sprint 2.6 mediu e **abandonou** uma `RoutingRule` por referência de conta. O motivo continua
válido, e define o que este ADR não faz: lá a referência era **deduzida de posição fixa** do campo
livre, e em boleto de cobrança a parte estável é a conta do **beneficiário**, que não distingue
pagadores.

## Decisão

### D1 — Degrau 3: o número informado pelo tenant, procurado por conter

- **Onde mora:** `BillExpectation.AccountReference`, que já existia e já era informado pelo usuário
  para separar contas do mesmo beneficiário. Nenhuma tabela nova (decisão do usuário: alternativa A).
- **Como compara** (`AccountReferenceMatchingService`): dígitos significativos (sem letras, sem
  zeros à esquerda) procurados no **campo livre do código de barras** (posição 20 em diante) ou como
  **sequência inteira** no texto do documento e no corpo do e-mail. Nunca por posição.
- **Confiança:** no código de barras com 8+ dígitos → `Strong`; no texto, ou com 6–7 dígitos →
  `Weak`. Menos de 6 dígitos não roteia (a matrícula `L18502` fica de fora — limite aceito).
- **Duas expectativas do tenant casando no mesmo documento → nenhuma.** A dúvida vai para a fila.
- **Fica depois dos negativos.** O número foi informado pelo tenant e nunca desfaz uma prova de que
  o boleto é de outra pessoa (pagador oficial ou pagador sob rótulo).

### D2 — Nenhuma consulta entre tenants

Cada tenant roteia a própria cópia do e-mail, pela própria fonte; o número de conta de outro
tenant é irrelevante para essa pergunta. A colisão continua coberta pela unicidade global do
instrumento (exceção 2 do doc 07). **As travessias autorizadas continuam sendo duas.**

### D3 — "Lembrar desta conta" na reivindicação

Sem cadastro prévio o degrau 3 não resolve nada. A reivindicação passa a oferecer, marcada por
padrão, *"Lembrar desta conta: <número>"*:

- **O candidato** sai da leitura por IA (`accountReference`) e só é gravado no item se os seus
  dígitos forem **encontrados de forma determinística** no documento (ADR-011).
- **Número editado ou digitado** passa pela mesma conferência; não encontrado, a reivindicação é
  recusada com erro claro.
- **O pedido fica anotado no item** e é cumprido depois da validação do boleto, quando o
  beneficiário já está resolvido: cria a expectativa, preenche a que não tinha número, ou cria mais
  uma se o beneficiário já tem outras contas. Sem beneficiário cadastrado, fica pendente.
- **Não é gravado:** CPF, nome, endereço, código de barras, posição do número, cópia do documento.

## Consequências

**Boa:** reivindicar cada conta uma única vez basta — o mês seguinte chega roteado. A fila de
reivindicação passa a se esvaziar sozinha.

**Ruim, e aceita:** número de conta digitado errado roteia para o tenant a conta de quem tiver
aquele número **na mesma caixa**. O dano é limitado pelo que já existia: a caixa é do próprio
tenant, e o boleto ainda passa pela validação e pela aprovação humana (ADR-007).

**Não muda:** o degrau 2 (documento impresso) e o seu negativo por rótulo, o isolamento da
regra de 2026-08-28, e a aprovação.

## Alternativas descartadas

- **Deduzir a posição da conta por emissor.** Medido e recusado nas sprints 2.6 e 2.7: a posição
  muda por emissor e, na cobrança, não distingue pagadores.
- **Caixa declarada exclusiva do tenant.** Retirada do plano pelo usuário.
- **Consultar se outro tenant cadastrou o mesmo número.** Seria uma terceira travessia de tenant
  sem necessidade (D2).
