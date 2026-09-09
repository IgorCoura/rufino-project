# ADR-022 — A varredura que reconcilia a ASSINATURA do webhook, e a rotação trimestral do token

**Status:** Aceito · **Data:** 2026-09-09 · **Complementa:**
[ADR-019](ADR-019-webhook-por-tenant-e-o-payload-como-aviso.md).

## Contexto

O ADR-019 montou o webhook por tenant em 2026-09-08. Em 2026-09-09 o usuário relatou que **nada
aparecia no painel do Asaas**: nenhum webhook cadastrado, nenhum log de entrega. Trocar a chave
da conta — o gesto que deveria disparar o provisionamento — não mudou nada.

A investigação achou **duas causas independentes**, e um conjunto de fragilidades ao redor.

### Causa 1 — o evento nunca chegava ao outbox

`BillPaymentDbContext.DrainDomainEvents` movia para `outbox_messages` os eventos dos agregados
rastreados usando um `switch` com **um `case` por Aggregate Root**, porque `AggregateRoot<TId>` é
genérico e não tinha base não-genérica comum. Havia `case` para `Bill`, `BillExpectation`,
`CaptureItem` e `PaymentOrder`. **Não havia para `PayerProfile`** — o único outro agregado que
emite evento.

Resultado: `AsaasAccountLinkedDomainEvent`, emitido por `PayerProfile.LinkAsaasAccount`, era
acumulado na lista interna do agregado e descartado no fim do escopo. Nenhuma linha no outbox,
nenhuma exceção, nenhum log. Um mês de webhooks que nunca existiram, em silêncio absoluto.

