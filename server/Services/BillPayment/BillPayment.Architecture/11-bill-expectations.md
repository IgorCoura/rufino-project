# 11 — Expectativa de boleto e lembretes

O sistema sabe o que espera receber e avisa quando não recebeu. Racional em [`adr/ADR-014`](adr/ADR-014-expectativa-e-lembretes.md); sem isso, toda falha de captura é silenciosa e a primeira notícia é a multa.

## `BillExpectation` — Aggregate Root (`BLP.EXP`)

| Campo | Tipo | Nota |
|---|---|---|
| `Id` / `TenantId` | | |
| `PayeeId` | `PayeeId` | Beneficiário esperado |
| `AccountReference` | `string` (vazio, nunca nulo) | **Informada no cadastro, nunca deduzida** — ver a nota abaixo. Vazio quando o tenant tem uma conta só com aquele beneficiário |
| `Label` | `string` | O que o usuário lê no alerta: "EDP — Casa Florentino" |
| `Recurrence` | `Recurrence` (Smart Enum) | `Monthly` \| `Bimonthly` \| `Quarterly` \| `Annual` |
| `ExpectedDueDay` | `int` | Dia do mês do vencimento, aprendido |
| `ObservedLeadDays` | `int` | Dias entre chegada e vencimento, aprendido — é o que define quando alertar |
| `AlertLeadDays` | `int` | Quando alertar se ainda não chegou. Default `max(3, ObservedLeadDays + 2)` |
| `Origin` | `ExpectationOrigin` | `Learned` \| `Manual` |
| `ObservationCount` | `int` | Quantos ciclos alimentaram o aprendizado |
| `HintSourceId` | `CaptureSourceId?` | Por onde costuma chegar — vira o link acionável do alerta |
| `IsActive` / `PausedUntil` | `bool` / `DateOnly?` | |

> ⚠️ **Corrigido pela medição na sprint 2.7 (2026-08-12).** O desenho tirava a referência de conta do documento, como a `RoutingRule` da 2.6 — que foi abandonada porque a chave não distinguia pagadores. Aqui a pergunta é outra e a conclusão também: a expectativa precisa separar **contas do mesmo tenant**, e isso importa porque **10 dos 20 grupos de beneficiário do arquivo real têm mais de uma conta** (quatro instalações da EDP, três do DAE). A referência existe no campo livre em arrecadação — EDP nos 13 dígitos finais, DAE no meio —, mas **a posição muda por emissor**, então ela é informada por quem cadastra. O `ExpectationLearningService` **recusa aprender** quando o histórico mostra mais de uma conta, e notifica em vez de adivinhar: uma expectativa por beneficiário seria cumprida pela primeira conta que chegasse e esconderia as demais.
>
> `ReferenceKind` não foi criado: sem dedução automática, o tipo da referência é informação de tela, não invariante de domínio.

**Invariantes**

1. Único por `(TenantId, PayeeId, AccountReference)` — `BLP.EXP01`.
2. Um ciclo aberto por `CompetencePeriod` — `BLP.EXP02`.
3. `Fulfill` só em ciclo `Waiting` — `BLP.EXP03`.
4. `MarkMissing` só depois de `AlertAt` — `BLP.EXP04`.
5. `AlertLeadDays >= 1` e menor que o intervalo da recorrência — `BLP.EXP05`.

**Métodos ricos**

| Método | Contrato |
|---|---|
| `BillExpectation.Learn(tenantId, payeeId, accountRef, observations)` | Factory a partir de N ocorrências regulares. Deriva recorrência, dia de vencimento e lead. |
| `BillExpectation.Register(...)` | Cadastro manual. |
| `OpenCycle(CompetencePeriod, DateOnly expectedDueDate)` | Job periódico. Calcula `AlertAt = expectedDueDate - AlertLeadDays`. |
| `Fulfill(cycleId, BillId, DateOnly actualDueDate)` | **Aprende**: reajusta `ExpectedDueDay` e `ObservedLeadDays` por média móvel. |
| `RecordCaptureFailure(cycleId, CaptureItemId, MissReason)` | Chegou algo e não deu para ler — cumprimento parcial. |
| `MarkMissing(cycleId, MissReason)` | Passou de `AlertAt` sem cumprimento. Emite evento. |
| `Waive(cycleId, UserId, reason)` | "Este mês não vem." |
| `Pause(DateOnly until)` / `Deactivate(reason)` | |

## `ExpectationCycle` — entidade interna

Uma por período. Nunca emite evento (só o Root emite).

| Campo | Tipo |
|---|---|
| `Competence` | `CompetencePeriod` (SharedKernel — foi desenhado para isto) |
| `ExpectedDueDate` / `AlertAt` | `DateOnly` |
| `Status` | `Waiting` \| `Fulfilled` \| `PartiallyCaptured` \| `Missing` \| `Waived` \| `Closed` |
| `FulfilledByBillId` | `BillId?` |
| `BlockedByCaptureItemId` | `CaptureItemId?` |
| `MissReason` | `NeverArrived` \| `CaptureFailed` \| `Locked` \| `LinkFailed` \| `Unrouted` \| `PortalUnavailable` |
| `AlertsSent` | `IReadOnlyCollection<AlertRecord>` — nível + instante |

## Os dois alertas são diferentes

Distinção que define a utilidade do recurso, porque a ação do usuário muda:

| | **Não chegou** | **Chegou e não consegui ler** |
|---|---|---|
| `MissReason` | `NeverArrived`, `PortalUnavailable` | `Locked`, `LinkFailed`, `Unrouted`, `CaptureFailed` |
| Status do ciclo | `Missing` | `PartiallyCaptured` |
| Mensagem | "a conta X não chegou; busque em [portal] ou confira [remetente]" | "a conta X chegou mas não consegui ler: [motivo]" |
| Ação oferecida | abrir o portal, importar à mão | informar senha, reivindicar, digitar a linha |

O segundo caso é o mais valioso: o sistema **já tem** o documento e sabe exatamente o que falta, então o alerta leva direto ao item resolvível em um clique.

## Escalonamento

Um alerta por nível por ciclo — nunca repetir o mesmo nível.

| Nível | Quando | Tom |
|---|---|---|
| `HeadsUp` | em `AlertAt` | informativo |
| `Warning` | D-3 do vencimento | ação recomendada |
| `Urgent` | no vencimento | ação necessária |
| `Overdue` | após o vencimento | encargos correndo |

Ciclo que vira `Fulfilled` a qualquer momento cancela os níveis seguintes.

## Aprendizado

`ExpectationLearningService` roda após cada `Bill` chegar a `Paid` ou `Approved`:

1. Agrupa Bills históricas por `(PayeeId, AccountReference)`.
2. Com **≥ 3 ocorrências** e espaçamento regular (tolerância de ±5 dias sobre o intervalo mediano), propõe recorrência, dia de vencimento e lead observado.
3. Cria a expectativa com `Origin = Learned` e **notifica**: "passei a monitorar a conta X; avisarei se não chegar". O usuário pode desativar num clique.

Criar em silêncio seria pior: a primeira notícia da existência da expectativa seria um alerta que o usuário não pediu.

**Cadastro manual** cobre o caso que o histórico não alcança — conta nova, contrato recém-assinado, anual que ainda não repetiu.

## Contra falso positivo

Alerta indevido treina o usuário a ignorar alerta, o que destrói o mecanismo. Cinco defesas, todas obrigatórias:

1. **`Waive` por ciclo** — um clique em "este mês não vem", sem desativar a expectativa.
2. **`Pause(until)`** — imóvel desocupado, obra parada, férias.
3. **Desativação automática** após K ciclos consecutivos `Missing` **e** não reivindicados (default 3). Silêncio do usuário é sinal de que a expectativa morreu.
4. **Nunca repetir nível** dentro do mesmo ciclo.
5. **Janela aprendida, não fixa.** Alertar por regra fixa de "3 dias antes" gera alarme cedo para conta que chega em cima da hora e tarde para conta que chega com folga.

## Domain Services

| Serviço | Por que é serviço |
|---|---|
| `ExpectationMatchingService` | Cruza `Bill` + `BillExpectation` — dois Aggregates. Casa pelo **vencimento** dentro da janela do ciclo (±15 dias) entre as expectativas do beneficiário. Ambiguidade devolve `null` em vez de desempatar: cumprir a expectativa errada apagaria o alerta da conta que não chegou. Quem muta é `expectation.Fulfill(...)`. |
| `ExpectationLearningService` | Cruza histórico de `Bill` + `Payee` para propor expectativas — e para **recusá-las**, com motivo (`TooFewOccurrences`, `Irregular`, `MultipleAccounts`). Devolve candidatas, nunca persiste. Não usa `RoutingRule`, que não existe. |

## Eventos e portas

Eventos: `BillExpectationLearnedDomainEvent`, `BillExpectationCycleOpenedDomainEvent`, `BillExpectationFulfilledDomainEvent`, `BillExpectationMissedDomainEvent`, `BillExpectationCaptureFailedDomainEvent`.

Porta nova: **`INotificationSender`** — `Task SendAsync(TenantId, NotificationKind, NotificationPayload, ct)`. Começa por e-mail. O BC `PeopleManagement` já integra Evolution API para WhatsApp; é o canal de reuso natural quando fizer sentido, não na primeira entrega.

## Casos de uso

| | Endpoint |
|---|---|
| Listar expectativas | `GET /api/v1/{tenantId}/expectations` |
| Cadastrar / editar | `POST` / `PUT /expectations` |
| Pausar / desativar | `POST /expectations/{id}/pause` · `/deactivate` |
| Dispensar um ciclo | `POST /expectations/{id}/cycles/{cycleId}/waive` |
| **Painel de pendências** | `GET /expectations/pending` — o que está atrasado, o que falhou na captura, o que vence em breve |

Recurso de autorização: `expectation` com ações `view` e `manage`.

## Testes obrigatórios

**Unitários:** aprendizado com 3 ocorrências regulares deriva recorrência e lead corretos; ocorrências irregulares **não** geram expectativa; `Fulfill` reajusta a média móvel; `MarkMissing` antes de `AlertAt` é recusado (`BLP.EXP04`); dois ciclos na mesma competência são recusados (`BLP.EXP02`); escalonamento não repete nível.

**Integração:** ciclo aberto → Bill correspondente chega → `Fulfilled` sem alerta; ciclo aberto → nada chega → `Missing` com alerta no nível certo na data certa; item preso em `Locked` → `PartiallyCaptured` com alerta de captura e link para o item; K ciclos `Missing` seguidos → desativação automática; `Waive` não desativa a expectativa.
