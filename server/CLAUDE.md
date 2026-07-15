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

- **Capabilities:** `IExpirationPolicy` (validity + `CanRenew`), `IWorkloadPolicy`, `IPeriodPolicy`, all under the `IDocumentPolicy` marker. `PolicyType` is the smart-enum discriminator.
- **Zero is absence, not a rule.** Policy constructors reject a non-positive duration (`PMD.DOCT11`) — a rule that expires nothing is absence wearing a rule's clothes, and the Composite reads presence as "active". Three places must agree: the constructor throws, `SyncPoliciesFromFields` **skips** (legacy rows store `00:00:00` and must not blow up on edit), and the migration backfill filters `> INTERVAL '0'`. Break one and templates either grow phantom rules or fail to rehydrate.
- **Persistence:** owned collection → child table `DocumentTemplatePolicies` (`Type` int + `Params` jsonb). `DocumentPolicyFactory` (de)serializes; durations travel **in ticks** so the migration backfill can reproduce the payload in SQL.
- **Source of truth:** `Create`/`Edit` accept an optional policy set. Informed → policies win and the legacy scalars (`DocumentValidityDuration`, `Workload`) are mirrored from them. Omitted → legacy path, policies derived from the scalars. The scalars are **kept and deprecated**; the read model still reads them.
- **API contract:** optional `policies` block per rule (`expiration`, `workload`) on Create/Edit — omit for legacy behavior, `{}` for no rules. `GET /documenttemplate/PolicyType` lists the supported rules.
- **`IPeriodPolicy` exists but is not consumed yet:** competência is still decided by the event flow (`DocumentService.GetPeriodInfoFromEvent`), not the template. It is deliberately absent from the API contract and from the scalar derivation until that moves to the template.

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
