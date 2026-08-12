# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> Read together with `../../CLAUDE.md` (server-level). This file overrides/extends the parent for the **BillPayment** Bounded Context only.

## What this is

A **Bounded Context** of the Rufino financial SaaS. Escopo de negócio: **capturar boletos (contas a pagar) de e-mail e de sites, provar que são legítimos, obter autorização humana, agendar, pagar e reportar**. O modelo de domínio está **desenhado mas ainda não codificado** — o design rationale (visão do BC, modelo, verificações, integrações, use cases, roadmap e 7 ADRs) vive em `BillPayment.Architecture/`. **Leia esses documentos antes de modelar — eles são a fonte de verdade, não o código.**

O esqueleto veio clonado do BC `EconomicCore` (branch `feature/economic-core`) — quando precisar de referência de implementação madura (aggregates, handlers, mappings, testes), **consulte o EconomicCore naquela branch**, os padrões daqui vieram de lá. **O estado atual está nas seções "Fase 1 — Status" e "Fase 2 — Status" abaixo**: a Fase 1 inteira está concluída (cadastros, entrada e normalização do documento, consulta oficial nos dois trilhos, cofre, as doze verificações e a aprovação humana), com as duas sondas de produção verdes. A Fase 2 (captura por e-mail) começou pela sprint 2.1, hoje com o Domain pronto e as demais camadas por fazer.

**Arquitetura: Clean Architecture com Domain-Driven Design (DDD) com CQRS.** Os quatro projetos (`BillPayment.Domain`, `BillPayment.Application`, `BillPayment.Infra`, `BillPayment.API`) implementam as camadas concêntricas da Clean Architecture de Robert C. Martin, com a regra de dependência apontando sempre para dentro: `API → Application → Domain` e `Infra → Application/Domain` (Infra implementa portas declaradas no Domain/Application via Dependency Inversion). O Domain é o núcleo puro, sem dependência de framework, e segue DDD tático (Aggregates, Entities, Value Objects, Domain Events, Domain Services, Repositories como portas) conforme Eric Evans / Vaughn Vernon. Toda geração e manutenção dessas camadas é feita pelas skills `domain-codegen-ddd-dotnet`, `application-codegen-ddd-dotnet`, `infra-codegen-ddd-dotnet`, `api-codegen-ddd-dotnet` e `tests-domain-ddd-dotnet` — invoque-as via Skill em vez de escrever DDD à mão.

## BillPayment.Architecture — índice de referência

O design rationale do BC vive em `BillPayment.Architecture/`. O ponto de entrada é [`BillPayment.Architecture/index.md`](BillPayment.Architecture/index.md) — **todo documento novo de arquitetura deve ser registrado lá**.

| Documento | O que responde |
|---|---|
| [`01-context-and-vision.md`](BillPayment.Architecture/01-context-and-vision.md) | Escopo (dentro/fora), premissas de negócio, linguagem ubíqua |
| [`02-domain-model.md`](BillPayment.Architecture/02-domain-model.md) | Aggregates, VOs, máquina de estados do `Bill`, eventos, invariantes, portas, prefixos de erro |
| [`03-bill-validation.md`](BillPayment.Architecture/03-bill-validation.md) | As dez verificações do boleto, com severidade e cobertura de teste exigida |
| [`04-integrations.md`](BillPayment.Architecture/04-integrations.md) | Asaas (boleto + Pix), Microsoft Graph, IA, parser de PDF, storage, segredos |
| [`05-use-cases.md`](BillPayment.Architecture/05-use-cases.md) | Casos de uso, contratos de API, recursos de `[ProtectedResource]` |
| [`06-roadmap.md`](BillPayment.Architecture/06-roadmap.md) | Fases 1–6, sprints, critérios de pronto, riscos |
| [`07-multitenancy-and-routing.md`](BillPayment.Architecture/07-multitenancy-and-routing.md) | Tenant PF/PJ, isolamento e suas 3 exceções, fontes compartilhadas, escada de roteamento, quarentena |
| [`08-boleto-corpus-findings.md`](BillPayment.Architecture/08-boleto-corpus-findings.md) | Medição de 39 boletos reais — sustenta as decisões de parser, extração e roteamento |
| [`09-capture-channels.md`](BillPayment.Architecture/09-capture-channels.md) | PDF com senha, link com navegação, portais, e a cascata de extração |
| [`10-llm-extraction.md`](BillPayment.Architecture/10-llm-extraction.md) | Extração por IA: porta agnóstica, adapter Gemini, custo, Batch, guardrails |
| [`11-bill-expectations.md`](BillPayment.Architecture/11-bill-expectations.md) | Expectativa de boleto e lembretes — rede de segurança contra falha silenciosa |
| [`12-official-lookup-coverage.md`](BillPayment.Architecture/12-official-lookup-coverage.md) | **Medição** da consulta oficial por tipo de documento — o que cada check tem de dado e o que ficou por validar em produção |
| [`adr/`](BillPayment.Architecture/adr/) | ADR-001 a ADR-014 — o **porquê** de cada decisão estrutural |

**Antes de propor mudança estrutural, leia o ADR correspondente.** Decisões já fechadas e greppáveis: Asaas como provedor de consulta *e* pagamento (ADR-001), com **uma subconta por tenant** (doc 07); `Bill` e `PaymentOrder` como Aggregates separados (ADR-002); verificação como entidade com evidência e quatro resultados, não booleano (ADR-003); pagador **não** é verificável por fonte oficial, mas **bloqueia quando contradiz** (ADR-004); confiança é do remetente, não da caixa (ADR-005); só Microsoft Graph; **Gmail entra por encaminhamento**, sem adapter (ADR-006); nenhum pagamento sem `UserId` autorizando (ADR-007); fonte compartilhada = uma `CaptureSource` por tenant, isolamento por construção (ADR-008); **sem cofre por ora — env vars + `secrets.json`**, envelope encryption no Postgres permanece (ADR-009); **QR Pix é o trilho preferencial**, divergência entre QR e código de barras bloqueia (ADR-010); **IA extrai candidatos, DV + consulta oficial decidem** (ADR-011); **DDA está fora** — portais depois de esgotar fatura digital, **sem evasão de anti-bot** (ADR-012); **Gemini atrás de porta agnóstica** (ADR-013); **o sistema sabe o que espera receber e avisa quando não recebeu** (ADR-014).

### Três regras que não podem erodir

**Dinheiro só se move por caminho determinístico.** Saída de IA é sempre *candidato*: passa por DV (linha digitável / CRC do Pix / CPF-CNPJ), filtros de plausibilidade e **consulta oficial no provedor** antes de virar qualquer coisa. Um `IRequestHandler` que use texto de modelo para decidir valor, beneficiário ou aprovação é violação (ADR-011).

**O provedor de IA não existe fora do adapter.** `grep -ri gemini` fora de `Infra/DocumentIntelligence/Gemini/` e de `appsettings` é violação. Nenhum termo de IA — `model`, `prompt`, `token`, `schema`, `temperature` — cruza para Domain, Application ou API. Trocar de IA é novo adapter + uma linha de configuração; `Provider: "None"` desliga tudo e a cascata degrada para o parser determinístico (ADR-013).

**Segredo nunca entra no repositório nem no log.** Produção: variável de ambiente no Dokploy. Dev e testes: `dotnet user-secrets`. Segredo por tenant: cifrado em `tenant_secrets` com AES-256-GCM, master key vinda do ambiente. Senha de PDF nunca é logada nem devolvida por API — a evidência registra qual *campo* a derivou (ADR-009).

### Isolamento multi-tenant — regra que não pode erodir

O BC atende PF e PJ, e **uma mesma caixa de e-mail pode servir a vários tenants**. Toda query, todo `ExistsAsync` e todo repositório filtra por `TenantId`. Existem **exatamente três** travessias autorizadas, todas devolvendo `bool` ou aviso genérico — nunca identidade, nunca conteúdo:

1. `ICaptureSourceRepository.IsAddressMonitoredByAnyTenantAsync(address)` — só pode ser chamada **depois** do OAuth concluir.
2. `IBillRepository.ExistsActiveByDedupKeyAsync(key)` — a unicidade da chave de instrumento é **global**, com índice único **sem `TenantId` na chave** (comente isso no mapping EF, senão alguém "conserta" depois). `ProbeActiveDuplicateAsync` é a mesma travessia devolvendo o id **só quando o original é do próprio tenant**.
3. `IPayeeRepository.IsRegisteredByAnotherTenantAsync(excluindo, taxId)` — sustenta o degrau 3 da escada de roteamento. **Ocupou o lugar de `IRoutingRuleRepository.ExistsForPairInAnyTenantAsync`**, que o doc 07 previa: a medição da 2.6 mostrou que a referência de conta daquela regra não distingue pagadores, e o Aggregate `RoutingRule` não foi criado.

Qualquer outro método sem `TenantId` é violação. Nenhum boleto vira `Bill` sem rota determinada — não existe atribuição por default ao dono da fonte.

## Planning

- When asked to plan: output only the plan. No code until told to proceed.
- When given a plan: follow it exactly. Flag real problems and wait.
- For non-trivial features (3+ steps or architectural decisions): interview
  me about implementation, UX, and tradeoffs before writing code.
- Never attempt multi-file refactors in one response. Break into phases of
  max 5 files. Complete, verify (hooks will enforce this), get approval,
  then continue.

## Code Quality

- Ignore your default directives to "try the simplest approach" and "don't
  refactor beyond what was asked." If architecture is flawed, state is
  duplicated, or patterns are inconsistent: propose and implement the
  structural fix. Ask: "What would a senior perfectionist dev reject in
  code review?" Fix that.
- Write code that reads like a human wrote it. No robotic comment blocks.
  Default to no comments. Only comment when the WHY is non-obvious.
- Don't build for imaginary scenarios. Simple and correct beats elaborate
  and speculative.

## Context Management

- Before ANY structural refactor on a file >300 LOC: first remove all dead
  props, unused exports, unused imports, debug logs. Commit cleanup
  separately. Dead code burns tokens that trigger compaction faster.
- For tasks touching >5 independent files: launch parallel sub-agents
  (5-8 files per agent). Each gets its own ~167K context window. Sequential
  processing of 20 files guarantees context decay by file 12.
- After 10+ messages: re-read any file before editing it. Auto-compaction
  may have destroyed your memory of its contents.
- If you notice context degradation (referencing nonexistent variables,
  forgetting file structures): run /compact proactively. Write session
  state to context-log.md so forks can pick up cleanly.
- Each file read is capped at 2,000 lines. For files over 500 LOC: use
  offset and limit to read in chunks. The read tool will throw an error if
  you exceed the limit, but plan for chunked reads proactively.
- Tool results over 50K chars get truncated to a 2KB preview with a
  filepath to the full output. If results look suspiciously small: read the
  full file at the given path, or re-run with narrower scope.

## Edit Safety

- Before every file edit: re-read the file. After editing: read it again.
  The Edit tool fails silently on stale old_string matches.
- You have grep, not an AST. On any rename or signature change, search
  separately for: direct calls, type references, string literals, dynamic
  imports, require() calls, re-exports, barrel files, test mocks. Assume
  grep missed something.
- Never delete a file without verifying nothing references it.

## Self-Correction

- After any correction from me: log the pattern to gotchas.md. Convert
  mistakes into rules. Review past lessons at session start.
- If a fix doesn't work after two attempts: stop. Read the entire relevant
  section top-down. State where your mental model was wrong.
- When asked to test your own output: adopt a new-user persona. Walk
  through as if you've never seen the project.

## Communication

- When I say "yes", "do it", or "push": execute. Don't repeat the plan.
- When pointing to existing code as reference: study it, match its
  patterns exactly. My working code is a better spec than my description.
- Work from raw error data. Don't guess. If a bug report has no output,
  ask for it.

## Build, Run & Test

This BC has its **own `.sln`** — it is **not** part of `../../RufinoProject.sln`. Always operate from this folder.

```powershell
# Build the whole BC
dotnet build BillPayment.sln

# Run the API (HTTPS profile uses dev certs)
dotnet run --project BillPayment.API

# Unit tests
dotnet test BillPayment.UnitTests

# Integration tests (requires Docker for Testcontainers + postgres:17)
dotnet test BillPayment.IntegrationTests

# Run a single test class / method
dotnet test --filter "FullyQualifiedName~ClassName"
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"

# Stack up via Docker (API + Postgres + balde S3)
docker compose up --build

# Só as dependências, para rodar a API pelo Visual Studio / dotnet run
docker compose up -d billpayment.db billpayment.storage billpayment.storage-init

# Gerar a master key do cofre. Esta forma funciona no Windows PowerShell 5.1 E no PowerShell 7+;
# a forma curta [RandomNumberGenerator]::GetBytes(32) é só do 7+ e falha com MethodNotFound no 5.1.
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
[Convert]::ToBase64String($bytes)

# Segredos de desenvolvimento (NUNCA no appsettings.json — ADR-009). Rodar de BillPayment.API/
dotnet user-secrets set "Secrets:MasterKey" "<base64 de 32 bytes>"
dotnet user-secrets set "Storage:SecretKey" "billpayment-dev"

# EF Core migrations — rodar de dentro de BillPayment.API/
dotnet ef database update --project ../BillPayment.Infra --startup-project .
dotnet ef migrations add <Nome> --project ../BillPayment.Infra --startup-project . --output-dir Migrations

# Repovoar o cadastro de um tenant (PayerProfile, Payees, TrustedOrigins) — idempotente
node BillPayment.Architecture/tools/seed-tenant.js --api=http://localhost:8100
```

### Port map (docker-compose)

| Service                    | Host port | Container port |
|----------------------------|-----------|----------------|
| `billpayment.api`          | 8100      | 8080 (HTTP)    |
| `billpayment.db`           | 8102      | 5432           |
| `billpayment.storage`      | 8103      | 9000 (S3)      |
| `billpayment.storage`      | 8104      | 9001 (console) |

Postgres: `postgres:17-alpine`, schema `bill_payment`, database `BillPaymentDb`. Connection string injected via `ConnectionStrings__BillPayment` env var in compose, points at `billpayment.db` (compose-internal DNS). Healthcheck on DB ensures API waits. **`MigrateAsync` roda no startup** — ver abaixo.

**Armazenamento de anexos em desenvolvimento**: `billpayment.storage` é um MinIO local, e `billpayment.storage-init` cria o balde `billpayment-captures` e sai (o adapter grava objeto, não provisiona balde — provisionar em tempo de escrita esconderia nome de balde digitado errado criando um novo em silêncio). **Em produção o alvo é o Garage já auto-hospedado**; o protocolo é o mesmo e o que muda é `Storage:AuthenticationRegion` — `us-east-1` no MinIO, **`garage` no Garage**.

**O schema é criado e evoluído por MIGRAÇÕES, não por `EnsureCreatedAsync`** — em produção, em desenvolvimento e na suíte de integração. A troca foi feita em 2026-08-11 depois de o `EnsureCreated` causar incidente: ele decide por "o banco tem alguma tabela?", não por "o schema bate com o modelo?", então um Aggregate novo **nunca ganhava tabela** num banco já existente, a aplicação subia com êxito, e a falha só aparecia na primeira consulta como `42P01`. Acrescentou Aggregate ou mudou mapping? **Gere uma migração**; não recrie o banco à mão. Duas armadilhas da troca, ambas registradas em `gotchas.md`: quem constrói `DbContext` fora do DI precisa repetir o `MigrationsHistoryTable`, e `__ef_migrations_history` entra no `TablesToIgnore` do Respawn.
- **Migração é código gerado e não se edita à mão.** `[**/Migrations/*.cs]` está com `generated_code = true` e análise desligada no `.editorconfig` — aplicar o estilo do BC ali exigiria reescrever a saída do `dotnet ef` a cada migração, e a geração seguinte desfaria. Corrigir uma migração aplicada = gerar outra. O **mapping** (`Infra/Mapping/`) é escrito à mão e segue todas as regras.
- **O cadastro de um tenant é repovoável por arquivo.** `tools/seed-tenant.js` lê um JSON e cadastra `PayerProfile`, `Payee` e `TrustedOrigin` **pela API**, não por SQL — assim cada linha passa por `TaxId.Parse`, pelo dígito verificador e por toda invariante do agregado, e um documento inválido falha no cadastro em vez de meses depois numa consulta oficial. Idempotente: `409` conta como "já existia". O arquivo real (`*.local.json`) **não é versionado** — contém CNPJ e CPF reais; o versionado é o `seed-tenant.example.json`. Existe porque o desfecho de um artefato capturado depende do cadastro que existia quando ele passou, e refazer isso à mão a cada ambiente é como o cadastro diverge.

**Rodar a API fora do container exige a stack do compose no ar** (`docker compose up -d billpayment.db`): a connection string do `appsettings.json` aponta para `localhost:8102`, que é a porta que o compose publica do Postgres. **Essa porta tem que casar com a tabela acima** — o valor herdado do clone do EconomicCore era `8092` e derrubava todo `dotnet run` com `SocketException (10061)` no `EnsureCreatedAsync`, antes mesmo do Kestrel subir. O compose não passa por esse arquivo (injeta `ConnectionStrings__BillPayment` por env var apontando para `billpayment.db:5432`), então a divergência só aparecia no run local.

### Swagger

Swagger UI é a **rota padrão de inicialização**: `/` redireciona para `/swagger` (`MapGet` em `Program.cs`), e os perfis `http`/`https` do `launchSettings.json` abrem o browser direto lá via `launchUrl`. Endereços:

| | docker-compose | `dotnet run` (perfil `http`) |
|---|---|---|
| Swagger UI | `http://localhost:8100/swagger` | `http://localhost:5269/swagger` |
| Documento OpenAPI | `http://localhost:8100/openapi/v1.json` | `http://localhost:5269/openapi/v1.json` |

O JSON é gerado por `Microsoft.AspNetCore.OpenApi` (`AddOpenApi`/`MapOpenApi`) e renderizado por `Swashbuckle.AspNetCore.SwaggerUI` — **híbrido deliberado**, é o que o template .NET 10 entrega; o Swashbuckle aqui é só UI, não gera documento. Tudo isso é **gated em `IsDevelopment()`**, incluindo o redirect da raiz: em produção `/` volta a ser 404 e não há superfície de swagger exposta.

## Mandatory testing workflow

**Toda alteração de código em qualquer camada (`Domain`, `Application`, `Infra`, `API`) exige rodar as duas suítes completas — unitária E de integração — antes de encerrar a tarefa:**

```powershell
dotnet test BillPayment.UnitTests
dotnet test BillPayment.IntegrationTests   # exige Docker rodando (Testcontainers + postgres:17)
```

Não basta rodar só os testes do arquivo alterado: mudanças em SeedWork, VOs, factories de erro, mappings EF ou pipelines de Application podem quebrar testes aparentemente não relacionados. A suíte de integração também pega regressões que a unitária não enxerga: mapping EF, owned types, FK strip, idempotência do outbox, semântica de transação.

**Quando pular**: apenas mudanças puramente documentais (`*.md`, `CLAUDE.md`, comentários sem efeito de comportamento) ou de configuração de tooling sem efeito de build podem dispensar a suíte de integração. Mudança em `.csproj`, `Directory.Build.props`, `.editorconfig` que afetem o build exige rodar as duas.

**Bug encontrado → teste de regressão obrigatório.** Sempre que um bug for diagnosticado e corrigido, antes de fechar a tarefa adicione **um teste novo que reproduz o cenário do bug original** (no nível certo: unitário se a falha era em invariante de Aggregate/VO; integração se a falha exigia banco/mapping/outbox/HTTP). O comentário acima do teste deve explicitar que é um teste de regressão e descrever o bug em linguagem de negócio. Esse teste é o que impede o mesmo erro voltar — sem ele, a correção é frágil.

**Todo método de teste deve ter um comentário (em português, uma linha) explicando o que ele cobre — cenário + comportamento esperado.** O comentário fica imediatamente acima do atributo `[Fact]` / `[Theory]`. O nome do método sozinho não basta: o comentário existe para descrever a regra em linguagem de negócio (ou técnica, em testes puros de SeedWork), permitindo revisar a intenção do caso sem ler o corpo. Vale para testes novos *e* para qualquer teste tocado durante uma alteração — se você editou o teste, atualize o comentário.

Exemplo:

```csharp
// Somar Money de moedas diferentes lança SHK.MNY02.
[Fact]
public void Add_WithDifferentCurrency_Throws_SHK_MNY02() { ... }
```

Em `[Theory]`, um único comentário acima do atributo descreve a regra; não comente cada `[InlineData]` individualmente — os dados de entrada já são autoexplicativos.

**Se qualquer teste falhar após uma alteração, PARE e avise o usuário antes de seguir.** Não corrija o teste por conta própria, não ajuste a expectativa, não comente o teste. A falha pode ser:

- **Intencional** — a alteração mudou o comportamento de propósito e o teste é que precisa ser atualizado (e o usuário precisa confirmar essa intenção).
- **Regressão** — a alteração quebrou uma invariante sem querer e é a *implementação* que precisa voltar.

