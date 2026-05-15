# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> Read together with `../../CLAUDE.md` (server-level). This file overrides/extends the parent for the **AccountsPayable** Bounded Context only.

## What this is

A **Bounded Context** of the Rufino financial SaaS (multi-tenant accounts-payable for Brazilian SMBs). Owns the financial obligation lifecycle: capture → classify → schedule → approve → request payment → reconcile. Does **not** own: bill ingestion (OCR/email), payment execution (PIX/boleto via Asaas), accounting/DRE — those are sibling BCs.

The full design rationale (use cases, ADRs, runtime diagrams, payable typology, approval traps) lives in `AccountsPayable.Architecture/Consolidado.md`. The phased implementation plan (walking skeleton, sprint-by-sprint) lives in `AccountsPayable.Architecture/accounts-payable-sprints.md`. **Read those before modeling — they are the source of truth, not the code yet.**

## Status

**Domain: Sprints 0–10 implemented.** Application, Infra and API projects are still scaffolding-only (csproj + `Program.cs`). Next sprint is **Sprint 11 — Contracts + ExpectedRecurringBill**.

Implemented so far (see `AccountsPayable.Architecture/accounts-payable-sprints.md` for the full plan):

| Sprint | Status | Scope landed in `AccountsPayable.Domain/` |
|---|---|---|
| 0 — Foundation / SeedWork | ✅ Done | `SeedWork/` (`Entity`, `AggregateRoot`, `EventSourcedAggregateRoot`, `ValueObject`, `Enumeration`, `IDomainEvent`, `DomainException`, `IEntityId`, `TenantId`, `UserId`) |
| 1 — Supplier | ✅ Done | `Suppliers/Supplier` + `SupplierBankAccount`, VOs (`LegalName`, `TradeName`, `TaxId`, `ContactInfo`, `EmailAddress`, `PhoneNumber`, `Address`, `PixKey`), enums (`SupplierStatus`, `TaxIdType`, `BankAccountType`, `PixKeyType`), `Services/SupplierUniquenessChecker` + `ISupplierTaxIdLookup` port |
| 2 — Payable mínimo (A+ES) | ✅ Done | `Payables/Payable` (Event-Sourced), VOs (`Money`, `DueDate`, `Description`, `PaymentProof`), enums (`PayableStatus`, `Currency`, `PaymentProofType`), events: `PayableCreated`, `PayableScheduled`, `PayableMarkedAsPaid`, `PayableCancelled` |
| 3 — Chart of Accounts + Cost Center | ✅ Done | `ChartOfAccounts/ChartOfAccounts` + `Account` entity, VOs (`AccountCode`, `AccountName`, `ChartOfAccountsName`), enum `AccountType`; `CostCenters/CostCenter` + VOs (`CostCenterCode`, `CostCenterName`) |
| 4 — Classificação manual do Payable | ✅ Done | `Payable.Classify(...)` + `PayableClassified` event; `Services/PayableClassificationValidator` (cross-aggregate rule) |
| 5 — Aprovação manual single approver | ✅ Done | `Payable.RequestApproval/Approve/Reject` + events `PayableApprovalRequested`, `PayableApproved`, `PayableRejected`; threshold passed by parameter (não vive no Aggregate) |
| 6 — PaymentOrder hooks (sem o Aggregate) | ✅ Done | `Payable.RequestPayment/ConfirmPaid/MarkPaymentFailed`; events `PayablePaymentRequested`/`PayablePaid`/`PayablePaymentFailed`; status `PaymentRequested`/`PaymentFailed` (não-terminal, suporta retry); Smart Enum `PaymentMethod` (Pix/BankSlip/Ted/Manual); referência fraca `PaymentOrderId` ao Aggregate que vive em `PaymentExecution`; idempotência em `ConfirmPaid` por `LastPaymentOrderId`; `RequiresApproval` passou a usar `ApprovedAt is not null` em vez de `Status == Approved` para sobreviver ao ciclo Approved→Scheduled→PaymentRequested |
| 7 — Integração com Bill Ingestion (gancho) | ✅ Done | `Payable.InitializeFromCapture(...)` factory + event `PayableCreatedFromCapture`; campo `CapturedBillId?` no estado; referência fraca `CapturedBillId` ao Aggregate da sibling BC `BillIngestion`. Dedup por `(TenantId, CapturedBillId)` é responsabilidade da Application/Infra (unique index) — o Domain só expõe o link |
| 8 — Parcelamento | ✅ Done | `InstallmentPlans/InstallmentPlan` (Aggregate Root tradicional, snapshot via EF), `InstallmentPlanId`, enums `InstallmentPlanStatus` (`Active`/`Cancelled`) e `InstallmentFrequency` (`Monthly`/`Weekly` com helper `DueDateFor`); events `InstallmentPlanCreated`/`PayableLinkedToInstallmentPlan`/`InstallmentPlanCancelled` (esse último carrega `LinkedPayableIds` para o handler aplicar cancel em cascata); `Services/InstallmentPlanFactory` (stateless, distribui centavos com resíduo na **última** parcela: 1000/3 = 333.33+333.33+**333.34**); `Payable.InitializeAsInstallment(...)` + event `PayableCreatedAsInstallment` + campos `InstallmentPlanId?`/`InstallmentNumber?`. Reusa VOs `Money` e `Description` cross-aggregate |
| 9 — Classificação automática | ✅ Done | `ExpenseClassificationRules/ExpenseClassificationRule` (Aggregate Root tradicional) com `CreateManual`/`LearnFromHistory`/`Update`/`Activate`/`Deactivate` e campo `LearnedFromUserId?`; VOs `ClassificationMatcher` (combina `SupplierId`/`Keyword`/faixa de valor com AND, partial+case-insensitive na keyword, faixa inclusiva) e `ClassificationAction` (`AccountId`+`CostCenterId`+`AutoApprove`); 4 events (`Created`/`Updated`/`Activated`/`Deactivated`); `Services/PayableAutoClassifier` retorna `ClassificationDecision?` (regra ativa de maior prioridade — menor número de `Priority` ganha; ignora inativas e cross-tenant); `Payable.ClassifyAutomatically(...)` + event `PayableAutoClassified` + campo `LastClassificationRuleId?` (sem `ClassifiedBy` — audit trail vai pela regra). Reclassificação manual após automática limpa `LastClassificationRuleId` |
| 10 — Auto-approval Policy (alçadas) | ✅ Done | `AutoApprovalPolicies/AutoApprovalPolicy` (Aggregate Root tradicional) com `Create`/`AddRule`/`RemoveRule`/`ActivateRule`/`DeactivateRule`; internal Entity `ApprovalRule` (mutada só via root); VOs `ApprovalMatchCriteria` (Supplier/Account/range com AND) e `ApproverRoles` (lista normalizada upper, duplicate-free, non-empty); 5 events; `Services/ApprovalRequirementCalculator` retorna `ApprovalRequirement(Required, RequiredCount, EligibleRoles)` — fallback `DefaultManual` quando nenhuma regra ativa casa; **mais restritiva ganha** = maior `RequiredApprovalCount`. **Fluxo paralelo no `Payable`** (Sprint 5 single-approver continua intacto): novo campo `RequiredApprovalCount`, `EligibleApproverRoles`, `ApprovalsReceived` (lista de `ApprovalRecord`); métodos `RequestMultiApproval(count, roles, at)` + `RecordApproval(user, role, at)`; events `PayableMultiApprovalRequested`/`PayableApprovalRecorded`/`PayableFullyApproved`. `Approve(user, at)` lança AP.PAY17 se chamado em multi-mode |

