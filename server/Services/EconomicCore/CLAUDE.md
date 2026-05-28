# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> Read together with `../../CLAUDE.md` (server-level). This file overrides/extends the parent for the **EconomicCore** Bounded Context only.

## What this is

A **Bounded Context** of the Rufino financial SaaS. Escopo de negócio ainda **não definido** — a pasta `EconomicCore.Architecture/` é onde mora (ou vai morar) o design rationale: visão do BC, ADRs, use cases, plano de sprints. **Leia esses documentos antes de modelar — eles são a fonte de verdade, não o código.**

**Arquitetura: Clean Architecture com Domain-Driven Design (DDD).** Os quatro projetos (`EconomicCore.Domain`, `EconomicCore.Application`, `EconomicCore.Infra`, `EconomicCore.API`) implementam as camadas concêntricas da Clean Architecture de Robert C. Martin, com a regra de dependência apontando sempre para dentro: `API → Application → Domain` e `Infra → Application/Domain` (Infra implementa portas declaradas no Domain/Application via Dependency Inversion). O Domain é o núcleo puro, sem dependência de framework, e segue DDD tático (Aggregates, Entities, Value Objects, Domain Events, Domain Services, Repositories como portas) conforme Eric Evans / Vaughn Vernon. Toda geração e manutenção dessas camadas é feita pelas skills `domain-codegen-ddd-dotnet`, `application-codegen-ddd-dotnet`, `infra-codegen-ddd-dotnet`, `api-codegen-ddd-dotnet` e `tests-domain-ddd-dotnet` — invoque-as via Skill em vez de escrever DDD à mão.

## EconomicCore.Architecture — índice de referência

A pasta `EconomicCore.Architecture/` contém o design rationale, a base teórica do BC e as fontes acadêmicas/normativas REA. **Leia esses documentos antes de modelar — eles são a fonte de verdade, não o código.**

O índice completo com tabelas de seções/linhas de cada documento está em [`EconomicCore.Architecture/index.md`](EconomicCore.Architecture/index.md). Consulte-o para localizar conteúdo específico por seção e linha dentro dos arquivos `.md`.

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
dotnet build EconomicCore.sln

# Run the API (HTTPS profile uses dev certs)
dotnet run --project EconomicCore.API

# Unit tests
dotnet test EconomicCore.UnitTests

# Integration tests (requires Docker for Testcontainers + postgres:17)
dotnet test EconomicCore.IntegrationTests

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
| `economiccore.api`    | 8090      | 8080 (HTTP)    |
| `economiccore.db`     | 8092      | 5432           |

Postgres: `postgres:17-alpine`, schema `economic_core`, database `EconomicCoreDb`. Connection string injected via `ConnectionStrings__EconomicCore` env var in compose, points at `economiccore.db` (compose-internal DNS). Healthcheck on DB ensures API waits. `EnsureCreatedAsync` runs at startup to create the schema.

Swagger UI: `http://localhost:8090/openapi/v1.json` + `http://localhost:8090/swagger`

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

## Fase 1 — Status

Walking Skeleton do aluguel pós-pago: **completo + ciclo de vida do contrato** (Draft → Active → Terminated, com vínculo a Resource/Agent persistidos via API).

| Camada | Status | Artefatos |
|---|---|---|
| Domain | ✅ | 4 Aggregates (EconomicEvent, EconomicResource, EconomicAgent, EconomicContract com `ResourceId`/`TermMonths`/`StartDate`/`Activate`/`Terminate`), `ContractStatus.Draft` adicionado, DualityMatchingService, 295 unit tests |
| Application | ✅ | 8 Commands (RegisterEconomicResource, RegisterEconomicAgent, RegisterEconomicContract, ActivateEconomicContract, TerminateEconomicContract, GenerateCommitments, RegisterConsumptionEvent, RegisterPaymentEvent), 2 Queries (GetCompetenceDRE, GetCashFlow) |
| Infra | ✅ | EconomicCoreDbContext (UoW + Outbox), 4 repositories (`HasOverlappingAsync` + `GetLastInflowPeriodForCommitmentsAsync` adicionados), EF mappings com `resource_id`/`term_months`/`start_date` no contract |
| API | ✅ | ResourcesController, AgentsController, ContractsController (+activate +terminate), EventsController (CommitmentId direto), ReportsController, DomainExceptionFilter |
| Integration Tests | ✅ | 34 tests: jornada completa de 12 meses (Draft→Activate→12 ciclos pay+occupy→Terminate), RegisterResource/Agent (happy + 2 erros cada), Create/Activate/Terminate/Payment/Occupancy exception suites, multi-tenant |

