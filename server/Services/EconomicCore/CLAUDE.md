# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> Read together with `../../CLAUDE.md` (server-level). This file overrides/extends the parent for the **EconomicCore** Bounded Context only.

## What this is

A **Bounded Context** of the Rufino financial SaaS. Escopo de negócio ainda **não definido** — a pasta `EconomicCore.Architecture/` é onde mora (ou vai morar) o design rationale: visão do BC, ADRs, use cases, plano de sprints. **Leia esses documentos antes de modelar — eles são a fonte de verdade, não o código.**

**Arquitetura: Clean Architecture com Domain-Driven Design (DDD).** Os quatro projetos (`EconomicCore.Domain`, `EconomicCore.Application`, `EconomicCore.Infra`, `EconomicCore.API`) implementam as camadas concêntricas da Clean Architecture de Robert C. Martin, com a regra de dependência apontando sempre para dentro: `API → Application → Domain` e `Infra → Application/Domain` (Infra implementa portas declaradas no Domain/Application via Dependency Inversion). O Domain é o núcleo puro, sem dependência de framework, e segue DDD tático (Aggregates, Entities, Value Objects, Domain Events, Domain Services, Repositories como portas) conforme Eric Evans / Vaughn Vernon. Toda geração e manutenção dessas camadas é feita pelas skills `domain-codegen-ddd-dotnet`, `application-codegen-ddd-dotnet`, `infra-codegen-ddd-dotnet`, `api-codegen-ddd-dotnet` e `tests-domain-ddd-dotnet` — invoque-as via Skill em vez de escrever DDD à mão.

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

## Status

**Domain — Fase 1 (Walking Skeleton) completa.** Os 7 prompts do guia `EconomicCore.Architecture/Instrucoes-Claude-Code.md` foram implementados (SeedWork + SharedKernel + 4 Aggregates + 1 Domain Service). **268 testes aprovados, 1 skipped, 0 falhas.** Application, Infra e API ainda como esqueleto vazio (csproj + `Program.cs` placeholder).

Implementado em `EconomicCore.Domain/`:

| Prompt | Status | Escopo aterrissado |
|---|---|---|
| 1 — SeedWork | ✅ Done | `SeedWork/` (`IEntityId<TSelf>` com `static abstract New/From/Empty`, `Entity<TId>`, `AggregateRoot<TId>` com `AddDomainEvent`/`PullDomainEvents`, `ValueObject` + `ToString` por reflexão, `Enumeration` + helpers `Try*`/`AbsoluteDifference`, `IDomainEvent`, `DomainException` com regex `XXX##` \| `XXX.YYY##`, `SeedWorkErrors` → `SWK01 - EmptyId`) + `EconomicCoreErrors` na raiz do Domain (`ECC01 - TenantMismatch`). Sigla `SWK` reservada para SeedWork. |
| 2 — SharedKernel | ✅ Done | `SharedKernel/` (pasta interna do Domain, **não** projeto separado — divergência consciente de §13.10/§7.2). Smart Enums `Currency` (BRL only) e `TaxIdKind` (CPF=11, CNPJ=14, prop `ExpectedLength`). VOs `Money` (`Amount`+`Currency`, banker's rounding, `Add`/`Subtract`/`Multiply`, sign props), `CompetencePeriod` (`Year`+`Month` ∈ [1900..9999]×[1..12], `Next`/`Previous` com wrap, `FirstDay`/`LastDay` respeitando leap year, `Contains`), `DateRange` (`DateOnly From`+`To`, invariante `From<=To`, `Days` inclusivo, `Contains`, `Overlaps` incluindo borda compartilhada), `TaxId` (sanitização digits-only, length match + check digit mod-11 + blacklist de repetidos, `Formatted()` com máscaras brasileiras). Erros: `SHK.MNY01..02`, `SHK.PER01..02`, `SHK.DRG01`, `SHK.TAX01..03`. |
| 3 — EconomicAgent | ✅ Done | `Operational/EconomicAgents/` AR `EconomicAgent` (props `TenantId`+`Scope`+`Name`+`TaxId?`+`Roles`, factory `Create`, setters privados `SetName`/`SetScope`, `public const NAME_MAX_LENGTH=200`); ID `EconomicAgentId`+`EconomicRoleId`; Smart Enum `AgentScope` (Inside/Outside); evento `EconomicAgentRegistered` (payload com `TaxId` decomposto em string+kind); erros `ECC.AGT01..03` (AGT03 slot reservado — validação real está no VO TaxId). `TenantId` criado em `SharedKernel/`. **`InternalsVisibleTo` habilitado** no Domain para `EconomicCore.UnitTests` (necessário para testar `internal static` factories de erro direto). |
| 4 — EconomicResource | ✅ Done | `Operational/EconomicResources/` AR `EconomicResource` (props `TenantId`+`TypeId?`+`Kind`+`Name`+`CustodianId?`, factory `Create`, setters privados `SetName`/`SetKind`, `public const NAME_MAX_LENGTH=200`); IDs `EconomicResourceId`+`EconomicResourceTypeId`; Smart Enum `ResourceKind` (4 valores: `CASH`/`SERVICE`/`LABOR_SERVICE`/`FISCAL_OBLIGATION`); evento `EconomicResourceRegistered` (payload com `TypeId?`/`CustodianId?` como `Guid?` para evitar JSON converter custom em outbox); erros `ECC.RES01..03` (RES03 = `CustodianMustBeInternal` é slot reservado para Domain Service cross-aggregate futuro). **Saldo não é campo do aggregate** — derivado de read model. |
| 5 — EconomicEvent | ✅ Done | `Operational/EconomicEvents/` AR `EconomicEvent` (factories `RegisterCovered`/`RegisterPaired` + método `CloseDuality` parcial/total); IDs `EconomicEventId`+`EconomicEventTypeId`; Smart Enums `FlowDirection` (Inflow/Outflow) + `ParticipationRole` (Provider/Recipient); VOs `Participation`, `DualityLink`, `CommitmentRef`, `EventTimestamp` (UTC-only); eventos `EconomicEventRegistered` + `DualityClosed`; erros `ECC.EVT01..13` (`EVT01..07` invariantes principais + extensões `EVT08..13` para validação de VOs internos — slot `EVT07` reservado documentando imutabilidade estrutural). Faz referência cross-aggregate por ID a `EconomicResourceId`, `EconomicAgentId` e `CommitmentId` (este último criado em `Prospective/EconomicContracts/CommitmentId.cs` como placeholder do Prompt 6). `UserId` adicionado a `SharedKernel/`. **Phase 1 simplification:** `DualityLink? Duality` é singular (1 contraparte por evento; acumula MatchedAmount em CloseDuality). O 1-pagamento-cobre-N-consumos pré-pago será refatorado na Fase 2. |
| 6 — EconomicContract + Commitment | ✅ Done | `Prospective/EconomicContracts/` AR `EconomicContract` (status `Active`/`Suspended`/`Terminated` com máquina de estados; comportamentos `Create`/`GenerateCommitmentsFor`/`MarkFulfilled`/`Expire`/`CancelCommitment`/`Suspend`/`Resume`/`Terminate`); Entity interna `Commitment` (Entity<CommitmentId>, status `Promised`/`Reserved`/`Fulfilled`/`Expired`/`Cancelled` com `CanTransitionTo`+`IsTerminal`, mutada só via Root); IDs `EconomicContractId`+`CommitmentId`; Smart Enums `ContractDirection`/`ContractStatus`/`CommitmentDirection`/`CommitmentStatus`/`Periodicity`; VOs `RecurrencePattern` (Periodicity+AnchorDay), `CommitmentTerms` (ExpectedAmount+TolerancePercent+WindowDays, método `IsWithinTolerance`), `ReciprocalLink`; eventos `EconomicContractCreated`/`CommitmentsGenerated`/`CommitmentFulfilled`/`CommitmentExpired`/`CommitmentCancelled`; erros `ECC.CTR01..13` (CTR01..05 invariantes principais — CTR04 = `AmountOutsideTolerance` slot reservado para sinalização soft, não bloqueia Phase 1 — + extensões CTR06..13 para VOs/lookup/máquina de estados). **CTR01 é estrutural**: pares outflow+inflow sempre gerados juntos com `ReciprocalLink` cruzado. Application supplies `CommitmentId` pair externally para idempotência. |

| 7 — DualityMatchingService | ✅ Done | `Services/DualityMatchingService.cs` — stateless `static class` (sem interface/async/infra). Método `Match(payment, coveringCommitmentId, consumption, occurredAt)` valida non-null, tenant match, ambos cobertos pelo mesmo Commitment, currency match; calcula `matchedAmount = min(payment_remaining, consumption_remaining)`; chama `CloseDuality` nos **dois** `EconomicEvent` (Application persiste os dois na mesma transação — exceção justificada a "1 aggregate por transação" porque duality é invariante de par, §5.4). Não emite eventos — os aggregates emitem `DualityClosed`. Sigla `ECC.DMS##`: `DMS01 - NullEvent`, `DMS02 - TenantMismatch`, `DMS03 - ConsumptionNotCoveredByCommitment`, `DMS04 - PaymentNotCoveredByCommitment`, `DMS05 - CurrencyMismatch`. |

**Próximas camadas** (fora deste guia — próximas sprints):
- **Application**: commands `RegisterEconomicContract`/`GenerateCommitments`/`RegisterConsumptionEvent`/`RegisterPaymentEvent`; queries `ListClaims`/`GetCompetenceDRE`/`GetCashFlow`/`ListUpcomingCommitments`. Idempotência via `IRequestManager`. Handler de matching post-paid orquestra `DualityMatchingService`.
- **Infra**: EF Core 10 + PostgreSQL + Outbox. Mapeamento EF de VOs (`Money`, `CompetencePeriod`, `DateRange`, `TaxId`, `Participation`, `DualityLink`, `CommitmentRef`, `EventTimestamp`, `RecurrencePattern`, `CommitmentTerms`, `ReciprocalLink`) + Smart Enums (converter para int). Multi-tenant filter por `TenantId` global. Unique index em `(TenantId, EconomicAgent.TaxId)` quando presente.
- **API**: controllers `EconomicCore.API`, autenticação Keycloak via `[ProtectedResource]`, filtros mapeando `DomainException` em HTTP 400/422 (preservar `Id`).
- **Read models**: handlers consumindo `EconomicEventRegistered`/`DualityClosed`/`CommitmentsGenerated`/`CommitmentFulfilled`/`CommitmentExpired`.
- **Sprint Fase 2** (opcional): `StandaloneCommitment` (sigla `ECC.STD##`) + refinamento do `DualityLink` para 1-to-many (pré-pago).

## Build, Run & Test

This BC has its **own `.sln`** — it is **not** part of `../../RufinoProject.sln`. Always operate from this folder.

```powershell
# Build the whole BC
dotnet build EconomicCore.sln

# Run the API (HTTPS profile uses dev certs)
dotnet run --project EconomicCore.API

# Unit tests
dotnet test EconomicCore.UnitTests

# Run a single test class / method
dotnet test --filter "FullyQualifiedName~ClassName"
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"

# Stack up via Docker (API + Postgres)
docker compose up --build

# EF Core migrations (run from EconomicCore.API folder once Infra has a DbContext)
dotnet ef database update --project ../EconomicCore.Infra
```

### Port map (docker-compose)

| Service               | Host port | Container port |
|-----------------------|-----------|----------------|
| `economiccore.api`    | 8060      | 80             |
| `economiccore.api`    | 8061      | 443            |
| `economiccore.db`     | 8062      | 5432           |

Postgres schema: `economic_core`. Database: `EconomicCoreDb`. Connection string is configured in `EconomicCore.API/appsettings.json` and points at the `economiccore.db` service name (compose-internal DNS).

## Mandatory testing workflow

**Toda alteração no `EconomicCore.Domain` exige rodar a suíte completa de testes unitários antes de encerrar a tarefa.** Não basta rodar só os testes da Aggregate alterada — execute `dotnet test EconomicCore.UnitTests` inteiro, porque mudanças em SeedWork, VOs e factories de erro podem quebrar Aggregates aparentemente não relacionados.

**Todo método de teste deve ter um comentário (em português, uma linha) explicando o que ele cobre — cenário + comportamento esperado.** O comentário fica imediatamente acima do atributo `[Fact]` / `[Theory]`. O nome do método sozinho não basta: o comentário existe para descrever a regra em linguagem de negócio (ou técnica, em testes puros de SeedWork), permitindo revisar a intenção do caso sem ler o corpo. Vale para testes novos *e* para qualquer teste tocado durante uma alteração — se você editou o teste, atualize o comentário.

Exemplo:

```csharp
// Aplicar um evento que o Aggregate não trata em When(...) lança SWK02.
[Fact]
public void Apply_EventWithoutWhenHandler_Throws_SWK02()
{
    var agg = TestEventSourcedAggregate.Initialize(TestId.New(), "first", Now);

    var ex = Assert.Throws<DomainException>(() => agg.ApplyUnhandled(Now));
    Assert.Equal("SWK02", ex.Id);
}
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
- **Aggregate, Domain Service, VO, Smart Enum, evento ou erro novo** — se a entidade é citada no CLAUDE.md (tabela de Status, seção "Architecture — what is non-obvious", "Project layout"), reflita o novo item. Não precisa listar cada VO trivial, mas qualquer Aggregate Root, Domain Service ou conceito estrutural (ex.: novo prefixo de erro `ECC.<AGG>##`) é obrigatório.
- **Decisão arquitetural ou convenção nova** (ADR, mudança de stack, novo padrão de pasta, nova sigla de erro, mudança de visibilidade de `*Errors.cs`, troca de mediator, nova porta no Domain) — adicione/edite na seção "Architecture — what is non-obvious".
- **Estrutura de pastas muda** (projeto novo, pasta nova de primeiro nível, renomeação) — atualize "Project layout".
- **Build/run/test workflow muda** (porta nova no docker-compose, novo `dotnet test` filtrável, schema/db trocados) — atualize "Build, Run & Test".
- **Nova convenção do skill/codegen ou nova regra do usuário** (ex.: "todo teste tem comentário", "Domain proíbe X") — adicione/edite em "Conventions inherited from the DDD skills" ou cria nova seção "Mandatory <X> workflow".

**Como aplicar**:

1. Antes de fechar a tarefa, leia o `CLAUDE.md` e pergunte: "alguma seção ficou mentindo depois das minhas alterações?" Se sim, edite.
2. Se você atualizou o código mas não tem certeza se o `CLAUDE.md` precisa mudar, **pergunte ao usuário** antes de concluir.
3. **Não duplique o que está em `EconomicCore.Architecture/`.** O CLAUDE.md aponta para esses arquivos; ele descreve *estado* e *convenção*, não *plano* nem *design rationale*.
4. Se uma sprint for implementada apenas parcialmente, marque-a como `🚧 Em andamento` na tabela e descreva o que ficou de fora.

**Falhar em atualizar o CLAUDE.md é considerado tarefa incompleta**, mesmo que o código compile e os testes passem. Esse arquivo é o que orienta as próximas sessões do Claude Code — se ele estiver desatualizado, o próximo agente parte de premissas erradas e o débito de contexto cresce em silêncio.

## Architecture — what is non-obvious

_A preencher conforme decisões arquiteturais forem tomadas. Prefixos de erro em uso: `SWK##` (SeedWork), `ECC##` (BC transversal), `ECC.<AGG>##` (Aggregate-specific — siglas reservadas: `AGT`, `RES`, `EVT`, `CTR`, `DMS`, `STD`, `SHK`). Convenções herdadas:_

- Aggregate Roots emitem Domain Events; Entities internas nunca.
- Cross-aggregate rules vão em Domain Services, nunca passe Entity de um Aggregate para método de outro (ver memória `feedback_ddd_aggregate_boundaries`).
- Cross-aggregate references to internal Entities devem ser ancoradas via composite VO (ex.: `AccountRef(ChartOfAccountsId, AccountId)` em vez de `AccountId` cru).
- `*Errors.cs` factories ficam em pasta única `EconomicCore.Domain/Errors/` (não co-locados com o type que servem); `SeedWorkErrors` é `public static`, demais são `internal static`.
- Tenancy: todo Aggregate Root carrega `TenantId` (strongly-typed `record struct : IEntityId<TenantId>`); queries e authorization filtram por `TenantId`.

## Project layout

```
EconomicCore/
├── EconomicCore.sln                  # isolated solution (not in RufinoProject.sln)
├── docker-compose.yml + override     # localized stack: API + Postgres
├── EconomicCore.API/                 # Web SDK host, Program.cs, appsettings, Dockerfile
├── EconomicCore.Application/         # Commands, Queries, Handlers
├── EconomicCore.Domain/              # Aggregates, SeedWork (a criar)
├── EconomicCore.Infra/               # EF Core DbContext, repositories, EF Exception processor
├── EconomicCore.UnitTests/           # xUnit
└── EconomicCore.Architecture/        # design rationale, ADRs, plano de sprints (a preencher)
```

## Conventions inherited from the DDD skills

These are enforced by the `domain-codegen-ddd-dotnet`, `application-codegen-ddd-dotnet`, `infra-codegen-ddd-dotnet`, `api-codegen-ddd-dotnet`, and `tests-domain-ddd-dotnet` skills — invoke them via Skill instead of generating DDD code by hand:

- Code in English; `DomainException` messages in Portuguese; conversation in Portuguese.
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` is set on Domain (mandatory by Sprint 0).
- Strongly-typed Ids (`record struct : IEntityId<TSelf>`), Smart Enums via `Enumeration`, VOs deriving from abstract `ValueObject`.
- Aggregate Roots only emit Domain Events (never internal Entities).
- Idempotency in Application: commands wrapped via `IRequestManager`.
- API uses `[ProtectedResource(resource, action)]` for Keycloak-backed granular authorization (planned — Keycloak is shared infra, configured at server level).