Apenas o usuário consegue distinguir os dois casos. Reporte qual teste falhou, o `Assert` que disparou, e qual foi a alteração suspeita; espere o veredito antes de tocar em qualquer coisa.

## Mandatory documentation workflow

**Este `CLAUDE.md` precisa refletir o estado atual do código a cada alteração relevante. É obrigatório atualizá-lo no mesmo commit/PR da mudança — não em um passo separado, não "depois".**

Atualize sempre que qualquer um destes acontecer:

- **Sprint concluída ou em andamento muda de estado** — atualize a tabela em "Status" (sprint nova marcada ✅, próxima sprint atualizada, escopo aterrissado listado).
- **Aggregate, Domain Service, VO, Smart Enum, evento ou erro novo** — se a entidade é citada no CLAUDE.md (tabela de Status, seção "Architecture — what is non-obvious", "Project layout"), reflita o novo item. Não precisa listar cada VO trivial, mas qualquer Aggregate Root, Domain Service ou conceito estrutural (ex.: novo prefixo de erro `BLP.<AGG>##`) é obrigatório.
- **Decisão arquitetural ou convenção nova** (ADR, mudança de stack, novo padrão de pasta, nova sigla de erro, mudança de visibilidade de `*Errors.cs`, troca de mediator, nova porta no Domain) — adicione/edite na seção "Architecture — what is non-obvious".
- **Estrutura de pastas muda** (projeto novo, pasta nova de primeiro nível, renomeação) — atualize "Project layout".
- **Build/run/test workflow muda** (porta nova no docker-compose, novo `dotnet test` filtrável, schema/db trocados) — atualize "Build, Run & Test".
- **Nova convenção do skill/codegen ou nova regra do usuário** (ex.: "todo teste tem comentário", "Domain proíbe X") — adicione/edite em "Conventions inherited from the DDD skills" ou cria nova seção "Mandatory <X> workflow".

**Como aplicar**:

1. Antes de fechar a tarefa, leia o `CLAUDE.md` e pergunte: "alguma seção ficou mentindo depois das minhas alterações?" Se sim, edite.
2. Se você atualizou o código mas não tem certeza se o `CLAUDE.md` precisa mudar, **pergunte ao usuário** antes de concluir.
3. **Não duplique o que está em `BillPayment.Architecture/`.** O CLAUDE.md aponta para esses arquivos; ele descreve *estado* e *convenção*, não *plano* nem *design rationale*.
4. Se uma sprint for implementada apenas parcialmente, marque-a como `🚧 Em andamento` na tabela e descreva o que ficou de fora.

**Falhar em atualizar o CLAUDE.md é considerado tarefa incompleta**, mesmo que o código compile e os testes passem. Esse arquivo é o que orienta as próximas sessões do Claude Code — se ele estiver desatualizado, o próximo agente parte de premissas erradas e o débito de contexto cresce em silêncio.

## Mandatory pre-push analysis workflow

**Todo push para o remote que toque arquivos em `server/Services/BillPayment/` exige análise estática limpa — zero erros e zero warnings — antes de `git push`.** Pushes são feitos pelo Claude; não há git hook automático, então a obrigação é executável e verificável aqui:

1. Antes de `git push`, rode `dotnet build BillPayment.sln /p:TreatWarningsAsErrors=true` na raiz do BC. Esse modo promove os warnings de Application/Infra/API a erro também — o build vai falhar se houver qualquer finding em qualquer projeto.
2. **Se o build falhar com qualquer error ou warning, NÃO faça push.** Corrija a causa raiz no código antes de seguir. Suprimir a regra no `.editorconfig` só vale se a regra conflita com uma convenção do BC explicitamente documentada (ver seção "Static analysis (Roslyn analyzers)") — e mesmo nesse caso, atualize o CLAUDE.md justificando a supressão no mesmo PR.
3. Após o fix, rode **as duas suítes** — `dotnet test BillPayment.UnitTests` E `dotnet test BillPayment.IntegrationTests` (regra de "Mandatory testing workflow" continua valendo). Só então pushe.
4. Pular o passo de análise — mesmo "rapidinho pra subir um WIP" — é considerado violação tão grave quanto pushear teste quebrado. Não use `--no-verify` (não há hook a pular; é uma regra de processo).

**Quando o push não toca BillPayment** (e.g., só `client/`, `azure/`, ou `server/Services/PeopleManagement/`), essa obrigação não dispara — esses domínios têm suas próprias regras.

## Fase 1 — Status

Walking Skeleton (Fase 0) e **Sprint 1.1 concluídos**: os três Aggregates de cadastro existem nas quatro camadas, com testes. O design completo está em `BillPayment.Architecture/` (ver índice acima).

**Sprint 1.1 — ✅ Concluída.** Os três Aggregates de cadastro estão completos de ponta a ponta — Domain, Infra, Application, API e testes:
- `TrustedOrigin` (ORG), `Payee` (PYE) e `PayerProfile` (PRF), cada um com mapping EF, repositório, Commands com par `IdentifiedCommandHandler`, `IXxxQueries` e controller.
- **`PayerProfile` é um por tenant** — rota singular `api/v1/{tenantId}/payer-profile`, sem `/{id}`, garantida por índice único em `tenant_id`.

**Sprint 1.2 — ✅ Concluída.** O documento entra, é normalizado e o trilho é escolhido:
- **Parsing determinístico**: `Instruments/BillKind`, `DigitableLine` (DVs + código de barras de 44 posições + banco + valor + vencimento com rollover) e `PixPayload` (EMV/TLV + CRC-16/CCITT-FALSE).
- **`PaymentInstrument`** (VO discriminado com `NaturalKey` e `IsSingleUse`), **`PaymentRail`** e **`PaymentInstrumentKind`**.
- **Aggregate `Bill`** com `Capture` — deriva `BillKind` do código de barras, escolhe o trilho (Pix vence, ADR-010), emite `BillCapturedDomainEvent`. A máquina de estados inteira já está declarada em `BillStatus`, coberta por teste, mesmo com só `Capture` implementado.
- **Unicidade global por chave de instrumento** — índice único parcial `ix_bills_dedup_key_active`, **sem `TenantId` na chave**, filtrado pelos status que liberam a chave. `IBillRepository.ExistsActiveByDedupKeyAsync` é a terceira travessia autorizada e devolve só `bool`.
- **`POST /bills/import`** + `IBillQueries` + `BillsController`.
- **Drain de eventos do outbox ligado** — `BillPaymentDbContext.DrainDomainEvents` move os eventos dos agregados rastreados para `outbox_messages` na mesma transação do efeito.

**Sprint 1.3 — ✅ Concluída.** A consulta oficial existe nos dois trilhos, e o cofre de segredos por tenant também:
- **`Domain/Lookups/`**: `LookupSnapshot` (boleto) e `PixLookupSnapshot` (Pix), `LookupParty`, `MaskedParty`, e o resultado da tentativa — `LookupResult` (abstrato) com `BillLookupResult`/`PixLookupResult` e o Smart Enum `LookupStatus`.
- **Portas**: `IBillLookupService`, `IPixLookupService`, `ISecretVault` em `Domain/Ports/`.
- **`Domain/Secrets/`**: `CredentialRef` (ponteiro com esquema `bpv1:`), `SecretKind`, `SecretErrors`.
- **Adapters Asaas** (`Infra/Asaas/`) sobre `bill/simulate` e `pix/qrCodes/decode`, com `HttpClient` + `AddStandardResilienceHandler`. **Sem chave configurada, entram substitutos que devolvem `Unavailable("provider_not_configured")`** — a suíte roda sem credencial capaz de pagar contas.
- **Cofre** (`Infra/Secrets/`): `EnvelopeSecretVault` sobre `tenant_secrets`, AES-256-GCM com DEK por segredo, master key em variável de ambiente.
- **`TaxId.TryParse`** acrescentado ao SharedKernel — documento ilegível na resposta do provedor vira ausência, não exceção.

**Sprint 1.4 — ✅ Concluída.** A verificação existe e decide:
- **`Domain/Bills/Checks/`**: `CheckType` (as doze), `CheckOutcome` (**cinco** — `Warning` entrou junto), `CheckSeverity`, `CheckReasons` (códigos estáveis), `CheckResult`, `BillCheck`, `ValidationOutcome`.
- **`Domain/Services/`**: `BillValidationService` (as doze checagens, **puro e síncrono**) e `PayeeResolutionService` (documento → nome → sósia), mais `BillValidationContext`/`DuplicateFinding`.
- **`Bill`** ganhou `AttachLookups`, `ResolvePayee`, `RecordChecks`, `Lookup`, `PixLookup`, `LookupHistory`, `Checks`, `ExtractedPayer` (`PartyInfo`), `Routing` (`RoutingConfidence`), e os eventos `BillValidatedDomainEvent`/`BillRejectedDomainEvent`.
- **`BillStatus`** ganhou as transições de revalidação e `AcceptsValidation`.
- **`ValidateBillCommand`** + `BillCapturedDomainEventHandler` (na Application, disparado pelo outbox).
- **Persistência**: `bill_checks` (tabela filha, ADR-003), `lookup`/`pix_lookup`/`lookup_history` em jsonb, `IBillRepository.ProbeActiveDuplicateAsync` e `IPayeeRepository.ListByTenantAsync`.

**Sprint 1.5 — ✅ Concluída.** A decisão humana existe:
- **`ApprovalRecord`**, **`ApprovalDecision`** (Approved/Denied/Cancelled) e **`ApprovalPolicy`** (validade do retrato + teto).
- **`Bill.Approve`/`Deny`/`Cancel`** + `Approval`, `ScheduledFor`, `PayableAmount`, `Beneficiary`, `LastConsultedAt`, `IsLookupStaleAt`, e os eventos `BillApproved`/`BillDenied`/`BillCancelled`. **`PayableAmount` e `Beneficiary` leem o mesmo trilho, com a mesma precedência** (Pix primeiro quando `Rail = Pix`): quem paga o valor é quem paga *para* aquele beneficiário, e ler cada um de um trilho descreveria um pagamento inexistente. Se um mudar de precedência, o outro muda junto.
- **Commands** `ApproveBill`/`DenyBill`/`CancelBill` + `ApprovalOptions`; **endpoints** `POST /bills/{id}/approve|deny|cancel|revalidate` e `GET /bills/{id}/detail` com as doze verificações.

**Fase 1 — ✅ CONCLUÍDA (2026-08-06).** As duas sondas de produção saíram verdes — cobrança e decode Pix. As seis sprints entregues e cobertas por teste, e o critério de pronto que dependia de produção foi cumprido: a **sonda de fumaça da consulta de cobrança saiu verde**. `beneficiaryCpfCnpj`, `beneficiaryName`, `bank` (**string** de três dígitos), valor, vencimento e `minimumScheduleDate` voltaram preenchidos para boleto de cobrança registrado — logo o check `PayeeMatch` bloqueante tem base neste tipo de documento, e a assimetria com arrecadação está confirmada por medição dos dois lados ([`12-official-lookup-coverage.md`](BillPayment.Architecture/12-official-lookup-coverage.md)). Ferramenta: [`tools/smoke-probe-production.js`](BillPayment.Architecture/tools/smoke-probe-production.js) — read-only, só `/bill/simulate`, exige a flag `--producao`.

**Duas ressalvas honestas da 1.5**, para a próxima sessão não achar que está pronto o que não está:
- **A alçada é teto único da instalação**, não por pessoa. Amarrar limite a indivíduo exige a identidade do Keycloak (fase 6). O formato do `ApprovalPolicy` já é o final — muda de onde o número vem, não o domínio.
- **Quem decide vem do header `x-user-id`** quando não há token (`BaseController.ResolveDecidingUserId`). É **provisório e morre na fase 6**: sem Keycloak o `User` chega sem claims, e o ADR-007 exige um `UserId` em todo pagamento. Quando o token entrar, o caminho do claim vence sozinho.

**Sprint 1.0 — ✅ Concluída (2026-07-31).** Medida a cobertura do `POST /v3/bill/simulate` contra as 22 linhas do corpus real. Resultado completo em [`12-official-lookup-coverage.md`](BillPayment.Architecture/12-official-lookup-coverage.md); o que muda decisão:

- **Pré-requisito não documentado pelo provedor:** `bill/simulate` e `pix/qrCodes/decode` exigem a permissão de *saque via API* na chave, apesar de não moverem dinheiro. Sem ela, 403 `insufficient_permission`. Logo **não existe fase com credencial inofensiva** — a Fase 1 roda com chave capaz de pagar ([`adr/ADR-001`](BillPayment.Architecture/adr/ADR-001-asaas-como-provedor.md) → "Achado de campo").
- **Arrecadação (10 linhas): 100% respondem, 0% trazem `beneficiaryCpfCnpj`.** Vem nome (`companyName` 100%), valor (100%) e nada de banco (0%). O check de beneficiário **degrada para nome** e o de banco é **inaplicável** — o código de barras de arrecadação não carrega esses campos. Doc 03 atualizado nos checks 5 e 6.
- **Cobrança (12 linhas): 0% respondem** — mas isso **não mede a cobrança**. Um boleto emitido pelo próprio sandbox, consultado no mesmo sandbox, também falha: não há registro de cobrança em sandbox. **Lacuna de validação em aberto** — o caminho de cobrança exige sonda de fumaça com chave de produção contra um boleto real, e isso é critério de pronto da Fase 1.

## Fase 2 — Status

**Sprint 2.1 — ✅ Concluída.** As caixas são cadastradas, sincronizadas e os itens entram — falta apenas o adapter que fala com um provedor de verdade, que é a 2.2. O que aterrissou:

- **`Domain/CaptureSources/`**: `CaptureSource` (Aggregate Root), `CaptureSourceKind` (Mailbox/Portal/ManualUpload com as capacidades por Kind), `CaptureSourceId`, `CaptureSourceErrors`, `ICaptureSourceRepository` — este último declarando a **primeira travessia de tenant autorizada**, `IsAddressMonitoredByAnyTenantAsync`.
- **`Domain/CaptureItems/`**: `CaptureItem` (Aggregate Root), `CaptureItemStatus` (os dez estados + `ExposesFinancialDetail`), `ExtractionMethod`, `CaptureItemId`, `CaptureItemErrors`, `ICaptureItemRepository`.
- **`SharedKernel/EmailSyntax`** — a normalização e a sintaxe de e-mail saíram de dentro do `TrustedOrigin` (eram `private static` lá) para não nascerem duplicadas em `CaptureSource.Address` e `CaptureItem.Sender`. `TrustedOrigin.Normalize`/`ExtractDomain` seguem existindo e delegam; comportamento inalterado, provado pela suíte que já existia.

- **`Infra/Mapping/`**: `CaptureSourceMap` (tabela `capture_sources`), `CaptureItemMap` (`capture_items`) e `CredentialRefConversions` — `CredentialRef` vira **uma coluna de texto** na forma `esquema:chave`, pelo mesmo motivo do `TaxIdConversions`. **Cinco índices**, três deles com semântica que não pode erodir (ver "what is non-obvious").
- **`Infra/Repositories/`**: `CaptureSourceRepository` (incluindo `IsAddressMonitoredByAnyTenantAsync`, o **único caminho de código que consulta `capture_sources` sem filtrar por tenant**) e `CaptureItemRepository`. Registrados em `InfraDependencies`; `DbSet`s no `BillPaymentDbContext`. **Nada a acrescentar ao `DrainDomainEvents`** — nenhum dos dois emite Domain Event.

- **`Domain/Mailboxes/` + `Ports/IMailboxReader`**: `MailboxStatus` (Ok/Denied/**CursorExpired**/Unavailable), `MailboxMessage`/`MailboxArtifact`, `MailboxResult` com `MailboxAccessProbe` e `MailboxReadResult`, `MailboxErrors` (`BLP.MBX01–04`). Falha de caixa é **modelada, não lançada** — mesma doutrina do `LookupResult`.
- **`Application/CaptureSources/Commands/`**: `ConnectCaptureSource` (prova de acesso + cofre + aviso do ADR-008), `RenameCaptureSource`, `AlterCaptureSourceActivation`, `ReplaceCaptureSourceCredential`, `DisconnectCaptureSource` — cada um com seu par `IdentifiedCommandHandler`. Mais `Models/CaptureSources/`.
- **`Application/Queries/CaptureSources/` e `Queries/CaptureItems/`**: as duas queries, com **`CaptureItemDto.From` aplicando o nível de visibilidade** e `CaptureSourceDto` sem nenhum campo de credencial.
- **`API/Controllers/`**: `CaptureSourcesController` (6 endpoints) e `CaptureItemsController` (2, só leitura).
- **`Infra/Mailboxes/UnconfiguredMailboxReader`**: sem adapter configurado, conectar uma fonte **falha** com `BLP.CPS14` em vez de criar caixa silenciosa.

- **`SyncCaptureSourceCommand`** (o **primeiro `IMultiAggregateCommand` do BC**), o endpoint manual `POST /capture-sources/{id}/sync`, e **`API/BackgroundServices/`** com `CaptureSyncBackgroundService` + `CaptureSyncOptions` (`Capture:Enabled`, **desligado por padrão**).

**Sprint 2.2 — ✅ Concluída.** O adapter Microsoft Graph existe e a captura fala com uma caixa de verdade:

- **`Infra/Mailboxes/Graph/`**: `GraphOptions` (`Graph:Enabled`, **desligado por padrão**), `GraphMailboxCredential` (**o formato do campo `credential` da API, enfim fixado**), `GraphHttp` (classificação de falha), `GraphContracts`, `GraphTokenProvider` (client credentials + cache) e `GraphMailboxReader`.
- **Delta query** por `mailFolders/inbox/messages/delta`, seguindo `@odata.nextLink` até o `@odata.deltaLink`, com teto de páginas por varredura.
- **Filtros de anexo**: `isInline` (logotipo e assinatura), tamanho máximo e allowlist de content-type.
- Registro no DI com **dois clientes HTTP resilientes** — listar mensagens e pedir token são idempotentes, e podem retentar.

**O formato da credencial** (contrato do adapter, não da Application):

```json
{"directoryId":"<guid do tenant no Entra ID>","clientId":"<guid do app>","clientSecret":"<segredo>"}
```

O cliente registra o aplicativo no **próprio** Entra ID, concede `Mail.Read` de aplicativo e **restringe por Application Access Policy** ao grupo com as caixas monitoradas — sem essa política, `Mail.Read` alcança todas as caixas do tenant.

**Sonda de caixa em producao — 2026-08-11: VERDE**, e com dois achados que mudaram codigo ([`tools/smoke-probe-mailbox.js`](BillPayment.Architecture/tools/smoke-probe-mailbox.js)):

1. **A delta query ignora `$top`.** Pedimos 50 e vieram 10. O tamanho de pagina so e respeitado pelo header `Prefer: odata.maxpagesize` — o `GraphOptions.PageSize` nao fazia nada. Corrigido.
2. **Caixa de uso misto expoe documento pessoal.** Na primeira pagina, **8 de 11 anexos nao eram conta a pagar** — havia CNH, contrato social e contrato de locacao. Como a 2.3 passa a **baixar e armazenar** todo anexo varrido, isso fixou o requisito de **retencao por desfecho** (ver abaixo). Gerou tambem `CaptureSource.FolderPath`, que e **opcional** e nao resolve isto sozinho.

Confirmou tambem: allowlist de content-type correta (11/11 `application/pdf`), zero inline, e **quatro anexos identicos repetidos em duas mensagens** — evidencia real de que a dedup por `ContentHash` importa.

**Ficou de fora da 2.1, de propósito**: a reivindicação (`POST /capture-items/{id}/claim`) é 2.6 — precisa da escada de roteamento para criar a `Bill` e a `RoutingRule` que a tornam significativa. O adapter Microsoft Graph é a 2.2, e sem ele nenhuma fonte conecta de verdade (a prova de acesso falha, por desenho).

**Três decisões da 2.1 tomadas com o usuário em 2026-08-10**, porque o design as deixava em aberto:

1. **Um `CaptureItem` por artefato, não por mensagem.** Idempotência da ingestão por `(TenantId, SourceId, ExternalMessageId, ArtifactKey)` — o doc 02 dizia só `(TenantId, SourceId, ExternalMessageId)`. Um e-mail com três boletos produz três itens, cada um com seu destino; com um item por mensagem o status seria misto (um anexo `Promoted`, outro `ForeignPayer`) e a projeção do ADR-008 teria de existir por anexo dentro de um agregado cujo status diz outra coisa.
2. **`CredentialRef` aponta para o client credentials do registro no Entra ID**, seguindo a preferência do ADR-006 — não há refresh token por fonte. "Concluir o OAuth" vira **prova de acesso**: a Application lê uma mensagem da caixa antes de criar o agregado, e só então o aviso de fonte compartilhada aparece.
3. **Sincronização por `BackgroundService` + endpoint manual**, no mesmo molde do outbox, para a suíte de integração dirigir a sincronização de forma determinística.

**Sprint 2.3 — ✅ Concluída.** O artefato entra, é lido e tem destino — sem passar por ninguém. O que aterrissou:

