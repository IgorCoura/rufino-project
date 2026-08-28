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
| `AnchorCompetence` | `CompetencePeriod` | **A fase da recorrência** — em quais *meses* a conta vence. Reancorada a cada cumprimento |
| `ObservedLeadDays` | `int` | Dias entre chegada e vencimento, aprendido — **é o que ABRE o ciclo** (teto 180, desamarrado da recorrência) |
| `AlertLeadDays` | `int` | Quando **alertar** se ainda não chegou. Default `max(3, ObservedLeadDays + 2)` |
| `WatchingSince` | `DateTime` | Piso da vigilância. Recomeça ao retomar de uma pausa |
| `LastSweptAt` | `DateTime` | Carimbo da varredura — **não** é `UpdatedAt` |
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
6. Nunca abrir ciclo cuja data de alerta preceda `WatchingSince` — guarda sem código de erro, porque o desfecho é *pular*, não recusar.

## Esperar e avisar são dois prazos — corrigido em 2026-08-27

> ⚠️ **O defeito de origem.** Até esta data a abertura do ciclo era governada por `AlertLeadDays`,
> e o efeito era o oposto do propósito do agregado. Cenário real relatado: conta que **vence dia
> 10 e chega 20 dias antes**, com aviso pedido para 2 dias antes. O ciclo de setembro só nascia em
> 08/09; o boleto chegava em 21/08, não encontrava ciclo, não cumpria nada — e em 08/09 o sistema
> alertava "a conta não chegou" sobre um boleto capturado, validado e aprovado.

| Pergunta | Campo | Data |
|---|---|---|
| Quando começo a **esperar**? | `ObservedLeadDays` (+ 5 de folga) | `OpensAtFor(competência)` |
| Quando **reclamo**? | `AlertLeadDays` | `AlertAtFor(competência)` |

`OpenLeadDays = max(ObservedLeadDays + 5, AlertLeadDays)` — o `max` fecha o caso patológico de
alguém pedir aviso com mais antecedência do que a conta chega, que faria o ciclo nascer já vencido
de alerta.

**A varredura olha para a frente.** A versão anterior derivava a competência do mês corrente, e por
isso o ciclo de setembro só podia nascer em setembro — uma conta que chega em agosto era
estruturalmente inalcançável. `OpenDueCycles` anda pelas competências da cadência até a primeira
cuja data de abertura ainda não chegou; como elas crescem em ordem, parar na primeira é seguro.

**A cadência exige âncora.** `ExpectedDueDay` diz o dia, não o mês. Sem `AnchorCompetence` a
varredura abria um ciclo **por mês** para toda expectativa — inclusive as anuais, que geravam doze
ciclos por ano, onze `Missing`, e se autodesativavam em três meses pela regra do silêncio.

**Piso de boas-vindas.** Expectativa cadastrada hoje não abre competência cuja data de alerta já
passou: seria abrir para marcá-la como não cumprida no mesmo instante. Vale também ao retomar de
uma pausa — as competências que venceram durante ela não viram alerta de uma conta que ninguém
observava.

**Métodos ricos**

| Método | Contrato |
|---|---|
| `BillExpectation.Learn(...)` | Factory a partir de N ocorrências regulares. Deriva recorrência, dia de vencimento, lead, **âncora e fonte habitual**. |
| `BillExpectation.Register(...)` | Cadastro manual. Aceita `anchorDueDate` e `hintSourceId` opcionais. |
| `OpenDueCycles(today, occurredAt)` | Job periódico. Abre **todas** as competências da cadência cuja janela de chegada abriu. |
| `OpenCycle(CompetencePeriod, occurredAt)` | Abertura de uma competência específica, sem conferir cadência — serve ao boleto que chegou antes de qualquer previsão. |
| `Fulfill(cycleId, BillId, actualDueDate, arrivedOn, arrivedThrough)` | **Aprende**: média móvel sobre `ExpectedDueDay` e `ObservedLeadDays`, reancora a cadência e atualiza a fonte habitual. |
| `ClearCaptureFailure(itemId)` | O artefato travado foi resolvido sem virar boleto — o ciclo volta a `Waiting`. |
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
| `Status` | `Waiting` \| `Fulfilled` \| `PartiallyCaptured` \| `Missing` \| `Waived` — **`Closed` nunca existiu no código**; `Missing` e `PartiallyCaptured` seguem abertos de propósito |
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
| `ExpectationMatchingService` | Cruza `Bill` + `BillExpectation` — dois Aggregates. Casa pela **competência do vencimento** primeiro; a janela de dias (**±3**, era ±15) sobra só para o vencimento que atravessa a virada do mês. Quando não há ciclo para aquela competência e há **uma única** expectativa vigiando, devolve qual é — e o handler abre o ciclo sob demanda, rede de segurança contra prazo de chegada subestimado. Ambiguidade devolve `null`. |
| `ExpectationCaptureMatchingService` | Cruza `CaptureItem` + `BillExpectation`. Um artefato travado falhou **antes** da extração: não tem beneficiário nem vencimento, e a única ponte é a **fonte** por onde entrou (`HintSourceId`) mais a janela do ciclo. Traduz o estado do item em `MissReason`. Ambiguidade devolve `null`. |
| `ExpectationLearningService` | Cruza histórico de `Bill` + `Payee` para propor expectativas — e para **recusá-las**, com motivo (`TooFewOccurrences`, `Irregular`, `MultipleAccounts`). Devolve candidatas, nunca persiste. Não usa `RoutingRule`, que não existe. |

