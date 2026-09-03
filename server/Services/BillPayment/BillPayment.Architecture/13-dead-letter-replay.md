# 13 — Replay operacional da dead-letter do outbox

> Roteiro devido da sprint 3.4. Com a fase 3, uma mensagem morta na dead-letter deixou de ser só
> atraso: uma aprovação morta é **um pagamento que nunca acontece**, e um `Paid` morto é um
> comprovante que nunca chega — os dois em silêncio. Enquanto não existe o endpoint administrativo
> (checklist pré-produção), o replay é feito por SQL, seguindo este roteiro. **Não improvise fora
> dele**: a ordem dos passos é o que mantém o replay seguro.

## Como o outbox falha

`OutboxBackgroundService` reivindica uma mensagem por vez (`FOR UPDATE SKIP LOCKED`) e despacha
para os handlers registrados. Falha incrementa `attempts` e grava `error`; o backoff
(`next_attempt_at`, base 30 s dobrando até 30 min) espaça as tentativas. Ao atingir
`Outbox:MaxAttempts` (default **5**, ~7,5 min de cobertura contra provedor fora), a linha é movida
de `bill_payment.outbox_messages` para **`bill_payment.outbox_dead_letters`** — com `event_type`,
`payload`, `occurred_at`, `attempts`, `error` e `failed_at` preservados. A partir daí **nada mais
tenta**: é trabalho de gente, e é para isso que este roteiro existe.

## Passo 1 — Identificar

```sql
SELECT id, event_type, occurred_at, failed_at, attempts, left(error, 200) AS error
FROM bill_payment.outbox_dead_letters
ORDER BY occurred_at;
```

O `event_type` é o `FullName` do evento de domínio (ex.:
`BillPayment.Domain.Bills.BillApprovedDomainEvent`,
`BillPayment.Domain.PaymentOrders.PaymentOrderPaidDomainEvent`). O `payload` é o JSON do evento —
nele estão os ids (`BillId`, `PaymentOrderId`, `TenantId`) para localizar os agregados envolvidos.

**Triagem por severidade** (o que cada tipo morto significa):

| Evento morto | Consequência silenciosa |
|---|---|
| `BillApprovedDomainEvent` | A ordem de pagamento **nunca é criada** — boleto aprovado que não paga |
| `PaymentOrderScheduled/Paid/Failed/CancelledDomainEvent` | O espelho no `Bill` não reflete — a tela mente sobre o estado do pagamento |
| `PaymentOrderPaidDomainEvent` (2º handler) | O **comprovante nunca é baixado** |
| `BillCapturedDomainEvent` | O boleto não é validado — fica em `Captured` para sempre |
| Eventos de expectativa / notificação | Alerta da rede de segurança do ADR-014 não sai |

## Passo 2 — Corrigir a causa antes de reemitir

Leia `error`. Replay sem corrigir a causa só devolve a mensagem à dead-letter cinco tentativas
depois. Causas típicas: handler não registrado em `ApplicationDependencies` (foi o defeito real do
`CaptureReceiptOnPaymentPaidHandler`), provedor fora por mais tempo que o backoff cobre, defeito
de código corrigido em versão posterior. **Confirme que a versão no ar contém a correção.**

## Passo 3 — Conferir que o replay é seguro (idempotência dos handlers)

O consumo do outbox é **at-least-once por construção** — todo handler já tolera reentrega. É isso
que torna o replay seguro, e vale conferir caso a caso:

- **`BillApprovedDomainEvent` reprocessado é seguro por construção, inclusive para dinheiro.**
  Dois cintos: `GetActiveByBillAsync` (não cria segunda ordem se há uma ativa) e o índice único
  parcial `ix_payment_orders_bill_active` (uma corrida entre duas entregas morre no banco). E a
  criação da ordem **não chama o provedor**: quem submete é a fila de submissão, que a partir da
  2ª tentativa consulta a `externalReference` antes de qualquer reenvio — não há caminho de
  pagamento duplicado via replay.
- **Reflexos no `Bill`** (`ReflectPaymentOnBillCommands`): guardados por status + ordem vinculada;
  reentrega ou evento defasado sai como `Applied: false`, sem segundo efeito.
- **Comprovante** (`CapturePaymentReceiptCommand`): segunda captura é `AlreadyStored` — um blob
  só, um download só.
- **Validação** (`BillCapturedDomainEvent`): revalidação silenciosa só em
  `Captured`/`AwaitingApproval`; não derruba aprovação vigente.
- **Notificações**: podem sair repetidas (at-least-once de efeito externo) — aceitável; o registro
  do alerta no agregado tem guarda de "um por nível".

## Passo 4 — Reemitir

Uma mensagem por vez, **da mais antiga para a mais nova** (`occurred_at`) — a monotonia dos
handlers tolera fora de ordem, mas não há motivo para provocá-la:

```sql
-- dentro de uma transação:
INSERT INTO bill_payment.outbox_messages
    (id, event_type, payload, occurred_at, created_at, processed, attempts, error, next_attempt_at)
SELECT id, event_type, payload, occurred_at, created_at, false, 0, NULL, NULL
FROM bill_payment.outbox_dead_letters
WHERE id = '<id>';

DELETE FROM bill_payment.outbox_dead_letters WHERE id = '<id>';
```

`attempts = 0` dá orçamento novo de tentativas; `next_attempt_at = NULL` a torna elegível já na
próxima varredura do worker. O `DELETE` na mesma transação impede a linha de existir nos dois
lugares. Exige `Outbox:Enabled = true` no deployment que processa (um só, sempre).

## Passo 5 — Conferir o efeito

Espere um ciclo do worker e confira, conforme o tipo:

```sql
-- a mensagem foi consumida?
SELECT processed, processed_at, attempts, error
FROM bill_payment.outbox_messages WHERE id = '<id>';

-- BillApproved: a ordem existe (e é UMA) e o espelho ligou?
SELECT id, status, hold, requested_schedule_date, effective_schedule_date
FROM bill_payment.payment_orders WHERE bill_id = '<billId>';
SELECT status, payment_order_id, scheduled_for FROM bill_payment.bills WHERE id = '<billId>';

-- PaymentOrderPaid: espelho e comprovante?
SELECT status FROM bill_payment.bills WHERE id = '<billId>';
SELECT receipt_storage_key FROM bill_payment.payment_orders WHERE id = '<orderId>';
```

Mensagem que voltou à dead-letter = causa não corrigida; volte ao passo 2. `CleanupAsync` purga as
processadas depois de `RetentionDays` — não é preciso limpar à mão.

## O que este roteiro NÃO cobre

- **Endpoint administrativo de replay** (com escopo próprio no realm) — segue no checklist
  pré-produção; este roteiro é o interim.
- **Dead-letter da fila de webhooks do provedor** — não existe: webhook malformado responde 2xx
  com desfecho registrado, e a rede de segurança é a conciliação por polling.
- **Editar `payload` à mão** — não faça. Payload que precisa de edição é defeito de código; corrija
  o handler e reemita o payload original.
