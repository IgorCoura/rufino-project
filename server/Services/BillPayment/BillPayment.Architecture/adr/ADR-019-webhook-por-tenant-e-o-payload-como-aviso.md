# ADR-019 — Webhook por tenant, e o payload do provedor tratado como aviso

**Status:** Aceito · **Data:** 2026-09-08 (medido em sandbox no mesmo dia)

## Contexto

O usuário relatou: cancelou um pagamento **Pix** no painel do Asaas e o app refletiu; cancelou um
**boleto** e deixou outro sem autorizar, e nenhum dos dois apareceu no banco. A investigação
encontrou um estado pior do que o relato sugeria.

**Não havia webhook nenhum funcionando.** O endpoint existia, mas:

- nada no código chamava `POST /v3/webhooks` — o provisionamento do ADR-016 nunca foi implementado
  e estava no checklist pré-produção;
- sem `PaymentWebhook__Token` no ambiente, o endpoint respondia **404 para todo evento**;
- o token era **da instalação**, não por tenant — com uma conta Asaas por tenant (ADR-016), um
  segredo compartilhado deixaria qualquer tenant forjar evento de qualquer outro.

O que refletiu o Pix foi a **conciliação por polling**, com atraso de até `StaleAfter` (1h).

### O que a sonda mediu (`smoke-probe-payment.js`, sandbox, 2026-09-08)

Cinco achados, e os dois primeiros são bugs de dinheiro que ninguém tinha visto:

1. **`POST /v3/pix/qrCodes/pay` DESCARTA o `externalReference`.** Enviado no corpo, volta `null` —
   tanto na transação quanto no `transfer` espelho. O doc 04 supunha "campo de busca, não chave de
   deduplicação"; a medição é pior: ele não é nem gravado.
2. **O filtro `?externalReference=` é IGNORADO pelo servidor.** Uma consulta com GUID inexistente
   devolveu a lista inteira (`totalCount=3`). O `AsaasPixPaymentGateway.FindByExternalReferenceAsync`
   pegava `data[0]` — **a transação de outro pagamento** — e a adotava como sendo a nossa, na
   retentativa de submissão.
3. **Não existem eventos `PIX_TRANSACTION_*`.** Testados 19 nomes; válidos são os 7 `BILL_*` mais
   `TRANSFER_CREATED/PENDING/BLOCKED/CANCELLED/DONE/FAILED`. Toda saída de Pix é notificada como
   **transferência**, e o objeto do evento é o `transfer` — cujo id **não** é o que guardávamos.
4. **O pagamento Pix nasce em `AWAITING_CRITICAL_ACTION_AUTHORIZATION`**, com `authorized: false`
   no transfer. É exatamente o "não autorizei" do relato — e o status cru era descartado, então a
   tela dizia "aceito pelo provedor".
5. **Contrato do webhook**: `authToken` exige ≥ 32 caracteres; `GET /webhooks/{id}` devolve
   `hasAuthToken: true` e **omite o valor**; a resposta traz `interrupted` e
   `penalizedRequestsCount`.

Não há API pública para autorizar ação crítica (`/criticalActions`, `/myAccount/criticalActionConfigs`
→ 404). A saída documentada continua sendo a **whitelist de IP** do ADR-001.

## Decisão

### 1. Um webhook por tenant, provisionado por nós

`IPaymentWebhookProvisioner` (porta) + `AsaasWebhookProvisioner` (adapter escrito contra o contrato
**medido**). Disparado por `AsaasAccountLinkedDomainEvent`, **fora** da transação que vincula a
chave — chamada externa ali faria o cadastro da chave falhar por indisponibilidade de um passo que
não é dele.

Idempotente: existindo webhook para a mesma URL, é atualizado em vez de duplicado. Sem isso, cada
revínculo de chave deixaria mais um webhook vivo e o provedor entregaria o mesmo evento N vezes.

### 2. O tenant vai na URL; o token prova quem é