## Eventos e portas

Eventos: `BillExpectationLearnedDomainEvent`, `BillExpectationCycleOpenedDomainEvent`, `BillExpectationFulfilledDomainEvent`, `BillExpectationMissedDomainEvent`, `BillExpectationCaptureFailedDomainEvent` e **`BillExpectationAlertRaisedDomainEvent`**.

> ⚠️ **É o `AlertRaised` que notifica, não o `Missed`.** A transição para `Missing` acontece uma vez
> por ciclo; o escalonamento acontece quatro. Enquanto o aviso pendurou no primeiro, os níveis
> `Warning`, `Urgent` e `Overdue` eram gravados no agregado e **nunca chegavam a ninguém** — a
> tabela de escalonamento acima existia só no papel. Corrigido em 2026-08-27.

O `CaptureItem` também passou a emitir: **`CaptureItemStuckDomainEvent`** e
**`CaptureItemUnstuckDomainEvent`**, os primeiros eventos daquele agregado. São o elo que faltava
para o alerta de "chegou e não consegui ler" existir — até então `RecordCaptureFailure` não era
chamado por nenhum código de produção, e a lista `captureFailed` do painel voltava sempre vazia.

Porta: **`INotificationSender`** — `Task SendAsync(TenantId, NotificationKind, NotificationPayload, ct)`.

> ⚠️ **A porta recebe o tenant, não um endereço** — e o BC não tinha nenhum dado de contato. Não era
> "falta configurar SMTP": faltava **destinatário**. Resolvido pelo Aggregate
> `TenantNotificationSettings` (`BLP.NTF`), cadastro local por tenant, e pelo
> `GraphNotificationSender`, que reaproveita o `GraphTokenProvider` da leitura de caixa com
> credencial **de instalação** (o remetente é nosso, não do cliente). O `ResilientNotificationSender`
> encadeia os dois e **nunca propaga exceção** — falha de envio não pode desfazer o registro do
> alerta nem fazer o outbox reentregar o mesmo aviso para sempre.

## Casos de uso

| | Endpoint |
|---|---|
| Listar expectativas | `GET /api/v1/{tenantId}/expectations` · `GET /expectations/{id}` |
| Cadastrar | `POST /expectations` |
| Editar | `PUT /expectations/{id}` — **tudo menos o beneficiário** |
| Excluir | `DELETE /expectations/{id}` — leva os ciclos junto |
| Pausar / retomar / desativar | `PUT /expectations/{id}/watch` |
| Dispensar um ciclo | `POST /expectations/{id}/cycles/{cycleId}/waive` |
| **Painel de pendências** | `GET /expectations/pending` — **quatro** listas: `missing` (não chegou, ainda no prazo), `overdue` (não chegou e já venceu), `captureFailed`, `dueSoon` |
| Destinatários do aviso | `GET`/`PUT /notification-settings` — recurso `expectation`, ações `view`/`manage` |

Recurso de autorização: `expectation` com ações `view`, `manage` e **`waive`** (esta com escopo próprio — dispensar um ciclo silencia a rede de segurança daquela conta).

**O beneficiário não é editável, e trocá-lo é excluir e cadastrar de novo.** Trocá-lo descreveria outra expectativa, não esta corrigida, e os ciclos já abertos passariam a esperar uma conta que nunca teve relação com eles. Três consequências que o código sustenta:

1. **Editar torna a expectativa `Manual`**, mesmo nascida do histórico. `Fulfill` reajusta a antecedência sozinho enquanto a origem for `Learned` — sem a virada, a antecedência escolhida à mão seria desfeita no próximo cumprimento, em silêncio. O aprendizado do calendário (dia de vencimento e prazo observado, por média móvel) continua; `ObservationCount` fica.
2. **Só os ciclos em `Waiting` são reposicionados.** A expectativa é a configuração, o ciclo é a história: redatar um `Missing` ressuscitaria um aviso que a pessoa já resolveu; não redatar o que ainda espera entregaria a edição sem entregar o efeito — e é para consertar o alerta errado que se edita.
3. **Excluir não é "nunca mais".** Uma expectativa `Learned` pode ser reaprendida no próximo boleto aprovado daquele beneficiário — é a auto-cura do ADR-014. Quem quer parar de monitorar de vez **desativa** por `PUT /{id}/watch`, que deixa a decisão registrada.

## Testes obrigatórios

**Unitários:** aprendizado com 3 ocorrências regulares deriva recorrência e lead corretos; ocorrências irregulares **não** geram expectativa; `Fulfill` reajusta a média móvel; `MarkMissing` antes de `AlertAt` é recusado (`BLP.EXP04`); dois ciclos na mesma competência são recusados (`BLP.EXP02`); escalonamento não repete nível.

**Integração:** ciclo aberto → Bill correspondente chega → `Fulfilled` sem alerta; ciclo aberto → nada chega → `Missing` com alerta no nível certo na data certa; item preso em `Locked` → `PartiallyCaptured` com alerta de captura e link para o item; K ciclos `Missing` seguidos → desativação automática; `Waive` não desativa a expectativa.