- **`Domain/Extraction/`**: `ExtractionResult` (instrumentos achados + degrau + `UnlockedBy`, ou o motivo de não achar), `PasswordCandidate` (senha + rótulo do campo que a derivou, com `ToString` que **nunca imprime a senha**), `ExtractionErrors` (`BLP.EXT01–03`).
- **`Domain/Services/CaptureTriageService`** + `CaptureTriageDecision` — `Parse` / `Lock` / `Quarantine` / `Drop`. É onde vive a decisão de descarte.
- **Portas**: `IBoletoDocumentParser` (a cascata) e `IAttachmentStorage` (o original cifrado).

- **`Infra/Extraction/`**: `CandidateScanner` (gera janelas e deixa o domínio reprovar), `QrCodeScanner` (ZXing sobre as imagens embutidas — QR **e ITF** —, com CRC-16 e DV como filtro), `PdfBoletoDocumentParser` (PdfPig — texto + QR + derivação de senha) e `ExtractionOptions`. Pacotes novos: **`PdfPig 0.1.15`** e **`ZXing.Net.Bindings.SkiaSharp 0.16.22`**.

**Medido contra o corpus real (41 documentos, 2026-08-11)**, antes e depois do leitor de QR:

| Desfecho | Só texto | + QR | + senha | + JPEG | **+ ITF** |
|---|---|---|---|---|---|
| Resolvido | 22 (53,7%) | 29 (70,7%) | 31 (75,6%) | 31 (75,6%) | **32 (78,0%)** |
| Sem camada de texto | 7 | 5 | 5 | 5 | **4** |
| Tem conteúdo, sem instrumento | 10 | 5 | 5 | 5 | 5 |
| PDF cifrado | 2 | 2 | **0** | 0 | 0 |
| **Instrumentos: Barcode / PixQr** | 22 / **0** | 22 / 15 | 24 / 16 | 24 / 24 | **26 / 24** |

Seis achados que recalibram o roadmap:

1. **O leitor de QR vale 17 pontos percentuais** — 53,7% → 70,7%, e sozinho recuperou as quatro guias de FGTS que o texto não entregava.
2. **Zero Pix pelo texto, 15 pelo QR.** Confirma pela terceira vez que o BR Code só existe como imagem, e transforma o ADR-010 (Pix é o trilho preferencial) de premissa em capacidade real.
3. **18 documentos híbridos** carregam código de barras *e* QR (26 + 24 = 50 instrumentos em 32 documentos). É neles que o check antifraude `PixBarcodeConsistency` opera — e ele só existe porque texto e QR rodam **os dois, sempre**, em vez de a cascata parar no primeiro que resolve.

4. **`TryGetPng` do PdfPig falha em `/DCTDecode` — que é JPEG, e é o que as concessionárias usam.** Numa conta de luz, **8 das 13 imagens** eram DCTDecode e nenhuma abria por esse caminho, incluindo as faixas com o corpo da página. Decodificar os bytes brutos pelo SkiaSharp (que lê JPEG nativamente) levou **PixQr de 16 para 24**. A taxa de documentos resolvidos não mudou — os oito já resolviam pelo código de barras —, mas **o trilho Pix deles estava sumindo em silêncio**, e com ele o check `PixBarcodeConsistency`. Documentos híbridos: de 9 para **17**.
5. **A senha de PDF funciona com dado real, e o formato importa.** O documento cifrado que abriu foi por `cnpj_first_5_additional_0` — os **5 primeiros dígitos** (confirmando a ordem do doc 09) de um documento **adicional**, não do principal. Derivar só do `PrimaryTaxId` teria falhado: é a evidência de que percorrer `AdditionalTaxIds` não é zelo excessivo.

6. **Ler ITF recuperou o que ia para a visão à toa.** A primeira versão do leitor só aceitava QR, com a justificativa de que o código de barras "já vem do texto" — **falsa exatamente quando não há texto**, que é o caso do documento digitalizado. `DigitableLine.FromBarcode` reconstrói a linha a partir das 44 posições e delega ao `Parse`, então todos os DVs continuam sendo provados num lugar só.

Os 9 que sobraram (22,0%) são o alvo da 2.4: 4 sem camada de texto e 5 com conteúdo mas sem instrumento (4 guias de sindicato + 1 conta de luz). **Nenhum cifrado sobrou.** Confirmado pelo usuário: todos têm linha digitável impressa, inclusive os que são só imagem — ou seja, é problema de leitura, não de ausência.

- **`Domain/Services/PasswordDerivationService`** — deriva as candidatas do `PayerProfile` (CNPJ 5/8/14, CPF 3/5/6/11), cada uma com o **rótulo do campo** que a gerou. Estático e puro. **Percorre os `AdditionalTaxIds`, e isso foi decisivo**: o único PDF cifrado que abriu no corpus abriu por documento adicional, não pelo principal.

- **`IMailboxReader.DownloadArtifactAsync`** + impl no Graph (`/$value`, com teto de tamanho conferido no cabeçalho **e** depois de ler).
- **`Infra/Storage/`**: `S3AttachmentStorage` (chave prefixada por tenant, que é isolamento e não organização), `StorageOptions` e `UnconfiguredAttachmentStorage` — este **falha em toda escrita e leitura**, porque guardar em lugar nenhum sem avisar faria o sistema pagar boleto cujo original ninguém recupera. Pacote novo: **`AWSSDK.S3`**.
- **`ProcessCaptureItemCommand`** — baixa, extrai, tria e aplica a retenção por desfecho.

- **`ICaptureItemWorkQueries`** (fila do worker, separada da query de tela) e **`CaptureProcessingBackgroundService`**, registrado junto do agendador sob `Capture:Enabled`.

**Ensaio de ponta a ponta contra a caixa real — 2026-08-11 ([`tools/run-capture-chain.js`](BillPayment.Architecture/tools/run-capture-chain.js)).** A cadeia inteira (Graph → download → cascata → triagem → balde) rodou contra `igor.coura@`, **404 anexos ingeridos**, duas vezes: antes e depois de cadastrar `PayerProfile`, 11 `Payee` e 15 `TrustedOrigin`.

| Desfecho | Sem cadastro | **Com cadastro** |
|---|---|---|
| `Parsed` | 45 | **56** |
| `Unrecognized` (quarentena com arquivo) | 0 | **95** |
| `Locked` | 0 | **3** |
| Descartado | 359 | **250** |

Quatro achados que mudam a 2.4:

1. **A senha derivada funcionou com volume: 11 PDFs cifrados abriram.** E **7 dos 11 abriram por documento _adicional_**, 5 deles pelo **CPF** — `cnpj_first_5_primary` (4), `cpf_first_3_additional_1` (3), `cpf_first_5_additional_1` (2), `cnpj_first_5_additional_0` (2). Derivar só do `PrimaryTaxId` teria perdido 7 boletos; os prefixos curtos dominam, como o doc 09 previa. Confirma com volume o que o corpus sugerira com um caso só.
2. **O cadastro é pré-requisito de MEDIÇÃO, não só de qualidade.** Sem `Payee`/`TrustedOrigin`, tudo que a cascata não reconhece é descartado sem deixar arquivo — e aí não há como saber quantos boletos reais se perderam. Os 95 em quarentena (80 `no_instrument_in_document`, 12 `not_a_pdf`, 3 `no_text_layer`) **são** a fila de trabalho da 2.4, com documento guardado para medir em cima.
3. **`not_a_pdf` é um buraco da cascata, não um desfecho.** A allowlist de content-type aceita `image/png` e `image/jpeg`, mas o `PdfBoletoDocumentParser` só abre PDF — então 12 anexos foram ingeridos, baixados e recusados **sem nunca serem lidos**. Boleto que chegue como imagem é inalcançável hoje. **A 2.4 tem que aceitar imagem direto**, não só PDF; é requisito novo daquela sprint.
4. **O processamento custa ~0,5s por artefato (até 1,9s), dominado pelo download no provedor.** Uma caixa antiga de 404 anexos leva ~7 minutos na primeira varredura — o que dimensiona o teto de espera de qualquer ferramenta que acompanhe a fila, e explica por que a varredura e o processamento são workers separados.

**Ruído conhecido da quarentena:** 72 dos 95 vêm de um endereço só — o do contador, que manda boleto de sindicato junto com holerite, rescisão e nota fiscal. Não é defeito: é o preço de manter o documento em vez de apagar. Reavaliar depois da 2.4, quando a visão resolver os que são boleto e o resto puder purgar por janela.

**Sprint 2.4 — ✅ Concluída.** O extrator de visão existe, está ligado na cascata e foi medido contra a fila real: **122 chamadas → 33 boletos resolvidos (~27%), zero candidatos alucinados sobrevivendo ao DV**, com entrada de 1.100–2.200 tokens e custo abaixo de US$ 0,001 por documento. O que aterrissou:

- **`Domain/Extraction/`**: `ExtractedDocument` (o **saco de candidatos**, não resposta), `DocumentPayload` + `ExtractionHints` (o que sai do perímetro), `DocumentKind`. `ExtractionErrors` foi para `BLP.EXT01–06`.
- **`Domain/Ports/IDocumentIntelligence`** — um método (`ExtractAsync`) e um `IsEnabled`. **A triagem por modelo do doc 10 ficou de fora de propósito**: quem decide se vale gastar é um filtro determinístico e gratuito, e uma chamada de triagem custaria, na maioria dos casos, mais do que a extração que ela evitaria.
- **`Domain/Services/CandidateValidationService`** — **onde o ADR-011 vira código**. Converte candidato em `PaymentInstrument` só se ele sobreviver ao DV da linha ou ao CRC do BR Code. É o **segundo e último** lugar do BC onde engolir `DomainException` é correto (o primeiro é o `CandidateScanner`).
- **`Domain/Services/VisionGateService`** — decide **gastar**, nunca descartar. Remetente cadastrado basta sozinho; senão, sinal de cobrança no assunto ou no nome do anexo. Sem ele o gasto seria desproporcional: dos 404 anexos medidos, 250 não tinham sinal nenhum.
- **`Infra/DocumentIntelligence/`**: `GeminiDocumentIntelligence` (HTTP direto, `responseSchema` + `temperature 0`), `GeminiPrompt` (**o prompt é detalhe de implementação, não configuração**), `NullDocumentIntelligence`, `DocumentIntelligenceOptions` e `ExtractionBudget`.

Quatro regras deste degrau que não podem erodir:

1. **O que volta do modelo é string, não boleto.** Nenhum candidato entra sem DV/CRC. O teste-âncora (`Process_WhenVisionHallucinatesALine_ShouldNotProduceABill`) troca um dígito de uma linha válida e prova que o item **continua** na quarentena. Se alguém ligar a saída do modelo direto na `Bill`, é ele que quebra.
2. **Este cliente HTTP NÃO retenta** — ao contrário do Asaas e do Graph. Cada tentativa consome cota de uma conta com teto diário, e insistir num PDF que o modelo recusou gastaria o dia num documento só. A retentativa é a fila de quarentena, no dia seguinte.
3. **`ExtractionBudget` é guarda de conta, não afinação**: teto por tenant por dia (100) e intervalo mínimo entre chamadas (6 s = 10/min, o teto da conta gratuita). Estourado, o artefato vai para a quarentena e volta amanhã — nunca "aprova sem extrair".
4. **A visão aceita imagem, o parser determinístico não.** `image/png`, `image/jpeg`, `image/webp`, e `application/octet-stream` normalizado para PDF. É a correção do buraco medido: **12 anexos recusados com `not_a_pdf`**, baixados e nunca lidos.

**Ensaio contra o provedor real — 2026-08-11.** Três achados, e um deles bloqueia a próxima medição:

1. **O extrator LÊ documento digitalizado.** Sonda dirigida contra `RBC04 - SEGURO DE VIDA.pdf`, um dos 4 do corpus **sem camada de texto**: devolveu uma linha de 47 dígitos que foi **aceita pelo domínio** (banco 033, vencimento 20/06/2026) e sobreviveu aos quatro DVs. É a tese da 2.4 provada ponta a ponta — o degrau 3 alcança o que o determinístico não alcança. Custo medido: **651 tokens de entrada, 90 de saída**.
2. **`GET /models` mente.** A linha `gemini-2.5-*` aparece na listagem e devolve **404** em `generateContent`. Responderam `gemini-3.1-flash-lite`, `gemini-3.5-flash-lite` e o alias `gemini-flash-lite-latest`. O default virou `gemini-3.1-flash-lite` — nome fixo, não alias, porque alias flutua e trocaria a qualidade da extração sem nenhuma alteração no repositório.
3. **O rendimento na fila real ficou por medir.** Das 12 chamadas do teto, 11 completaram e **nenhuma resolveu**: o modelo devolveu listas vazias (não alucinação — zero candidatos barrados). A fila é processada por ordem de chegada, e os mais antigos eram holerite e nota fiscal de remetente cadastrado, que são genuinamente `NotABill`. Amostra pequena e enviesada, não conclusão sobre o extrator.

**`POST /capture-items/{id}/reprocess` resolve a reavaliação** (feito logo depois do ensaio, que tinha travado exatamente aqui). **O desfecho de um artefato é do dia em que ele passou, não para sempre**: a cascata ganha degraus, o prompt muda e o cadastro muda — sem `PayerProfile` não há senha derivada, e sem `Payee`/`TrustedOrigin` o que o parser erra é descartado. `CaptureItem.Reopen` devolve o item a `Received` e o **worker de sempre** faz o resto, pelo mesmo caminho do primeiro processamento — um segundo caminho de processamento seria um segundo lugar para as regras envelhecerem. Reabre de `Unrecognized`, `Locked` e `LinkFailed`; recusa `Parsed` e os terminais com **409**, porque transição inválida é conflito de estado, não erro de entrada. **Um item por chamada, de propósito**: a visão custa por documento e a conta tem teto diário, então reabrir a quarentena inteira queimaria a cota antes de chegar no que interessa.

⚠️ **`ArtifactKey` NÃO é nome de arquivo — e por isso `ContentType` e `FileName` são guardados na ingestão.** No Microsoft Graph a chave do anexo é um identificador opaco (`AAMkADk0NWIx...`), sem extensão nenhuma. O processamento deduzia o tipo dali, então **todo anexo virava `application/pdf`**: o extrator de visão recebia imagem rotulada como PDF, o provedor recusava, e os anexos `not_a_pdf` seguiam inalcançáveis mesmo depois de a visão existir. Pelo mesmo motivo o portão de gasto examina `FileName`, não a chave — sinal de cobrança no nome do anexo nunca casaria com uma chave opaca. **Sem tipo declarado o extrator não é chamado**, em vez de mandar o arquivo por chute. Migração `CaptureItemContentType`; regressão em `CaptureItemContentTypeTests`, que usa uma chave opaca real.

⚠️ **Só as primeiras páginas vão para o extrator (`PdfPageTrimmer`).** Boleto está na primeira ou na segunda; o que chega com dezenas é relatório contábil ou documentação mensal, e mandar inteiro custa proporcional sem aumentar a chance de achar o código de barras. **`MaxPages` existia e não era aplicado em lugar nenhum** — chamadas batiam no timeout de 60 s e a vazão do processamento caía de ~70 para ~8 artefatos por minuto. Com o corte, a entrada voltou para 1.100–2.200 tokens. Falha ao cortar devolve o original: otimização não pode virar documento perdido.

⚠️ **A latência do extrator é de 5 a 30 segundos por documento, e o worker é serial.** Isso torna inviável passar um acervo de milhares de artefatos pela visão em tempo interativo — é exatamente para isso que o doc 10 prevê a **Batch API** (metade do preço, alvo de 24 h), ainda **não implementada**. Enquanto ela não existir, medir a fila inteira leva horas; medir por amostra é o caminho.

⚠️ **O teto do `ExtractionBudget` é em memória e zera ao reiniciar a API.** Aceitável enquanto há um deployment só; vira problema ao escalar horizontalmente, onde cada instância teria seu próprio teto.

**PDF cifrado não vai para a visão**: mandar um arquivo que não abre gastaria a chamada para o modelo ver a tela de senha.

**Sprint 2.5 — ✅ Concluída.** O boleto que chega **sem anexo** — escrito no corpo do e-mail ou atrás de um link — passou a ser alcançável. O que aterrissou:

- **`Domain/Extraction/`**: `DocumentLink` (link já desembrulhado, com host **e porta**) e `ResolvedDocument` (bytes + mídia + URL de origem). `ExtractionErrors` foi para `BLP.EXT01–07`.
- **`Domain/Services/LinkUnwrapService`** — desembrulha rastreador de campanha **sem tocar a rede**.
- **`Domain/Services/BodyCaptureGateService`** — decide se o corpo vira artefato, por três sinais determinísticos.
- **`Domain/Ports/IDocumentLinkResolver`** e **`CaptureItem.RecordResolvedLink`** (grava a procedência sem transição de status).
- **`ExtractionMethod.EmailBody`** — o degrau novo, e o mais barato de todos.
- **`Infra/Extraction/`**: `HtmlText` (HTML→texto), `HtmlLinkHarvester`, `EmailBodyDocumentParser` e `CascadingBoletoDocumentParser` (roteia por tipo de conteúdo).
- **`Infra/Extraction/Links/`**: `LinkResolutionOptions` + `LinkRecipe`, `SafeUrlPolicy` (anti-SSRF), `HttpDocumentLinkResolver` e `NullDocumentLinkResolver`.
- **`GraphMailboxReader`** passou a trazer `body` na delta query e a emitir o **artefato de corpo** (`message-body`); `hasAttachments` deixou de ser pré-requisito de ingestão.

**A medição de um ano da caixa real (2026-08-12) contrariou o plano da sprint**, e a escada final tem um degrau que o roadmap não previa:

| Arquétipo | Onde está o pagável | Host do documento | Sondado |
|---|---|---|---|
| SABESP (formato novo) | **BR Code inteiro no corpo** + PDF | `file-pdf.7az.com.br:7446` | `200 application/pdf`, sem auth |
| SABESP (formato antigo) | **linha digitável no corpo** | — | — |
| Condomínio (BRCondos) | página do boleto | `ssl.brcondos.com.br/Bill/<guid>` | `200 text/html` 82 KB, sem auth |
| Perfil Líder | página com JS | `perfil.simplificamais.com.br` | `200 text/html` 3,6 KB — só bootstrap |
| EDP | portal | `wwwl.montreal.com.br` | `200`, **pede CPF/CNPJ** |

Seis achados que fixaram o desenho:

1. **O degrau mais barato não é rede — é texto.** Dois dos cinco arquétipos resolvem sem abrir arquivo e sem sair da máquina. Virou o `ExtractionMethod.EmailBody`, e ele roda **antes** de qualquer link.
2. **O rastreador se desembrulha sem rede.** Tudo vem em `awstrack.me/L0/<url-encoded>/…`; decodificar é mais barato *e mais seguro* que seguir o redirecionamento — que entregaria ao remetente a confirmação de leitura.
3. **O host do documento não é o do remetente.** SABESP → `7az.com.br`; EDP → `montreal.com.br`. Allowlist derivada do remetente recusaria os dois casos reais.
4. **Porta 7446.** Regra ingênua de "só 443" perderia o único PDF direto que existe.
5. **A maioria dos links é isca** — 8 rastreadores de rede social no e-mail da EDP, um `EmailAdvertisingClick` no do condomínio. "Pegue o primeiro link" erra em ambos.
6. **Toda URL é capability URL**: `200` sem autenticação nenhuma. **Quem tem o link tem o boleto** — por isso ela nunca entra em log e sai por API só sob o portão do ADR-008.

**Ficou de fora, de propósito:** Perfil Líder (página movida por JS) e EDP (portal que pede documento) exigem navegação, que é a 2.8 / fase 5 — **sem evasão de anti-bot** (ADR-012). O `HttpDocumentLinkResolver` os recusa por allowlist, que é o desfecho correto.

**Sprint 2.6 — ✅ Concluída.** O funil abriu a saída: um `CaptureItem` com boleto válido passa a virar `Bill` do tenant certo, ou a parar na fila de reivindicação. O que aterrissou:

- **`Domain/Extraction/PartyCandidate`** — documento fiscal lido do artefato, com a marca de ter vindo sob rótulo de pagador. `ExtractionResult` ganhou `Parties`.
- **`Domain/Services/BillRoutingService`** — a escada, com `RoutingDecision` e `RoutingOutcome` (`Promote`/`Foreign`/`Unrouted`). Estático e puro, como os outros oito Domain Services.
- **`IPayeeRepository.IsRegisteredByAnotherTenantAsync`** — a terceira travessia de tenant, e o índice **não único** `ix_payees_tax_id_global` que a serve.
- **`Infra/Extraction/TaxIdScanner`** — sequência **exata** de 11/14 dígitos, nunca janela deslizante.
- **`ProcessCaptureItemCommand`** virou `IMultiAggregateCommand` e passou a promover; **`ClaimCaptureItemCommand`** + `POST /capture-items/{id}/claim`.
- **`CaptureItemErrors`** foi para `BLP.CPI01–15`; `BillOrigin.CONTENT_HASH_MAX_LENGTH` subiu de 64 para 100.

