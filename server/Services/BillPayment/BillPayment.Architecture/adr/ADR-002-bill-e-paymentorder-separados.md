# ADR-002 — `Bill` e `PaymentOrder` são Aggregates distintos

**Status:** Aceito · **Data:** 2026-07-31

## Contexto

O boleto e o pagamento dele têm relação 1:1 na maior parte do tempo. A tentação é manter tudo em `Bill`, com o status de pagamento como mais alguns valores no mesmo Aggregate.

## Decisão

Dois Aggregate Roots. `Bill` cobre captura, verificação e decisão humana. `PaymentOrder` cobre a execução no provedor. Ligação por id, sem navegação. Comunicação por Domain Event via Outbox.

`PaymentOrder` é a **fonte de verdade** da execução financeira. `Bill.Status` mantém um espelho (`Scheduled`/`Paid`/`Failed`) para a listagem, atualizado exclusivamente por handler de evento da `PaymentOrder` — nunca por escrita direta de um handler de Application.

## Razões

- **Ciclos de vida com donos diferentes.** `Bill` muda por ação humana e por validação nossa; `PaymentOrder` muda por webhook do provedor, fora de ordem e em horário arbitrário. Juntar os dois faz um Aggregate com duas máquinas de estado disputando a mesma linha de banco.
- **Regra do BC: um Aggregate mutado por transação.** Um webhook chegando enquanto um usuário edita o Bill seriam duas escritas concorrentes no mesmo Root.
- **Retentativa.** Um pagamento que falha pode gerar uma nova ordem; a história de tentativas pertence ao lado da execução, não ao boleto.
- **Idempotência.** `externalReference` = `PaymentOrderId` só funciona limpo se a ordem tem identidade própria.

## Consequências

- Espelhar status abre risco de divergência. Mitigações obrigatórias: `Bill` só muda de `Scheduled` para frente por evento; job de conciliação compara os dois lados e alerta; `ApplyProviderStatus` é monotônica (não regride de `Paid`).
- Consultas de tela precisam juntar os dois — resolvido no read side (`IBillQueries` / `IPaymentReportQueries`), não com navegação no Domain.
- A janela entre `Approved` e `Scheduled` é observável (o outbox ainda não processou). A UI mostra "aprovado, agendamento em processamento"; não é erro.

## Alternativa descartada

**Tudo em `Bill`** — mais simples de ler no começo, mas coloca escrita de webhook e escrita de usuário no mesmo Aggregate e quebra a regra de um Aggregate por transação assim que a fase 3 entra.