Tests for each Aggregate / VO / Service live in `AccountsPayable.UnitTests/` mirroring the Domain folders.

Not started yet: Application layer (commands/queries/handlers via custom mediator — pendências críticas: **handler para `InstallmentPlanCancelled` aplicando cancel em cascata** (Sprint 8), **handler que roda `PayableAutoClassifier` ao consumir `PayableCreatedFromCapture`** (Sprint 9), **handler que roda `ApprovalRequirementCalculator` e escolhe entre `RequestApproval`/`RequestMultiApproval`** (Sprint 10), **handler de não-retroatividade — política em vigor é a do momento do `RequestMultiApproval`, não a atual; já é natural ao snapshot em estado mas exige cuidado no orquestrador**), Infra (DbContext, repositories, EF mappings, event store para `Payable`, unique index `(TenantId, CapturedBillId)` para dedup do Sprint 7), API (controllers, Keycloak auth, filters), and Sprint 11. The `PaymentOrder` Aggregate (Sprint 6) and the `CapturedBill` Aggregate (Sprint 7) themselves live in sibling BCs — this BC only carries weak Id references (`PaymentOrderId`, `CapturedBillId`) and consumes/produces integration events at the seams.

## Build, Run & Test

This BC has its **own `.sln`** — it is **not** part of `../../RufinoProject.sln`. Always operate from this folder.