A rota vira `POST /webhooks/asaas/{tenantId}`, e cada conta recebe a sua no provisionamento. O
token é gerado por nós, guardado cifrado no cofre (`SecretKind.AsaasWebhookToken`) e comparado em
tempo constante. **O token da instalação foi apagado.**

`SecretKind` próprio porque o tipo entra no dado autenticado da cifra: um token de webhook
apresentado como chave de subconta não decifra. A direção das duas credenciais é oposta — a chave
autentica *nós* no provedor; o token autentica *o provedor* em nós — e usar a chave para os dois
papéis faria a credencial que **paga contas** trafegar em todo header de entrada.

A marca de idempotência passa a ser `(tenant, event id)`: com N contas, dois ids iguais
descartariam um evento em silêncio.

### 3. O payload é AVISO, não verdade

O provedor **não assina o corpo** — oferece apenas o token no header, sem HMAC. Quem obtivesse o
token forjaria qualquer conteúdo. Então o handler usa o payload só para **descobrir de qual ordem
se trata** e em seguida **relê a ordem no provedor** com a chave do tenant, aplicando o que a
leitura afirmar.

Custo: uma chamada de API por evento. Benefício: um corpo forjado vira, no pior caso, uma chamada
desperdiçada. Resolve de brinde a divergência de vocabulário entre `transfer` e transação Pix.

### 4. Duas chaves de resolução

`BILL_*` resolve por `externalReference` (o pague-contas a preserva). `TRANSFER_*` resolve pelo
**`ProviderTransferId`**, coluna nova gravada na submissão a partir do `transferId` da resposta.
Sem ela, todo webhook de Pix seria descartado como "referência desconhecida".

### 5. A idempotência do Pix passa a viver no `description`

Como o `externalReference` some e o filtro é ignorado, o marcador `RUF:{orderId}` vai no
`description` — o único campo que o provedor comprovadamente grava e devolve. A busca lista as
transações recentes e casa **localmente**.

**Não achando dentro da janela varrida, a resposta é `Unavailable` — nunca `NotFound`.** "Não achei
numa lista que não sei se é completa" não autoriza reenviar dinheiro. A ordem entra no hold novo
`AwaitingManualReconciliation`, visível, para uma pessoa conferir no provedor. Entre pagar duas
vezes e pedir ajuda, este BC pede ajuda.

### 6. O status cru do provedor é persistido

`provider_raw_status` e `provider_authorized` em `payment_orders`. O `RawStatus` já viajava no
retrato e era **jogado fora** — por isso a tela não conseguia distinguir "aceito, vai processar na
data" de "travado esperando alguém digitar um código no celular".

## Consequências

- **`PaymentWebhookOptions` deixa de guardar segredo.** Sobra `PublicBaseUrl` e
  `NotificationEmail` — endereço, que é público por natureza. **Sem `PublicBaseUrl` nenhum webhook
  é provisionado** e a instalação depende só da conciliação.
- O polling **continua**, e deve continuar: a fila do provedor é sequencial e ele a interrompe
  depois de falhas repetidas (`penalizedRequestsCount`); a varredura de comprovante não tem
  equivalente em webhook; e só o polling detecta "o provedor não conhece mais esta ordem". Com o
  webhook comprovado em produção, o `StaleAfter` pode subir de 1h para ~12h — configuração, não
  reescrita.
- `payer_profiles` ganha `asaas_webhook_ref`, `asaas_webhook_id` e `last_webhook_event_at`.
- **A whitelist de IP do ADR-001 continua sendo o único caminho** para dispensar a autorização de
  ação crítica, e é **por conta**: cada tenant precisa cadastrar o IP de saída da nossa instalação
  na conta dele. Trocar de VPS quebra todos os tenants de uma vez.

## Alternativas descartadas

- **HMAC.** É o padrão da indústria e o provedor não o oferece. A releitura é a compensação.
- **Manter o token da instalação.** Simples e errado com uma conta por tenant.
- **Buscar a ordem Pix por `externalReference` e confiar.** Foi o que se fazia; a medição mostrou
  que adotaria a transação de outro pagamento.
- **Reenviar o Pix quando a busca não conclui.** É o pagamento duplicado que o hold evita.
