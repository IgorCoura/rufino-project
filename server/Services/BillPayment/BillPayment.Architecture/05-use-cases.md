# 05 — Casos de uso e contratos de API

Convenções: rota `api/v1/{tenantId}/[controller]`, comandos de escrita embrulhados em `IdentifiedCommand` com header `x-requestid`, leitura por `IXxxQueries` injetada direto no controller (sem mediator). Autorização granular via `[ProtectedResource(recurso, ação)]` — **plugada em 2026-08-15**, nos endpoints existentes — **57** depois dos dois de documento original (2026-08-19).

## Recursos e ações de autorização

| Recurso | Ações | Estado |
|---|---|---|
| `bill` | `view`, `import`, `validate`, `approve`, `deny`, `cancel` | ✅ aplicado |
| `payer-profile` | `view`, `manage` | ✅ aplicado |
| `payee` | `view`, `manage` | ✅ aplicado |
| `origin` | `view`, `manage` | ✅ aplicado |
| `capture-source` | `view`, `manage`, `sync` | ✅ aplicado |
| `capture-item` | `view`, `claim`, **`reprocess`** | ✅ aplicado |
| `expectation` | `view`, `manage`, **`waive`** | ✅ aplicado |
| `captured-message` | `view`, **`recapture`** | ✅ aplicado (2026-08-19) |
| `capture-retention` | `view`, `manage` | ✅ aplicado (2026-08-19) |
| ~~`routing-rule`~~ | — | **abandonado na 2.6** — a chave que a regra usaria não distingue pagadores |
| `payment` | `view`, `cancel` | previsto (fase 3) |
| `report` | `view`, `export` | previsto (fase 4) |

**Os dois recursos de 2026-08-19 são próprios de propósito.** `captured-message` é o livro-caixa da captura — um registro por e-mail lido, **inclusive o descartado**, que não deixa `CaptureItem`. Pendurá-lo em `capture-item` misturaria fila de trabalho com histórico, que são coisas diferentes: a fila mostra o que falta resolver, o histórico responde "o que houve com o e-mail que eu mandei". `recapture` tem escopo próprio pelo mesmo motivo de `capture-item:reprocess` — reingerir consome cota do extrator de visão. E `capture-retention:manage` fica com o administrador porque decidir por quanto tempo o histórico existe é decisão de instalação, não de operação.

**Três escopos não estavam neste catálogo e a implementação os acrescentou**, cada um porque a ação que ele cobre custa algo que a leitura não custa:

- **`capture-item:reprocess`** — reprocessar chama o extrator de visão, que consome cota de uma conta com teto diário. Sob o escopo de leitura, quem só revisa a quarentena poderia queimar o teto do dia e parar a captura.
- **`expectation:waive`** — dispensar um ciclo silencia a rede de segurança daquela conta (ADR-014). Fica com quem decide, não com quem cadastra.
- **`expectation` como recurso** — a 2.7 criou o Aggregate depois deste catálogo ser escrito.

`bill:approve` é a ação sensível — é ela que autoriza dinheiro sair, e é onde a alçada por usuário se aplica **quando ela existir**: hoje o teto é único da instalação, e amarrá-lo a pessoa é trabalho ainda por fazer (ver `06-roadmap.md`, fase 6).

A configuração do realm que materializa esta tabela vive em [`utils/KeyCloakConfig/bill-payment-authz-config.json`](../../../../utils/KeyCloakConfig/bill-payment-authz-config.json), com quatro papéis: `bill-admin`, `bill-approver` (decide e dispensa ciclo), `bill-operator` (opera, não decide) e `bill-viewer`.

---

## Fase 1 — Captura manual, validação e aprovação

### UC-01 — Importar boleto manualmente

`POST /api/v1/{tenantId}/bills/import`

Duas formas: `multipart/form-data` com o PDF, ou JSON com `{ "digitableLine": "..." }`. Com PDF, o parser extrai a linha e o pagador; sem PDF, o pagador fica inextraível e o check correspondente sai `Inconclusive`.

Fluxo: `Bill.Capture` → persiste → `BillCapturedDomainEvent` → validação assíncrona pelo outbox.

