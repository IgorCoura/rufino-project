# 04 — Integrações externas

Toda integração entra por uma **porta no Domain** (`BillPayment.Domain/Ports/`) com adapter na Infra. Nenhum DTO de provedor cruza a fronteira: o adapter traduz para VO do Domain. Resiliência (retry/timeout/circuit breaker) via Polly no `HttpClient` do adapter — entrou na sprint 1.3 como `Microsoft.Extensions.Http.Resilience` (Polly v8) + `Polly.Core` (ver seção "Dependências" no `CLAUDE.md`).

> ⚠️ **O cliente HTTP da consulta retenta; o do pagamento não pode.** Simular e decodificar são read-only e idempotentes, então repetir depois de um timeout é seguro e o `AddStandardResilienceHandler` está ligado neles. O adapter de pagamento da fase 3 precisa de um cliente **próprio, sem retry automático** — sobretudo o de Pix, cujo endpoint não documenta idempotência nenhuma (ver o aviso na seção Pix abaixo). Reaproveitar o cliente da consulta lá seria transformar uma retentativa de rede em pagamento duplicado.

**Falha de consulta é modelada, não lançada.** O adapter devolve `BillLookupResult`/`PixLookupResult` com um `LookupStatus`, e a distinção que interessa é entre `Unresolved` (o provedor respondeu que não conhece o documento — retentar dá o mesmo) e `Unavailable` (timeout, 5xx, circuito aberto, credencial ausente — nada foi aprendido sobre o documento). Colapsar os dois faria a verificação tratar indisponibilidade de rede como suspeita do boleto. A medição da 1.0 mostra que `Unresolved` é o caso comum, não o excepcional — fluxo comum não se modela com exceção.

**Sem chave configurada, os adapters são substituídos** por versões que devolvem `Unavailable("provider_not_configured")`. Isso deixa a aplicação subir e a suíte de integração rodar sem credencial — os testes não devem ter chave capaz de pagar contas — e, sobretudo, registra a ausência como *"não foi possível verificar"* em vez de check pulado com aparência de aprovado.

---

## Asaas — consulta e pagamento de contas

Provedor escolhido para **as duas coisas**: consultar o título e pagar. Racional e alternativas descartadas em [`adr/ADR-001-asaas-como-provedor.md`](adr/ADR-001-asaas-como-provedor.md).

- Base: `https://api.asaas.com/v3` (produção) / `https://api-sandbox.asaas.com/v3` (sandbox).
- Autenticação: header `access_token` com a chave da conta.
- **Uma subconta Asaas por tenant** (`POST /v3/accounts`), decidido em [`07-multitenancy-and-routing.md`](07-multitenancy-and-routing.md): a segregação de dinheiro entre clientes é garantida pelo provedor, não pelo nosso código, e saldo/extrato/taxas/comprovantes já saem separados. PF abre com CPF; PJ com CNPJ e `companyType`.
- A chave de cada subconta fica cifrada e é referenciada por `PayerProfile.AsaasAccountRef`. A chave da **conta-plataforma** (usada só para criar subcontas) é segredo de infraestrutura, separado. Nenhuma das duas em `appsettings.json` — ver [`adr/ADR-009`](adr/ADR-009-cofre-de-segredos.md).
- Onboarding: enquanto o cliente não conclui o cadastro/KYC no Asaas, `AsaasAccountRef` fica nulo e o tenant usa o sistema até `Approved`, mas não consegue agendar. Isso é estado do tenant, não erro.

### Consulta oficial — `POST /v3/bill/simulate` (fase 1)

Entrada: `identificationField` (linha digitável) ou `barCode`.

Resposta → mapeada para `LookupSnapshot`:

| Campo Asaas | Campo do VO | Uso |
|---|---|---|
| `bankSlipInfo.beneficiaryName` | `BeneficiaryName` | check `PayeeMatch` (secundário) |
| `bankSlipInfo.beneficiaryCpfCnpj` | `BeneficiaryTaxId` | check `PayeeMatch` (**principal**) |
| `bankSlipInfo.bank` | `BankCode` | check `ReceivingBankMatch` |
| `bankSlipInfo.value` | `Amount` | check `AmountMatch` |
| `bankSlipInfo.originalValue` | `OriginalAmount` | check `LookupConsistency` |
| `bankSlipInfo.dueDate` | `DueDate` | checks `LookupConsistency`, `DueDateSanity` |
| `bankSlipInfo.isOverdue` | `IsOverdue` | check `DueDateSanity` |
| `bankSlipInfo.minValue` / `maxValue` / `allowChangeValue` | idem | `AmountMatch` (pula quando valor é aberto) |
| `bankSlipInfo.interestValue` / `fineValue` / `discountValue` | evidência | mostra por que o valor mudou |
| `bankSlipInfo.companyName` | evidência | nome comercial do beneficiário |
| `minimumScheduleDate` | `MinimumScheduleDate` | agendamento e `DueDateSanity` |
| `fee` | `Fee` | custo Asaas, entra no relatório |

**Não existe campo de pagador** — consequência inteira em [`03-bill-validation.md`](03-bill-validation.md).

O simulate é chamada **read-only** e não move dinheiro — mas **exige a permissão de saque na chave de API** (403 `insufficient_permission` sem ela, medido em sandbox; o mesmo vale para `pix/qrCodes/decode`). Ou seja: a Fase 1 não movimenta dinheiro, porém roda com uma credencial que **pode** movimentar. Consequências de segurança em [`adr/ADR-001`](adr/ADR-001-asaas-como-provedor.md) → "Achado de campo".

### Pagamento — `POST /v3/bill` (fase 3)

Entrada: `identificationField` (obrigatório), `scheduleDate`, `description`, `value`, `dueDate`, `discount`, `interest`, `fine`, **`externalReference`**.

`externalReference` recebe o `PaymentOrderId` — é a chave de idempotência de ponta a ponta: retentativa após timeout consulta por essa referência antes de reenviar, evitando pagamento duplicado por falha de rede.

Regras de agendamento do provedor, que o `PaymentSchedulingService` precisa espelhar:

- Sem `scheduleDate` → paga no vencimento.
- Data em dia não útil → processa no próximo dia útil.
- Requisição **após as 14h** → processa no dia útil seguinte.
- Conta vencida → processa imediatamente, sem agendamento.
- `minimumScheduleDate` do simulate é o piso.

Status: `PENDING`, `BANK_PROCESSING`, `PAID`, `FAILED`, `CANCELLED`, `REFUNDED`, `AWAITING_CHECKOUT_RISK_ANALYSIS_REQUEST`. Mapeados para `PaymentOrderStatus`; `AWAITING_CHECKOUT_RISK_ANALYSIS_REQUEST` cai em `Pending` com motivo visível (é análise de risco do provedor, não falha).

### Pix — consulta e pagamento (trilho preferencial)

Decidido em [`adr/ADR-010`](adr/ADR-010-pix-preferido-sobre-boleto.md): havendo QR Pix, paga-se por Pix.

**`POST /v3/pix/qrCodes/decode`** — entrada: `payload` do BR Code, mais o opcional `expectedPaymentDate` (a instituição recalcula juros/multa/desconto para a data prevista — informe a data de agendamento pretendida, não a de hoje).

Resposta → `PixLookupSnapshot`:

| Campo Asaas | Uso |
|---|---|
| `name`, `tradingName` | evidência de `PayeeMatch` |
| **`cpfCnpj`** | check `PayeeMatch` (**principal**) — o equivalente Pix do `beneficiaryCpfCnpj` |
| `value` / `totalValue` | `AmountMatch` usa `totalValue` (já com encargos) |
| `interest` / `fine` / `discount` | evidência: explica por que o valor mudou |
| `dueDate` / `expirationDate` | `DueDateSanity`; `expirationDate` não tem equivalente no boleto |
| `canBePaidWithDifferentValue` | pula `AmountMatch` quando o valor é aberto |
| `changeValue` / `canBeModifyChangeValue` | Pix Troco — fora de escopo, mas registrar se presente |
| **`ispb` / `ispbName`** (em `receiver`) | **check `ReceivingBankMatch` no trilho Pix** — ver a ressalva de mapeamento abaixo |
| `personType` / `accountType` | evidência: PF×PJ do recebedor deve bater com o tipo do `Payee` |
| **`canBePaid` / `cannotBePaidReason`** | **porteira anterior a tudo**: QR que o provedor já sabe que não paga não deve consumir verificação nem chegar à aprovação |
| `type` (`STATIC`/`DYNAMIC`/…) | QR estático não tem valor nem vencimento — mais campos saem `Skipped` |
| `conciliationIdentifier` | conciliação com a ordem de pagamento |
| `payer.name` / `payer.cpfCnpj` (**mascarado**) | ver "o pagador aparece, parcialmente" abaixo |

**Ressalva do ISPB — resolvida.** O trilho Pix identifica a instituição por **ISPB (8 dígitos)**; o código de barras usa **COMPE (3 dígitos)**. `Payee.AcceptedBanks` guarda COMPE. A tradução vem da relação de participantes do STR do Bacen — ver "Banco Central" logo abaixo. ISPB sem código de três dígitos correspondente (instituição só de Pix) → check `Inconclusive`, nunca reprovado.

**O pagador aparece, parcialmente.** O decode devolve `payer.cpfCnpj` **mascarado**. Isso não revoga o [`adr/ADR-004`](adr/ADR-004-pagador-nao-autoritativo.md) — máscara não identifica ninguém e não serve para atribuir um documento a um tenant. Mas serve para o que o ADR-004 já autoriza: **contradizer**. Se os dígitos visíveis do pagador mascarado não podem pertencer ao `PayerProfile` do tenant, isso é evidência de contradição, e contradição bloqueia. Nunca o contrário: máscara compatível não confirma nada.

**`POST /v3/pix/qrCodes/pay`** — entrada: `qrCode: { payload, changeValue? }`, `value` (obrigatório), `description`, e **`scheduleDate`**. A resposta traz `endToEndIdentifier`, `scheduledDate`, `conciliationIdentifier`, `externalReference`, `chargedFeeValue`, status, `canBeCanceled` e `canBeRefunded`.

> ⚠️ **O endpoint de pagamento Pix não documenta mecanismo de idempotência.** `externalReference` é descrito como "campo livre para busca", não como chave de deduplicação — diferente do que o `POST /v3/bill` oferece. **Uma retentativa de rede pode pagar duas vezes.** Mitigação obrigatória na sprint 3.x: antes de qualquer retentativa, consultar por `externalReference` (= nosso `PaymentOrderId`) e só reenviar se nada voltar. Isso é dever do adapter, e sem ele o trilho Pix é mais arriscado que o de boleto apesar de ADR-010 preferi-lo.

Status: `AWAITING_BALANCE_VALIDATION`, `SCHEDULED`, `REQUESTED`, `DONE`, `REFUSED`, `CANCELLED`, entre outros (11 no total) — mapeados para `PaymentOrderStatus` no adapter.

**O agendamento existe e o cancelamento existe.** Isso é o que torna o Pix o trilho preferencial em vez de um atalho arriscado: o fluxo aprovar → agendar → pagar na data → poder cancelar antes é o mesmo dos dois lados.

### Banco Central — relação de participantes do STR (dado de referência)