```powershell
# Build the whole BC
dotnet build AccountsPayable.sln

# Run the API (HTTPS profile uses dev certs)
dotnet run --project AccountsPayable.API

# Unit tests
dotnet test AccountsPayable.UnitTests

# Run a single test class / method
dotnet test --filter "FullyQualifiedName~ClassName"
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"

# Stack up via Docker (API + Postgres)
docker compose up --build

# EF Core migrations (run from AccountsPayable.API folder once Infra has a DbContext)
dotnet ef database update --project ../AccountsPayable.Infra
```

### Port map (docker-compose)

| Service                 | Host port | Container port |
|-------------------------|-----------|----------------|
| `accountspayable.api`   | 8050      | 80             |
| `accountspayable.api`   | 8051      | 443            |
| `accountspayable.db`    | 8052      | 5432           |

Postgres schema: `accounts_payable`. Database: `AccountsPayableDb`. Connection string is configured in `AccountsPayable.API/appsettings.json` and points at the `accountspayable.db` service name (compose-internal DNS).

## Mandatory testing workflow

**Toda alteração no `AccountsPayable.Domain` exige rodar a suíte completa de testes unitários antes de encerrar a tarefa.** Não basta rodar só os testes da Aggregate alterada — execute `dotnet test AccountsPayable.UnitTests` inteiro, porque mudanças em SeedWork, VOs e factories de erro podem quebrar Aggregates aparentemente não relacionados.

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
- **Aggregate, Domain Service, VO, Smart Enum, evento ou erro novo** — se a entidade é citada no CLAUDE.md (tabela de Status, seção "Architecture — what is non-obvious", "Project layout"), reflita o novo item. Não precisa listar cada VO trivial, mas qualquer Aggregate Root, Domain Service ou conceito estrutural (ex.: novo prefixo de erro `AP.<AGG>##`) é obrigatório.
- **Decisão arquitetural ou convenção nova** (ADR, mudança de stack, novo padrão de pasta, nova sigla de erro, mudança de visibilidade de `*Errors.cs`, troca de mediator, nova porta no Domain) — adicione/edite na seção "Architecture — what is non-obvious".
- **Estrutura de pastas muda** (projeto novo, pasta nova de primeiro nível, renomeação) — atualize "Project layout".
- **Build/run/test workflow muda** (porta nova no docker-compose, novo `dotnet test` filtrável, schema/db trocados) — atualize "Build, Run & Test".
- **Nova convenção do skill/codegen ou nova regra do usuário** (ex.: "todo teste tem comentário", "Domain proíbe X") — adicione/edite em "Conventions inherited from the DDD skills" ou cria nova seção "Mandatory <X> workflow".

**Como aplicar**:

1. Antes de fechar a tarefa, leia o `CLAUDE.md` e pergunte: "alguma seção ficou mentindo depois das minhas alterações?" Se sim, edite.
2. Se você atualizou o código mas não tem certeza se o `CLAUDE.md` precisa mudar, **pergunte ao usuário** antes de concluir.
3. **Não duplique o que está em `AccountsPayable.Architecture/accounts-payable-sprints.md` ou no `Consolidado.md`.** O CLAUDE.md aponta para esses arquivos; ele descreve *estado* e *convenção*, não *plano* nem *design rationale*.
4. Se uma sprint for implementada apenas parcialmente, marque-a como `🚧 Em andamento` na tabela e descreva o que ficou de fora.

**Falhar em atualizar o CLAUDE.md é considerado tarefa incompleta**, mesmo que o código compile e os testes passem. Esse arquivo é o que orienta as próximas sessões do Claude Code — se ele estiver desatualizado, o próximo agente parte de premissas erradas e o débito de contexto cresce em silêncio.

## Architecture — what is non-obvious

**Event Sourcing is selective (ADR D-405).** The `Payable` Aggregate is **Event-Sourced** (Apply/Mutate/When pattern, reidratation from stream, no `private set` outside `When`). All **other** Aggregates in this BC (`Supplier`, `ChartOfAccounts`, `CostCenter`, `InstallmentPlan`, `ExpenseClassificationRule`, etc.) are **traditional** snapshot-based via EF Core. When you touch `Payable`, use the A+ES template described in the sprint plan — do not apply the same conventions as the traditional aggregates.