O boleto é do tenant que importou — não passa pela escada de roteamento, e o check `TenantRouting` sai `Skipped`. O `PayerMatch` continua valendo: se o PDF traz um pagador que contradiz o `PayerProfile`, o boleto é bloqueado mesmo tendo sido importado à mão.

Resposta `201` com `{ id, status: "Captured", digitableLine, kind }`. Duplicata **do mesmo tenant** devolve `409` com o id da Bill existente; duplicata de outro tenant devolve `409` com aviso genérico, sem identificar.

### UC-02 — Validar / revalidar boleto

`POST /api/v1/{tenantId}/bills/{id}/validate`

Roda o pipeline de [`03-bill-validation.md`](03-bill-validation.md). Executado automaticamente após a captura; o endpoint existe para reexecução manual (consulta que falhou, cadastro de `Payee` criado depois, snapshot velho).

Resposta `200` com o Bill e a lista de checks.

### UC-03 — Listar boletos

`GET /api/v1/{tenantId}/bills?status=&from=&to=&payeeId=&search=&cursor=`

Cursor pagination. Ordenação padrão: os que exigem atenção primeiro (`Rejected`, depois `AwaitingApproval` com advisory falhando), depois por vencimento ascendente. A tela de trabalho do usuário é essa lista.

> **Estado (2026-08-17):** `status=` está implementado, no mesmo contrato do filtro de capture-items — casa pelo nome do `BillStatus`, case-insensitive, e valor desconhecido devolve a lista inteira. `from`/`to`/`payeeId`/`search` e a ordenação por atenção seguem por fazer; a ordenação real é `CreatedAt DESC, Id DESC`.

### UC-04 — Detalhar boleto

`GET /api/v1/{tenantId}/bills/{id}`

Devolve origem com evidência, snapshot da consulta, **todos os checks com motivo e evidência** e a decisão humana. É a tela onde a aprovação acontece — a evidência precisa estar completa aqui, senão o aprovador aprova no escuro.

> **Estado (2026-08-19):** o detalhe **não devolve a linha digitável** — quem tem os dígitos, paga, e essa decisão é anterior a este documento. O "link do PDF original" que esta seção prometia virou endpoint próprio: **`GET /bills/{id}/artifact`** serve o arquivo como ele chegou (`bill:view`), e responde 404 para boleto de outro tenant e para boleto importado à mão, que nasce só com os dígitos. O irmão dele é **`GET /capture-items/{id}/artifact`** (`capture-item:view`), que serve a quarentena sob o gate do ADR-008 — sem ele, reivindicar era decidir no escuro.

### UC-05 — Aprovar boleto

`POST /api/v1/{tenantId}/bills/{id}/approve` — body `{ "scheduleFor": "2026-08-10", "note": "..." }`

Pré-condições verificadas pelo Aggregate: status `AwaitingApproval`, todos os checks obrigatórios executados, nenhum `Blocking` falhando, `scheduleFor` ≥ hoje e ≥ `MinimumScheduleDate`, snapshot não expirado. Alçada do usuário conferida contra `Lookup.Amount`.

Na fase 1 o Bill fica em `Approved` sem execução. Na fase 3 o evento passa a criar a `PaymentOrder`.

**Ações acessórias na mesma tela** (comandos separados, não efeitos colaterais do approve): cadastrar o beneficiário da consulta como `Payee`, aprender o banco recebedor, marcar a origem como confiável.

### UC-06 — Recusar boleto

`POST /api/v1/{tenantId}/bills/{id}/deny` — body `{ "reason": "..." }`. `reason` obrigatório: recusa sem motivo é buraco na auditoria.

### UC-07 — Cancelar boleto

`POST /api/v1/{tenantId}/bills/{id}/cancel`. Proibido depois de `Paid`. Na fase 3, cancela também a ordem no provedor se ainda for cancelável.

### UC-08 — CRUD de beneficiários

`GET|POST /api/v1/{tenantId}/payees`, `GET|PUT /payees/{id}`, `POST /payees/{id}/banks`, `PUT /payees/{id}/amount-policy`, `POST /payees/{id}/deactivate`.

### UC-09 — CRUD de origens confiáveis

`GET|POST /api/v1/{tenantId}/trusted-origins`, `DELETE /trusted-origins/{id}`. `POST` aceita `{ kind, value, decision, note }`.

