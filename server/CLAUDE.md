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

- **Capabilities:** `IExpirationPolicy` (validity + `CanRenew`), `IWorkloadPolicy`, `IPeriodPolicy`, `ISignaturePolicy`, all under the `IDocumentPolicy` marker. `PolicyType` is the smart-enum discriminator.
- **Signature is a policy.** Presence of `SignaturePolicy` = the template accepts signature, and it **carries the placements** — so a placement without acceptance is unrepresentable in the persisted model. `AcceptsSignature` and `PlaceSignatures` are derived getters (`Ignore`d in EF); there is no `AcceptsSignature` column and no `PlaceSignature` table. **The signature always comes from the `acceptsSignature`/`placeSignatures` parameters, never from the `policies` set** — the API sends it separately, so letting the set drive it would wipe the signature on every Edit carrying only the other rules. The old contradiction check survives at that parameter boundary (`SetSignature`), which is the last place the two can disagree.
- **Zero is absence, not a rule.** Policy constructors reject a non-positive duration (`PMD.DOCT11`) — a rule that expires nothing is absence wearing a rule's clothes, and the Composite reads presence as "active". Three places must agree: the constructor throws, `SyncPoliciesFromFields` **skips** (legacy rows store `00:00:00` and must not blow up on edit), and the migration backfill filters `> INTERVAL '0'`. Break one and templates either grow phantom rules or fail to rehydrate.
- **Persistence:** owned collection → child table `DocumentTemplatePolicies` (`Type` int + `Params` jsonb). `DocumentPolicyFactory` (de)serializes; durations travel **in ticks** so the migration backfill can reproduce the payload in SQL.
- **Source of truth:** `Create`/`Edit` accept an optional policy set. Informed → policies win and the legacy scalars (`DocumentValidityDuration`, `Workload`) are mirrored from them. Omitted → legacy path, policies derived from the scalars. The scalars are **kept and deprecated**; the read model still reads them.
- **API contract:** optional `policies` block per rule (`expiration`, `workload`) on Create/Edit — omit for legacy behavior, `{}` for no rules. `GET /documenttemplate/PolicyType` lists the supported rules.
- **`IPeriodPolicy` drives competência (from the template).** The template's `PeriodPolicy` (granularity + retroactive) is the source. `DocumentService` reads it via `GetPolicy<IPeriodPolicy>()` and passes it to `Document.Create`, which **copies and freezes** `PeriodType`/`UsePreviousPeriod` at birth — changing the template later never rewrites documents already issued. The event no longer decides competência; it only triggers generation and supplies "now" as the reference date. `GetPeriodInfoFromEvent`/`ConvertFrequencyToPeriodType` are gone.
  - `Document.PeriodType` (nullable smart-enum column) = the frozen granularity; null = not by competência. `Document.IsPeriod` derives from it.
  - `DocumentUnit.Create`/`NewDocumentUnit` take an optional `referenceDate`: with a date, the unit lands in that competência; **without a date, it lands in `Period.CreateMinimum` (year `MIN_YEAR` = 1900)** — a placeholder replaced when a real date arrives via `UpdateDetails`. `CreateMinimum` ignores `UsePreviousPeriod` on purpose (there is no period before the floor; computing "previous" there would underflow).

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