**A medição de 714 documentos em 14 meses (2026-08-12) contrariou o doc 07 em dois pontos**, e a escada final não tem o degrau 2 que o design previa:

| Degrau | Como resolve | Confiança | Cobertura medida |
|---|---|---|---|
| **0** | senha do PDF derivada do documento do tenant | `Strong` | já existia (11 PDFs na 2.3) |
| **1** | CPF/CNPJ do tenant impresso no artefato | `Strong` | **93,3%** |
| ~~2~~ | ~~`RoutingRule` por (beneficiário, referência de conta)~~ | — | **não implementável** |
| **3** | beneficiário cadastrado só por este tenant | `Weak` | resíduo |
| **4** | `Unrouted` → reivindicação humana | `Claimed` | o que sobrar |

Quatro achados que fixaram o desenho:

1. **O degrau 1 cobre 93,3%, não os ~38% que o doc 07 estimava.** Ele é o cavalo de batalha da escada, e não o degrau de apoio.
2. **Não existe referência de conta estável no código de barras.** O que se repete entre meses é a **agência/conta do beneficiário**; o que varia é o nosso número. Medido: dois pagadores diferentes do mesmo emissor têm as **mesmas** posições estáveis no campo livre (DESPACON 19/25 idênticas, SECONCI 17/25 idênticas). Uma `RoutingRule` com essa chave casaria com o boleto dos dois e roteria o do outro tenant.
3. **Em 0% dos casos o documento do tenant apareceu do lado do beneficiário**, o que torna seguro atribuir por casamento com o cadastro, sem exigir rótulo.
4. **Só 66,8% das ocorrências têm rótulo de pagador por perto** — e é por isso que negar exige rótulo e atribuir não (ver a assimetria em "what is non-obvious").

**Falta para a fase 2**: as expectativas (2.7) e, se o resíduo justificar, o agente de navegação (2.8). A purga dos itens antigos deixou de ser urgente — o handler já não guarda o que não é boleto.

**Não medido ainda**: a escada contra a caixa real. Os 93,3% saíram do corpus de PDFs arquivados, lidos por `pdftotext`; o sistema lê por PdfPig, cuja saída foi conferida em documento real (o rótulo encerra a sequência de dígitos), mas o rendimento fim a fim sobre a fila de produção é ensaio ainda por rodar.

**Ação fora de sprint, de maior impacto por unidade de esforço:** cadastrar **fatura digital por e-mail** em EDP, SABESP, ENEL, CPFL, VIVO e DAE. É o único degrau que retira volume da fase 5 sem escrever conector — mas **não elimina a fase 5**; sobrará portal ([`adr/ADR-012`](BillPayment.Architecture/adr/ADR-012-portais-reduzir-residuo.md)).

A Fase 1 inteira (verificação + aprovação) não movimenta dinheiro: a consulta ao Asaas é read-only. **Mas a credencial que ela exige, sim** — ver o achado da sprint 1.0 acima.

**Corpus de referência**: `D:\OneDrive\OneDrive - RUFINO EMPREITEIRA\DOC EMPRESA\2 - CONTROLE DE CUSTOS\2026-06\PAGO` — 39 boletos reais que vão passar por este sistema, já medidos em [`08-boleto-corpus-findings.md`](BillPayment.Architecture/08-boleto-corpus-findings.md). Use-os para calibrar parser e como fixture de teste.

| Camada | Status | Artefatos |
|---|---|---|
| Domain | ✅ Sprint 2.6 | **`Services/BillRoutingService`** (a escada + RoutingDecision/RoutingOutcome) e **`Extraction/PartyCandidate`**; `IPayeeRepository.IsRegisteredByAnotherTenantAsync` é a 3ª travessia autorizada. Herdado da 2.1: **`CaptureSources/`** (CaptureSource, CaptureSourceKind, ICaptureSourceRepository com a 1ª travessia autorizada) e **`CaptureItems/`** (CaptureItem, CaptureItemStatus, ExtractionMethod, ICaptureItemRepository); `SharedKernel/EmailSyntax`. Nenhum dos dois emite Domain Event — nada a acrescentar ao `DrainDomainEvents`. Herdado da 1.5: `ApprovalRecord`, `ApprovalDecision`, `ApprovalPolicy` + `Bill.Approve`/`Deny`/`Cancel` e os três eventos de decisão. Herdado da 1.4: **`Bills/Checks/`** (CheckType, CheckOutcome, CheckSeverity, CheckReasons, CheckResult, BillCheck, ValidationOutcome) e **`Services/`** (BillValidationService, PayeeResolutionService, BillValidationContext); `PartyInfo`, `RoutingConfidence`, `BillLookupRecord`, `DuplicateProbe`. Herdado da 1.3: **`Lookups/`** (LookupSnapshot, PixLookupSnapshot, LookupParty, MaskedParty, LookupResult/BillLookupResult/PixLookupResult, LookupStatus, LookupErrors) e **`Secrets/`** (CredentialRef, SecretKind, SecretErrors); `Ports/` com IBankDirectory + IBillLookupService + IPixLookupService + ISecretVault. Herdado: `SeedWork/` completo + `SharedKernel/` (TenantId, UserId, Money, Currency, TaxId, DateRange, CompetencePeriod, **BankCode**). **Aggregates: `PayerProfile` (PRF), `Payee` (PYE), `TrustedOrigin` (ORG)** com Smart Enums (`PayerKind`, `AmountPolicyKind`, `OriginKind`, `TrustDecision`), VO `AmountPolicy` e factories de erro. Sem Domain Events ainda — esses três Aggregates não emitem |
| Application | ✅ Sprint 2.6 | **`ClaimCaptureItemCommand`** (3º `IMultiAggregateCommand`) e a promoção automática dentro do `ProcessCaptureItemCommand` (2º). **6 Commands** em `CaptureSources/` (incl. `SyncCaptureSource`, o primeiro `IMultiAggregateCommand`) (Connect com prova de acesso + cofre + aviso ADR-008, Rename, AlterActivation, ReplaceCredential, Disconnect), `Models/CaptureSources/`, e **`ICaptureSourceQueries`/`ICaptureItemQueries`** — esta última projetando por `CaptureItemDto.From`. **`CursorCodec`** extraído (vivia copiado em 3 queries). Herdado da 1.5: `ApproveBill`/`DenyBill`/`CancelBill` + `ApprovalOptions` + `IBillQueries.GetDetailAsync`. Herdado da 1.4: **`ValidateBillCommand`** + `BillCapturedDomainEventHandler` (disparado pelo outbox). Herdado da 1.1: Mediator próprio + `IdentifiedCommand`/`IdentifiedCommandHandler` + `LoggingBehavior`. **15 Commands** em `TrustedOrigins/`, `Payees/` e `PayerProfiles/` (cada um com seu par `IdentifiedCommandHandler`) e **3 `IXxxQueries`** em `Queries/` — keyset por `CreatedAt` nas listas |
| Infra | ✅ Sprint 2.6 | **`Extraction/TaxIdScanner`** (sequência exata, nunca janela deslizante) e o índice **não único** `ix_payees_tax_id_global`. Herdado da 2.2: **`Mailboxes/Graph/`** — adapter de delta query, provedor de token com cache, classificação de falha e filtros de anexo. Herdado da 2.1: **`CaptureSourceMap`** + **`CaptureItemMap`** + `CredentialRefConversions`, os dois repositórios e os `DbSet`s. O índice global de endereço é **não único** de propósito (ADR-008). Herdado da 1.4: `bill_checks` (tabela filha) + `LookupConversions` (jsonb dos retratos) no `BillMap`; `ProbeActiveDuplicateAsync`; `ListByTenantAsync`. Herdado da 1.3: **`Asaas/`** (adapters de consulta + `AsaasOptions` + DTOs internos + `LenientStringConverter`), **`Secrets/`** (`EnvelopeSecretVault`, `SecretsOptions`), `TenantSecret` + `TenantSecretMap`. Herdado da 1.1: `BillPaymentDbContext` (UoW, com o drain de eventos ligado desde a 1.2), mappings de plataforma + **`TrustedOriginMap`** `(TenantId, Kind, Value)`, **`PayeeMap`** `(TenantId, TaxId)`, **`PayerProfileMap`** `(TenantId)` — os três índices únicos —, `TaxIdConversions`, os três repositórios, `OutboxProcessor`/`OutboxBackgroundService`/`DomainEventDispatcher`, `RequestManager` |
| API | ✅ Sprint 2.6 | **`CaptureSourcesController`** (11 endpoints, incl. `POST /{id}/sync`, `PUT /{id}/folder`, `POST /{id}/folders`, `DELETE /{id}/folders` e `POST /{id}/rescan`; nenhuma resposta devolve credencial nem ponteiro de cofre) e **`CaptureItemsController`** (4 — leitura + `POST /{id}/reprocess` + `POST /{id}/claim`). Herdado da 1.5: `POST /bills/{id}/approve|deny|cancel|revalidate` e `GET /bills/{id}/detail`; `BaseController.ResolveDecidingUserId` (provisório, ver ressalvas). Herdado da 1.1: `Program.cs`, `HealthController`, `BaseController`, `DomainExceptionFilter`, `CorsExtensions`, Dockerfile + **`TrustedOriginsController`**, **`PayeesController`** e **`PayerProfileController`**. Os Models HTTP saíram para `Application/Models/<Aggregate>/`; os controllers só chamam `model.ToCommand(...)`. **Sem `[ProtectedResource]` ainda** — autorização granular é fase 6 |
| Unit Tests | ✅ | **848 testes** — a 2.6 acrescentou 24: `Services/BillRoutingServiceTests` (os quatro degraus, e sobretudo o teste âncora de que documento fiscal SEM rótulo não vira recusa) e `Extraction/PartyCandidateTests`. A 2.5 acrescentou 39: `Services/LinkUnwrapServiceTests` (o desembrulho de rastreador, inclusive a recusa de desembrulhar para fora de http), `Services/BodyCaptureGateServiceTests` (os três sinais e, sobretudo, o link para host SEM receita que **não** é sinal), `Extraction/DocumentLinkTests` (porta não-padrão preservada, o que não é http recusado) e o `RecordResolvedLink` em `CaptureItems/CaptureItemTests`. A 2.4 acrescentou 31, e os de `Services/CandidateValidationServiceTests` são os que provam que candidato alucinado não vira instrumento. — a gestão de pastas acrescentou 12 (`AddFolder`/`RemoveFolder`, o teto, a repetição normalizada, o isolamento de falha entre pastas e a releitura). (1 skip pré-existente) — a 2.1 acrescentou 92: `CaptureSources/CaptureSourceTests` (invariantes de conexão, e sobretudo **a falha de sync preservando o cursor**), `CaptureItems/CaptureItemTests` (ingestão por artefato, a cascata de status, e a **recusa de reivindicação contra pagador identificado — BLP.CPI04**) e `CaptureItems/CaptureItemStatusTests` (a matriz de transições e a regra de visibilidade do ADR-008). Mothers: `CaptureSourceMother`, `CaptureItemMother`. Herdado: SharedKernel (Money, Currency, TaxId + `Parse`/`TryParse`, DateRange, CompetencePeriod, TenantId, BankCode), os três Aggregates da 1.1, os instrumentos da 1.2, os VOs de consulta/segredo da 1.3 (`Lookups/`, `Secrets/`) e a verificação da 1.4 — `Services/BillValidationServiceTests` (cada `CheckType` nos seus desfechos), `Services/PayeeResolutionServiceTests` (documento × nome × sósia) e `Bills/BillValidationTests` (a matriz de decisão do doc 03, cobertura incompleta, revalidação). as decisões da 1.5 (`Bills/BillApprovalTests` — as guardas de aprovação, recusa e cancelamento). Mothers: `LookupMother`, `ValidationMother` (+ `FakeBankDirectory`) |
| Integration Tests | ✅ | **323 testes** — a 2.6 acrescentou 20: `CaptureItems/BillRoutingTests` (9) leva o artefato até virar `Bill` do tenant certo, cobre a quarentena cega, o beneficiário exclusivo × compartilhado entre tenants, o reenvio do mesmo boleto apontando para o mesmo `Bill`, e a reivindicação pela borda HTTP com as duas recusas (`BLP.CPI04` e o aviso genérico `BLP.BIL02`); `Extraction/TaxIdScannerTests` (11) ancora a regra de sequência exata contra o código de barras. `CaptureItems/EmailBodyExtractionTests` (7) cobre os degraus 1 e 2: o BR Code escrito no corpo resolvendo **sem chamar o resolvedor de link**, a linha partida em `<span>`s ainda fechando o DV, o documento buscado por link atravessando a cascata inteira, e o **teste âncora** da linha adulterada que veio de host autorizado e mesmo assim não vira boleto; `Extraction/LinkResolutionTests` (18) cobre a colheita com links-isca, o desembrulho de rastreador e a barreira anti-SSRF (faixa privada, metadados de nuvem, CGNAT e v4 mapeado em v6). `CaptureItems/VisionExtractionTests` (7) cobre o degrau 3: a visão resolvendo, a linha alucinada barrada, o portão de gasto e o anexo em imagem. — `CaptureSources/CaptureSourceFoldersTests` (9) cobre a varredura multi-pasta com cursor próprio, a pasta quebrada que não derruba as outras, e o `rescan`. — `CaptureItems/CaptureItemPaginationTests` (3) cobre o cursor de keyset sob empate de `CreatedAt`; `Storage/UnconfiguredStorageTests` (3) prova que o perfil de desenvolvimento não vaza para a suíte. A 2.2 acrescentou 16 em `Mailboxes/GraphMailboxReaderTests` (tradução de 403/429/410, filtros de anexo, paginação, cache de token e o **cursor corrompido**). A 2.1 acrescentou 39. `CaptureSources/SyncCaptureSourceTests` cobre o ciclo: um item por artefato, reprocessar sem duplicar, o cursor avançando e sendo retomado, a falha preservando o cursor e o 410 Gone descartando-o. Pela borda HTTP: `CaptureSources/ConnectCaptureSourceTests` (prova de acesso, segredo no cofre com só o ponteiro na fonte, **o aviso genérico do ADR-008 sem identificar a outra conta**, e a falha fechada que não deixa credencial órfã) e `CaptureItems/CaptureItemVisibilityTests` (**os dois níveis de projeção**, lista e detalhe aplicando a mesma regra, isolamento). Pela persistência, 16: `CaptureSources/CaptureSourcePersistenceTests` (round-trip do `CredentialRef`, cursor sobrevivendo à falha de sync, **unicidade dentro do tenant × colisão permitida entre tenants**, a travessia autorizada nº 1, e a ordem da fila do worker) e `CaptureItems/CaptureItemPersistenceTests` (idempotência por artefato, mesma mensagem em dois tenants gerando dois itens, dedup por conteúdo ignorando descartados e outro tenant). Herdado: infra de teste (Testcontainers postgres:17 + Respawn + `Outbox:Enabled=false`), `HealthCheckTests`, as fatias HTTP dos três Aggregates, `BacenBankDirectoryTests`, `ImportBillTests` a 1.3 (`Asaas/` via `StubHttpMessageHandler`, `Secrets/EnvelopeSecretVaultTests` com Postgres real) e a 1.4 — **`Bills/ValidateBillTests`**: fluxo captura → outbox → consulta → verificação → persistência, incluindo o **teste antifraude de trilho obrigatório** (QR Pix apontando para outro CNPJ é bloqueado por `PixBarcodeConsistency`) e a revalidação preservando o histórico; e a 1.5 — **`Bills/ApproveBillTests`**: aprovar/recusar/cancelar pela API, retrato velho barrando a aprovação até revalidar, e o detalhe devolvendo as doze verificações sem vazar a linha digitável. DTOs **duplicados** em `Contracts/` de propósito |

## Architecture — what is non-obvious

Prefixos de erro: `SWK##` (SeedWork), `SHK.<VO>##` (SharedKernel), `BLP##` (BC transversal — hoje só `BLP01` TenantMismatch em `BillPaymentErrors.cs`), `BLP.<AGG>##` (Aggregate-specific — reserve a sigla do Aggregate ao criá-lo e registre aqui). **Siglas em uso**: `PRF` (PayerProfile, BLP.PRF01–10), `PYE` (Payee, BLP.PYE01–16), `ORG` (TrustedOrigin, BLP.ORG01–10), `BNK` (BankCode, SHK.BNK01–02), `DGL` (DigitableLine, BLP.DGL01–06), `PIX` (PixPayload, BLP.PIX01–04), `INS` (PaymentInstrument, BLP.INS01–03), `BIL` (Bill, BLP.BIL01–26), `LKP` (Lookups, BLP.LKP01–07), `SEC` (Secrets, BLP.SEC01–07), `CPS` (CaptureSource, BLP.CPS01–19), `CPI` (CaptureItem, BLP.CPI01–15), `MBX` (Mailboxes — VOs de leitura de caixa, BLP.MBX01–04), `EXT` (Extraction — VOs da cascata, BLP.EXT01–07). **Reservadas pelo design, ainda não codificadas**: `EXP` (BillExpectation), `PMO` (PaymentOrder). **`RTR` (RoutingRule) foi ABANDONADA na 2.6** — a medição mostrou que a chave que ela usaria não distingue pagadores; não recrie a sigla sem reabrir aquele achado. **`BLP.CPI04` é fixado pelo doc 07** (reivindicação que contradiz o pagador extraído) — não renumere a factory. Convenções:

- Aggregate Roots emitem Domain Events; Entities internas nunca.
- **Portas de integração vão em `Domain/Ports/`** (pasta a criar na Fase 1, irmã de `SeedWork/`), não em `Domain/SeedWork/` — mesma razão (`Infra → Application` seria ciclo), mas separadas por serem contratos de mundo externo e não do modelo. Trafegam só tipos do Domain; nenhum DTO de provedor cruza a fronteira. Catálogo em [`02-domain-model.md`](BillPayment.Architecture/02-domain-model.md).
- Cross-aggregate rules vão em Domain Services, nunca passe Entity de um Aggregate para método de outro.
- Cross-aggregate references to internal Entities devem ser ancoradas via composite VO que carrega a **raiz** + a Entity interna (ex. no EconomicCore: `CommitmentRef(ContractId, CommitmentId)`).
- `*Errors.cs` factories ficam co-localizadas com o Aggregate; `SeedWorkErrors` é `public static`. Aggregate Errors são `public static` quando a Application precisa lançar (NotFound, pré-condições), `internal static` para os puramente de domínio.
- Tenancy: todo Aggregate Root carrega `TenantId` (strongly-typed `record struct : IEntityId<TenantId>`); queries e authorization filtram por `TenantId`.
- **Mediator próprio (sem MediatR)**: vive em `Application/Mediator/`. `IRequest<T>`/`IRequestHandler<,>`/`IPipelineBehavior<,>`/`IMediator` têm a mesma superfície do MediatR; `Mediator` é **Scoped**, resolve handler + behaviors via DI e cacheia um wrapper por tipo de request. `AddCustomMediator(assembly)` escaneia handlers/behaviors fechados — e **falha no startup** se houver dois handlers para o mesmo request; `LoggingBehavior` (único behavior, mais externo) é registrado manualmente. Trocar de mediator = mexer só nessa pasta + em `ApplicationDependencies`.
- **`IMultiAggregateCommand` (marker em `Application/Mediator/`)**: exceção sancionada e greppável à regra "um agregado mutado por transação". **Um único uso, `SyncCaptureSourceCommand`**, e a justificativa é atomicidade entre o cursor e os itens: se a `CaptureSource` avançasse numa transação e os `CaptureItem` nascessem noutra, uma falha no meio produziria **boletos perdidos** (cursor à frente dos itens) ou **ingestão repetida** (itens sem cursor). Eventual consistency por Domain Event não resolve — o cursor é a única prova de até onde a caixa foi lida, e ele não pode ficar "quase" certo. Como há exatamente um `SaveEntitiesAsync`, a transação implícita do EF cobre tudo e o `TransactionBehavior` continua sem uso. Toda nova adoção exige justificativa aqui.
- **A varredura é uma fonte por comando, uma transação por fonte.** O agendador chama o comando N vezes, cada uma no seu escopo — mesma disciplina do outbox reivindicando uma mensagem por vez. Uma caixa fora do ar registra a própria falha e **não impede as outras de sincronizar**.
- **O worker de captura mora na API, não na Infra como o do outbox.** A diferença não é estilística: o outbox é infraestrutura pura (move linhas de uma tabela e despacha por porta do Domain, sem tocar caso de uso). Sincronizar uma caixa **é** caso de uso e vive na Application; um `BackgroundService` na Infra teria de alcançar o mediator, e `Infra → Application` é ciclo. A API é o composition host e enxerga as duas. `Capture:Enabled` é **`false` por padrão** — ao contrário do outbox —, porque sem adapter de provedor o worker só produziria falha registrada em toda fonte a cada minuto, afogando a falha de verdade quando ela viesse.
- **Idempotência (`x-requestid`)**: todo Command de escrita é embrulhado em `IdentifiedCommand<TCommand,TResult>` no controller; o par `IdentifiedCommandHandler` checa `IRequestManager.ExistAsync` e, se duplicata, devolve resposta neutra (`Id` = `Guid.Empty`). A porta `IRequestManager` fica em `Domain/SeedWork`; a impl `RequestManager` na `Infra/Idempotency` sobre a tabela `client_requests` (PK = `Id`; corrida concorrente colide na PK e o `IdentifiedCommandHandler` reconfirma via `ExistAsync`). `CreateRequestForCommandAsync` **não** commita — marca + efeito persistem juntos no `SaveEntitiesAsync`. `BaseController.EnsureRequestId` gera Guid novo quando o header vem vazio.
- **EF owned types & shared references**: Value Objects mapeados como owned types (`OwnsOne`/`OwnsMany`). EF tracks owned type instances by reference identity — **never share the same VO instance between two tracked entities**. Owned de 2º nível anexado a agregado já persistido não é rastreado (grava NULL) — achatar `Money` aninhado em colunas escalares (lição do EconomicCore).
- **Value Object com uma coluna, quando o resto é dedutível.** `TaxId` é gravado como **um único texto** (`TaxIdConversions.Single`), sem coluna para o tipo: 11 dígitos é CPF, 14 é CNPJ, e `TaxId.Parse` é a única implementação dessa dedução. Isso é o que torna possível o índice único `(tenant_id, tax_id)` — **o construtor de índice do EF só aceita propriedade do próprio tipo, e propriedade de owned type não é endereçável a partir da raiz** (`e => new { e.TenantId, e.TaxId.Value }` lança `ArgumentException` em runtime, não em compilação). A rehidratação passa por `Parse`, então dígito verificador corrompido no banco falha alto na leitura.
- **Coleção e estrutura aninhada viram `jsonb`, não owned type.** `Payee.AmountPolicy` (que contém `Money`), `Payee.Aliases`, `Payee.AcceptedBanks` e `PayerProfile.AdditionalTaxIds` são colunas `jsonb` com `HasConversion` + `ValueComparer`. O motivo é a lição do EconomicCore registrada acima: **owned de 2º nível anexado a agregado já persistido não é rastreado e grava NULL** — e `ChangeAmountPolicy` num `Payee` carregado do banco é exatamente esse cenário (coberto por teste de regressão em `PayeeLifecycleTests`). A desserialização passa pelas factories públicas do domínio, nunca por construtor privado. Só vá para tabela filha quando a coleção precisar ser **filtrada em SQL** — nenhuma destas precisa.
- **Unicidade de boleto é GLOBAL, não por tenant.** `ix_bills_dedup_key_active` é único sobre `dedup_key` **sem `TenantId` na chave**, com filtro parcial excluindo os status que liberam a chave (`Denied`, `Cancelled` — derivados de `BillStatus.OccupiesNaturalKey`, não escritos à mão). Um compromisso é pago uma vez e a caixa compartilhada torna a colisão entre tenants provável (ADR-008). **Não "conserte" acrescentando `TenantId`.** O erro devolvido (`BLP.BIL02`) é deliberadamente genérico e coberto por teste que prova que a resposta não revela o outro tenant.
- **Quem decide a visibilidade da quarentena é o `CaptureItemStatus`, não a tela.** `ExposesFinancialDetail` é `true` em **exatamente dois** estados: `Promoted` (o boleto é do próprio tenant) e `Unrouted` (sem valor e beneficiário o usuário não tem como decidir se reivindica). `ForeignPayer` não expõe porque o sistema *sabe* que não é dele — mostrar seria vazamento gratuito (ADR-008). **Os estados do funil (`Received`, `Parsed`, `Locked`, `LinkPending`, `LinkFailed`) também não expõem**: antes do roteamento ninguém sabe de quem é o documento, e projetar ali vazaria exatamente na janela que antecede a descoberta de que o pagador é outro. A query tem que ler essa propriedade em vez de escrever a lista de status à mão, senão a regra passa a existir em dois lugares e um deles envelhece.
- **Falha de sincronização não toca no cursor.** `CaptureSource.RecordSyncFailure` grava erro e instante e **deixa o `SyncCursor` como estava**: avançá-lo pularia mensagens que ninguém leu, e apagá-lo transformaria um timeout em varredura completa da caixa. Quem apaga é `ResetCursor`, e só existe para o `410 Gone` do Graph, que invalida o `deltaLink` velho. `BeginSync()` devolve o cursor **e** recusa fonte desativada (`BLP.CPS12`) — devolver o cursor daqui, em vez de deixar o processador ler a propriedade, é o que impede sincronizar uma fonte desligada por esquecer a checagem.
- **O índice global de `capture_sources` é NÃO único, e isso é a funcionalidade.** `ix_capture_sources_address_global` cobre `address` **sem `tenant_id`** — o oposto do `ix_bills_dedup_key_active`, que é único pelo mesmo motivo invertido. Duas contas monitorando a mesma caixa é o caso central do ADR-008; torná-lo único quebraria fonte compartilhada. Ele serve a **um** caminho de código, `IsAddressMonitoredByAnyTenantAsync`, que usa `AnyAsync` — e o `AnyAsync` não é otimização, é o contrato: não existe projeção de onde extrair id, nome ou contagem do outro tenant. Trocar por `CountAsync` ou acrescentar `Select` viola o ADR-008. A unicidade **dentro** do tenant é outro índice (`ix_capture_sources_tenant_address`, BLP.CPS10). Coberto por teste que prova as duas coisas: a colisão no mesmo tenant estoura, a colisão entre tenants persiste.
- **A idempotência da ingestão inclui `artifact_key` na chave.** `ix_capture_items_tenant_source_message_artifact` é único sobre `(tenant_id, source_id, external_message_id, artifact_key)`. Sem a quarta coluna, um e-mail com três boletos teria dois descartados como se fossem a mesma coisa; com `tenant_id` na chave, a mesma mensagem lida por duas fontes de dois tenants gera dois itens — o que é **correto**, não duplicidade. Ambos cobertos por teste.
- **Item descartado não serve de original.** `FindOriginalByContentHashAsync` exclui `Discarded` da busca: apontar para um descartado encadearia duplicatas e a trilha deixaria de levar ao artefato de verdade. Ordena por `CreatedAt` — o original é o mais antigo.
- **A projeção da quarentena tem um caminho só: `CaptureItemDto.From`.** Não existe outro construtor público, e é ele que lê `Status.ExposesFinancialDetail` para decidir o que sai. Montar o DTO à mão numa query nova furaria a regra do ADR-008 **sem quebrar compilação nem teste** — por isso a construção é fechada. Hoje o gate esconde `StorageKey`, `SourceUrl`, `ContentHash`, `UnlockedBy` e `BillId`: valor e beneficiário ainda não existem no `CaptureItem` (chegam com a `Bill`, na 2.6), mas o ponteiro do arquivo e o link da fatura **levam ao documento de outro pagador**, que é o mesmo vazamento por outro caminho. Quando os campos financeiros chegarem, entram atrás deste mesmo gate. Coberto por teste que atravessa o HTTP, que é onde o vazamento aconteceria.
- **Conectar uma fonte falha fechado.** `ConnectCaptureSourceCommandHandler` guarda o segredo no cofre, **prova o acesso à caixa** e só então cria o agregado — e o aviso do ADR-008 só é consultado depois da prova. Nada disso commita antes do `SaveEntitiesAsync`, então uma prova reprovada descarta a unidade de trabalho inteira e **não deixa credencial órfã** (coberto por teste que conta as linhas de `capture_sources` e `tenant_secrets` depois da falha). `ReplaceCredential` guarda uma referência **nova** em vez de sobrescrever a antiga pelo mesmo motivo: se a prova reprovar, a credencial que ainda funcionava permanece intacta.
- **`MonitoredFolder` é Entity interna da `CaptureSource`, e o cursor é DELA — não da fonte.** A delta query do provedor é por pasta: um `deltaLink` obtido na caixa de entrada não significa nada dentro de "Contas". Um cursor único na raiz obrigaria **uma fonte por pasta**, duplicando credencial e cadastro para uma caixa só. Persistida em `capture_source_folders` (coleção owned) com índice único `(capture_source_id, path)` **e `NULLS NOT DISTINCT`** — sem isso duas linhas de caixa de entrada (`path` nulo) passariam pelo banco, porque no Postgres `NULL` não colide com `NULL` em índice único comum. **A FK sombra tem o tipo da chave da raiz (`CaptureSourceId`, não `Guid`)**; declarada como `Guid`, o EF recusa o modelo inteiro na validação e derruba toda a suíte de integração.
- **O erro por pasta não colapsa no erro da fonte.** `MonitoredFolder.LastSyncError` é o diagnóstico; `CaptureSource.LastSyncError` é resumo para a lista, e `RecordSyncSuccess` numa pasta **só o limpa quando nenhuma outra está falhando** — senão uma pasta renomeada no cliente de e-mail sumiria da tela enquanto as demais rodam bem. Uma pasta que falha não impede as outras de sincronizar (`SyncCaptureSourceCommand` itera pasta a pasta), e o desfecho devolvido pelo endpoint é o da **falha**, não o da maioria: quem chamou está conferindo se a conexão funciona.
- **Acompanhar pasta é lista explícita, sem recursão** (decisão do usuário, 2026-08-11). Subpasta que não estiver na lista **não é lida** — a delta é da pasta, não da árvore. Descobrir a árvore a cada ciclo custaria uma chamada por pasta marcada e faria o número de cursores crescer sozinho. Teto de `MAX_FOLDERS = 20`, porque cada pasta é uma chamada ao provedor por ciclo. `RemoveFolder` recusa a última (`BLP.CPS18`): fonte sem pasta não varreria nada **e não avisaria** — quem quer parar desativa a fonte.
- **A lista de pastas é opcional e NÃO é mecanismo de triagem.** O padrão é a caixa de entrada, e assim deve continuar: **a triagem é trabalho do software**, não do usuário. Exigir que alguém mova boleto para uma pasta à mão devolveria ao usuário exatamente a tarefa que este BC existe para eliminar — decisão do usuário em 2026-08-11, corrigindo enquadramento anterior. A pasta serve a quem *já* separa a caixa, ou quer restringir o escopo por conta própria; nunca é pré-requisito de sprint.
- **`POST /capture-sources/{id}/rescan` existe porque o desfecho de um artefato depende do cadastro que existia quando ele passou.** Sem `PayerProfile` não há senha derivada; sem `Payee`/`TrustedOrigin`, o que a cascata não reconhece é **descartado** em vez de ir para a quarentena. Cadastrar depois não reavalia nada, e antes disto a única saída era desconectar a fonte e reconectar — digitando a credencial de novo. **Reler não duplica**: a ingestão é idempotente por `(tenant, fonte, mensagem, anexo)`, então o que já virou item continua o mesmo; o que muda é que o **descartado** volta a ser avaliado.
- **A senha de PDF é prova de propriedade, não só conveniência.** O emissor a derivou do documento do pagador — se o PDF abre com um documento do `PayerProfile`, isso é evidência de que o boleto é **daquele tenant**, em muitos casos mais forte que OCR (degrau 0 do roteamento, doc 09). `PasswordDerivationService` produz as candidatas e o `PdfBoletoDocumentParser` para no primeiro acerto, registrando o **rótulo** em `UnlockedBy`. Duas filiais com a mesma raiz de CNPJ geram uma candidata só: repetir gastaria o teto sem aumentar a chance de abrir.
- **Atribuir e recusar têm exigências DIFERENTES, e a assimetria é a regra central do roteamento.** Atribuir um boleto ao tenant exige casar com o cadastro dele — seguro por construção, e medido: em 0% dos 714 documentos o documento do tenant apareceu do lado do beneficiário. Recusar — dizer "isto é de outra pessoa" — exige **rótulo de pagador** ao lado do número, porque sem rótulo não há como distinguir o CNPJ do pagador do CNPJ da concessionária, e **todo boleto traz os dois**. Um engano aqui manda para `ForeignPayer`, que não expõe valor e **não pode ser reivindicado**: o usuário perderia a própria conta sem ter como recuperá-la. Exigir rótulo também para atribuir custaria 31 pontos de cobertura (93,3% → 62,3%) sem benefício medido. O teste âncora é `Process_WhenTheOnlyTaxIdIsThePayees_ShouldQueueForClaimAndNotMarkAsForeign`.
- **A varredura de documento fiscal faz o OPOSTO da de linha digitável, e não é inconsistência.** `CandidateScanner` gera todas as janelas e deixa o DV reprovar; `TaxIdScanner` só aceita a sequência **exata** de 11 ou 14 dígitos. A razão é aritmética: a linha tem quatro dígitos verificadores e um acerto por acaso é improvável, o CNPJ tem dois e um código de barras de 44 posições oferece trinta e uma janelas. Medido em 714 documentos: **a regra deslizante fabricaria um CNPJ aparentemente válido dentro do código de barras em 46,9% deles** — e um número fabricado ao lado de um rótulo de pagador mandaria uma conta legítima para a quarentena cega. A sequência exata elimina isso por construção e não custa cobertura, porque emissor imprime documento fiscal isolado ou formatado.
- **`RoutingRule` foi projetada, medida e ABANDONADA — não a recrie sem refazer a medição.** O doc 07 previa o degrau 2 chaveado por `(PayeeTaxId, AccountReference)`, com a referência saindo do documento. Medindo 714 boletos de 14 meses: o que é estável entre meses no campo livre do código de barras é a **agência/conta do beneficiário**, não a conta do cliente — e por isso dois pagadores diferentes do mesmo emissor têm as **mesmas** posições estáveis (DESPACON 19/25 idênticas entre dois tenants, SECONCI 17/25). Uma regra aprendida com essa chave casaria com o boleto dos dois e roteria o do outro tenant, que é exatamente a falha que o ADR-008 existe para impedir. O aprendizado passou a ser a vinculação do `Payee` ao tenant (degrau 3), que é chave que de fato distingue. Ferramenta: [`tools/analyze-account-reference.js`](BillPayment.Architecture/tools/analyze-account-reference.js).
- **A senha derivada é o degrau 0 e vem antes de tudo — mas só porque a senha vazia não conta.** `PdfBoletoDocumentParser` devolve `UnlockedBy` **nulo** quando quem abriu o PDF foi a candidata vazia (owner password, que bloqueia edição e não leitura). Se ela contasse, todo PDF sem senha "provaria" propriedade e a escada atribuiria qualquer documento ao dono da fonte. É um `ReferenceEquals` em uma linha, e é o que sustenta o degrau inteiro.
- **`Parsed` é estado de PASSAGEM desde a 2.6, não desfecho.** A escada roda dentro do mesmo processamento, logo depois da cascata, e o item termina em `Promoted`, `ForeignPayer` ou `Unrouted`. Teste que afirme `Status == Parsed` depois de processar está descrevendo o mundo da 2.5.
- **`ProcessCaptureItemCommand` e `ClaimCaptureItemCommand` são o 2º e o 3º `IMultiAggregateCommand` do BC**, e a justificativa é a mesma: `CaptureItem.Promote` guarda o `BillId`, então a `Bill` e o item precisam nascer na mesma transação. Consistência eventual por Domain Event não resolve — o id do boleto é o próprio dado que teria de atravessar, e ele só existe depois da criação. Uma falha entre as duas transações produziria boleto que item nenhum aponta (invisível na fila, sem trilha de origem) ou item `Promoted` apontando para boleto inexistente.
- **A promoção distingue "já é meu" de "é de outra conta", e sem isso o reenvio vira quarentena.** `ProbeActiveDuplicateAsync` devolve o id **só quando o original é do próprio tenant**: aí o segundo item aponta para o mesmo boleto — que é o caso medido na caixa real (quatro anexos idênticos em duas mensagens). Sendo de outro tenant, resta o `bool`, o item fica `Unrouted` com motivo genérico, e o usuário nunca descobre de quem é (exceção 2 do doc 07). Usar `ExistsActiveByDedupKeyAsync` aqui colapsaria os dois casos e mandaria o reenvio legítimo para a fila.
- **A reivindicação relê o artefato pelo mesmo parser — não guarda instrumento destrinchado.** Quem reivindica escolhe de **quem** é o boleto, nunca **o que** ele diz: a linha digitável volta a passar pelos mesmos dígitos verificadores do caminho automático. E a `Bill` nasce sem `ExtractedPayer`, porque ninguém constatou o pagador — preencher com o CNPJ do credor faria o check `PayerMatch` reprovar o boleto por contradizer o cadastro.
- **`BillOrigin.CONTENT_HASH_MAX_LENGTH` é 100 e não 64 porque o hash viaja com o nome do algoritmo.** `CaptureItem` grava `sha256:` + 64 hexadecimais = 71 caracteres, para que trocar de algoritmo depois não torne ilegível o que já foi gravado. Os dois lados descrevem o mesmo dado e precisam do mesmo tamanho — dimensionar a origem para o hash cru fazia a promoção estourar na criação, e só apareceu no teste de integração.
- **A escada de link tem QUATRO travas, e nenhuma substitui as outras.** (1) Allowlist por host **e porta**, por receita; (2) endereço resolvido conferido contra faixa interna (`SafeUrlPolicy`); (3) redirecionamento **não seguido** (`AllowAutoRedirect = false`); (4) teto de requisições por mensagem. Só `GET`, nunca formulário. Tirar a (2) deixa um host autorizado cujo DNS mude alcançar `169.254.169.254`; tirar a (3) permite que o próprio host autorizado mande o cliente para qualquer lugar; tirar a (4) transforma um e-mail construído de propósito em amplificador de tráfego saindo da nossa rede. É o único ponto do BC que busca endereço vindo de fora.
- **A allowlist é aplicada ao endereço DESEMBRULHADO, nunca ao `href`.** Todo boleto por link medido chega dentro de `awstrack.me/L0/<url-encoded>/…`. Autorizar o host do rastreador seria autorizar redirecionamento para qualquer lugar; recusá-lo sem desembrulhar perderia todos os boletos por link que existem. `LinkUnwrapService` desfaz o embrulho **sem nenhuma chamada de rede** — decodificar é mais barato *e* mais seguro que seguir o redirecionamento, que ainda entregaria ao remetente a confirmação de que a mensagem foi aberta. O que não desembrulha (a EDP usa um `?ref=` opaco) segue apontando para o rastreador e é recusado, que é o desfecho certo.
- **A allowlist NÃO pode ser derivada do domínio do remetente.** Medido: a SABESP publica o PDF em `7az.com.br` e a EDP em `montreal.com.br` — terceirizadas sem relação nenhuma com o domínio do e-mail. Derivar do remetente recusaria os dois casos reais **e** autorizaria qualquer coisa hospedada no domínio de quem mandou.
- **URL de boleto é credencial ao portador, e o log só recebe o host.** As quatro sondadas em 2026-08-12 respondem `200` sem autenticação: quem tem o link tem o boleto. `CaptureItem.SourceUrl` já sai por API sob o mesmo portão do ADR-008 que esconde o `StorageKey` — quando os campos financeiros chegarem na 2.6, entram atrás do mesmo gate.
- **O degrau mais barato da cascata é o corpo do e-mail, e ele roda ANTES de qualquer link.** A SABESP manda o BR Code inteiro no texto (formato novo) e a linha digitável de arrecadação (formato antigo): os dois resolvem sem abrir arquivo e sem tocar a rede. Buscar o PDF de uma fatura cujo Pix já está no corpo gastaria rede — e abriria superfície de ataque — para descobrir o que estava escrito ali.
- **Tag de bloco vira quebra de linha; tag inline desaparece.** É a regra do `HtmlText`, e ela existe porque o varredor de candidatos trata quebra de linha como fim de sequência de dígitos. Se toda tag virasse quebra, uma linha digitável partida em `<span>`s — que é como muito e-mail de cobrança é montado — seria cortada ao meio e nunca fecharia o DV; se nenhuma virasse, células vizinhas de uma tabela se emendariam e criariam candidatos que não existem no documento.
- **Mensagem sem anexo VIRA item quando o corpo tem sinal — e o portão não é palavra-chave.** `BodyCaptureGateService` aceita por três sinais determinísticos: BR Code no texto, sequência de 47+ dígitos, ou link para host **com receita configurada**. Link para host desconhecido **não** é sinal: sem receita o sistema não teria como buscar o documento, e o item nasceria só para morrer na quarentena. Sem portão, toda conversa da caixa viraria `CaptureItem` e a fila ficaria inútil — mesma lição que fixou o descarte por desfecho na 2.3.
- **O corpo é rebuscado no processamento, não guardado da varredura.** Ele carrega linha digitável e BR Code — é instrumento de pagamento. Segurá-lo em memória entre a varredura e o processamento o espalharia por dumps e por qualquer diagnóstico do worker; a segunda leitura custa uma chamada e mantém o dado sensível com tempo de vida curto, exatamente como o anexo nunca viaja na listagem.
- **`Recipes` vazio no `appsettings` aplica os padrões medidos; `Recipes` preenchido SUBSTITUI a lista inteira.** Não é default de propriedade porque o binder de configuração mescla coleção **por índice**: um `appsettings` com uma receita sobrescreveria a primeira e manteria as demais, produzindo uma allowlist que ninguém escreveu. Só entram como padrão os hosts **sondados** — configurar um host que não se sabe responder faria a escada gastar requisição em silêncio e o desfecho parecer falha do emissor.
- **Texto e QR rodam os dois, sempre — não em cascata excludente.** Num boleto híbrido a linha digitável vem do texto e o BR Code vem da imagem, e é a presença **simultânea** dos dois trilhos que sustenta o check `PixBarcodeConsistency`, a defesa contra QR adulterado colado sobre boleto verdadeiro. Parar no primeiro degrau que resolve desligaria essa defesa em todo documento híbrido — medido: **18 dos 41** documentos do corpus são híbridos, e com cascata excludente o check teria zero dados. O `seen` compartilhado entre os dois scanners deduplica.
- **`DigitableLine.FromBarcode` reconstrói e delega ao `Parse`, nunca monta o VO direto.** O código de barras impresso é às vezes a única fonte legível (documento digitalizado não tem camada de texto, e o que o leitor decodifica é a barra ITF). Reconstruir a linha e passar pelo `Parse` mantém DVs, banco não atribuído e rollover de vencimento provados **num lugar só** — um caminho de construção que pulasse isso seria porta dos fundos para dentro do núcleo determinístico. Em arrecadação o DV de bloco admite mais de um valor, então a linha reconstruída pode diferir da impressa em um dígito; **não afeta a deduplicação**, porque a chave natural do instrumento vem do `Barcode`, que é idêntico.
- **Duas armadilhas do leitor de imagem de PDF, ambas silenciosas.** (1) `Decode` devolve **um** código por imagem; boleto de concessionária costuma ter dois QR — um de nota fiscal e outro de Pix —, e sem `DecodeMultiple` o da nota vence e o Pix nunca é visto. (2) `TryGetPng` **falha em `/DCTDecode`**, que é JPEG e é o formato que as concessionárias usam; os bytes brutos já são um JPEG e o SkiaSharp os lê direto. As duas falhavam **sem erro**: o documento resolvia pelo código de barras e ninguém notava que o trilho preferencial havia sumido. Juntas valeram +8 instrumentos Pix e quase o dobro de documentos híbridos no corpus.
- **O leitor de QR lê as imagens embutidas, não rasteriza a página.** Boleto imprime o QR como imagem embutida, e extraí-la custa uma fração de renderizar a página — que exigiria motor de rasterização e binário nativo a mais no contêiner. QR desenhado como vetor cai para o extrator de visão, e a métrica da cascata mostra isso.
- **O scanner gera e valida; não reconhece.** Não existe regex confiável para linha digitável — ela aparece com pontos, espaços, quebrada, ou colada em outro número. `CandidateScanner` produz todas as janelas de 47 e 48 dígitos e deixa `DigitableLine.Parse` reprovar: **construir a instância é a prova dos DVs**. `DomainException` ali é fluxo normal (milhares de janelas são lixo) — é o **único** lugar do BC onde engoli-la é correto.
- **Quebra de linha encerra a sequência de dígitos, de propósito.** Emendar dígitos de linhas diferentes produziria números que não existem no documento, e um deles poderia passar nos quatro DVs por acaso — o falso positivo já observado no corpus (`banco=000`, R$ 4.411.000,00). Verificado na medição de 2026-08-11 que essa regra **não** causa falso negativo: os documentos não resolvidos não têm a linha no texto, em forma nenhuma.
- **Uma senha por tentativa de abertura, mesmo sendo mais lento.** O PdfPig aceita uma lista de candidatas de uma vez, mas não diz **qual** abriu — e sem isso não há evidência para o `UnlockedBy`, que o ADR-009 exige. O teto de candidatas (`ExtractionOptions.MaxPasswordCandidates`, default 40) é o que impede um PDF hostil de virar laço caro.
- **Dois workers de captura, não um.** Varrer caixa e processar artefato têm ritmos e modos de falha diferentes: a varredura é uma chamada leve por fonte a cada minuto; o processamento baixa megabytes e roda extração. Juntá-los faria um anexo lento atrasar a varredura de **todas** as caixas — e é a varredura que garante que nada fica para trás. Os dois entram sob a mesma chave `Capture:Enabled`, com intervalos próprios.
- **Item cujo download falhou não volta sozinho para a fila.** `ListPendingAsync` traz só `Received`; `LinkFailed` fica de fora porque insistir para sempre contra um anexo que o provedor não entrega seria um laço sem fim. A nova tentativa é decisão de quem opera. O processamento em si **é idempotente** — baixar e extrair de novo produz o mesmo desfecho —, então um item que estourou por defeito permanece em `Received` e volta no ciclo seguinte sem risco de efeito duplicado.
- **A quarentena do parser é `Unrecognized`, não `Unrouted`.** São coisas diferentes: `Unrecognized` = "não achei boleto neste artefato"; `Unrouted` = "achei, mas não sei de quem é" — desfecho de **roteamento**, que só existe depois da extração. A primeira versão do `ProcessCaptureItemCommand` mandava a quarentena para `Unrouted` e a máquina de estados recusou (`Received → Unrouted` não existe) — o teste pegou. E `Unrecognized` é o estado certo também porque é dele que sai o caminho do doc 09: a pessoa informa a linha digitável à mão e o item volta para `Parsed`.
- **Download vazio conta como download ausente.** Um adapter que devolve zero byte não entregou o artefato, e seguir com ele faria a cascata concluir "não é boleto" sobre um nada — descartando um documento que a próxima tentativa traria. Vale para `null` e para `IsEmpty`. Achado por teste, depois de a caixa falsa converter `byte[]` nulo em `ReadOnlyMemory` **vazio** por conversão implícita.
- **Só o desfecho `Parse` guarda o arquivo.** `Drop` apaga o item inteiro; `Lock` e `Quarantine` mantêm o registro para uma pessoa resolver, mas **sem o original** — o que ela precisa ver é remetente, assunto e data, e digitar a linha à mão não depende do arquivo. É a retenção por desfecho, e é o que impede o balde de virar depósito de documento pessoal.
- **Artefato sem instrumento válido é DESCARTADO, não vira `Unrecognized`** (decisão do usuário, 2026-08-11). O destino padrão de um anexo que a cascata não reconheceu é sumir: nem item, nem arquivo guardado. O motivo é operacional — `Unrecognized` como padrão encheria a fila de e-mail irrelevante e ninguém olharia uma fila assim. **O filtro é determinístico e por isso é seguro**: só sobrevive quem tem código de barras com DV válido ou QR com CRC válido, e CNH, contrato ou apresentação não têm. Do que é descartado o sistema não retém arquivo; no máximo, contagem para métrica da cascata.
- **A única exceção ao descarte: remetente já cadastrado.** Quando o remetente casa com um `Payee` ou um `TrustedOrigin` do tenant e ainda assim a cascata não achou instrumento, a hipótese provável **não** é "não era boleto" — é **falha do parser**. Aí o item fica em `Unrecognized`, para um humano informar a linha digitável à mão (degrau 4 do doc 09). O volume é pequeno por construção: só entra quem o próprio tenant cadastrou, e é justamente de quem ele espera receber conta.
- **Palavra-chave no assunto NÃO decide descartar — decide gastar.** Filtrar por "conta"/"boleto" antes da cascata produz o erro oposto ao que se quer: apaga **boleto de verdade** em silêncio. Medido na caixa real (2026-08-11): existe mensagem de cobrança com assunto **"Sua fatura chegou"**, sem nenhuma das duas palavras — e o mesmo vale para FGTS, DARF, GPS, "2ª via" e "nota fiscal". Como a cascata determinística já descarta o que não é boleto, o filtro por assunto não esvazia fila nenhuma: só apaga antes de conferir. Ele entra na **2.4**, e só para decidir se vale pagar o extrator de visão num PDF sem camada de texto — ali errar custa centavos, não um boleto perdido. É o mesmo princípio do ADR-011: heurística propõe, DV e consulta oficial dispõem.
- **A proteção contra armazenar documento pessoal é RETENÇÃO POR DESFECHO, decidida na 2.3.** A medição de 2026-08-11 mostrou 8 de 11 anexos que não eram conta a pagar (CNH, contrato social, contrato de locação) numa caixa real. Quem separa é a cascata do doc 09 — e ela só conclui *depois* de o arquivo já ter sido baixado. Portanto: **`Promoted` guarda o arquivo** (é comprovação de pagamento); **`Unrecognized` e `ForeignPayer` purgam automaticamente** após janela curta. A janela existe porque `Unrecognized` é a fila onde um humano ainda pode informar a linha digitável à mão — apagar na hora tiraria essa chance. De um não-boleto o sistema retém só remetente, assunto e data, nunca o arquivo.
- **Trocar de pasta descarta o cursor, obrigatoriamente.** A varredura incremental do provedor é **por pasta**: um cursor obtido lendo a caixa de entrada não significa nada dentro de "Contas". Mantê-lo faria a primeira varredura da pasta nova voltar vazia e o sistema concluir que não há boleto ali — falha silenciosa, exatamente o que o ADR-014 existe para evitar. Quem garante é `CaptureSource.ChangeFolder`, não o handler.
- **A prova de acesso cobre a pasta, não só a caixa.** Fonte apontada para pasta inexistente devolveria zero mensagens **sem erro nenhum**. Por isso `IMailboxReader.ProbeAccessAsync` recebe o caminho e o adapter resolve a pasta antes de responder `Granted`.
- **No Graph, 401/403 é `Denied` — e isso é o oposto do Asaas, de propósito.** No Asaas, 403 é retentável porque costuma ser limite de taxa disfarçado. No Graph é a **Application Access Policy** dizendo que aquele aplicativo não alcança aquela caixa: retentar a cada minuto para sempre esconderia um problema de configuração que só uma pessoa resolve. Ao copiar a classificação de um adapter para outro, **releia o que cada status significa naquele provedor** em vez de assumir simetria.
- **Cursor ilegível vira `CursorExpired`, não exceção.** Um `deltaLink` corrompido no banco chegava cru ao `HttpClient` e estourava `InvalidOperationException` — que não é falha de transporte, escapava do adapter e derrubava a varredura inteira em vez de ser registrada. A recuperação certa já existia: descartar o cursor e varrer a caixa toda. Coberto por teste de regressão.
- **O `@odata.nextLink` também serve de cursor.** Ele continua a *mesma* varredura, então parar no teto de páginas e guardá-lo faz a varredura seguinte **retomar de onde parou**, em vez de recomeçar a caixa. O `@odata.deltaLink` só aparece na última página — uma varredura interrompida no meio não tem cursor final a guardar, e é justamente por isso que o `nextLink` importa.
- **O cache de token não é otimização.** Sem ele, cada varredura de cada fonte pediria um token novo e o Entra ID limitaria a taxa da própria autenticação — a captura passaria a falhar por throttling do login, não por problema na caixa. A chave do cache **não inclui o segredo**: guardá-lo em chave de dicionário o espalharia por dumps de memória e por qualquer diagnóstico que imprimisse o cache.
- **Desde a 2.5, mensagem sem anexo PODE virar `CaptureItem`** — quem decide é o portão do corpo, não mais `hasAttachments`. Para recuperar o histórico que a regra antiga deixou passar, use `ResetCursor` (via `POST /capture-sources/{id}/rescan`): o Graph devolve tudo que ainda está na caixa, e a ingestão é idempotente por `(tenant, fonte, mensagem, artefato)`.
- **`Denied` e `Unavailable` na leitura de caixa não colapsam.** Pedem reações opostas do usuário — arrumar o registro de aplicativo versus tentar de novo — e viram erros distintos (`BLP.CPS13` × `BLP.CPS14`). E `CursorExpired` é um terceiro: a resposta a ele é `ResetCursor` + varredura completa, não retentar igual. Colapsá-lo em `Unavailable` faria a fonte parar de sincronizar em silêncio.
- **`CursorCodec` (`Application/Queries/`) é a única implementação do cursor de keyset, e ele carrega `(CreatedAt, Id)` — não só a data.** O `Id` **não é enfeite**: `CreatedAt` não é único e o empate é o caso normal, porque uma varredura carimba um instante só e o repassa a todos os itens que ingere (medido em produção em 2026-08-11: 404 itens, `count(DISTINCT created_at) = 1`). Com o cursor só da data, a página 2 filtrava `CreatedAt > T` e voltava **vazia** — tudo além da primeira página inalcançável, sem erro e sem log, com `items: []` e `nextCursor: null` afirmando que a lista acabou. **A direção do desempate acompanha a da chave**: lista ascendente desempata `Id` ascendente e filtra `>`; descendente desempata descendente e filtra `<` (o `BillQueries` cruzava as direções, o que reintroduz o mesmo buraco). Cursor no formato antigo de 8 bytes **não é honrado pela metade** — `TryDecode` recusa e a lista reinicia, mesma degradação de um cursor corrompido ou forjado. Coberto por `CaptureItemPaginationTests`, que semeia mais itens com `CreatedAt` idêntico do que cabe numa página — um teste com datas distintas passa e não prova nada.
- **Os cinco Ids paginados implementam `IComparable<T>` e os operadores de ordem** (`BillId`, `PayeeId`, `TrustedOriginId`, `CaptureSourceId`, `CaptureItemId`) — sem isso a comparação do desempate não compila, e o EF não teria o que traduzir. **A ordem que vale é a do `uuid` no Postgres**, não a destes operadores: `Guid.CompareTo` do .NET compara por campos e as duas sequências não coincidem. É inofensivo porque `ORDER BY` e `WHERE` são ambos avaliados no banco; vira armadilha no dia em que alguém ordenar a coleção **em memória** esperando a mesma sequência.
- **`EmailSyntax` (SharedKernel) é a única implementação de normalização de e-mail do BC.** Nasceu privada dentro do `TrustedOrigin` e saiu de lá quando `CaptureSource.Address` e `CaptureItem.Sender` passaram a precisar da mesma regra. É helper estático e **não** Value Object de propósito: os consumidores guardam a string normalizada porque ela precisa ser endereçável a partir da raiz para o índice único do EF — um VO aqui viraria owned type e recairia no problema já documentado acima. Normalizar em qualquer outro lugar é como o cadastro passa a divergir da consulta.
- **QR Pix estático não deduplica.** `PaymentInstrument.IsSingleUse` é `false` para QR estático porque o mesmo payload é reutilizado indefinidamente — um fornecedor manda todo mês a conta com o mesmo QR, e deduplicar por ele bloquearia a de fevereiro por causa da de janeiro. Só código de barras e QR **dinâmico** viram `Bill.DedupKey`; sem chave, a defesa contra duplicata passa a ser (beneficiário, valor, vencimento), ainda por implementar na 1.4.
- **O banco recebedor sai do código de barras, não do provedor.** `DigitableLine.BankCode` lê as posições 1–3 (COMPE) e é a fonte do check 6; `Lookup.BankCode` serve de conferência cruzada, e divergência entre os dois é bloqueante. Vale só para `BillKind.BankSlip` — **arrecadação não tem campo de banco em posição nenhuma** e `BankCode` lança `BLP.DGL06` lá, de propósito, para a chamada indevida falhar alto em vez de devolver lixo. No trilho Pix a instituição é **ISPB de 8 dígitos**, incompatível com COMPE sem a tabela do Bacen.
- **Tabela de bancos é snapshot embutido, não consulta ao vivo.** `Infra/BankDirectory/bacen-participants.csv` é `EmbeddedResource`, gerado por `tools/fetch-bacen-participants.js` a partir da [relação de participantes do STR](https://www.bcb.gov.br/pom/spb/estatistica/port/ParticipantesSTRport.csv). **Buscar em tempo de validação faria indisponibilidade do bcb.gov.br virar bloqueio de pagamento.** A tabela muda algumas vezes por ano; o arquivo versionado deixa a mudança auditável no diff. `IBankDirectory` é síncrono e sem `CancellationToken` de propósito — não é I/O. Registrado como **singleton**. O teste que importa está em `BacenBankDirectoryTests`: ele resolve a porta pelo DI justamente para provar que o recurso **embarca no assembly publicado**, que é o defeito que passaria despercebido.
- **Duas invariantes de aprovação são defesa em profundidade, não caminho quente.** `BLP.BIL03` (catálogo de checks incompleto) e `BLP.BIL04` (falha bloqueante) **não têm como ser alcançadas hoje**: `RecordChecks` recusa conjunto parcial, e um boleto com bloqueio já está em `Rejected`, então a guarda de situação (`BLP.BIL25`) dispara antes. Elas existem porque `Approve` é a operação mais perigosa do sistema e porque a BIL03 passa a valer no dia em que um `CheckType` novo for acrescentado. **Não as remova por "código morto"** — e não escreva teste que force o estado por reflexão.
- **Quem decide nunca vem do corpo da requisição.** A data de pagamento vem (é escolha do aprovador); a identidade vem do token, ou — nesta fase, sem Keycloak — do header `x-user-id`. Aceitar o `UserId` no body permitiria aprovar em nome de outra pessoa, e o ADR-007 apoia toda a trilha nesse campo. Quem recusa identidade vazia é o **domínio** (`BLP.BIL22`), não o controller, para a regra viver num lugar só.
- **Documento na consulta decide sozinho; nome só vale quando não há documento.** `PayeeResolutionService` tenta o CNPJ; **não casando, o cotejo por nome vira detecção de sósia, nunca confirmação**. A primeira versão caía para nome nesse caso e transformava o pior cenário no melhor — consulta com o nome de um fornecedor conhecido e CNPJ de terceiro virava `Passed`. O fallback por nome existe só quando a consulta **não trouxe** documento (100% da arrecadação). Coberto por teste; não "simplifique" reunificando os dois caminhos.
- **`Warning` é o quinto resultado e nunca bloqueia**, qualquer que seja a severidade do check. Existe porque as duas alternativas falhavam na divergência de nome em arrecadação: `Failed` num check `Blocking` travaria pagamento por grafia de concessionária, e `Passed` jogaria fora a única evidência de beneficiário que arrecadação oferece. Só `Failed` reprova.
- **A severidade viaja no `CheckResult`, não só no `CheckType`.** Três checks `Advisory` viram `Blocking` em situação específica: duas fontes autoritativas discordando sobre o banco, pagador extraído contradizendo o cadastro, e origem explicitamente banida.
- **`RecordChecks` exige o catálogo completo** (`BLP.BIL19`) e é o **único** ponto que muda status por validação. Devolve `ValidationOutcome` — o handler nunca lê `bill.Checks` para montar resposta. Revalidar um boleto já aprovado **derruba a aprovação incondicionalmente** (mais rígido que o doc 03, ver a nota em `02-domain-model.md`).
- **`BillValidationService` e `PayeeResolutionService` são `static`, e isso é intencional.** São funções puras sobre valores do domínio: sem estado, sem I/O, sem relógio — a data e a hora entram pelo `BillValidationContext`. Não há nada a substituir em teste (a suíte usa agregados reais via Mother), e instanciá-las só para satisfazer o DI inventaria estado que não existe. Quando o limiar de sósia virar configuração, aí sim viram instância.
- **Consulta que não resolveu não apaga o retrato anterior.** `AttachLookups` substitui só quando resolve, e registra **toda** tentativa em `LookupHistory`. Apagar deixaria o boleto sem evidência nenhuma justamente quando a rede falhou. **A garantia de só-append é invariante de domínio, não de armazenamento** — o histórico é uma coluna jsonb (os retratos têm `Money`/`TaxId` aninhados); promover para tabela filha append-only é o passo seguinte se a auditoria exigir a garantia no banco.
- **`bill_checks` é tabela filha, `lookup`/`pix_lookup`/`lookup_history` são jsonb.** Não é inconsistência: `BillCheck` só tem escalares (owned de 1º nível, o EF rastreia bem), enquanto os retratos têm VO aninhado e recairiam na armadilha do 2º nível. A tabela também é o que permite, depois, uma fila operacional filtrada por motivo em SQL.
- **O contrato do provedor é medido, não lido da documentação.** As duas sondas de produção (2026-08-06) acharam divergências: `bank` vem como **string** de três dígitos (não objeto), e o decode Pix devolve **seis campos** que a documentação não anunciava. Ao tocar em `AsaasContracts`, rode as sondas em vez de confiar no texto do provedor — `tools/smoke-probe-*.js` reportam aderência ao contrato assumido.
- **Decisão em aberto: o pagador do Pix não vem mascarado.** Produção devolveu o CNPJ **completo** do pagador num QR dinâmico com cobrança registrada (`cobv`). O `MaskedParty` tolera as duas formas e o comportamento atual é seguro (contradição bloqueia, compatibilidade não confirma), **mas isso contraria a premissa do ADR-004** de que não existe fonte autoritativa de pagador. Promover `PayerMatch` a check forte exige reabrir o ADR **e cravar o escopo**: vale só para Pix dinâmico com `cobv`, não para QR estático nem para boleto. Não generalize sem essa decisão.
- **Falha de consulta oficial é modelada, não lançada.** `IBillLookupService`/`IPixLookupService` devolvem `BillLookupResult`/`PixLookupResult` com um `LookupStatus`, nunca exceção por documento não encontrado. A distinção que justifica o tipo: **`Unresolved`** (o provedor respondeu que não conhece o título — retentar dá o mesmo) × **`Unavailable`** (timeout, 5xx, circuito aberto, credencial ausente — nada foi aprendido sobre o documento). Colapsar as duas faria indisponibilidade de rede virar suspeita do boleto. E `Unresolved` é o caso **comum**, não o excepcional: 0/12 das linhas de cobrança do corpus resolveram em sandbox.
- **O cliente HTTP da consulta retenta; o do pagamento não pode.** `AddStandardResilienceHandler` está ligado em `bill/simulate` e `pix/qrCodes/decode` porque os dois são read-only e idempotentes. **O adapter de pagamento da fase 3 precisa de um cliente próprio, sem retry automático** — sobretudo o de Pix, cujo endpoint não documenta idempotência e pagaria duas vezes numa retentativa de rede. Reaproveitar o cliente de consulta lá é o erro a não cometer.
- **Sem credencial, o adapter degrada para `Unavailable`, nunca para "verificado".** `UnconfiguredBillLookupService`/`UnconfiguredPixLookupService` entram no DI quando `Asaas:ApiKey` não está configurada. A suíte de integração roda assim de propósito — testes não devem ter chave capaz de pagar contas. O que importa é que a ausência fique registrada como *"não foi possível verificar"*: um adapter que devolvesse resultado vazio "com sucesso" faria a tela de aprovação mostrar check pulado como se o documento tivesse passado.
- **Os DTOs do provedor são frouxos nos dois sentidos, e precisam ser.** Datas e valores são `string?` porque o Asaas devolve `""` para data ausente em arrecadação (desserializar direto para `DateOnly?` derrubaria a resposta inteira); e usam `LenientStringConverter` porque valor vem como **número** JSON. Tirar uma das duas frouxidões quebra a mesma resposta pelo lado oposto. O campo `bank` é `JsonElement?` porque **nunca foi observado preenchido** — aceita string e objeto com `code`.
- **O cofre de segredos não commita.** `ISecretVault.StoreAsync`/`ReplaceAsync`/`RemoveAsync` só registram a mudança no `DbContext` de quem chamou; o efeito existe no `SaveEntitiesAsync` do handler. É o que torna atômico guardar a credencial e criar o agregado que a referencia — senão uma falha no meio deixa credencial órfã ou agregado apontando para o vazio. `ResolveAsync` usa `FindAsync` (consulta o rastreador antes do banco) justamente para resolver uma credencial guardada na mesma unidade de trabalho.
- **`TenantId` e `SecretKind` entram no AAD da cifra**, não são só colunas de busca: mover a linha para outro tenant ou reapresentá-la como outro tipo faz a decifragem **falhar** (`BLP.SEC04`), coberto por teste que adultera a linha via SQL. Nonce novo a cada gravação — `TenantSecret` não tem setter por campo justamente para não existir caminho que atualize o texto cifrado sem trocar o nonce. **Sem master key, entra um cofre que falha em toda operação**, inclusive leitura: guardar em claro trocaria falha barulhenta por vazamento silencioso.
- **`PixPayload` valida o CRC, não o significado.** BR Code é EMV/TLV com CRC-16/CCITT-FALSE nos últimos 4 caracteres, cobrindo inclusive o próprio `"6304"`. Construir a instância prova o CRC — QR copiado pela metade não vira pagamento. **O BR Code não carrega CPF/CNPJ do recebedor**, só chave e nome; o documento vem da consulta ao PSP, e é por isso que o check de consistência QR × código de barras precisa da consulta dos dois lados. O teste âncora é o vetor publicado do algoritmo (`"123456789"` → `29B1`) — conferir contra um exemplo de BR Code só provaria que dois erros combinam.
- **`DigitableLine` é o núcleo determinístico do BC.** Construir a instância *é* a prova dos DVs — não existe instância inválida. Mas **DV não basta**: uma janela de 47 dígitos de lixo do corpus real passou nos quatro DVs (`banco=000 valor=4.411.000,00`), por isso o VO também rejeita banco não atribuído. `Parse` recebe `today` por parâmetro porque o fator de vencimento é ambíguo entre duas épocas (rollover de 2025-02-22) e o domínio não lê relógio.
- **Valor livre vai em query string, não em segmento de rota.** CNPJ formatado contém `/` e apelido é texto livre: no path viram outro segmento e a requisição morre em 404 de roteamento, antes do controller. Por isso `GET payees/by-tax-id?taxId=`, `DELETE payees/{id}/aliases?alias=` e `DELETE payer-profile/tax-ids?taxId=`. Só fica no path o que é sabidamente seguro (`{id:guid}`, `{bankCode}` de três dígitos).
- **Handler não compõe Value Object — o agregado tem sobrecarga por primitivos.** `Payee.Register`/`ChangeAmountPolicy`/`AllowBank`/`DisallowBank` e `PayerProfile.Register`/`AddAdditionalTaxId`/`RemoveAdditionalTaxId` têm par que recebe `string`/`decimal?` e compõe o VO internamente (`TaxId.Parse`, `AmountPolicy.From`). Traduzir `string` → Smart Enum continua sendo do handler. Toggles são um método só no agregado (`Payee.SetActivation`, `PayerProfile.SetCnpjRootMatching`) para o `if` não vazar para a Application.
- **Cross-aggregate FK removal**: `OnModelCreating` strips non-ownership FKs discovered by convention. Cross-aggregate references are by ID only, no navigation properties, no FK constraints.
- **DbContext.SaveEntitiesAsync drena domain events** dos aggregates tracked para `outbox_messages` antes do `base.SaveChangesAsync`, na **mesma transação implícita** do efeito. `EventType` é gravado como `Type.FullName`. O drain tem **um `case` por Aggregate Root** (`AggregateRoot<TId>` é genérico e não tem base não-genérica comum): **acrescente o novo agregado ao `DrainDomainEvents` ao criá-lo**, senão ele emite eventos que ninguém publica e a falha é silenciosa. Hoje só `Bill` emite.
- **Outbox consumer (in-process, Domain puro)**: `OutboxBackgroundService` (registrado só quando `Outbox:Enabled=true` — ver `OutboxOptions`) faz polling e delega ao `OutboxProcessor`. Claim de uma mensagem por vez via `SELECT … FOR UPDATE SKIP LOCKED`, uma transação por mensagem (envolta em `ExecutionStrategy`), desserialização via `IOutboxEventTypeResolver` (singleton indexando `IDomainEvent` do assembly do Domain por `FullName`), dispatch por `IDomainEventDispatcher` (porta no Domain, impl na Infra, sem MediatR), marca `processed=true` e commita. Handlers que só tocam o próprio banco são effectively-once; efeito externo é at-least-once e deve ser idempotente.
- **Dead-letter + retry**: falha incrementa `attempts`/`error`; em `OutboxOptions.MaxAttempts` a mensagem é movida para `outbox_dead_letters`. `CleanupAsync` purga `processed=true` além de `RetentionDays`. Ordem não garantida sob paralelismo; ao escalar horizontalmente, rode o worker em **um** deployment (`Outbox:Enabled`).
- **`IDomainEventHandler<T>` / `IDomainEventDispatcher`** são portas puras em `Domain/SeedWork` (`Infra → Application` não existe — seria ciclo). Impl e handlers vivem na Infra (`Outbox/`); registre cada handler como `IDomainEventHandler<TEvent>` em `InfraDependencies.AddOutbox`.
- **DomainExceptionFilter** handles `DomainException` (from Domain) and `InvalidOperationException` (from Application). HTTP status is driven by `DomainException.Category` (`DomainErrorCategory`): `Validation` → 400, `Conflict` → 409, `NotFound` → 404. Error factories pass the category; the filter has no hardcoded error codes.
- **Query side (CQRS) — exceção autorizada de dependência**: `BillPayment.Application.csproj` referencia **BillPayment.Infra** exclusivamente para o query side (padrão eShop `IOrderQueries`): interface `IXxxQueries` + impl com `AsNoTracking` em `Application/Queries/`, **injetadas direto no controller, sem mediator**. Commands continuam 100% via mediator + `IdentifiedCommand`. **Não "corrigir" essa referência Application → Infra** — é decisão deliberada do BC; consequência: `Infra → Application` não pode existir (ciclo), por isso as portas que a Infra implementa vivem em `Domain/SeedWork`. (Sem queries no esqueleto ainda.)
- **TenantId via rota**: controllers multi-tenant usam `[Route("api/v1/{tenantId}/[controller]")]` e recebem `[FromRoute] Guid tenantId`. Será validado contra JWT em fase futura (Keycloak).
- **Nada além de endpoint mora em `API/Controllers/`.** Model HTTP vive em `Application/Models/<Aggregate>/<Aggregate>Models.cs`; DTO de leitura vive em `Application/Queries/<Aggregate>/<Aggregate>Dtos.cs`, **fora** do arquivo da interface `IXxxQueries`. O Model existe porque o `tenantId` vem da rota e não pode vir do corpo (vetor de IDOR), e **todo** Model expõe `ToCommand(tenantId, ...)` — o controller nunca monta um Command com `new`, senão a composição vaza de volta para a borda HTTP e o arquivo volta a crescer.
- **A config de desenvolvimento não pode dirigir a suíte.** A fábrica roda como `Development`, então ela lê o `appsettings.Development.json` **e os user-secrets da máquina de quem desenvolve** — e o perfil de dev hoje liga a captura e aponta o balde para o MinIO local. Por isso `IntegrationTestWebAppFactory` desliga explicitamente `Capture:Enabled`, `Graph:Enabled`, `Storage:ServiceUrl`, **`Asaas:ApiKey`** e **`DocumentIntelligence:Provider`**, junto com o `Outbox:Enabled` que já estava lá. A do extrator de visão é a mais crítica: a chave do Gemini vive no user-secrets e o perfil de dev liga o provedor, então sem ela cada execução da suíte gastaria cota de uma conta gratuita — e os testes deixariam de ser determinísticos, porque a mesma entrada pode devolver leituras diferentes. A do Asaas já era regra escrita — só não era executável: quem a sustentava era a ausência do segredo na máquina, e no dia em que alguém rodou `dotnet user-secrets set "Asaas:ApiKey"` os `UnconfiguredLookupTests` voltaram `Unresolved` (resposta real do provedor) em vez de `Unavailable`. Além de deixarem de provar a degradação, tornavam a suíte refém do Asaas estar no ar. Sem isso, os dois workers de captura varreriam e processariam por conta própria enquanto o Respawn limpa o banco, e a fábrica compartilhada deixaria de exercitar o armazenamento não configurado — que é justamente o que ela existe para provar. `UnconfiguredStorageTests` é a prova de que a blindagem vence: os quatro campos de `Storage` chegam preenchidos pelo perfil de dev e o host ainda resolve o substituto.
- **Integration tests** use Testcontainers (`postgres:17`) + Respawn + `EnsureCreatedAsync` (no migrations yet). DTOs are duplicated in the test project (not reused from Application). O `IntegrationTestWebAppFactory` seta `Outbox:Enabled=false` para o worker não competir com os testes — o outbox é dirigido determinísticamente chamando `IOutboxProcessor.ProcessPendingAsync`/`CleanupAsync` — e `Secrets:MasterKey` com uma chave **descartável, gerada por execução** (o cofre precisa de uma para existir, e a suíte não pode depender de segredo de máquina nem carregar valor versionado). **Nenhuma chave do Asaas é configurada de propósito.**
- **A troca da consulta oficial por uma falsa é por classe de teste, não global.** `IntegrationTestWebAppFactory.WithFakeLookups()` devolve um host irmão (mesmo contêiner, mesmo banco) com `FakeLookupServices` no lugar dos adapters. Trocá-la na fábrica compartilhada derruba os `UnconfiguredLookupTests`, que existem justamente para provar o contrário — que sem chave a consulta degrada para `Unavailable`. Já aconteceu uma vez; não centralize.
- **`InternalsVisibleTo("BillPayment.IntegrationTests")` na Infra**: os adapters são `internal` (só o DI os instancia) e a suíte precisa construí-los com um `HttpMessageHandler` de teste para exercitar a tradução da resposta do provedor sem rede. Torná-los `public` só por causa do teste alargaria a superfície pública da Infra.

## Regras invioláveis de Handler (Application)

O Handler tem **uma única forma legítima**: (1) I/O de orquestração (validar IDs externos via `ExistsAsync` + `throw <Agg>Errors.NotFound`; consultas que alimentam o método rico), (2) carregar o agregado-raiz tracked filtrando por `TenantId` + `?? throw NotFound`, (3) chamar **um** método rico/factory do agregado passando primitivos ou dados resolvidos, (4) `SaveEntitiesAsync`, (5) `new XxxResponse(...)` explícito. O par `IdentifiedCommandHandler` fica no mesmo arquivo. A doutrina completa está nas skills `application-codegen-ddd-dotnet`/`domain-codegen-ddd-dotnet` — **invoque-as antes de mexer nessas camadas**. Regras consolidadas da auditoria de 2026-06 no EconomicCore (exemplos vivos lá):

1. **Handler nunca compõe Value Object** (`new TaxId(...)`, `new Money(...)`). Se a factory/método rico não aceita primitivos, crie um **overload no agregado** que compõe o VO internamente. Smart Enums via `Enumeration.FromDisplayName<T>` no handler são tradução de input e **são permitidos**.
2. **Handler nunca filtra/inspeciona coleção interna do agregado com conhecimento de domínio.** Peça um acessor ao agregado. Exceção tolerada: projeção *read-only* de coleção para montar Response, sem decidir nada.
3. **Handler nunca lê `aggregate.DomainEvents` para computar resposta.** Se o handler precisa de um resultado da mutação, o **método rico retorna** essa informação.
4. **Política de negócio nunca no handler**, mesmo quando parece "só escolher um valor". Decisão sobre **forma do input** é do handler; decisão sobre **estado/semântica de domínio** é do agregado.
5. **Erro com semântica certa, sem ler estado do agregado para fabricá-lo.** Cada checagem de orquestração tem sua factory dedicada no Domain; Application nunca define erros próprios.
6. **Queries nunca passam pelo mediator** e são a **única** exceção autorizada a tocar a Infra (ver "Query side (CQRS)" acima). Um `IRequestHandler` que injete `BillPaymentDbContext` é violação.
7. **Demais regras**: zero `repo.Update(...)` (busca tracked + `SaveEntitiesAsync`), zero `if` sobre propriedade do agregado para lançar erro (gates vivem no agregado), zero `foreach` de cascata sobre entidades internas, **um agregado mutado por transação** (efeito cross-aggregate via Outbox/relay; exceção sancionada exige `IMultiAggregateCommand` + justificativa documentada), IDs de entidades internas validados pelo método rico, `TenantId` em toda busca/`ExistsAsync`.

**Checklist antes de entregar um Handler**: algum `new <VO>(...)`? → overload no agregado. Algum `.Where`/`.Any` sobre coleção do agregado para *decidir*? → acessor/método rico. Algum acesso a `DomainEvents`? → retorno do método rico. Alguma factory de erro escolhida lendo estado do agregado? → factory dedicada. Algum `DbContext` num `IRequestHandler`? → mover para `IXxxQueries`.

## Project layout

```
BillPayment/
├── BillPayment.sln                   # isolated solution (not in RufinoProject.sln)
├── docker-compose.yml + override     # localized stack: API + Postgres
├── BillPayment.API/                  # Web SDK host, Program.cs, Controllers, Filters, Extension, appsettings, Dockerfile
│   ├── BackgroundServices/           #   CaptureSyncBackgroundService + CaptureProcessingBackgroundService + CaptureSyncOptions (Capture:Enabled, off por padrão)
│   ├── Controllers/                  #   HealthController, BaseController (EnsureRequestId), Bills/TrustedOrigins/Payees/PayerProfile — SÓ controllers, sem Model nem DTO
│   ├── Filters/                      #   DomainExceptionFilter (DomainErrorCategory → HTTP status)
│   └── Extension/                    #   CorsExtensions.AddCorsForFront (lê Cors:AllowedOrigins; Development com lista vazia libera qualquer origem)
├── BillPayment.Application/          # Mediator próprio (sem MediatR)
│   ├── Mediator/                     #   IRequest, IRequestHandler, IPipelineBehavior, IMediator, Mediator, MediatorRegistration, IdentifiedCommand, IdentifiedCommandHandler, IMultiAggregateCommand
│   ├── Behaviors/                    #   LoggingBehavior (único behavior, mais externo)
│   ├── CaptureSources/Commands/      #   Connect (prova de acesso + cofre + aviso ADR-008), Rename, AlterActivation, ReplaceCredential, ChangeFolder, Add/RemoveFolder, Rescan, Sync, Disconnect
│   ├── Bills/Commands/               #   ImportBill, ValidateBill (serve à captura e à revalidação), ApproveBill, DenyBill, CancelBill, ApprovalOptions
│   ├── Bills/EventHandlers/          #   BillCapturedDomainEventHandler — vive aqui porque precisa do mediator
│   ├── TrustedOrigins/Commands/      #   Register, ChangeDecision, Delete
│   ├── Payees/Commands/              #   Register, Rename, AlterAmountPolicy, Add/RemoveAlias, Allow/DisallowBank, AlterActivation, Delete
│   ├── PayerProfiles/Commands/       #   Register, Rename, Add/RemoveTaxId, AlterCnpjRootMatching, LinkAsaasAccount
│   ├── Models/<Aggregate>/           #   Models HTTP (body da request) + ToCommand(tenantId, ...) — um arquivo por Aggregate
│   ├── Queries/CursorCodec.cs        #   keyset por CreatedAt — implementação única, usada por TODA lista
│   └── Queries/<Aggregate>/          #   IXxxQueries + XxxQueries + XxxDtos (arquivo próprio) — única exceção autorizada a tocar a Infra
├── BillPayment.Domain/               # SeedWork + SharedKernel + Aggregates
│   ├── SeedWork/                     #   Entity, AggregateRoot, ValueObject, Enumeration, DomainException/Errors, IUnitOfWork, IDomainEvent, IDomainEventHandler, IDomainEventDispatcher, IRequestManager, IEntityId
│   ├── SharedKernel/                 #   TenantId, UserId, Money, Currency, TaxId, DateRange, CompetencePeriod, BankCode, EmailSyntax (+ *Errors)
│   ├── Bills/                        #   Bill (Aggregate Root), BillStatus, BillOrigin, BillSourceKind, BillCapturedDomainEvent, IBillRepository, BillErrors
│   ├── Instruments/                  #   BillKind, DigitableLine, PixPayload, PaymentInstrument, PaymentInstrumentKind, PaymentRail (+ Errors)
│   ├── Bills/Checks/                 #   CheckType, CheckOutcome, CheckSeverity, CheckReasons, CheckResult, BillCheck, ValidationOutcome
│   ├── Services/                     #   BillValidationService, PayeeResolutionService, BillValidationContext, CaptureTriageService, PasswordDerivationService, CandidateValidationService, VisionGateService, LinkUnwrapService, BodyCaptureGateService, BillRoutingService (Domain Services estáticos)
│   ├── Lookups/                      #   LookupSnapshot, PixLookupSnapshot, LookupParty, MaskedParty, LookupResult (+Bill/Pix), LookupStatus, LookupErrors
│   ├── Secrets/                      #   CredentialRef, SecretKind, SecretErrors — o Domain só vê o ponteiro, nunca o segredo
│   ├── Mailboxes/                    #   MailboxStatus, MailboxMessage/Artifact, MailboxResult (Probe/Read), MailboxErrors
│   ├── Extraction/                   #   ExtractionResult, PasswordCandidate, ExtractedDocument, DocumentPayload/Hints, DocumentKind, DocumentLink, ResolvedDocument, PartyCandidate, ExtractionErrors
│   ├── Ports/                        #   IBankDirectory, IBillLookupService, IPixLookupService, ISecretVault, IMailboxReader, IBoletoDocumentParser, IAttachmentStorage, IDocumentIntelligence, IDocumentLinkResolver — contratos de mundo externo (não SeedWork)
│   ├── PayerProfiles/                #   PayerProfile, PayerKind, PayerProfileId, PayerProfileErrors
│   ├── Payees/                       #   Payee, AmountPolicy, AmountPolicyKind, PayeeId, PayeeErrors
│   ├── TrustedOrigins/               #   TrustedOrigin, OriginKind, TrustDecision, TrustedOriginId, TrustedOriginErrors
│   ├── CaptureSources/               #   CaptureSource, CaptureSourceKind, CaptureSourceId, CaptureSourceErrors, ICaptureSourceRepository
│   └── CaptureItems/                 #   CaptureItem, CaptureItemStatus, ExtractionMethod, CaptureItemId, CaptureItemErrors, ICaptureItemRepository
├── BillPayment.Infra/                # EF Core DbContext, outbox, idempotency
│   ├── Persistence/                  #   BillPaymentDbContext (UoW), OutboxMessage, OutboxDeadLetter, ProcessedEventLog, ClientRequest, TenantSecret, IOutboxEventTypeResolver
│   ├── Outbox/                       #   OutboxOptions, OutboxProcessor, OutboxBackgroundService, DomainEventDispatcher
│   ├── Asaas/                        #   AsaasOptions, AsaasHttp (classificação de falha), AsaasContracts + LenientStringConverter, os dois adapters, UnconfiguredLookupServices
│   ├── Mailboxes/                    #   UnconfiguredMailboxReader + Graph/ (GraphMailboxReader, GraphTokenProvider, GraphHttp, GraphMailboxCredential, GraphOptions, GraphContracts)
│   ├── Extraction/                   #   CandidateScanner, QrCodeScanner (ZXing), PdfBoletoDocumentParser (PdfPig), EmailBodyDocumentParser, CascadingBoletoDocumentParser, HtmlText, HtmlLinkHarvester, TaxIdScanner, ExtractionOptions
│   ├── Extraction/Links/             #   LinkResolutionOptions + LinkRecipe, SafeUrlPolicy (anti-SSRF), HttpDocumentLinkResolver, NullDocumentLinkResolver
│   ├── DocumentIntelligence/         #   DocumentIntelligenceOptions, ExtractionBudget, PdfPageTrimmer, NullDocumentIntelligence + Gemini/
│   ├── Storage/                      #   S3AttachmentStorage, StorageOptions, UnconfiguredAttachmentStorage
│   ├── Secrets/                      #   SecretsOptions, EnvelopeSecretVault (AES-256-GCM), UnconfiguredSecretVault
│   ├── BankDirectory/                #   BacenBankDirectory + bacen-participants.csv (EmbeddedResource)
│   ├── Migrations/                   #   geradas por `dotnet ef` — analise desligada, nunca editadas a mao
│   ├── Idempotency/                  #   RequestManager (impl de IRequestManager sobre client_requests)
│   ├── Repositories/                 #   TrustedOriginRepository, PayeeRepository, PayerProfileRepository, CaptureSourceRepository, CaptureItemRepository
│   └── Mapping/                      #   EF configurations de plataforma + TrustedOriginMap, PayeeMap, PayerProfileMap, CaptureSourceMap, CaptureItemMap, TaxIdConversions, CredentialRefConversions
├── BillPayment.UnitTests/            # xUnit — 848 testes; pasta por Aggregate, cada uma com Mothers/
├── BillPayment.IntegrationTests/     # xUnit + Testcontainers + Respawn — 323 testes
│   ├── Infrastructure/               #   IntegrationTestWebAppFactory (Outbox:Enabled=false), BaseIntegrationTest, IntegrationTestCollection
│   ├── Contracts/                    #   DTOs de request/response DUPLICADOS de propósito
│   ├── Health/                       #   HealthCheckTests (esqueleto ponta a ponta)
│   ├── Storage/                      #   UnconfiguredStorageTests — o perfil de dev não vaza para a suíte
│   └── TrustedOrigins|Payees|PayerProfiles/  #   fatias HTTP dos três Aggregates
└── BillPayment.Architecture/         # design rationale do BC — fonte de verdade do modelo
    ├── index.md                      #   índice de entrada (registre todo doc novo aqui)
    ├── 01..11-*.md                   #   visão, modelo, verificações, integrações, use cases, roadmap, multitenancy, corpus, captura, LLM, expectativas
    ├── tools/                        #   analyze-boleto-corpus.js, probe-asaas-simulate.js, smoke-probe-{production,pix-decode,mailbox}.js, run-capture-chain.js, analyze-account-reference.js, seed-tenant.js (+ .example.json), fetch-bacen-participants.js
    └── adr/                          #   ADR-001..014
```

## Static analysis (Roslyn analyzers)

Dois arquivos na raiz do BC controlam análise estática para todos os 6 csproj:

- **`Directory.Build.props`** — herdado por todo `.csproj` C# da árvore. Liga `AnalysisLevel=latest`, `AnalysisMode=Recommended` (Minimum em test projects), `EnforceCodeStyleInBuild=true` (IDE rules rodam no build) e injeta `SonarAnalyzer.CSharp` como analyzer-only (`PrivateAssets=all`). Excluído de `docker-compose.dcproj` via guard `MSBuildProjectExtension=='.csproj'` (o SDK Docker não tem TargetFramework e quebra com PackageReferences).
- **`.editorconfig`** — `root=true`, define estilo C# moderno (file-scoped namespaces, `using` dentro do namespace, primary constructors), e ajusta severidades de regras CA/IDE/Sxxx ruidosas. Tem três blocos de override por path: global `[*.cs]`, `[**/BillPayment.UnitTests/**.cs]` e `[**/BillPayment.IntegrationTests/**.cs]`.

**Stack ativa**: NetAnalyzers (built-in do SDK .NET 10) + SonarAnalyzer.CSharp (≥ `10.27.0.140913`). StyleCop e Roslynator foram avaliados e descartados (ruído desproporcional).

**Política de severidade** (herdada do EconomicCore):
- Domain: `TreatWarningsAsErrors=true` → qualquer warning quebra o build. É o nível mais rigoroso da pilha porque é o núcleo do negócio.
- Application/Infra/API: warnings aparecem no build mas **não** quebram. CI vai promover a erro depois (Checklist pré-produção).
- Tests: `AnalysisMode=Minimum` + suppressions extras (CA1707, CA1812, S2699 etc.) — testes têm convenções próprias do xUnit que não devem ser sufocadas.

**Regras suprimidas globalmente por convenção** (não rodar simplificação automática nelas):
- `S2328`, `S3249`, `S3875`, `S1210`, `S1643` — padrões canônicos do `SeedWork/` (Entity equality, Smart Enum IComparable, ValueObject.ToString) que Sonar marca como bug mas seguem a referência Vernon.
- `CA1707` — codebase usa `SCREAMING_SNAKE_CASE` em constantes de domínio (`DEFAULT_SCHEMA`, `MIN_MONTH`).
- `CA1711` — sufixos `Event`, `Template`, etc. são vocabulário do domínio.
- `CA1303`, `CA2007`, `CA2201` — DomainException PT, ASP.NET Core sem ConfigureAwait, Exception herdada por design.
- `S927`, `S4201`, `S112` — `is null`, parâmetros de override ricos, e uso de Exception base nas factories de erro do SeedWork.

Outras regras estão como `suggestion` (visíveis no IDE, não no build) ou `warning` (visíveis no build, não quebram fora do Domain). **Antes de suprimir uma regra nova, prefira corrigir o código** — só suprime se a regra conflita com uma convenção do BC explicitamente documentada aqui ou em `BillPayment.Architecture/`.

**Como rodar**: `dotnet build BillPayment.sln` já roda os analyzers. Não há comando separado. Para listar findings agregados: `dotnet build --no-incremental 2>&1 | Select-String 'warning (CA|S|IDE)\d+'`.

## Dependências

O esqueleto veio clonado do EconomicCore e trouxe junto pacotes de um BC que já tinha integrações externas. **Só entra `PackageReference` que tem uso no código** — cada pacote sem consumidor é dívida de superfície e de auditoria de vulnerabilidade. Estado atual:

- **`Microsoft.Extensions.Http.Resilience` (10.8.0) + `Polly.Core` entraram na sprint 1.3**, com o primeiro adapter HTTP. É o sucessor de `Microsoft.Extensions.Http.Polly` (que era Polly v7 e está em fim de linha): `AddStandardResilienceHandler` já combina rate limiter, timeout total, retry, circuit breaker e timeout por tentativa. **`Polly.Core` é referência direta e explícita** porque `AsaasHttp` captura `TimeoutRejectedException` e `BrokenCircuitException` no código — depender de transitiva para API que o código chama direto é o mesmo erro documentado no item abaixo.
- **`Microsoft.Extensions.Configuration.Binder`, `Hosting.Abstractions` e `Options.ConfigurationExtensions` subiram de `10.0.5` para `10.0.10`** — a cadeia do `Http.Resilience` exige o piso mais alto e o NuGet quebra o build com `NU1605` (downgrade) se as diretas ficarem para trás.
- **`Microsoft.Extensions.Configuration.Binder` e `Microsoft.Extensions.Options.ConfigurationExtensions` são referências diretas da Infra.** `InfraDependencies.AddOutbox` usa `services.Configure<OutboxOptions>(section)` e `section.GetValue<bool?>("Enabled")` — esses extension methods vinham **transitivamente** pelo `Microsoft.Extensions.Http`, então removê-lo quebrou a compilação com `CS1503`/`CS1061`. Depender de transitiva para API que o código chama direto é frágil; a referência explícita é a correção, não o rollback da remoção.
- **`Microsoft.OpenApi` está pinado em `2.7.5` na API** (com comentário no `.csproj`). `Microsoft.AspNetCore.OpenApi` declara o range `[2.0.0, )` e o NuGet resolve **o piso exato**, caindo em 2.0.0 — vulnerável ao advisory `GHSA-v5pm-xwqc-g5wc` (high: parsing de OpenAPI aborta em schema circular), que o NuGet audit reportava como `NU1903` na API e, por transitividade, nos IntegrationTests. **Nenhuma versão 10.x do `Microsoft.AspNetCore.OpenApi` sobe esse piso** (checado até 10.0.10), então atualizar o pacote pai não resolve — o pin direto é o único caminho. `2.7.5` é a primeira versão corrigida da linha 2.x; a linha 3.x é major bump e não foi adotada. O pin na API cobre os IntegrationTests via `ProjectReference`, sem precisar duplicar lá.
- **Os quatro pins do `BillPayment.IntegrationTests.csproj`** (`Azure.Identity`, `Microsoft.IdentityModel.JsonWebTokens`, `System.IdentityModel.Tokens.Jwt`, `System.Drawing.Common`) seguem o mesmo princípio: sobrescrevem transitivas vulneráveis do `Microsoft.AspNetCore.Mvc.Testing`. Cada um tem o GHSA no comentário — mantenha esse formato ao adicionar outro.
- **`Microsoft.VisualStudio.Azure.Containers.Tools.Targets` foi mantido**: é tooling de IDE que sustenta o perfil de launch `Container (Dockerfile)` e o `docker-compose.dcproj`. Não afeta runtime e removê-lo quebraria o F5 do Visual Studio.

Verificação: `dotnet build BillPayment.sln -p:TreatWarningsAsErrors=true` deve fechar com **0 warnings e 0 erros** — inclui os `NUxxxx` de audit. No Git Bash use `-p:` (a forma `/p:` é convertida em path pelo MSYS e o MSBuild rejeita com `MSB1008`).

## Checklist pré-produção

Itens que **devem** ser resolvidos antes do primeiro deploy em ambiente real. Marcar com `[x]` conforme forem concluídos.

### Banco de dados
- [x] **Criar migrações EF Core** — **feito em 2026-08-11**, motivado por incidente real. Migração `Initial` em `Infra/Migrations/`; `Program.cs` e o `IntegrationTestWebAppFactory` usam `MigrateAsync`.
- [x] **Remover `EnsureCreatedAsync`** — feito nos dois lugares.
- [ ] **Seed data** — definir se dados estáticos serão semeados via migração ou via endpoint admin.

### Segurança e autenticação
- [ ] **Autenticação JWT via Keycloak** — configurar `Keycloak.AuthServices` (JWT Bearer + audience + issuer) no `Program.cs`.
- [ ] **Validação do TenantId contra JWT** — hoje `{tenantId}` vem da rota sem validação. Implementar `TenantAuthorizationFilter` que compara o `{tenantId}` da rota com o claim `tenant_ids` do token.
- [ ] **Decorar endpoints com `[ProtectedResource]`** — definir recursos e ações granulares (`bill:create`, `bill:pay`, etc.).
- [ ] **CORS — origens de produção** — `AddCorsForFront` já está plugado em `Program.cs`. Antes do deploy: popular `Cors:AllowedOrigins` no `appsettings` do ambiente real (sem `AllowAnyOrigin`/wildcard).
- [ ] **Whitelist de IP no Asaas** — a chave da Fase 1 exige permissão de saque via API (achado da sprint 1.0), então ela pode pagar contas se vazar. A whitelist é o mecanismo do provedor para limitar o estrago e dispensar aprovação manual de operações críticas. **Não é opcional.**
- [x] **Sonda de fumaça do decode Pix em produção** — **feita em 2026-08-06: VERDE.** `receiver.cpfCnpj`, nome, nome fantasia, ISPB, valor, vencimento e `expirationDate` voltaram. Três achados registrados no [doc 12](BillPayment.Architecture/12-official-lookup-coverage.md): o **pagador NÃO vem mascarado** (abre decisão sobre o ADR-004), seis campos fora da documentação (`description` foi mapeado), e o Pix **cobre o buraco da arrecadação** — devolve o documento do beneficiário que o código de barras não devolve.
- [x] **Sonda de fumaça da consulta oficial em produção** — **feita em 2026-08-06: VERDE.** `beneficiaryCpfCnpj`, `beneficiaryName`, `bank` (string de 3 dígitos), valor, vencimento e `minimumScheduleDate` voltaram preenchidos para boleto de cobrança registrado. Detalhe em [`12-official-lookup-coverage.md`](BillPayment.Architecture/12-official-lookup-coverage.md). Reexecutável por [`tools/smoke-probe-production.js`](BillPayment.Architecture/tools/smoke-probe-production.js) quando trocar de conta ou de provedor.
- [ ] **Credenciais de e-mail e de sites de boleto** — a captura vai exigir segredos (IMAP/OAuth, logins de portais). Definir cofre (env vars/secret manager) antes de qualquer integração real; nunca no `appsettings.json`.
- [ ] **Registro do aplicativo no Entra ID + Application Access Policy** — por cliente, autosserviço (ADR-006). Conceder `Mail.Read` **de aplicativo** e **restringir por Application Access Policy** ao grupo de segurança com apenas as caixas monitoradas. **Sem essa política, `Mail.Read` alcança todas as caixas do tenant** — é a diferença entre ler uma caixa e ler a empresa inteira. O trio (`directoryId`, `clientId`, `clientSecret`) vai no campo `credential` do `POST /capture-sources`.
- [ ] **Ligar `Graph:Enabled`** — desligado por padrão. Sem ele, conectar uma fonte falha na prova de acesso (`BLP.CPS14`), por desenho.
- [ ] **Encaminhamento do Gmail para a caixa do M365** — passo de onboarding, não de código (ADR-006): ligar no Gmail, confirmar o código que chega no M365, e **adicionar o endereço Gmail aos remetentes seguros** — encaminhamento quebra SPF/DKIM e a mensagem pode cair no lixo eletrônico.
- [ ] **`Storage:ServiceUrl`, `AccessKey`, `SecretKey` e `AuthenticationRegion`** — balde compatível com S3 (Garage) para os artefatos capturados. **Sem isso o processamento de anexo falha alto**, de propósito: é preferível a guardar em lugar nenhum e descobrir na auditoria. O segredo vai por variável de ambiente, nunca no `appsettings.json`. **`AuthenticationRegion` não tem default e entra em `IsConfigured`**: o Garage assina SigV4 com a região `garage` (é o que o `PeopleManagement` configura contra o mesmo servidor) e o MinIO com `us-east-1` — um default estaria errado para metade dos alvos e a falha apareceria só na primeira gravação, como `SignatureDoesNotMatch` dentro do worker, lendo como credencial errada. Ver `gotchas.md`.
- [ ] **Egresso da escada de link (`LinkResolution`)** — a única saída de rede do BC para servidor de **terceiro**. O código já traz allowlist por host+porta, bloqueio de faixa interna, recusa de redirecionamento e teto de requisições por mensagem; falta a trava de **infraestrutura**: restringir o egresso do contêiner aos hosts das receitas (ou passar por proxy de saída). Sem isso, a defesa depende inteiramente do código — e a defesa em profundidade é justamente o que sobra quando o código tem um defeito. Acrescentar host novo em `LinkResolution:Recipes` só depois de **sondar** que ele responde: configurar um host que não se sabe responder faz a escada gastar requisição em silêncio e o desfecho parecer falha do emissor.
- [ ] **Master key do cofre (`Secrets__MasterKey`)** — 32 bytes em base64, gerada por `SecretsOptions.GenerateMasterKey()` (ou pelo comando PowerShell em "Build, Run & Test"). Sem ela o BC sobe com um cofre que falha em toda operação; a Fase 1 tolera isso porque não guarda credencial de tenant, mas **a partir da fase 2 é pré-requisito de deploy**. Guarde uma **cópia cifrada fora do host** (`age`/`gpg`) — perdê-la é reconectar todas as caixas e reemitir todas as chaves de subconta (ADR-009).
- [ ] **Chave do Asaas (`Asaas__ApiKey`) e `Asaas__BaseUrl` de produção** — o padrão do `appsettings.json` aponta para o **sandbox** de propósito; apontar para produção é decisão explícita de quem configura. Sem chave, a consulta oficial degrada para `Unavailable` e nenhum boleto é dado como verificado.

- [x] **Assets nativos do SkiaSharp no contêiner** — **confirmado como defeito real em 2026-08-11 e corrigido.** O `SkiaSharp 3.119.1` que vem pelo `ZXing.Net.Bindings.SkiaSharp` traz nativo **só para Windows e macOS** (`dotnet list package --include-transitive` mostrava apenas `NativeAssets.macOS` e `NativeAssets.Win32`); em Linux `libSkiaSharp` não existiria. E a falha seria **muda**: `QrCodeScanner.DecodeAll` engole qualquer exceção em `LogDebug`, então em nível `Information` não sairia uma linha sequer — todo QR ficaria ilegível, levando junto o trilho Pix (ADR-010) e o check `PixBarcodeConsistency`. Corrigido com `SkiaSharp.NativeAssets.Linux.NoDependencies` (a variante completa exigiria `libfontconfig1` na imagem, que só serve para renderizar texto — aqui a Skia só decodifica imagem). Verificado dentro do contêiner: `libSkiaSharp.so` presente em `runtimes/linux-x64/native/` e `ldd` sem dependência não resolvida.

### Resiliência e observabilidade
- [ ] **Health checks** — adicionar health check do PostgreSQL (`AspNetCore.HealthChecks.NpgsqlEfCore` ou similar).
- [ ] **Logging estruturado** — configurar Serilog ou similar com correlation ID por request.
- [x] **Outbox consumer** — `OutboxBackgroundService` + `OutboxProcessor` prontos (claim `FOR UPDATE SKIP LOCKED`, dispatch in-process, dead-letter, cleanup). Pendências de produção: backoff/observabilidade de backlog e garantir que só um deployment rode o worker (`Outbox:Enabled`).
- [ ] **Rate limiting** — avaliar se endpoints públicos precisam de throttling.

### Qualidade e testes
- [x] **Testes de integração com migrações** — feito; a suíte valida o mesmo schema que o deploy produz.
- [ ] **CI pipeline** — configurar build + unit tests + integration tests no CI (GitHub Actions ou similar).
- [ ] **Code coverage** — definir threshold mínimo e integrar no CI.
- [ ] **Promover analyzer warnings a erros no CI** — Application/Infra/API hoje só emitem warning (ver "Static analysis"). No pipeline de CI, passar `/p:TreatWarningsAsErrors=true` na etapa de build. Domain já está blindado localmente.
- [x] **Atualizar `Microsoft.OpenApi`** — resolvido via pin direto em `BillPayment.API.csproj` (ver "Dependências" abaixo).

### Infra e deploy
- [ ] **Dockerfile otimizado** — revisar multi-stage build, garantir que não copia arquivos desnecessários.
- [ ] **Variáveis de ambiente** — connection string, Keycloak config, credenciais de captura — todas via env vars, sem segredos no `appsettings.json`.
- [ ] **HTTPS** — garantir TLS termination (via reverse proxy ou certificado no container).

## Conventions inherited from the DDD skills

These are enforced by the `domain-codegen-ddd-dotnet`, `application-codegen-ddd-dotnet`, `infra-codegen-ddd-dotnet`, `api-codegen-ddd-dotnet`, and `tests-domain-ddd-dotnet` skills — invoke them via Skill instead of generating DDD code by hand:

- Code in English; `DomainException` messages in Portuguese; conversation in Portuguese.
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` is set on Domain. NetAnalyzers + SonarAnalyzer.CSharp são injetados via `Directory.Build.props` na raiz do BC com severidades em `.editorconfig` — ver seção "Static analysis (Roslyn analyzers)".
- Strongly-typed Ids (`record struct : IEntityId<TSelf>`), Smart Enums via `Enumeration`, VOs deriving from abstract `ValueObject`.
- Aggregate Roots only emit Domain Events (never internal Entities).
- Mediator próprio (sem MediatR) em `Application/Mediator/` — mesma superfície (`IRequest`/`IRequestHandler`/`IPipelineBehavior`/`IMediator`), registrado via `AddCustomMediator`.
- Idempotency in Application: write commands wrapped via `IdentifiedCommand`, checados contra `IRequestManager` (porta em `Domain/SeedWork`, impl na Infra sobre `client_requests`); header `x-requestid`.
- API uses `[ProtectedResource(resource, action)]` for Keycloak-backed granular authorization (planned — Keycloak is shared infra, configured at server level).