### UC-09b — Cadastro fiscal do tenant (`PayerProfile`)

`GET|PUT /api/v1/{tenantId}/payer-profile` — `{ kind: "Individual"|"Company", legalName, primaryTaxId, additionalTaxIds[], matchByCnpjRoot }`.

É pré-requisito do check `PayerMatch` e do degrau 1 do roteamento: sem cadastro fiscal não há contra o que comparar, e o check sai `Skipped`. `matchByCnpjRoot` só é aceito para `Company`.

`POST /payer-profile/asaas-account` cria a subconta Asaas do tenant (fase 3) e grava o `AsaasAccountRef`.

---

## Fase 2 — Captura por e-mail

### UC-10 — Cadastrar fonte de captura

`POST /api/v1/{tenantId}/capture-sources` — `{ kind, displayName, address }`. A conexão OAuth é feita em fluxo separado (`GET /capture-sources/{id}/authorize` → redirect → callback), e o token vai para o cofre; a API devolve só o `CredentialRef`.

**Aviso de fonte compartilhada:** se outro tenant já monitora aquele endereço, o callback devolve `{ sharedWithOtherAccount: true }` e a UI mostra *"esta caixa já é monitorada por outra conta do sistema"* — sem dizer quem, quantos, ou qualquer outra coisa. O aviso só aparece **depois** do OAuth concluir; perguntar antes transformaria o endpoint em oráculo de endereços cadastrados ([`adr/ADR-008`](adr/ADR-008-fontes-compartilhadas-e-isolamento.md)).

Cada tenant tem sua própria `CaptureSource` para a mesma caixa, com credencial e cursor próprios. Nenhum vê a do outro.

### UC-11 — Sincronizar caixa

Automático por job periódico; `POST /capture-sources/{id}/sync` força execução. Para cada mensagem nova: cria `CaptureItem` (idempotente por `(TenantId, SourceId, ExternalMessageId)`), baixa anexos PDF, guarda no storage, extrai e valida candidatos a linha digitável, e roda o `BillRoutingService`.

Desfecho por item — a pipeline **nunca** atribui ao dono da fonte por default:

| Rota | Status | Vira Bill? |
|---|---|---|
| Degrau 0 ou 1 apontou para este tenant | `Promoted` | sim, confiança `Strong` |
| Degrau 3 (beneficiário exclusivo) | `Promoted` | sim, confiança `Weak`, destacado na aprovação |
| Pagador identificado e é de outro | `ForeignPayer` | não |
| Nada resolveu | `Unrouted` | não — vai para a fila de reivindicação |
| Nenhum boleto válido no anexo | `Unrecognized` | não |

### UC-12 — Revisar a quarentena

`GET /api/v1/{tenantId}/capture-items?status=Unrouted|ForeignPayer|Unrecognized`

**A projeção muda por status**, e isso é regra de segurança, não de UI: `ForeignPayer` devolve só remetente, assunto, data e motivo — sem valor, beneficiário ou linha digitável, porque o sistema já sabe que não é deste usuário. `Unrouted` devolve também beneficiário e valor, que é o mínimo para o usuário decidir se é dele.

### UC-12b — Reivindicar um boleto não roteado

`POST /api/v1/{tenantId}/capture-items/{id}/claim`

Promove o item a `Bill` deste tenant. **Não cria `RoutingRule`** — o Aggregate foi abandonado na 2.6 ([doc 07](07-multitenancy-and-routing.md)); quem faz o próximo boleto do mesmo beneficiário rotear sozinho é o `Payee` cadastrado, pelo degrau 3. É o mecanismo que faz o sistema convergir: trabalho manual no primeiro boleto de cada conta recorrente, automático nos seguintes.

Recusas: `409` se o pagador extraído contradiz este tenant (`BLP.CPI04` — a escada já sabia que não era dele); `409` com aviso genérico se o mesmo instrumento já está sob gestão de outra conta (`BLP.BIL02`, unicidade global).

A `Bill` resultante nasce com `TenantRouting = Claimed`, com `UserId` e instante na evidência — aprovar um boleto reivindicado é decisão consciente, nunca caminho silencioso.

### UC-12c — Promover item não reconhecido

