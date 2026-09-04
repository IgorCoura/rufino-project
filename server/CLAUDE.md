# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

.NET 8 (C# 12) REST API for employee and document management ("People Management"). Uses Clean Architecture with Domain-Driven Design (DDD) and CQRS pattern via MediatR.

> **Este arquivo descreve o BC `PeopleManagement`.** O servidor tem três Bounded Contexts, cada um
> com `.sln`, banco e deploy próprios — e os outros dois têm CLAUDE.md próprio, que **sobrepõe**
> este:
>
> | BC | Pasta | O que faz | Portas (host) |
> |---|---|---|---|
> | PeopleManagement | `Services/PeopleManagement/` | Funcionários e documentos | 8040–8042 |
> | BillPayment | `Services/BillPayment/` | Captura, valida e paga boletos | 8100–8104 |
> | TenantManagement | `Services/TenantManagement/` | **Emite o `TenantId`** e diz quem acessa cada tenant (PF e PJ) | 8110, 8112 |
>
> Desde 2026-08-18 a **`RufinoProject.sln` inclui os três BCs** (pastas de solução
> `Services/TenantManagement` e `Services/BillPayment`) — as `.sln` por BC continuam existindo
> para trabalho isolado. O `docker-compose.yml` desta pasta sobe a plataforma inteira:
>
> ```bash
> docker compose --profile api up --build   # 3 APIs + 3 bancos + storage S3 + Keycloak local
> docker compose up -d                      # só as dependências (bancos + storage)
> docker compose --profile keycloak up -d   # dependências + Keycloak local (realm rufino
>                                           # importado de utils/KeyCloakConfig na 1ª subida)
> ```
>
> Variáveis opcionais em `server/.env` (ver `.env.example`): `KEYCLOAK_URL` e
> `KEYCLOAK_LOCAL_URL` — as APIs apontam para a nuvem por padrão; defina as duas, **com a mesma
> URL**, para usar o Keycloak local. **Segredo NÃO vai no `.env`** (ver o aviso abaixo).
> ⚠️ O compose da raiz e os composes por BC publicam as mesmas portas —
> use um OU outro. O realm importado é
> `utils/KeyCloakConfig/RufinoRealm/realm-import-2026-08-18.json` — o export da nuvem de 18/08
> **mais** o client `bill-payment-api` (authz completo), os papéis `bill-*`, os mappers
> `bp_tenants`/`pm_tenants`/audience no `tenant-scope` e a **declaração de `bp_tenants` e
> `pm_tenants` no User Profile** (gerado em 2026-08-18; o mesmo arquivo
> serve para importar o BillPayment no Keycloak da nuvem via partial import). ⚠️ Exports do
> admin console mascaram secrets: depois de importar, regenere os secrets dos clients
> confidenciais e configure-os nas APIs.
>
> ⚠️ **Mapper e atributo são coisas separadas, e faltar o segundo falha em silêncio.** O mapper
> (em `clientScopes`) só diz como um atributo vira claim; **quem autoriza o atributo a existir é o
> User Profile** (em `components` → `org.keycloak.userprofile.UserProfileProvider`). Como
> `unmanagedAttributePolicy` está ausente no realm, atributo não declarado é **descartado na
> escrita com HTTP 204** — o provisionador recebe "sucesso", marca o vínculo como `Done`, e o
> atributo nunca existe. Foi o que aconteceu em 2026-08-19: os mappers estavam certos, o claim
> nunca chegava no token, e todo endpoint do BillPayment respondia 403. **Produto novo exige os
> dois passos**: o mapper no `tenant-scope` e a declaração no User Profile (multivalorado,
> `view: [admin, user]`, `edit: [admin]` — copie a entrada de `tenants`).
>
> O `Company` deste BC e o `Tenant` do TenantManagement **não são a mesma entidade**: o Tenant é o
> registro-mestre da identidade, o `Company` continua sendo o cadastro local do RH. **Mas o id é o
> mesmo** — é isso que o backfill preserva —, e desde 2026-09-03 este BC lê o claim **`pm_tenants`**
> no lugar do `companies` legado (ver "Auth" abaixo).

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
- **Expiration is two policies under one capability.** `ExpirationPolicy` renews forever (`CanRenew(_) => true`); `ExpirationLimitedPolicy(Duration, MaxRenewals)` renews `MaxRenewals` times then stops (`CanRenew(count) => count < MaxRenewals`). Both share `PolicyType.Expiration` — the discriminator between them is `ExpirationParams.MaxRenewals` (`int?`): **null = forever, present = limited**. So rows written before the limited variant (and the backfill) rehydrate as `ExpirationPolicy` with no data migration. `ToPersistence` must match `ExpirationLimitedPolicy` **before** `IExpirationPolicy` — matching by the interface would drop `MaxRenewals`. Limited invariants: `Duration > 0` (`PMD.DOCT11`) and `MaxRenewals >= 1` (`PMD.DOCT12`). **`DocumentDepreciationService` consumes it**: on expiration it reads the template's `IExpirationPolicy` and gates renewal on `CanRenew(renewalCount)`, where `renewalCount` = number of **Deprecated units** (`IDocumentRepository.CountDeprecatedUnitsAsync`, counted separately so the mutation's single-unit load stays untouched). No policy ⇒ renews forever (back-compat). Known caveat: supersession (re-upload/re-validation) also deprecates the old OK unit, so the count over-counts renewals when a document was corrected — accepted deliberately, queued to be split by a dedicated `Expired` status later.
- **Signature is a policy.** Presence of `SignaturePolicy` = the template accepts signature, and it **carries the placements** — so a placement without acceptance is unrepresentable in the persisted model. `AcceptsSignature` and `PlaceSignatures` are derived getters (`Ignore`d in EF); there is no `AcceptsSignature` column and no `PlaceSignature` table. **The signature always comes from the `acceptsSignature`/`placeSignatures` parameters, never from the `policies` set** — the API sends it separately, so letting the set drive it would wipe the signature on every Edit carrying only the other rules. The old contradiction check survives at that parameter boundary (`SetSignature`), which is the last place the two can disagree. **Read and write contracts differ for signature, on purpose.** The **write** takes `acceptsSignature` + `placeSignatures` as top-level parameters (the domain reads them there, never from the `policies` set). The **read** (`GET`) is standardized: signature is a block inside `policies` — `policies.signature` present = accepts, and it **carries the placements** (`SignaturePolicyDto.PlaceSignatures`), mirroring every other rule. `DocumentTemplateQueries.ToPoliciesDto` sources it from `GetPolicy<ISignaturePolicy>()`, independent of `TemplateFileInfo` — so a template that accepts signature without a file still returns its placements. The placements are **not** on `TemplateFileInfoDto` anymore (they were, and gating that block on `TemplateFileInfo != null` dropped signatures on GET). Top-level `AcceptsSignature` stays on the DTO (back-compat + the `simple` list endpoint), consistent with `policies.signature`'s presence.
- **Zero is absence, not a rule.** Policy constructors reject a non-positive duration (`PMD.DOCT11`) — a rule that expires nothing is absence wearing a rule's clothes, and the Composite reads presence as "active". Three places must agree: the constructor throws, `SyncPoliciesFromFields` **skips** (legacy rows store `00:00:00` and must not blow up on edit), and the migration backfill filters `> INTERVAL '0'`. Break one and templates either grow phantom rules or fail to rehydrate.
- **Persistence:** owned collection → child table `DocumentTemplatePolicies` (`Type` int + `Params` jsonb). `DocumentPolicyFactory` (de)serializes; durations travel **in ticks** so the migration backfill can reproduce the payload in SQL.
- **Source of truth:** `Create`/`Edit` accept an optional policy set. Informed → policies win and the legacy scalars (`DocumentValidityDuration`, `Workload`) are mirrored from them. Omitted → legacy path, policies derived from the scalars. The scalars are **kept and deprecated**; the read model still reads them.
- **API contract:** optional `policies` block per rule (`expiration`, `workload`, `period`) on Create/Edit — omit for legacy behavior, `{}` for no rules. The `expiration` block carries `durationInDays` + optional `maxRenewals` (`int?`): omit/null → `ExpirationPolicy` (renews forever), present → `ExpirationLimitedPolicy` (renews N times). The read DTO (`ExpirationPolicyDto`) echoes `maxRenewals` back (null for forever). The `period` block carries `periodTypeId` + `usePreviousPeriod`; a present block sets the `PeriodPolicy`, and `SyncFieldsFromPolicies` mirrors `usePreviousPeriod` into the legacy scalar. `GET /documenttemplate/PolicyType` lists the supported rules; `GET /documenttemplate/PeriodType` lists the competência granularities for the UI dropdown.
- **`IPeriodPolicy` drives competência — read LIVE from the template, never copied.** "Template é a configuração, a unit é a história": every operation reads the template's `PeriodPolicy` at that moment and passes **values** (`PeriodType?`, `usePreviousPeriod`) into the aggregate methods (`NewDocumentUnit`, `UpdateDocumentUnitDetails`, `DocumentUnit.Create/SetPeriod/UpdateDetails`) — never the template entity (aggregate boundary). There is **no** `Document.PeriodType`/`UsePreviousPeriod` column (dropped by `RemovePeriodConfigFromDocuments`, no backfill needed — legacy docs work as soon as the template has the policy, which `DeriveDocumentTemplatePeriodPolicies` backfilled). Editing the template takes effect **immediately** for the next operations on every document; competências already stamped on units are per-unit history and never move by themselves. This mirrors how expiration/workload/signature were always consumed (live at update/expiration/send time) — period used to be the lone frozen copy. The event only triggers generation and supplies "now" as the reference date.
  - Callers that create units must have the template in hand: `DocumentService` (both generation flows load ALL involved templates, not just the ones without documents), `DocumentDepreciationService` (renewal), `SignDocumentService.InvalidateSessionDocuments` (replacement units, batch-loads the session's templates), `BatchCreateDocumentUnitsCommandHandler`. The read DTO's `UsePreviousPeriod` now comes from the template join (`DocumentQueries`), same wire contract.
  - `DocumentUnit.Create`/`NewDocumentUnit` take the period config + an optional `referenceDate`: with a date, the unit lands in that competência; **without a date, it lands in `Period.CreateMinimum` (year `MIN_YEAR` = 1900)** — a placeholder replaced when a real date arrives via `UpdateDetails`. `CreateMinimum` ignores `UsePreviousPeriod` on purpose (there is no period before the floor; computing "previous" there would underflow).
  - **Minimum-period pendings survive granularity changes.** When the candidate period is the minimum and no exact match exists, `NewDocumentUnit` reuses ANY pending sitting at `MIN_YEAR` regardless of its (old) granularity and re-situates it via `ResetPeriodToMinimum` — otherwise editing Monthly→Yearly would orphan the waiting pending and create a duplicate. `UpdateDetails` with a period config also **situates units that had no period yet** (legacy docs heal on their next date update); without a config, existing periods are left untouched (history).

**Document dashboard (read model):** `api/v1/{company}/document-dashboard` (`GET /summary` + `GET /units`, both `[ProtectedResource("document","view")]`) serves the client's company-wide document dashboard from `DocumentDashboardQueries` (`Application/Queries/DocumentDashboard/`). Units are classified into five mutually-derived buckets — Expired, Expiring, Pending, AwaitingSignature, RequiresValidation — and summary counts and the paginated list share the SAME predicate per bucket, so they can never disagree. Semantics worth preserving: **Expired** = Deprecated/Invalid units NOT superseded by a resolved unit (OK/RequiresValidation/AwaitingSignature/Warning/NotApplicable, or a newer Deprecated/Invalid) in the same period group, plus OK/Warning units whose `Validity` is already past (covers the gap between the validity date and the depreciation job); **Expiring** = OK/Warning with `Validity` within `expiringInDays` (query param, default 30) of "today" computed in the configured `TimeZoneOptions` zone. Filters: employee status/name, document group, document template. Ordering: validity ascending (nulls last) for Expired/Expiring, unit date for the rest. Covered by `DocumentDashboardTests` (integration).

**Database:** PostgreSQL via EF Core 9 (Npgsql). Schema: `people_management`. Unit of Work pattern implemented in `PeopleManagementContext`. Domain events are dispatched during `SaveEntitiesAsync`.

- **A tabela de histórico de migração TEM que ser declarada com o schema explícito**, nos dois `UseNpgsql` do `Program.cs` **e** no da `PeopleManagementWebApplicationFactory`: `MigrationsHistoryTable("__EFMigrationsHistory", PeopleManagementContext.DEFAULT_SCHEMA)`. A connection string traz `SearchPath=people_management`, e o `Migrate()` cria essa tabela **antes** de aplicar qualquer migração — ou seja, antes do `EnsureSchema` que criaria o schema. Sem o schema explícito o `CREATE TABLE` sai sem qualificação, o Postgres procura num `search_path` que aponta para schema inexistente, e **todo banco virgem morre em `3F000: no schema has been selected to create in`**. Com o schema declarado, o EF emite o `CREATE SCHEMA IF NOT EXISTS` junto com a tabela e a ordem se resolve sozinha. Aconteceu de verdade em 2026-08-19, quando o volume de desenvolvimento foi recriado; o defeito era latente desde que um `ExecuteSqlRaw("CREATE SCHEMA IF NOT EXISTS …")` foi comentado no `Program.cs` sob o raciocínio de que a migração já criava o schema — verdade, mas tarde demais. **Não restaure aquele comando**: SQL cru antes do `Migrate()` abre conexão num database que pode não existir e reintroduz o `3D000` que o comentário acima do `Migrate()` documenta.
- **O nome tem que continuar `__EFMigrationsHistory`**, o padrão do EF — e **não** o `__ef_migrations_history` que o BillPayment usa. Os ambientes já existentes gravaram o histórico com o nome padrão (o `search_path` resolvia para `people_management` quando o schema existia), então renomear faria o EF não encontrar registro nenhum e tentar reaplicar as 14 migrações num banco que já as tem.
- **A registração que vale é a última.** `AddDbContextFactory` re-registra `DbContextOptions<PeopleManagementContext>`, então é ela que o contexto resolvido pelo DI recebe — inclusive o que roda o `Migrate()`. É por isso que a fábrica de teste, que só substitui o `IDbContextFactory`, também precisa repetir a configuração: sem isso a suíte validaria um layout de histórico que o deploy nunca produz.
- **O histórico entra no `TablesToIgnore` do Respawn** (`PeopleManagementWebApplicationFactory`). Ele vive dentro de `people_management`, que está em `SchemasToInclude`, e histórico de migração não é dado de teste — apagá-lo faz o host seguinte concluir que o banco está vazio e morrer em `42P07 relation already exists`. Mesma dupla de armadilhas registrada no `gotchas.md` do BillPayment.

**Auth:** Keycloak JWT Bearer tokens. Custom `[ProtectedResource("resource", "action")]` attribute for route-level authorization. Auth config in `PeopleManagement.API/Authentication/` and `Authorization/`.

- ⚠️ **As convenções de nome de client, papel, recurso, escopo e seção de `appsettings` são
  normativas e vivem em [`utils/KeyCloakConfig/CONVENCOES.md`](../utils/KeyCloakConfig/CONVENCOES.md).**
  Leia antes de criar API, papel ou seção nova — o realm já teve três convenções simultâneas.
- **A permissão é buscada UMA VEZ POR TOKEN, não por requisição** (2026-09-04). O
  `AuthorizationServerClient` pede o retrato inteiro ao Keycloak (`response_mode=permissions`
  **sem** o parâmetro `permission`) e o `RptCache` o guarda, chaveado pelo **SHA-256 do token**
  (nunca pelo `sub`: dois tokens da mesma pessoa podem ter escopos diferentes). O TTL é o menor
  entre `Keycloak:RptCacheTtl` (60 s) e o que resta do `exp`. **Falha não é cacheada**, mas o
  retrato anterior sobrevive a ela dentro de `Keycloak:RptStaleGrace` (*fail-static*, 10 min):
  Keycloak fora do ar deixa de derrubar quem já estava usando. Token RECUSADO nunca é servido por
  retrato velho. **A suíte roda com `Keycloak:RptCacheEnabled=false`** — quem prova o cache é
  `Tests/Authorization/RptCacheTests`, que troca o token de propósito.
- **O tipo do claim casa por igualdade EXATA**, não `Contains` (2026-09-04). Com `Contains`,
  `"bp_tenants".Contains("tenants")` é verdadeiro e uma API que lesse o `tenants` genérico
  aceitaria o claim de outro produto.
- **O fallback de autorização exige autenticação**: endpoint sem atributo nasce FECHADO. Quem
  precisa ser anônimo declara `[AllowAnonymous]` — hoje só o `HealthController` (`api/health`),
  que existe desde essa mudança porque antes a única rota anônima era o `GET /Test`, removido.
- **O `OnChallenge` define o status ANTES de escrever o corpo.** Escrever primeiro faz a resposta
  ser commitada com o 200 padrão: uma requisição SEM TOKEN devolvia
  `200 {"error": "Unauthorized access"}`, e qualquer cliente que olhe o status trataria a negativa
  como sucesso. O defeito viveu aqui desde o início do BC e foi corrigido em 2026-09-04, quando o
  `RouteGuardTests` novo finalmente o exercitou.
- **Swagger é gated em Development**, e CORS lê a allowlist de `Cors:AllowedOrigins` (era
  `AllowAnyOrigin()`). Cabeçalhos de segurança (`nosniff`, `X-Frame-Options: DENY`,
  `Referrer-Policy`) em toda resposta, `UseHsts()` fora de Development, e limitador de taxa por
  pessoa (`RateLimiting`, 300/min global).

- **O guard de rota lê `pm_tenants` desde 2026-09-03, não mais o `companies` legado.** O
  `RouteAccessRequirement` confere o `{company}` da rota contra o claim POR PRODUTO emitido pelo
  TenantManagement (ADR-005 de lá): ele traz só os tenants em que a pessoa tem vínculo ativo **e**
  o PeopleManagement está habilitado. O `companies` era **escrito à mão no console do Keycloak** —
  nenhum código o produzia — e suspender um cliente não o afetava, porque o
  `KeycloakTenantAccessProvisioner` o preserva de propósito. O valor não mudou: é o mesmo Guid do
  `Company.Id`, que o backfill preservou, então o nome do parâmetro de rota continua `{company}`.
  **Não troque o claim por `tenants`**: ele é o genérico e daria acesso a quem só assinou o
  BillPayment. Desde 2026-09-04 o handler casa o tipo por igualdade exata (antes era `Contains`,
  e `"bp_tenants".Contains("tenants")` é verdadeiro). **Ordem de deploy obrigatória**: backfill → declarar `pm_tenants` no User Profile do
  realm → mappers → reprovisionar TODOS os tenants → só então esta configuração; fora dela o
  atributo nasce vazio e todo cliente legítimo toma 403. Cliente que não tiver o produto
  `PeopleManagement` ativo no TenantManagement some do claim — auditar antes de virar a chave.
- **A comparação do valor é case-insensitive** (alinhada ao BillPayment): o parâmetro vem da URL
  como o cliente escreveu e o claim como o provisionador gravou, e um Guid com caixa diferente dos
  dois lados produzia 403 sem explicação.
- ✅ **O guard de produção passou a ser coberto por teste em 2026-09-04.** A fábrica lê o nome do
  parâmetro de rota e o do claim do MESMO `AuthorizationOptions` que a produção monta (eram
  escritos à mão como `("company", "companies")` — o claim legado), e
  `Tests/Authorization/RouteGuardTests` exige que o claim configurado seja o mesmo que a suíte
  envia. Junto vieram `EndpointProtectionTests` (erosão de rota) e `RealmContractTests` (todo
  `[ProtectedResource]` do código existe no authz-config versionado).
- ⚠️ **`CompanyController` está fora do guard, e trocar o claim não muda isso.** Desde 2026-09-04
  a exceção é NOMEADA em `EndpointProtectionTests.KnownUnguardedRoutes`, com o motivo escrito: o
  buraco fica visível no código em vez de esquecido, e a próxima rota sem `{company}` reprova o
  teste. A rota é
  `api/v1/[controller]`, sem `{company}`, e o handler **concede** quando o parâmetro é nulo — é o
  que permite endpoint que não é de empresa nenhuma. Os três endpoints de leitura recebem os ids
  por query string e não os validam contra claim nenhum.

- **Segredo de desenvolvimento vem do `dotnet user-secrets`, e o compose NÃO pode injetá-lo por variável de ambiente.** Vale para os três BCs. A forma `${VAR:-}` do Compose **não deixa a variável ausente** quando `VAR` não está definida: ela define a variável com **string vazia**. E variável de ambiente vem **depois** do user-secrets na ordem de configuração do ASP.NET Core — então a string vazia **sobrescreve o segredo do user-secrets**, em silêncio, porque o valor existe e só está vazio. Aconteceu em 2026-08-19: `TenantProvisioning:ClientSecret` estava corretamente configurado no `secrets.json`, o container montava a pasta e enxergava a chave, e mesmo assim o DI registrava o `UnconfiguredTenantAccessProvisioner` e todo vínculo saía `Failed` — porque `TenantProvisioning__ClientSecret=` chegava vazio pelo compose. Os cinco pontos que faziam isso foram removidos do `server/docker-compose.yml` e do `Services/BillPayment/docker-compose.yml`. **Se algum dia precisar injetar segredo pelo compose, use `${VAR:?mensagem}`** — falha alto quando ausente — **nunca `${VAR:-}`**.
- **Rodar fora do contêiner aponta para a NUVEM.** Os `appsettings.json` dos três BCs trazem `Keycloak:AuthServerUrl` (e o `TenantProvisioning:AuthServerUrl` do TenantManagement) apontando para `https://keycloak.couratechsafety.cloud`. Quem sobrepõe para o Keycloak local é o compose, via `KEYCLOAK_URL`. Logo, `dotnet run` fora do Docker — workflow documentado nos CLAUDE.md por BC — fala com a nuvem mesmo com a stack local no ar, e o token do Keycloak local é recusado. Para esse caminho, configure a URL local em `appsettings.Development.json` ou no user-secrets da API.

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
- **Policy full-cycle coverage:** `DocumentPeriodLifecycleTests` (competência after birth: minimum→real via update, `UsePreviousPeriod`, period moves with the date, duplicate-pending invalidation, template-edit applies to the NEXT operations — live read, not freeze), `DocumentPeriodLiveReadTests` (granularity change reuses+resituates the minimum-period pending instead of orphaning it, a document born before the template had a `PeriodPolicy` heals on its next update, delivered units keep their recorded periods), `DocumentUnitDetailsPolicyTests` (expiration→`Validity`, workload→`WorkloadEndDate`/working-day guard, and the absent-policy counterpoints), `SignDocumentPolicyGuardTests` (unsignable template rejected before any external call), `DocumentPolicyFullCycleTests` (all four policies composed: generate→update→OK→expire→renew-at-minimum-period→expire→stop at the renewal cap). Note: `DocumentUnit.Validity`'s setter rejects past validity, so expiration scenarios must anchor dates on "today" — fixed 2024 dates only work for templates without an expiration policy.
