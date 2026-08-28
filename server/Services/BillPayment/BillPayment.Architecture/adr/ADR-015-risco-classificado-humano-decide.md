# ADR-015 — O sistema classifica o risco; quem decide é sempre o humano

**Status:** Aceito · **Data:** 2026-08-27

## Contexto

Desde a sprint 1.4, falha em check **Blocking** levava o boleto direto a `Rejected`: a validação
decidia sozinha que aquele documento não seria pago, e a aprovação recusava qualquer boleto com
bloqueio pendente. O usuário pediu a inversão explícita do modelo: *"pare de bloquear os boletos
e só coloque um aviso bem visível e colorido no início das verificações, com Seguro, Atenção e
Perigo — e deixe que o usuário defina se autorizará ou não."*

O pedido tem base: os "bloqueios" cobrem situações de gravidade muito diferente (consulta fora do
ar não é fraude), o dono do dinheiro é o usuário, e um sistema que rejeita sozinho ensina o
usuário a brigar com o sistema em vez de olhar a evidência.

## Decisão

1. **A validação classifica, nunca rejeita.** `RecordChecks` deixa de transicionar para
   `Rejected`. Todo boleto validado vai para `AwaitingApproval` com um **`RiskLevel`** derivado
   dos checks: falha que era bloqueante → **Perigo**; falha advisory, aviso ou inconclusivo →
   **Atenção**; tudo limpo → **Seguro**. A severidade dos checks continua existindo — mudou o
   consequente, não a apuração.
2. **Perigo exige aceite explícito.** `Bill.Approve` recusa boleto em Perigo sem
   `acknowledgeRisk` (`BLP.BIL27`), listando os motivos. Com o aceite, aprova — e o
   `ApprovalRecord` grava o **nível de risco visto no instante da decisão** (`RiskAtDecision`),
   que é a prova de auditoria de que o aprovador viu o alerta.
3. **Transparência total.** O detalhe da API expõe os retratos das consultas oficiais por
   inteiro (`Lookups`), além da leitura por IA (`Reading`) — o aprovador decide com a mesma
   informação que o sistema usou para classificar.
4. **`Rejected` fica no enum, sem produtor automático** — mesmo tratamento do `Learned` do
   roteamento: ids persistidos não se renumeram. `Deny`/`Cancel` continuam sendo os desfechos
   negativos, sempre humanos.

## O que NÃO mudou

- **Linha digitável com DV inválido continua não virando `Bill`** — o VO não se constrói.
  Integridade estrutural não é veto: não há o que pagar.
- **A deduplicação na captura continua** não criando segundo boleto para o mesmo instrumento.
  O check `Duplicate` vira sinal de Perigo, não rejeição.
- **A revalidação automática por retrato velho (`BLP.BIL06`) permanece** — é frescor de dado,
  não veto à vontade do usuário.
- **ADR-007 intacto**: nenhum pagamento sem um humano autorizando. Este ADR só remove o caso em
  que o sistema decidia *contra* o humano sem perguntar.

## Consequências

- A matriz de decisão do doc 03 foi reescrita: não existe mais linha "→ `Rejected`".
- Boleto com consulta oficial indisponível fica **visível e aprovável sob aceite** — antes
  ficava em `Rejected` com botão de revalidar. O aceite explícito e o banner de Perigo são a
  proteção; a evidência do check diz que não houve consulta.
- A UI ganhou o banner colorido (Seguro/Atenção/Perigo) no topo das verificações, a seção
  "Consulta oficial" e a caixa de aceite no fluxo de aprovação — o botão nem habilita sem ela.
- Testes que codificavam "bloqueante → `Rejected`" foram reescritos para o modelo de risco no
  mesmo commit.