`POST /api/v1/{tenantId}/capture-items/{id}/promote` com a linha digitável informada à mão. Válvula de escape do parser e fonte de dados para melhorá-lo. Passa pela mesma escada de roteamento.

### UC-12d — Gerir regras de roteamento

`GET /api/v1/{tenantId}/routing-rules`, `DELETE /routing-rules/{id}`. Existe para desfazer uma reivindicação errada — apagar a regra faz os próximos boletos daquela conta voltarem para a quarentena.

---

## Fase 3 — Agendamento e pagamento

### UC-13 — Agendar e pagar

Sem endpoint próprio: consequência de UC-05. `BillApprovedDomainEvent` → handler cria `PaymentOrder` → `PaymentSchedulingService` calcula a data efetiva → `IBillPaymentGateway.ScheduleAsync` → `Bill.LinkPaymentOrder`.

### UC-14 — Receber notificação do provedor

`POST /api/v1/webhooks/asaas/bills` — sem `tenantId` na rota (o provedor não o conhece); resolve pela `externalReference`. Autenticado por token, idempotente por id de evento. Ver [`04-integrations.md`](04-integrations.md).

### UC-15 — Conciliar ordens pendentes

Job periódico que consulta no provedor as ordens paradas em `Pending`/`BankProcessing` além do prazo. Rede de segurança para webhook perdido.

### UC-16 — Consultar pagamento

`GET /api/v1/{tenantId}/payments/{id}` — status, datas pedida × efetiva, valor, taxa, motivos de falha, comprovante.

---

## Fase 4 — Histórico e relatórios

### UC-17 — Histórico de pagamentos

`GET /api/v1/{tenantId}/payments?from=&to=&payeeId=&status=&cursor=`

Uma linha por pagamento: data, beneficiário, valor, taxa, banco, quem aprovou, status, link do comprovante e link para o boleto de origem. É a trilha completa "de onde veio até quem autorizou".

### UC-18 — Relatório de pagamentos

`GET /api/v1/{tenantId}/reports/payments?from=&to=&groupBy=payee|month|status`

Conteúdo:

- **Total pago no período**, contagem de boletos, total de taxas do provedor.
- **Por beneficiário**: valor total, quantidade, ticket médio, variação contra o período anterior.
- **Previsto × realizado**: aprovados e agendados no período × efetivamente pagos.
- **Encargos evitáveis**: soma de juros e multa paga por boleto vencido — o número que justifica o produto.
- **Fila de exceção**: recusados, rejeitados por checagem e não pagos por falha, com motivo agregado.
- **Confiabilidade da captura**: itens ingeridos × reconhecidos × promovidos a Bill, por fonte.

`GET /reports/payments/export?format=csv` para exportação. Formato PDF fica para depois de o conteúdo estabilizar.

### UC-19 — Indicadores de acompanhamento

`GET /api/v1/{tenantId}/reports/summary` — a pagar nos próximos 7/30 dias, aguardando aprovação, vencendo hoje, rejeitados sem tratamento. Alimenta o painel inicial.

---

## O que não tem endpoint por decisão

- **Alterar o resultado de um check.** Check é fato apurado, não opinião editável. O aprovador aprova *apesar* do check, e essa decisão fica gravada com o motivo — o check permanece `Failed` para sempre no histórico.
- **Editar o valor de um Bill.** O valor vem da consulta oficial. Boleto com valor em aberto (`allowChangeValue`) é o único caso em que o valor é informado, e isso entra no `approve`, não num `PATCH`.
- **Apagar Bill ou pagamento.** Só `Cancel`. Histórico financeiro não é deletável.
- **Ver a fonte, os itens ou os boletos de outro tenant** — inclusive quando os dois monitoram a mesma caixa. Os únicos vazamentos autorizados são três avisos genéricos (fonte já monitorada, boleto já sob gestão de outra conta, item já reivindicado), que devolvem um booleano e nada mais.
- **Consultar se um endereço de e-mail já está cadastrado**, antes de concluir o OAuth daquele endereço. Seria um oráculo para enumerar clientes da plataforma.
- **Atribuir automaticamente ao dono da fonte** um boleto que não roteou. É exatamente o erro que a quarentena existe para impedir.
