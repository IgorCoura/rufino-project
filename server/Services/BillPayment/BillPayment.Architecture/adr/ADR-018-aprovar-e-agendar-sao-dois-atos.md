# ADR-018 — Aprovar e agendar são dois atos, e decisão terminal humana se desfaz

**Status:** Aceito · **Data:** 2026-09-08 (decisão do usuário)

## Contexto

Três defeitos observados em uso, todos com a mesma raiz: **o BC tratava "autorizar o pagamento" e
"mandar pagar" como um ato só**.

1. **Cancelar um agendamento matava o boleto.** `Bill.MarkScheduleCancelled` levava a `Cancelled`,
   que é terminal. Quem cancelava o agendamento apenas para trocar a data perdia o boleto e a
   aprovação junto, e precisava reimportar o documento — que só era possível porque `Cancelled`
   libera a chave natural. O usuário relatou exatamente isso: *"o correto seria o retorno para a
   aba aprovados a re realizar o agendamento"*.
2. **Aprovar era irreversivelmente aprovar-e-pagar.** `Bill.Approve` exigia a data, e o evento de
   aprovação criava a `PaymentOrder`. Não existia o estado "autorizado, ainda sem data", que é
   como o trabalho de fato acontece: quem aprova o mérito da despesa nem sempre é quem decide o
   dia do desembolso.
3. **Recusar ou cancelar por engano era definitivo.** Os dois estados são terminais, e a única
   saída era reimportar o documento — perdendo a trilha, e impossível quando o documento veio de
   um e-mail já processado.

O quarto problema é de auditoria e apareceu ao desenhar os outros três: `Bill.Approval` guarda
**apenas a última decisão**. Quem consultasse um boleto meses depois via "cancelado por Fulano" e
nada mais — nem que ele fora aprovado antes, nem por quem, nem quantas vezes o agendamento foi
refeito.

## Decisão

### 1. Aprovar e agendar viram métodos separados

- `Bill.Approve` perde a data e as guardas de calendário. Mantém as de mérito: cobertura de
  checks, alçada de risco, aceite de risco, frescor do retrato, teto de valor. Emite
  `BillApprovedDomainEvent`, que **deixa de criar ordem de pagamento**.
- `Bill.Schedule` nasce exigindo `Approved` sem data. Aplica as guardas de calendário
  (`BLP.BIL05`, `BLP.BIL31`, `BLP.BIL35`) e emite `BillSchedulingRequestedDomainEvent` — **este
  sim** é o que a fase 3 consome para criar a `PaymentOrder`.
- **O frescor do retrato é reconferido no agendamento**, e não é redundância: separar os atos
  abriu uma janela entre eles, e é no agendamento que o dinheiro anda. Aprovar ontem contra um
  retrato de ontem é legítimo; agendar hoje contra aquele mesmo retrato não é.
- `Approved` passa a comportar dois significados — *sem data* (esperando agendamento) e *com data*
  (ordem a caminho). **Não** criamos status novo: custaria aresta na máquina, migração e mudança
  em todo o espelho do ADR-002 para expressar o que `ScheduledFor` já diz.

### 2. Cancelar o agendamento devolve a `Approved`

`Bill.UnschedulePayment` substitui `MarkScheduleCancelled` e `ReturnToApprovalAfterScheduleCancellation`.
Aceita os dois instantes em que a ordem pode morrer — em `Draft`, antes de o boleto virar
`Scheduled`, e depois de agendado — porque o significado de negócio é um só: **a execução foi
desfeita, a autorização humana continua de pé**. A aresta `Scheduled → Approved` entra na matriz.

Nota: o comportamento anterior do caso `Draft` (voltar a `AwaitingApproval`) foi **unificado** no
novo. Descartar uma aprovação que ninguém desfez era um efeito colateral de o cancelamento não ter
para onde voltar.

### 3. Recusa e cancelamento se desfazem, por um caminho próprio

`Bill.UndoDecision` devolve `Denied`/`Cancelled` a `AwaitingApproval` e emite
`BillDecisionUndoneDomainEvent`, que dispara a **revalidação automática** — o retrato que
sustentava a decisão anterior é velho por definição.

`IsTerminal` **continua verdadeiro** para os dois. A reversão não passa pela matriz de transições,
e isso é deliberado: torná-los não-terminais abriria a porta para um reflexo de pagamento atrasado
ressuscitar um boleto morto. A exceção é nomeada (`BillStatus.CanBeUndone`), tem alçada própria, e
**não vale para `Paid`** — dinheiro que saiu não volta por decisão nossa.

Duas pré-condições ficam no caso de uso, porque exigem consulta:

- **a chave natural pode ter sido reocupada** (negar/cancelar a liberam, e o documento pode ter
  entrado de novo legitimamente) → recusa com `BLP.BIL02`;
- **pode haver ordem viva no provedor** — o caso do boleto cancelado cuja ordem o provedor recusou
  cancelar → recusa com `BLP.BIL39`.

### 4. O boleto guarda a trilha inteira

`BillHistoryEntry` (Value Object em coleção owned, como `BillCheck`) registra **ação, instante,
autor e transição**. É acrescentada **pelos próprios métodos ricos do agregado**, nunca por
handler — é isso que impede a trilha de divergir da máquina de estados: não existe caminho que
mude o status sem registrar.

**A trilha também grava DE ONDE partiu a ação** (`BillActionOrigin`: `User`, `Provider`,
`System`). Sem isso ela respondia mal à pergunta que mais importa numa auditoria de dinheiro:
cancelar no painel do provedor e cancelar pelo nosso app passavam pelo mesmo caminho de código e
gravavam a **mesma linha, com autor "Sistema"**. Quem lesse o histórico não teria como saber quem
praticou o ato. `Provider` deliberadamente **não** separa "painel" de "decisão do provedor": a API
devolve o mesmo `CANCELLED` nos dois casos, e inventar a distinção seria afirmar na trilha algo
que não foi medido.

Corolário aplicado no Value Object: **autor só é aceito quando a origem o admite.** Um `UserId`
chegando junto de uma mudança do provedor é descartado — atribuir a alguém um ato que essa pessoa
não praticou é pior que não ter autor nenhum.

`ActorName` é desnormalizado de propósito. Trilha de auditoria grava o nome **como era na hora**, e
este BC não tem cadastro de pessoas: guardar só o `UserId` faria a tela mostrar um GUID ou criaria
dependência de outro contexto para renderizar histórico.

`Approval` continua existindo e responde outra pergunta: é a decisão **vigente** que as guardas
consultam. Uma decide, a outra explica.

### 5. Três alçadas, não uma

| Escopo | O que autoriza | Papel |
|---|---|---|
| `bill:approve` | autorizar o mérito da despesa | `bill-approver` |
| `bill:schedule` **+ `bill:cancel`** | **mandar pagar** e parar um pagamento ou um boleto | `bill-scheduler` |
| `bill:undo-decision` | desfazer recusa/cancelamento | `bill-approver-undo` |

`cancel` **sai** da permissão de decisão: tirar o boleto do fluxo e parar um pagamento são o mesmo
tipo de poder, e nenhum dos dois decorre de poder aprovar. "Aprovar e agendar" numa chamada só
exige **as duas** alçadas — conferido na borda, porque o `[ProtectedResource]` do endpoint é fixo.

⚠️ **Consequência de migração:** quem hoje é `bill-approver` perde `cancel` até receber
`bill-scheduler`. O `migrate-realm.sh` concede o papel novo a todos os portadores do antigo, para
ninguém acordar sem permissão; separar de verdade quem agenda de quem aprova é decisão de quem
administra o realm, depois, com a alçada já existindo.

## Consequências

- **Uma migração** (`BillHistoryAndTenantWebhooks`): tabela `bill_history_entries` e a coluna
  `requested_by` em `payment_orders` (quem agendou pode não ser quem aprovou).
- `CreatePaymentOrderOnBillApprovedHandler` passa a reagir ao evento de agendamento. Um boleto
  aprovado e nunca agendado **não tem ordem nenhuma** — é o que o mantém disponível na aba de
  aprovados.
- A suíte existente continua valendo como rede de regressão: os testes antigos chamam um helper
  `ApproveAndSchedule` que preserva a semântica anterior, e os testes do que é novo chamam os
  métodos separados.
- **Contradiz parcialmente o ADR-002** na parte em que ele fazia a aprovação ser o gatilho da
  ordem. O espelho continua intacto: do `Scheduled` em diante quem manda é a `PaymentOrder`.

## Alternativas descartadas

- **Status novo `SchedulingRequested`.** Expressaria a diferença entre aprovado-sem-data e
  aprovado-com-ordem, ao custo de aresta nova, migração e mudança em todo o espelho. `ScheduledFor`
  já distingue os dois, e a tela mostra isso com um selo.
- **Tornar `Denied`/`Cancelled` não-terminais.** Simplificaria a reversão e abriria a porta para
  reflexo atrasado de pagamento mover um boleto que uma pessoa encerrou.
- **Reimportar o documento em vez de reverter.** É o que se fazia, e perde a trilha — além de ser
  impossível quando o documento veio de um e-mail já processado.