O comentário logo acima do `switch` advertia exatamente isso ("acrescente o novo agregado aqui ao
criá-lo, senão ele emite eventos que ninguém publica e a falha é silenciosa"). A advertência não
bastou, e não tinha como bastar: **uma lista mantida por memória humana falha por definição.**

Nenhum teste cobria o caminho — `ProvisionPaymentWebhookCommand` e `IPaymentWebhookProvisioner`
não apareciam em teste nenhum da suíte.

### Causa 2 — sem endereço público não há webhook

`PaymentWebhook:PublicBaseUrl` está vazia no `appsettings.json` e não é definida em compose, env
ou script nenhum do repositório. Mesmo com o evento chegando, o handler sairia por
`Skipped("public_base_url_missing")`. Já constava como pendência aberta no checklist de deploy.

### O que mais a varredura encontrou

1. **O outbox tem backoff FINITO** (5 tentativas, ~7,5 min). Provedor fora do ar por dez minutos
   no instante em que o tenant vincula a chave esgotava as tentativas, e o webhook **nunca mais**
   seria provisionado. Sem um mecanismo periódico, "esgotou" significava "para sempre".
2. `IPaymentWebhookProvisioner.GetHealthAsync` e `RemoveAsync` estavam implementados e **ninguém
   os chamava**; `PayerProfile.LastWebhookEventAt` era gravado e **nunca lido**. A "varredura de
   saúde" citada em três comentários do código nunca havia sido construída.
3. `UnlinkAsaasAccountCommand` limpava só o ponteiro da chave: o webhook continuava vivo na conta
   do cliente apontando para esta API, o token dele ficava órfão no cofre, e o perfil seguia
   respondendo `HasWebhook` com `AsaasAccountRef` nulo — todo evento entrante passava na
   conferência do token e morria na releitura, em laço.
4. O **401 deliberado** para "tenant sem webhook" era uma armadilha: o Asaas
   [só considera sucesso o HTTP 200](https://docs.asaas.com/docs/fila-pausada) (201 e 204 já
   contam como falha) e **interrompe a fila sequencial da conta após 15 falhas consecutivas** —
   evento represado ali é descartado em 14 dias. Um desvínculo pausava a conta do cliente em
   quinze entregas.
5. `SUBSCRIBED_EVENTS` não incluía `TRANSFER_CREATED` nem `TRANSFER_IN_BANK_PROCESSING` (o
   comentário do código citava `TRANSFER_IN_BANK_ACCOUNT`, que não é o nome real), e o payload
   omitia `apiVersion`, que a documentação lista como obrigatório.

### O que o PeopleManagement faz com a ZapSign, e por que não se copia

`ZapSignWebHookManagementService.RefreshWebHookEvent` roda em job Hangfire diário mais um
`Enqueue` a cada boot. O **mecanismo** é bom e foi copiado: criar o novo antes de apagar o velho,
rollback quando a gravação falha, falha na criação mantém o antigo ativo, e rodar no arranque
além do intervalo.

A **política** não se copia. Lá o webhook não tem token próprio: o
`SigningServiceAccountTokenProvider` embute um `Authorization: Bearer <JWT do Keycloak>` no header
do webhook, e o endpoint receptor valida esse JWT. Um token com `accessTokenLifespan` de 300 s
carimbado num webhook renovado a cada 24 h fica expirado quase o dia inteiro — é o que motiva a
rotação diária de lá, e provavelmente é um defeito, não um design. *(A verificar: se o client do
service account sobrescreve `access.token.lifespan`.)*

O `authToken` do Asaas é outra coisa: segredo estático de alta entropia gerado por nós, que **não
expira**. Rotacionar diariamente não compraria segurança e custaria risco — `GET /v3/webhooks/{id}`
devolve apenas `hasAuthToken: true` e **nunca o valor**, então cada rotação é uma janela em que
falhar entre gravar no provedor e gravar no cofre deixa o webhook mudo sem conserto que não seja
recriá-lo.

## Decisão

**1. O dreno de eventos deixa de ter lista.** `AggregateRoot<TId>` passa a implementar a interface
não-genérica `IHasDomainEvents`, e `DrainDomainEvents` pergunta `is IHasDomainEvents` em vez de
consultar um `switch`. Agregado novo entra sozinho. **A lista não volta** — dois testes de erosão
em `DomainEventDrainErosionTests` falham se algum Aggregate Root deixar de ser drenável, ou se
algum tipo passar a acumular `IDomainEvent` fora da base.

**2. Existe uma varredura periódica de ASSINATURA**, irmã da conciliação de ORDEM:
`PaymentWebhookSweepBackgroundService` → `SweepPaymentWebhookCommand`, a cada 30 min e **também no
arranque**. Por tenant com conta vinculada, ela lê a saúde no provedor e decide:

| Estado observado | Ação |
|---|---|
| Sem webhook no perfil | provisiona, token novo (é o caso do outbox esgotado) |
| `NotFound` no provedor | recria, token novo |
| Desabilitado, fila interrompida, ou URL diferente da esperada | **cura com o MESMO token** |
| `penalizedRequestsCount` ≥ 5 | alerta operacional |
| Mudo além da tolerância **e com ordem viva** | alerta operacional |
| Provedor indisponível | não faz nada; volta no próximo ciclo |
| Íntegro e token dentro da validade | não faz nada |

**Indisponibilidade nunca vira conserto**: recriar por não conseguir ler deixaria um webhook órfão
na conta do tenant a cada instabilidade.

**3. Curar não é rotacionar.** `EnsureAsync` ganhou o parâmetro `authToken`: `null` gera token novo
(rotação), valor vindo do cofre mantém o vigente (cura). No agregado, `LinkAsaasWebhook` carimba
`AsaasWebhookRotatedAt` e `ConfirmAsaasWebhook` **não mexe nesse relógio**. Sem essa distinção, uma
varredura de meia em meia hora trocaria o segredo o dia inteiro.

**4. A rotação é TRIMESTRAL** (`TokenRotationInterval`, 90 dias), por higiene — não diária. O
motivo está no Contexto: o token não expira, e cada rotação é risco sem contrapartida.

**5. O 401 fica só para token ERRADO.** "Tenant sem webhook configurado" passa a responder **200
`NotConfigured`**. Represar a fila de quem tenta forjar continua sendo o comportamento certo;
represar a fila do provedor esvaziando entregas de uma conta recém-desvinculada, não.

**6. Desvincular a conta derruba o webhook no provedor** (best-effort, antes de o segredo da chave
sair do cofre) e remove **os dois** segredos. Provedor fora do ar não impede o tenant de tirar a
chave dele daqui: o que fica é um webhook que o próprio provedor interromperá, visível no painel
dele e sem efeito aqui.

**7. Duas alavancas manuais**: `GET /api/v1/{tenantId}/payer-profile/asaas-webhook` diz, ao vivo,
se o webhook está entregando (sem vazar segredo), e
`POST .../asaas-webhook/reprovision` recria com token novo. O reprovisionamento manual é o **único
conserto possível** quando o cofre e o provedor saem de sincronia — defeito que a varredura não
consegue diagnosticar, porque o provedor nunca devolve o token.

**8. `apiVersion: 3` explícito** no payload, e `TRANSFER_CREATED` + `TRANSFER_IN_BANK_PROCESSING`
acrescentados aos eventos assinados.

## Consequências

- **O padrão fica completo.** O evento avisa rápido, a releitura na API diz a verdade (ADR-019), a
  conciliação vigia as ordens, e a varredura vigia o canal. Era a quarta perna que faltava, e é a
  recomendação corrente da indústria para integração por webhook.
- **`PublicBaseUrl` continua sendo pré-requisito de ambiente**, e sem ela tanto o provisionamento
  quanto a varredura saem por `Skipped` com log de aviso — alto, mas sem derrubar nada.
- **Uma leitura por tenant a cada 30 min** no provedor. Barato, e o teto é `BatchSize` (200).
- A varredura **não usa claim nem aluguel**: o efeito que ela dispara é idempotente, e duas
  réplicas passando pelo mesmo tenant custam uma chamada a mais, não um webhook a mais.
- **Resíduo conhecido:** trocar a chave por uma de OUTRA conta Asaas deixa o webhook antigo vivo na
  conta anterior. Não há como removê-lo — a chave daquela conta já saiu do cofre. Ele passa a
  receber 401, o provedor o interrompe, e a remoção é manual no painel do cliente.
- `StaleAfter` da conciliação pode finalmente ser rebaixado de 1h para ~12h **depois** de o webhook
  estar comprovado em produção — agora há como comprovar, pelo endpoint de inspeção.