Fonte da tabela de instituições: **`https://www.bcb.gov.br/pom/spb/estatistica/port/ParticipantesSTRport.csv`**, publicada no [Portal de Dados Abertos](https://dadosabertos.bcb.gov.br/dataset/lista-de-participantes-do-str). Colunas usadas: `ISPB`, `Nome_Reduzido`, `Número_Código` (o código de três dígitos, sucessor do COMPE) e `Participa_da_Compe`.

> A URL sob `content/estabilidadefinanceira/str1/` que aparece no portal responde **404**. A que serve o arquivo é a de `pom/spb/estatistica/port/`.

Estado medido em 2026-07-31: **357 instituições, 347 com código de três dígitos, 95 participando da Compe, zero códigos duplicados**. O código `000` não existe — o que sustenta o guard `BLP.DGL05` da `DigitableLine`.

**Snapshot embutido, não consulta ao vivo.** O arquivo é gerado por [`tools/fetch-bacen-participants.js`](tools/fetch-bacen-participants.js) e embarcado como `EmbeddedResource` na Infra. O motivo é o mesmo que rege o resto do BC: **esta tabela decide um check que autoriza pagamento**, e buscá-la em tempo de validação transformaria indisponibilidade do bcb.gov.br em bloqueio de pagamento. A tabela muda algumas vezes por ano; o arquivo versionado deixa cada mudança visível no diff, que é o comportamento desejado para um dado com esse peso.

Contrato no domínio: `IBankDirectory` (`Domain/Ports/`) — `IsKnown`, `ParticipatesInCompe`, `NameOf`, `FromIspb`. Síncrono e sem `CancellationToken` de propósito: não é I/O.

### Webhooks (fase 3)

Eventos: `BILL_CREATED`, `BILL_PENDING`, `BILL_BANK_PROCESSING`, `BILL_PAID`, `BILL_CANCELLED`, `BILL_FAILED`, `BILL_REFUNDED`.

Endpoint próprio, fora do padrão multi-tenant por rota (o Asaas não conhece nosso `tenantId`): resolve o tenant pela `externalReference`. Requisitos:

1. **Autenticação** do webhook por token de acesso configurado no Asaas, validado em constant-time.
2. **Idempotência**: o `id` do evento entra em `processed_event_log` antes do processamento.
3. **Fora de ordem**: `PaymentOrder.ApplyProviderStatus` é monotônica — não regride de `Paid`.
4. **Fallback por polling**: job periódico reconcilia ordens em `Pending`/`BankProcessing` há mais de N horas via `GET /v3/bill/{id}`. Webhook perdido não pode deixar ordem órfã.

### Saldo

O pague-contas do Asaas debita do **saldo da conta**. Consequências operacionais para a fase 3, a confirmar em sandbox antes da sprint 3.2:

- Verificar saldo antes de agendar e alertar quando insuficiente (falha de pagamento por saldo é falha operacional, não de domínio).
- Definir se cada tenant tem conta Asaas própria (subconta white-label) ou se há conta única com segregação lógica. **Recomendação: conta por tenant** — segregação de dinheiro entre clientes não deve depender de código nosso.

---

## Microsoft Graph — caixas de e-mail Microsoft 365 (fase 2)

- OAuth 2.0. Preferência por **client credentials com `Mail.Read` limitado por Application Access Policy** (sem sessão de usuário, sem refresh token expirando) em vez de authorization code por caixa.
- Sincronização por **delta query** (`/users/{id}/mailFolders/{folder}/messages/delta`); o `deltaLink` é o `SyncCursor` do `CaptureSource`.
- Anexos via `/messages/{id}/attachments`; filtrar por content-type PDF e por tamanho máximo.
- Alternativa a avaliar: subscription (webhook) do Graph para latência menor. Delta por polling é o suficiente para a fase 2 — contas a pagar não são tempo real.

## Gmail — sem integração (fase 2)

**Não há adapter Gmail.** A conta é pessoal (`@gmail.com`, sem Workspace), onde o escopo `gmail.readonly` exigiria verificação com avaliação CASA — semanas, custo e renovação anual para uma caixa.

As mensagens entram por **encaminhamento automático do Gmail para a caixa do Microsoft 365**, configurado uma vez. O `From:` original é preservado, então `OriginTrust` opera sobre o remetente verdadeiro. Passos de onboarding, não de código: ligar o encaminhamento, confirmar o código que chega no M365, e **adicionar o endereço Gmail aos remetentes seguros** (encaminhamento quebra SPF/DKIM).

Racional e o fallback IMAP sancionado em [`adr/ADR-006`](adr/ADR-006-captura-email-oauth.md).


## Extração do PDF (fase 2)

Porta `IBoletoDocumentParser`. Estratégia em camadas, da mais barata para a mais cara:

1. **Texto embutido** (PdfPig ou similar) — resolve a maioria dos boletos gerados digitalmente.
2. **Geração e validação de candidatos**: achatar o texto, extrair **todas** as janelas de 47 e 48 dígitos, validar DV em cada uma, aceitar só as que passam. Mesmo tratamento para CPF/CNPJ do pagador (rótulos `Pagador`, `Sacado`, `Cliente`, `Contribuinte`) — validar DV antes de tratar como identidade.
3. **Derivação de senha** quando o PDF vem cifrado — a senha costuma ser o documento fiscal do pagador, o que a torna também um sinal de roteamento ([`09-capture-channels.md`](09-capture-channels.md)).
4. **Extrator de visão** (`IDocumentIntelligence`) para PDF sem camada de texto ou com layout hostil. Substitui o OCR clássico como caminho principal de fallback: um modelo de visão lê boleto escaneado, torto e ruidoso sem uma regra por concessionária, e devolve de quebra o pagador e a referência de conta — que é o que o roteamento precisa. Tesseract permanece como alternativa auto-hospedada se a dependência externa se tornar indesejável.

**O fallback é caminho de primeira classe, não exótico**: 18% dos boletos reais do corpus não têm camada de texto nenhuma. E a validação de DV não é preciosismo: um boleto de telefonia do corpus renderiza a fonte do código de barras como texto e produziu 214 falsos CNPJs — e um falso positivo que passou até no DV. Detalhes e números em [`08-boleto-corpus-findings.md`](08-boleto-corpus-findings.md); a cascata completa em [`09-capture-channels.md`](09-capture-channels.md).

O parser **nunca** é fonte de verdade para valor, vencimento ou beneficiário — só para linha digitável (que a consulta oficial depois confirma) e para o pagador (que ninguém confirma, e que por isso entra no roteamento com as ressalvas do [`ADR-004`](adr/ADR-004-pagador-nao-autoritativo.md)).

**Métrica de regressão**: a taxa de extração sobre o corpus real é o indicador da sprint 2.4. Meta ≥ 90% com OCR ligado; queda entre versões é regressão e trava a entrega.

## Armazenamento de anexos (fase 2)

Porta `IAttachmentStorage` sobre S3-compatível — o BC `PeopleManagement` já usa Garage.io com o AWS SDK; reaproveitar a mesma infra, que também atende à premissa de software open source auto-hospedado. Chave: `{tenantId}/{yyyy}/{MM}/{captureItemId}.pdf`. O PDF original é evidência de auditoria e não é apagado enquanto a Bill existir.

**Numa fonte compartilhada, o mesmo anexo é gravado por tenant.** Cada `CaptureSource` ingere independentemente ([`adr/ADR-008`](adr/ADR-008-fontes-compartilhadas-e-isolamento.md)), então o prefixo por `tenantId` mantém o isolamento no storage também — não deduplique por hash de conteúdo entre tenants.

## Portais de fornecedor (fase 5)

Automação headless (Playwright) por conector. Deliberadamente a **última** fase: cada portal é um conector próprio, quebra sem aviso quando o site muda, e exige guardar credencial de acesso do cliente. Requisitos mínimos antes de começar: credenciais em cofre com rotação, um conector isolado por processo, detecção de quebra com alerta (conector que devolve zero boletos por N execuções seguidas é falha, não silêncio), e desativação automática do `CaptureSource` após falhas consecutivas.

## Segredos

Porta `ISecretVault` — **implementada na sprint 1.3** (`EnvelopeSecretVault`, tabela `tenant_secrets`). Decisão em [`adr/ADR-009`](adr/ADR-009-cofre-de-segredos.md): **sem cofre dedicado por enquanto.**

Duas escolhas de implementação que valem registro:

- **O cofre não commita.** As escritas registram a mudança no `DbContext` de quem chamou; o efeito só existe no `SaveEntitiesAsync` do handler. É isso que torna atômico "guardar a credencial" e "criar o agregado que a referencia" — sem isso, uma falha no meio deixaria credencial órfã no cofre ou agregado apontando para o vazio. `ResolveAsync` usa `FindAsync` justamente para enxergar a linha recém-adicionada antes do commit.
- **`TenantId` e `SecretKind` entram no dado autenticado (AAD).** Não são só colunas de busca: mover a linha para outro tenant, ou reapresentá-la como outro tipo de segredo, faz a decifragem **falhar** em vez de devolver o segredo. Coberto por teste que adultera a linha via SQL.

Sem master key configurada, entra um cofre que **falha em todas as operações, inclusive nas de leitura** — a alternativa (guardar em claro, ou devolver vazio) trocaria uma falha barulhenta no primeiro uso por um vazamento silencioso. A Fase 1 sobe sem chave porque não guarda credencial de tenant; **a partir da fase 2 a master key é pré-requisito de deploy**.

- **Segredos de infraestrutura** (senha do Postgres, chave da conta-plataforma Asaas, token do webhook, chave da API Claude, master key) em **variáveis de ambiente** — Dokploy em produção, `dotnet user-secrets` / `secrets.json` em desenvolvimento e testes. Nada em `appsettings.json`, nada versionado.
- **Segredos por tenant** (tokens OAuth de Graph/Gmail, chave da subconta Asaas, credenciais de portais, senhas de PDF aprendidas) continuam cifrados no próprio Postgres por **envelope encryption**: DEK por segredo, `AES-256-GCM` (`System.Security.Cryptography.AesGcm`, built-in), DEK envelopado pela master key, que vem da variável de ambiente e só existe em memória.
- **Cópia da master key cifrada fora do host** — não é opcional. Perdê-la é reconectar todas as caixas e reemitir todas as chaves de subconta.
- Trocar para um cofre (Infisical self-hosted é a escolha registrada) depois é mudança de **uma linha de configuração**: a camada por tenant não muda.
- **Descartados**: HashiCorp Vault (BUSL-1.1, não é open source desde 2023) e qualquer secrets manager de nuvem.

## Extração por LLM

Porta `IDocumentIntelligence` — Claude API, usada como **fallback do parser determinístico**, nunca no lugar dele. Contrato, schema, escolha de modelo, custo por boleto e Batch API em [`10-llm-extraction.md`](10-llm-extraction.md); a regra de fronteira em [`adr/ADR-011`](adr/ADR-011-llm-propoe-codigo-dispoe.md).

É a **única dependência externa paga da stack**, contra a premissa de só software open source — decisão consciente, custo abaixo de um dólar por mês no volume real medido.

## Busca de links de e-mail

Porta `ILinkFetcher`. Muitas faturas digitais chegam como link em vez de anexo. É a maior superfície de ataque nova do sistema; os controles obrigatórios (allowlist de domínio, anti-SSRF com revalidação pós-redirect, egresso isolado sem rota para serviços internos, só `GET`, verificação de bytes mágicos, teto de tamanho e tempo, renderização em container descartável) estão em [`09-capture-channels.md`](09-capture-channels.md).