## Architecture — what is non-obvious

Prefixos de erro em uso: `SWK##` (SeedWork), `ECC##` (BC transversal), `ECC.<AGG>##` (Aggregate-specific — siglas reservadas: `AGT`, `RES`, `EVT`, `CTR`, `DMS`, `STD`, `SHK`). Convenções herdadas:

- Aggregate Roots emitem Domain Events; Entities internas nunca.
- Cross-aggregate rules vão em Domain Services, nunca passe Entity de um Aggregate para método de outro (ver memória `feedback_ddd_aggregate_boundaries`).
- Cross-aggregate references to internal Entities devem ser ancoradas via composite VO (ex.: `AccountRef(ChartOfAccountsId, AccountId)` em vez de `AccountId` cru).
- `*Errors.cs` factories ficam co-localizadas com o Aggregate (`Operational/<Agg>/`, `Prospective/<Agg>/`); `SeedWorkErrors` é `public static`. Os Aggregate Errors são `public static` quando a Application precisa lançar (NotFound de agregados, validações de pre-condição), `internal static` para os puramente de domínio. Atualmente todos os 4 Aggregate Errors são `public` por causa do uso pela Application (Resource/Agent NotFound, Contract overlap/term/start-date/amount-mismatch/termination-date).
- Tenancy: todo Aggregate Root carrega `TenantId` (strongly-typed `record struct : IEntityId<TenantId>`); queries e authorization filtram por `TenantId`.
- **EF owned types & shared references**: Value Objects mapeados como owned types (`OwnsOne`/`OwnsMany`). EF tracks owned type instances by reference identity — **never share the same VO instance between two tracked entities** (e.g., use `new Money(...)` for each, don't pass `commitment.ExpectedAmount` directly to `EconomicEvent.RegisterCovered`). This causes "same entity tracked as different entity types" warnings and data loss.
- **Cross-aggregate FK removal**: `OnModelCreating` strips non-ownership FKs discovered by convention (e.g., `resource_id` → `economic_resources`). Cross-aggregate references are by ID only, no navigation properties, no FK constraints.
- **DbContext.SaveEntitiesAsync** drains domain events from all tracked aggregates into `outbox_messages` before calling `base.SaveChangesAsync`.
- **DomainExceptionFilter** handles `DomainException` (from Domain) and `InvalidOperationException` (from Application). HTTP status is driven by `DomainException.Category` (`DomainErrorCategory` enum in SeedWork): `Validation` → 400, `Conflict` → 409, `NotFound` → 404. Error factories pass the category; the filter has no hardcoded error codes. `InvalidOperationException` always maps to 400.
- **TenantId via rota**: todos os controllers multi-tenant usam `[Route("api/v1/{tenantId}/[controller]")]` e recebem `[FromRoute] Guid tenantId`. Será validado contra JWT em fase futura (Keycloak).
- **Integration tests** use Testcontainers (`postgres:17`) + Respawn + `EnsureCreatedAsync` (no migrations yet). DTOs are duplicated in the test project (not reused from Application).
- **EconomicContract lifecycle**: contrato nasce `Draft` (não materializa commitments), `Activate(occurredAt, factory)` gera N=`TermMonths` pares Outflow/Inflow numa única operação e transita para `Active`. `Terminate` é permitido a partir de Draft (descarte) ou Active/Suspended; cancela commitments futuros (Period.FirstDay() > terminationDate) antes de transicionar. Pagamento parcial é **rejeitado** (ECC.CTR19 PaymentAmountMismatch) — pagamento exato obrigatório, sem `EconomicClaim` modelado nesta fase.
- **Handlers de Payment/Consumption** validam `commitment.Status ∈ {Promised, Reserved}` e que não exista outro `EconomicEvent` já cobrindo o mesmo commitment (via `IEconomicEventRepository.FindCoveredByCommitmentAsync`) antes de criar evento — protege contra duplicatas mesmo quando o pair ainda não chegou para fechar duality.
- **Cross-aggregate references no Contract**: `EconomicContract.ResourceId` referencia o `EconomicResource` (imóvel) por ID; sem FK no banco (convention strip). `RegisterEconomicContract` valida `ExistsAsync` para Resource e Counterparty antes de criar, e `HasOverlappingAsync` para detectar contratos Draft/Active sobrepostos no mesmo recurso.

## Project layout

```
EconomicCore/
├── EconomicCore.sln                  # isolated solution (not in RufinoProject.sln)
├── docker-compose.yml + override     # localized stack: API + Postgres
├── EconomicCore.API/                 # Web SDK host, Program.cs, Controllers, Filters, appsettings, Dockerfile
│   ├── Controllers/                  #   ResourcesController, AgentsController, ContractsController (+activate +terminate), EventsController, ReportsController
│   └── Filters/                      #   DomainExceptionFilter (maps DomainErrorCategory → HTTP status: Validation→400, Conflict→409, NotFound→404)
├── EconomicCore.Application/         # Commands, Queries, Handlers (MediatR 12.4.1)
│   ├── Commands/                     #   RegisterEconomicResource, RegisterEconomicAgent, RegisterEconomicContract, ActivateEconomicContract, TerminateEconomicContract, GenerateCommitments, RegisterConsumptionEvent, RegisterPaymentEvent
│   └── Queries/                      #   GetCompetenceDRE, GetCashFlow
├── EconomicCore.Domain/              # Aggregates, SeedWork, SharedKernel, Domain Services
│   ├── Operational/                  #   EconomicEvent, EconomicResource, EconomicAgent (+ repository interfaces)
│   ├── Prospective/                  #   EconomicContract + Commitment entity (+ repository interface)
│   ├── Services/                     #   DualityMatchingService
│   └── SeedWork/                     #   Entity, AggregateRoot, ValueObject, Enumeration, IUnitOfWork
├── EconomicCore.Infra/               # EF Core DbContext, repositories, mappings, outbox
│   ├── Persistence/                  #   EconomicCoreDbContext (UoW), OutboxMessage
│   ├── Mapping/                      #   EF entity configurations (4 aggregates + outbox)
│   └── Repositories/                 #   4 repository implementations
├── EconomicCore.UnitTests/           # xUnit — 295 domain unit tests (inclui EconomicContractActivateTests, EconomicContractTerminateTests)
├── EconomicCore.IntegrationTests/    # xUnit + Testcontainers + Respawn — 34 integration tests
│   ├── Infrastructure/               #   WebApplicationFactory, BaseIntegrationTest, KnownIds
│   ├── Mothers/                      #   RentScenarioMother (seed direct DB + SeedResourceAndAgentViaApi)
│   ├── Contracts/                    #   Duplicated DTOs (not reusing Application's)
│   └── Rent/                         #   FullLifecycle 12 meses, Register{Resource,Agent}Tests, {CreateContract,ActivateContract,TerminateContract,RegisterPayment,RegisterOccupancy}ExceptionTests, multi-tenant
└── EconomicCore.Architecture/        # design rationale, fontes REA (ver index.md)
    ├── index.md                      #   índice completo com tabelas de seções/linhas de cada documento
    ├── Modelo-REA-Conceitual.md      #   modelo conceitual REA puro (fonte de verdade conceitual)
    ├── Modelo-REA-Tatico.md          #   aterrissagem tática DDD (fonte de verdade para codegen)
    ├── Instrucoes-Claude-Code.md     #   guia de execução dos 7 prompts da Fase 1
    ├── LIVROS/                       #   PDFs originais das fontes acadêmicas
    ├── ISO_IEC_15944-4_2015(en).md   #   ISO/IEC 15944-4:2015 — ontologia contábil/econômica
    ├── The_REA_Accounting_Model_A_Generalized_Framework.md  # McCarthy 1982
    ├── The_Ontological_Foundations_of_REA_Enterprise_Information_Systems_2000.md  # Geerts & McCarthy 2000
    └── Model-Driven_Design_Using_Business_Patterns-Pavel_Hruby.md  # Hruby 2006 (OCR)
```

## Checklist pré-produção

Itens que **devem** ser resolvidos antes do primeiro deploy em ambiente real. Marcar com `[x]` conforme forem concluídos.

### Banco de dados
- [ ] **Criar migrações EF Core** — hoje o schema é criado via `EnsureCreatedAsync()` (Program.cs), que não suporta alterações incrementais. Trocar para `db.Database.MigrateAsync()` e gerar a migração inicial: `dotnet ef migrations add Initial --project ../EconomicCore.Infra` (rodar de dentro de `EconomicCore.API/`).
- [ ] **Remover `EnsureCreatedAsync`** do Program.cs e do `IntegrationTestWebAppFactory` (testes devem usar `MigrateAsync` também).
- [ ] **Seed data** — definir se dados estáticos (ex.: tipos de recurso econômico, moedas) serão semeados via migração ou via endpoint admin.

### Segurança e autenticação
- [ ] **Autenticação JWT via Keycloak** — configurar `Keycloak.AuthServices` (JWT Bearer + audience + issuer) no `Program.cs`.
- [ ] **Validação do TenantId contra JWT** — hoje `{tenantId}` vem da rota sem validação. Implementar `TenantAuthorizationFilter` que compara o `{tenantId}` da rota com o claim `tenant_ids` do token.
- [ ] **Decorar endpoints com `[ProtectedResource]`** — definir recursos e ações granulares (`contract:create`, `event:register`, `report:read`, etc.).
- [ ] **CORS** — configurar origens permitidas para o front-end.

### Resiliência e observabilidade
- [ ] **Health checks** — adicionar health check do PostgreSQL (`AspNetCore.HealthChecks.NpgsqlEfCore` ou similar).
- [ ] **Logging estruturado** — configurar Serilog ou similar com correlation ID por request.
- [ ] **Outbox consumer** — implementar worker/background service que processa `outbox_messages` (hoje são escritas mas não consumidas).
- [ ] **Rate limiting** — avaliar se endpoints públicos precisam de throttling.

### Qualidade e testes
- [ ] **Testes de integração com migrações** — após criar migrações, trocar `EnsureCreatedAsync` por `MigrateAsync` nos testes.
- [ ] **CI pipeline** — configurar build + unit tests + integration tests no CI (GitHub Actions ou similar).
- [ ] **Code coverage** — definir threshold mínimo e integrar no CI.

### Infra e deploy
- [ ] **Dockerfile otimizado** — revisar multi-stage build, garantir que não copia arquivos desnecessários.
- [ ] **Variáveis de ambiente** — connection string, Keycloak config, S3/storage — todas via env vars, sem segredos no `appsettings.json`.
- [ ] **HTTPS** — garantir TLS termination (via reverse proxy ou certificado no container).

## Conventions inherited from the DDD skills

These are enforced by the `domain-codegen-ddd-dotnet`, `application-codegen-ddd-dotnet`, `infra-codegen-ddd-dotnet`, `api-codegen-ddd-dotnet`, and `tests-domain-ddd-dotnet` skills — invoke them via Skill instead of generating DDD code by hand:

- Code in English; `DomainException` messages in Portuguese; conversation in Portuguese.
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` is set on Domain (mandatory by Sprint 0).
- Strongly-typed Ids (`record struct : IEntityId<TSelf>`), Smart Enums via `Enumeration`, VOs deriving from abstract `ValueObject`.
- Aggregate Roots only emit Domain Events (never internal Entities).
- Idempotency in Application: commands wrapped via `IRequestManager`.
- API uses `[ProtectedResource(resource, action)]` for Keycloak-backed granular authorization (planned — Keycloak is shared infra, configured at server level).