**No MediatR in Application by convention.** The skill `application-codegen-ddd-dotnet` declares a custom mediator (the parent BC `PeopleManagement` uses MediatR, but `AccountsPayable` deliberately diverges). Commands/Queries dispatch via the project's own mediator abstraction. The `MediatR` package in `AccountsPayable.Application.csproj` exists only as a transitional dependency until the custom mediator lands.

**Tenancy.** Every Aggregate Root carries `TenantId` (a strongly-typed `record struct : IEntityId<TenantId>`). Multi-tenancy is a discriminator column (ADR D-400, server-wide). All queries and authorization checks must filter by `TenantId`; `TenantAuthorizationFilter` is the IDOR-prevention guard at the API edge.

**Domain error IDs.** Prefix scheme is `AP.<AGG>##` — e.g., `AP.PAY01 - InvalidAmount`, `AP.SUP##`, `AP.PCL##` for classification.

**All `*Errors.cs` factories live in a single folder: `AccountsPayable.Domain/Errors/`** with namespace `AccountsPayable.Domain.Errors`. This includes factories for Aggregates (`SupplierErrors`, `PayableErrors`), internal Entities (`SupplierBankAccountErrors`), Value Objects (`TaxIdErrors`, `MoneyErrors`, etc.), Domain Services (`SupplierUniquenessCheckerErrors`), and SeedWork (`SeedWorkErrors`). Do not co-locate `*Errors.cs` next to the type it serves — keep the whole error catalog in one place so adding a new error means touching one folder and confirming uniqueness of the `AP.<AGG>##` / `SWK##` Id at a glance. The aggregate/VO/service sigla table lives as a header comment in `Errors/SeedWorkErrors.cs` — extend it whenever a new sigla is introduced.

Visibility: `SeedWorkErrors` is `public static` (called from base classes in `SeedWork/`); all others are `internal static`. Consumers (Aggregates, VOs, Services) add `using AccountsPayable.Domain.Errors;` at the top — there is no other reason to import that namespace.

**Cross-aggregate rules go in Domain Services.** A rule that needs two Aggregates (e.g., `PayableClassificationValidator` reads `ChartOfAccounts` + `CostCenter`) is a stateless Domain Service — never pass an Entity of Aggregate A into a method of Aggregate B (see user-level memory `feedback_ddd_aggregate_boundaries`).

## Project layout

```
AccountsPayable/
├── AccountsPayable.sln                  # isolated solution (not in RufinoProject.sln)
├── docker-compose.yml + override        # localized stack: API + Postgres
├── AccountsPayable.API/                 # Web SDK host, Program.cs, appsettings, Dockerfile
├── AccountsPayable.Application/         # Commands, Queries, Handlers (custom mediator, see above)
├── AccountsPayable.Domain/              # Aggregates at root (no BoundedContext/ subfolder); SeedWork shared
├── AccountsPayable.Infra/               # EF Core DbContext, repositories, EF Exception processor
├── AccountsPayable.UnitTests/           # xUnit; Domain-focused, no integration tests project yet
└── AccountsPayable.Architecture/        # Consolidado.md (design), accounts-payable-sprints.md (plan)
```

There is **no** `AccountsPayable.Services` project (unlike PeopleManagement). Domain event handlers, if any, are wired inside Application until the BC grows enough to justify a dedicated host project.

## Conventions inherited from the DDD skills

These are enforced by the `domain-codegen-ddd-dotnet`, `application-codegen-ddd-dotnet`, `infra-codegen-ddd-dotnet`, `api-codegen-ddd-dotnet`, and `tests-domain-ddd-dotnet` skills — invoke them via Skill instead of generating DDD code by hand:

- Code in English; `DomainException` messages in Portuguese; conversation in Portuguese.
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` is set on Domain (mandatory by Sprint 0).
- Strongly-typed Ids (`record struct : IEntityId<TSelf>`), Smart Enums via `Enumeration`, VOs deriving from abstract `ValueObject`.
- Aggregate Roots only emit Domain Events (never internal Entities).
- Idempotency in Application: commands wrapped via `IRequestManager` (no `IdentifiedCommand<T>` here — that's PeopleManagement's pattern).
- API uses `[ProtectedResource(resource, action)]` for Keycloak-backed granular authorization (planned — Keycloak is shared infra, configured at server level).
