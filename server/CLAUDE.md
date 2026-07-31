# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

.NET 8 (C# 12) REST API for employee and document management ("People Management"). Uses Clean Architecture with Domain-Driven Design (DDD) and CQRS pattern via MediatR.

## Build, Run & Test

```bash
# Build
dotnet build

# Run API (from server/Services/PeopleManagement/PeopleManagement.API/)
dotnet run
# HTTP: localhost:5000 | HTTPS: localhost:5001 | Swagger: localhost:5001/swagger

# Run with Docker
docker-compose -f docker-compose.yml up --profile api

# Run all unit tests
dotnet test Services/PeopleManagement/PeopleManagement.UnitTests/

# Run all integration tests
dotnet test Services/PeopleManagement/PeopleManagement.IntegrationTests/

# Run a single test class
dotnet test --filter "FullyQualifiedName~ClassName"

# EF Core migrations (from PeopleManagement.API/)
dotnet ef database update --project ../PeopleManagement.Infra
```

### Certificado HTTPS de desenvolvimento (acesso pelo IP da LAN)

Em Docker a API é servida em `https://<ip>:8041` (8041→443). O dev-cert padrão do ASP.NET só tem SAN para `localhost`/`127.0.0.1`, então **acessar pelo IP da rede o Chrome bloqueia** (`ERR_CERT_COMMON_NAME_INVALID`). Um cert [mkcert](https://github.com/FiloSottile/mkcert) cobre esse caso.

- **Cert em uso:** `%APPDATA%\ASP.NET\Https\PeopleManagement.API.lan.pfx`, senha `changeit`, apontado explicitamente pelas envs `ASPNETCORE_Kestrel__Certificates__Default__Path/Password` no `docker-compose.override.yml`. O nome é `.lan.pfx` **de propósito**: o Visual Studio regenera/sobrescreve `PeopleManagement.API.pfx` (a convenção `Kestrel:Certificates:Development`) e apagaria o nosso. Config explícita `Certificates:Default` vence a convenção.
- **SAN não aceita wildcard de IP** — `192.168.15.*` é inválido em X.509, o match de IP é exato. A cobertura da faixa vem de listar os 254 IPs individualmente.
- **A pasta montada é `%APPDATA%\ASP.NET\Https`** (do override), não `%USERPROFILE%\.aspnet\https` (do `docker-compose.yml`) — os dois montam o mesmo destino e **o override vence**; confira com `docker compose --profile api config`.
- Regenerar (renovar, ou trocar a faixa quando o IP da rede mudar):

```bash
mkcert -install   # cria/instala a CA local no trust store do Windows (Chrome usa esse)
cd "$APPDATA/ASP.NET/Https"
# ajuste a faixa se a rede mudar; repita o -pkcs12 se precisar de outra sub-rede
mkcert -pkcs12 -p12-file PeopleManagement.API.lan.pfx \
  localhost 127.0.0.1 ::1 host.docker.internal $(seq -f "192.168.15.%g" 1 254)
```

Outras máquinas/celulares da rede não confiam nessa CA: instale nelas o `rootCA.pem` de `mkcert -CAROOT`. Certificado é só para desenvolvimento.

## Architecture

```
Services/PeopleManagement/
├── PeopleManagement.API/            # Controllers, auth, filters, DI setup (Program.cs)
├── PeopleManagement.Application/    # CQRS Commands & Queries, DTOs, FluentValidation validators
├── PeopleManagement.Domain/         # Aggregates, entities, value objects, domain events
├── PeopleManagement.Infra/          # EF Core DbContext, repositories, migrations, external service clients
├── PeopleManagement.Services/       # Domain event handlers
├── PeopleManagement.UnitTests/
└── PeopleManagement.IntegrationTests/
```

**Request flow:** Controller → MediatR dispatch → Command/Query Handler → Repository/Domain → DbContext

**CQRS:** Commands (writes) and Queries (reads) are separate, both dispatched via MediatR. Commands use `IdentifiedCommand<T>` wrapper for idempotency via `x-requestid` header.

**Domain:** Aggregates under `Domain/AggregatesModel/` — key ones: Employee, Document, Company, Department, Role, Position, Workplace, DocumentTemplate, RequireDocuments, DocumentGroup. Value objects: CPF, Name, Contact, Address, Image, etc.

**DocumentTemplate policies:** `DocumentTemplate` composes an opt-in set of rules (`Policies/`) instead of one nullable field per rule — **presence in the set = rule active**. Consumers read by capability (`GetPolicy<IExpirationPolicy>()`), never off the raw fields. A new rule is a new class, not a new column.

- **Capabilities:** `IExpirationPolicy` (validity + `CanRenew`), `IWorkloadPolicy`, `IPeriodPolicy`, `ISignaturePolicy`, `INewContractDeprecationPolicy`, all under the `IDocumentPolicy` marker. `PolicyType` is the smart-enum discriminator.
- **Expiration is two policies under one capability.** `ExpirationPolicy` renews forever (`CanRenew(_) => true`); `ExpirationLimitedPolicy(Duration, MaxRenewals)` renews `MaxRenewals` times then stops (`CanRenew(count) => count < MaxRenewals`). Both share `PolicyType.Expiration` — the discriminator between them is `ExpirationParams.MaxRenewals` (`int?`): **null = forever, present = limited**. So rows written before the limited variant (and the backfill) rehydrate as `ExpirationPolicy` with no data migration. `ToPersistence` must match `ExpirationLimitedPolicy` **before** `IExpirationPolicy` — matching by the interface would drop `MaxRenewals`. Limited invariants: `Duration > 0` (`PMD.DOCT11`) and `MaxRenewals >= 1` (`PMD.DOCT12`). **`DocumentDepreciationService` consumes it**: on expiration it reads the template's `IExpirationPolicy` and gates renewal on `CanRenew(renewalCount)`, where `renewalCount` = number of **Deprecated units** (`IDocumentRepository.CountDeprecatedUnitsAsync`, counted separately so the mutation's single-unit load stays untouched). No policy ⇒ renews forever (back-compat). Known caveat: supersession (re-upload/re-validation) also deprecates the old OK unit, so the count over-counts renewals when a document was corrected — accepted deliberately, queued to be split by a dedicated `Expired` status later.
- **Signature is a policy.** Presence of `SignaturePolicy` = the template accepts signature, and it **carries the placements** — so a placement without acceptance is unrepresentable in the persisted model. `AcceptsSignature` and `PlaceSignatures` are derived getters (`Ignore`d in EF); there is no `AcceptsSignature` column and no `PlaceSignature` table. **The signature always comes from the `acceptsSignature`/`placeSignatures` parameters, never from the `policies` set** — the API sends it separately, so letting the set drive it would wipe the signature on every Edit carrying only the other rules. The old contradiction check survives at that parameter boundary (`SetSignature`), which is the last place the two can disagree. **Read and write contracts differ for signature, on purpose.** The **write** takes `acceptsSignature` + `placeSignatures` as top-level parameters (the domain reads them there, never from the `policies` set). The **read** (`GET`) is standardized: signature is a block inside `policies` — `policies.signature` present = accepts, and it **carries the placements** (`SignaturePolicyDto.PlaceSignatures`), mirroring every other rule. `DocumentTemplateQueries.ToPoliciesDto` sources it from `GetPolicy<ISignaturePolicy>()`, independent of `TemplateFileInfo` — so a template that accepts signature without a file still returns its placements. The placements are **not** on `TemplateFileInfoDto` anymore (they were, and gating that block on `TemplateFileInfo != null` dropped signatures on GET). Top-level `AcceptsSignature` stays on the DTO (back-compat + the `simple` list endpoint), consistent with `policies.signature`'s presence.
- **New-contract deprecation is a parameterless policy.** `NewContractDeprecationPolicy` (`PolicyType.NewContractDeprecation` = 6) has **no fields** — presence is the whole rule, and its jsonb `Params` is `{}` (written from an empty record so a future parameter lands without changing the discriminator; `ToPolicy` never reads it back). **The trigger is `CompleteAdmission`**, the only path that opens an employment contract (`Employee.NewContract` is private and only it calls). `EmployeeEventHandler` gates on `EmployeeEvent.COMPLETE_ADMISSION_EVENT` and calls `IDocumentDepreciationService.DeprecateDocumentsForNewContract` **before** `CreateDocumentUnitsForEvent`, so the document reaches its final state before generation adds the new contract's pendings. The rule crosses two aggregates (template decides, document mutates), so it lives in the service, not in `Document`; the policy is read **live**, so editing the template applies from the next admission on.
  - **`Document.DeprecateDeliveredUnits()` is the aggregate side.** Delivered = `OK` **or** `Warning` — Warning is an OK unit that is expiring (`MarkAsWarning` only fires from OK), so leaving it alive would carry a previous-contract document across and let the expiration job renew it under the wrong contract. Pending / AwaitingSignature / RequiresValidation are still in flight and survive; Deprecated / Invalid / NotApplicable are already out. Unlike `MakeAsDeprecated`, which invalidates everything. It returns the count and is idempotent (a second admission counts 0).
  - **Loads ALL units.** The service `Include`s the whole `DocumentsUnits` collection because `DeprecateDeliveredUnits` calls `RefreshDocumentStatus`, which scans the collection — a partial load would make it lie. It also does **not** `SaveChanges`: it runs inside domain-event dispatch, which happens *before* the triggering UoW's `SaveChangesAsync`, so saving there would re-dispatch the queued events.
  - **A deprecated unit next to a fresh pending leaves the document `Deprecated`, not `RequiresDocument`** — `GetStatusFromGroup` ranks Deprecated above the fallback. Same reading as the expiration flow, and what the dashboard already buckets as "Vencidos".
  - **Contract:** `newContractDeprecation` is an empty block on Create/Edit (`{}` = on, omitted = off) and on the read DTO. Listed by `GET /documenttemplate/PolicyType`.
- **Zero is absence, not a rule.** Policy constructors reject a non-positive duration (`PMD.DOCT11`) — a rule that expires nothing is absence wearing a rule's clothes, and the Composite reads presence as "active". Three places must agree: the constructor throws, `SyncPoliciesFromFields` **skips** (legacy rows store `00:00:00` and must not blow up on edit), and the migration backfill filters `> INTERVAL '0'`. Break one and templates either grow phantom rules or fail to rehydrate.
- **Persistence:** owned collection → child table `DocumentTemplatePolicies` (`Type` int + `Params` jsonb). `DocumentPolicyFactory` (de)serializes; durations travel **in ticks** so the migration backfill can reproduce the payload in SQL.
- **Source of truth:** `Create`/`Edit` accept an optional policy set. Informed → policies win and the legacy scalars (`DocumentValidityDuration`, `Workload`) are mirrored from them. Omitted → legacy path, policies derived from the scalars. The scalars are **kept and deprecated**; the read model still reads them.
- **API contract:** optional `policies` block per rule (`expiration`, `workload`, `period`) on Create/Edit — omit for legacy behavior, `{}` for no rules. The `expiration` block carries `durationInDays` + optional `maxRenewals` (`int?`): omit/null → `ExpirationPolicy` (renews forever), present → `ExpirationLimitedPolicy` (renews N times). The read DTO (`ExpirationPolicyDto`) echoes `maxRenewals` back (null for forever). The `period` block carries `periodTypeId` + `usePreviousPeriod`; a present block sets the `PeriodPolicy`, and `SyncFieldsFromPolicies` mirrors `usePreviousPeriod` into the legacy scalar. `GET /documenttemplate/PolicyType` lists the supported rules; `GET /documenttemplate/PeriodType` lists the competência granularities for the UI dropdown.
- **`IPeriodPolicy` drives competência — read LIVE from the template, never copied.** "Template é a configuração, a unit é a história": every operation reads the template's `PeriodPolicy` at that moment and passes **values** (`PeriodType?`, `usePreviousPeriod`) into the aggregate methods (`NewDocumentUnit`, `UpdateDocumentUnitDetails`, `DocumentUnit.Create/SetPeriod/UpdateDetails`) — never the template entity (aggregate boundary). There is **no** `Document.PeriodType`/`UsePreviousPeriod` column (dropped by `RemovePeriodConfigFromDocuments`, no backfill needed — legacy docs work as soon as the template has the policy, which `DeriveDocumentTemplatePeriodPolicies` backfilled). Editing the template takes effect **immediately** for the next operations on every document; competências already stamped on units are per-unit history and never move by themselves. This mirrors how expiration/workload/signature were always consumed (live at update/expiration/send time) — period used to be the lone frozen copy. The event only triggers generation and supplies "now" as the reference date.
  - Callers that create units must have the template in hand: `DocumentService` (both generation flows load ALL involved templates, not just the ones without documents), `DocumentDepreciationService` (renewal), `SignDocumentService.InvalidateSessionDocuments` (replacement units, batch-loads the session's templates), `BatchCreateDocumentUnitsCommandHandler`. The read DTO's `UsePreviousPeriod` now comes from the template join (`DocumentQueries`), same wire contract.
  - `DocumentUnit.Create`/`NewDocumentUnit` take the period config + an optional `referenceDate`: with a date, the unit lands in that competência; **without a date, it lands in `Period.CreateMinimum` (year `MIN_YEAR` = 1900)** — a placeholder replaced when a real date arrives via `UpdateDetails`. `CreateMinimum` ignores `UsePreviousPeriod` on purpose (there is no period before the floor; computing "previous" there would underflow).
  - **Minimum-period pendings survive granularity changes.** When the candidate period is the minimum and no exact match exists, `NewDocumentUnit` reuses ANY pending sitting at `MIN_YEAR` regardless of its (old) granularity and re-situates it via `ResetPeriodToMinimum` — otherwise editing Monthly→Yearly would orphan the waiting pending and create a duplicate. `UpdateDetails` with a period config also **situates units that had no period yet** (legacy docs heal on their next date update); without a config, existing periods are left untouched (history).

**Document content snapshot (`Content` da unidade):** o `DocumentUnit.Content` é um **snapshot** dos dados (empresa, funcionário, cargo, …) tirado no `UpdateDocumentUnitDetails` e consumido depois por `GeneratePdf`/`GeneratePdfRange` e pelo fluxo de assinatura (`SignDocumentService` chama `GeneratePdf`). Como o cadastro muda com o tempo, o snapshot envelhece em silêncio — daí a verificação abaixo.

- **`IDocumentContentBuilder` é a ÚNICA definição do formato** (`Services/DocumentContentBuilder.cs`). Gravação e verificação passam por ele, para que a comparação seja contra uma string produzida exatamente do mesmo jeito. Recebe **valores** (`recoverDataTypes`, `date`, `validity`, `workloadEndDate`), nunca o `DocumentTemplate` — não cruza fronteira de aggregate. Mexer no formato aqui muda PDFs em produção; `DocumentContentOutdatedTests` tem a regressão que pega isso (grava pelo update e confere que o check imediatamente seguinte não acusa divergência).
- **Comparação é `string.Equals(..., Ordinal)` contra o `Content` cru** — sem normalizar, reserializar ou converter. O merge de `JsonObject` preserva ordem de inserção e nunca muta as fontes, então mesma entrada ⇒ mesma string.
- **As datas vêm da unidade, não do template.** `CheckOutdatedContent` alimenta o builder com `unit.Date`/`unit.Validity`/`unit.WorkloadEndDate` já gravados. Recalcular a carga horária exigiria `VerifyTimeConflictBetweenDocument`, que escreve e lança — inaceitável num caminho de leitura. Consequência deliberada: o check enxerga **divergência de dado recuperado**, não mudança de regra do template (se a validade do template mudou, o check não acusa; o refresh corrigiria).
- **Falha ao recuperar ⇒ indeterminado, nunca "desatualizado".** `RecoverInfoToDocument` sempre pulou o tipo que falha, então o bloco sai ausente do JSON — indistinguível de um dado que mudou. O builder devolve `FailedTypes`; havendo qualquer falha o item volta `CheckFailed = true` com `IsOutdated = false`, para não empurrar o usuário a sobrescrever conteúdo bom por causa de um erro transitório.
- **Memo por (funcionário, tipo de dado)** dentro do builder (scoped ⇒ vale a request). Lotes repetem o mesmo par dezenas de vezes; dado de funcionário não muda no meio da request.
- **Endpoints** (`DocumentController`, ambos com body `{ items: [{documentUnitId, documentId, employeeId}] }`): `POST /document/content/check-outdated` (`[ProtectedResource("document","generate")]`, leitura pura — sem `IdentifiedCommand`, sem `SaveChanges`) devolve `{ items: [{documentUnitId, isOutdated, checkFailed}] }`; `POST /document/content/refresh` (`[ProtectedResource("document","edit")]`, com `IdentifiedCommand`) regrava o snapshot reusando `UpdateDocumentUnitDetails` **com a data já gravada** na unidade — renovar nunca move a data do documento, e só vale para unidade `Pending` (mesma guarda da geração). O refresh existe em vez de reaproveitar `PUT /document/DocumentUnit` justamente para o cliente não precisar reenviar a data (round-trip de formatação erraria a data do documento).

**Document dashboard (read model):** `api/v1/{company}/document-dashboard` (`GET /summary` + `GET /units`, both `[ProtectedResource("document","view")]`) serves the client's company-wide document dashboard from `DocumentDashboardQueries` (`Application/Queries/DocumentDashboard/`). Units are classified into five mutually-derived buckets — Expired, Expiring, Pending, AwaitingSignature, RequiresValidation — and summary counts and the paginated list share the SAME predicate per bucket, so they can never disagree. Semantics worth preserving: **Expired** = Deprecated/Invalid units NOT superseded by a resolved unit (OK/RequiresValidation/AwaitingSignature/Warning/NotApplicable, or a newer Deprecated/Invalid) in the same period group, plus OK/Warning units whose `Validity` is already past (covers the gap between the validity date and the depreciation job); **Expiring** = OK/Warning with `Validity` within `expiringInDays` (query param, default 30) of "today" computed in the configured `TimeZoneOptions` zone. Filters: employee status/name, document group, document template. Ordering: validity ascending (nulls last) for Expired/Expiring, unit date for the rest. Covered by `DocumentDashboardTests` (integration).

**Database:** PostgreSQL via EF Core 9 (Npgsql). Schema: `people_management`. Unit of Work pattern implemented in `PeopleManagementContext`. Domain events are dispatched during `SaveEntitiesAsync`.

**Auth:** Keycloak JWT Bearer tokens. Custom `[ProtectedResource("resource", "action")]` attribute for route-level authorization. Auth config in `PeopleManagement.API/Authentication/` and `Authorization/`.

**API routes:** All follow `/api/v1/{company}/{resource}` pattern. The `{company}` segment scopes operations to a company.

## Key External Integrations

- **Document Signing:** ZapSign API (with webhook callbacks at `/document/insert/signer`)
- **File Storage:** S3-compatible (Garage.io) via AWS SDK
- **Background Jobs:** Hangfire with PostgreSQL storage. Dashboard at `/hangfire`. Two queues: `default` and `whatsapp` (serial, 1 worker). The two `AddHangfireServer` (workers) in `Program.cs` are gated off when `ASPNETCORE_ENVIRONMENT == "IntegrationTest"` — storage/client stay registered, but no job is processed (determinism in tests).
- **PDF Generation:** PuppeteerSharp (requires Chrome/Chromium — bundled in Docker)
- **WhatsApp:** Evolution API for messaging
- **Timezone:** `E. South America Standard Time` (Brazil)

## DI Registration

Dependencies are registered via extension methods in Program.cs:
- `AddInfraDependencies()` — repositories, DbContext, external services
- `AddApplicationDependencies()` — MediatR handlers, validators, behaviors
- `AddServicesDependencies()` — domain event handlers

## Error Handling

`ApplicationExceptionFilter` maps EF Core exceptions and `DomainException` to HTTP responses. Domain uses `Error` class with code/message and `Result<T>` pattern.

## Integration Tests

`PeopleManagement.IntegrationTests` runs against real containers via Testcontainers (Postgres + LocalStack for S3). Setup notes:

- **Shared fixture:** `PeopleManagementWebApplicationFactory` is a single **`ICollectionFixture`** (`IntegrationTestCollection`) for the whole suite — one set of containers, and all test classes share it. Because they live in one collection, xUnit runs them **serially**.
- **Isolation:** `BaseIntegrationTest` (base of every test class) resets the `people_management` schema via **Respawn** in `DisposeAsync` after each test. Tests seed their own data; there is no shared seed between tests. The reset also disposes the `IServiceScope`s handed out by `_factory.GetContext()`.
- **Shared helpers (`BaseIntegrationTest`):** reuse these instead of re-inlining boilerplate — `GetContext()`/`CreateClient()`; `PdfMultipartContent(params (name,value)[])` (multipart with the sample PDF under `formFile`); `GetDocumentAsync(id)` (fresh scope + `AsNoTracking` + `Include(DocumentsUnits)`); `AssertBlobExistsAsync(name, companyId)` (S3 download > 0 bytes).
- **Seeding:** data is built through the EF Object Mother extensions in `Data/PopulateDataBase` (`InsertCompany`, `InsertEmployeeActive`, `InsertDocument`, …) — seed through the domain factories, not raw SQL.
- **Environment `IntegrationTest`:** the factory sets `UseEnvironment("IntegrationTest")`, which (a) gates off the Hangfire workers in `Program.cs` and (b) skips `PopulateDb` (clean baseline). Jobs are only scheduled in storage, never processed.
- **Auth** is mocked (`MockAuthenticationHandler` + `MockAccessRequirementHandler`); `ConfigsUtils.InputHeaders(companies, authorization, xRequestId)` sets the request headers — `companies` goes as a single comma-separated value (what the handler `Split(',')`s), and a deterministic `xRequestId` can be passed to exercise `IdentifiedCommand` idempotency.
- **PDF templates:** the `CopyTemplatesToAppFiles` MSBuild target copies `DataForTests/templates/**` to `app_files/templates/**` in the output, where `PdfService` reads them (`DocumentTemplatesOptions:SourceDirectory`).
- **Skipped tests** depend on external ZapSign API/URLs or on an active Hangfire worker (incompatible with the deterministic setup) — see the `Skip` reasons.
- **Snapshot coverage:** `DocumentContentOutdatedTests` (funcionário intocado ⇒ não desatualizado — que é também a regressão de formato byte a byte do `IDocumentContentBuilder`; contato alterado ⇒ desatualizado; unidade sem `Content` ⇒ nada a avisar; lote misto marca só a alterada; refresh regrava o snapshot e preserva a data; unidade não-pendente é recusada no refresh). O template do seed usa **só** `RecoverDataType.Employee`: tipos que falham ao recuperar (PGR sem dado) marcariam a verificação como inconclusiva.
- **Policy full-cycle coverage:** `DocumentPeriodLifecycleTests` (competência after birth: minimum→real via update, `UsePreviousPeriod`, period moves with the date, duplicate-pending invalidation, template-edit applies to the NEXT operations — live read, not freeze), `DocumentPeriodLiveReadTests` (granularity change reuses+resituates the minimum-period pending instead of orphaning it, a document born before the template had a `PeriodPolicy` heals on its next update, delivered units keep their recorded periods), `DocumentUnitDetailsPolicyTests` (expiration→`Validity`, workload→`WorkloadEndDate`/working-day guard, and the absent-policy counterpoints), `SignDocumentPolicyGuardTests` (unsignable template rejected before any external call), `DocumentPolicyFullCycleTests` (all four policies composed: generate→update→OK→expire→renew-at-minimum-period→expire→stop at the renewal cap), `NewContractDeprecationPolicyTests` (real dismissal→readmission through the endpoints: the delivered unit of the template *with* the rule is deprecated and a fresh pending is generated, the one *without* it survives OK, and an in-flight pending is untouched). Note: `DocumentUnit.Validity`'s setter rejects past validity, so expiration scenarios must anchor dates on "today" — fixed 2024 dates only work for templates without an expiration policy.
