# ADR-001 — Asaas como provedor de consulta e de pagamento de contas

**Status:** Aceito · **Data:** 2026-07-31

## Contexto

O BC precisa de duas capacidades externas: **consultar oficialmente** um título a partir da linha digitável (para saber quem é o beneficiário de verdade) e **executar o pagamento**. As alternativas eram integrar direto com cada banco via Open Finance / API proprietária, usar um PSP/BaaS, ou não executar pagamento nenhum (só lembrar o usuário).

## Decisão

**Asaas** para as duas coisas: `POST /v3/bill/simulate` como consulta oficial e `POST /v3/bill` como execução, com conciliação por webhook `BILL_*`.

O Domain fala com duas portas separadas — `IBillLookupService` e `IBillPaymentGateway` — implementadas pelo mesmo adapter. A separação é intencional: a fase 1 depende só da primeira, que é read-only e não move dinheiro.

## Razões

- **Uma integração cobre todos os bancos.** Integração direta exigiria homologação banco a banco, com prazo fora do controle do projeto.
- **A consulta e o pagamento vêm do mesmo lugar**, então o dado que sustenta a decisão é o mesmo que executa a ordem. Consultar em um provedor e pagar em outro abre janela para divergência entre o que foi aprovado e o que foi pago.
- **O simulate entrega exatamente o que a verificação precisa**: `beneficiaryCpfCnpj`, `beneficiaryName`, `bank`, `value`, `originalValue`, `dueDate`, `isOverdue`, `minValue`/`maxValue`/`allowChangeValue`, `interestValue`/`fineValue`/`discountValue`, `minimumScheduleDate` e `fee`.
- **`externalReference`** permite idempotência de ponta a ponta amarrada ao nosso `PaymentOrderId`.
- **Sandbox completo**, o que torna possível cobrir a fase 3 com teste antes de qualquer dinheiro real.

## Achado de campo — a consulta exige credencial que move dinheiro

**Medido em sandbox em 2026-07-31** ([`tools/probe-asaas-simulate.js`](../tools/probe-asaas-simulate.js)):

| Endpoint | Resultado com a chave de sandbox |
|---|---|
| `GET /v3/customers` | 200 |
| `GET /v3/finance/balance` | 200 |
| `POST /v3/bill/simulate` | **403 `insufficient_permission`** |
| `POST /v3/pix/qrCodes/decode` | **403 `insufficient_permission`** |

Mensagem do provedor: *"A chave de API fornecida não possui permissão para realizar operações de saque via API."*

**As duas consultas oficiais — boleto e Pix — estão atrás da permissão de saque**, apesar de nenhuma delas movimentar dinheiro. Leitura comum passa; a consulta de título, não.

Isso **corrige uma premissa deste ADR**. Continua verdade que o simulate é read-only e que a Fase 1 não movimenta dinheiro. Deixa de ser verdade que ela roda com credencial inofensiva: **a chave que a Fase 1 precisa é uma chave habilitada a pagar contas**. Se ela vazar, o atacante paga — mesmo que o nosso código nunca chame o endpoint de pagamento.

Consequências que decorrem disso:

- **A postura de segredo da Fase 1 é a mesma da Fase 3.** Não existe janela de "chave só de leitura enquanto não pagamos". Isso pesa contra o adiamento do cofre registrado no [`ADR-009`](ADR-009-cofre-de-segredos.md) — o gatilho de reabertura ("primeiro cliente externo") deveria ser reavaliado, porque a exposição já é de credencial pagadora desde a Fase 1.
- **Whitelist de IP deixa de ser opcional.** É o mecanismo do provedor para limitar o estrago de uma chave vazada e para dispensar aprovação manual de operações críticas. Entra no checklist pré-produção.
- A verificação de cobertura foi feita depois de a permissão ser habilitada. Resultado completo em [`12-official-lookup-coverage.md`](../12-official-lookup-coverage.md); o resumo que muda decisão está abaixo.

## Achado de campo — a consulta não entrega beneficiário para arrecadação

**Medido em 2026-07-31, com a permissão já habilitada**, contra as 22 linhas do corpus real:

- **Arrecadação (10 linhas): 100% respondem, 0% trazem `beneficiaryCpfCnpj`.** Vem `companyName` em 100% e `beneficiaryName` em 60% — ou seja, o beneficiário é identificável **por nome, nunca por documento**. `bank` vem nulo em 100%.
- **Cobrança bancária (12 linhas): 0% respondem**, todas com `unregistered_bank_slip`. **Esse zero não é sobre a cobrança** — um boleto emitido pelo próprio sandbox, consultado no mesmo sandbox, também falha. Não há registro de cobrança em sandbox.

O que isso corrige neste ADR: a afirmação de que *"o simulate entrega exatamente o que a verificação precisa"* vale para cobrança bancária (a validar em produção) e **não vale para arrecadação**. Para arrecadação o provedor entrega valor e nome, não documento nem banco — e parte disso é estrutural, porque o código de barras de arrecadação não carrega esses campos.

Consequências:

- **O check de beneficiário tem duas forças.** Documento contra documento na cobrança; nome contra `Payee.LegalName` + `Aliases` na arrecadação. A segunda é evidência de apoio, não prova.
- **O check de banco recebedor é inaplicável a arrecadação**, e isso não muda trocando de provedor.
- **Fica uma lacuna de validação**: o caminho de cobrança bancária não pode ser exercido em sandbox. A Fase 1 precisa de uma sonda de fumaça em produção — uma consulta, um boleto real — antes de o check ser considerado confiável.

## Consequências

- O pague-contas debita do **saldo da conta Asaas** — o cliente precisa manter saldo, e o sistema precisa checar saldo antes de agendar e alertar quando insuficiente. Aporte de saldo é operação manual do cliente, fora do escopo.
- Decisão pendente antes da sprint 3.1: **subconta Asaas por tenant** (recomendado — segregação de dinheiro entre clientes não deve depender do nosso código) × conta única com segregação lógica.
- Uma chave `access_token` por tenant, no cofre. Nunca em `appsettings.json`.
- Custo por transação (`fee`) entra no relatório como linha própria.
- Regras de agendamento do provedor (dia útil, corte das 14h, vencido paga na hora, `minimumScheduleDate`) viram regra de domínio no `PaymentSchedulingService` — se o provedor mudar, muda ali.
- Cobertura da consulta para boletos de **arrecadação/concessionária** ainda não foi validada. Verificar em sandbox na sprint 1.3; se for parcial, `LookupAvailability` precisa de tratamento por `BillKind`.

## Alternativas descartadas

- **Open Finance / API bancária direta** — melhor custo unitário e mais controle, mas uma integração e uma homologação por banco. Reabrir quando o volume justificar.
- **Consulta em um provedor, pagamento em outro** — divergência entre o dado aprovado e o dado pago não compensa a eventual economia.
- **Sem execução de pagamento** — reduz o produto a um lembrete; o cliente continua digitando linha digitável à mão, que é exatamente o passo onde a fraude acontece.
