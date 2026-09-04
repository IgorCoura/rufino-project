# Rufino Client — CLAUDE.md

+> **MANDATORY — Tests required on every code change.**
Every feature, refactor, or bug fix **must** include tests (unit for ViewModels/repos/models, widget for screens). Run `flutter test` before considering the task done. Non-negotiable.

## Language Convention

**All code in English** (classes, methods, variables, files, comments, commits). Only exception: **user-facing UI text in Brazilian Portuguese**.

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

## Code Documentation

All public APIs and any non-trivial private member **must** have a doc comment. Use the Dart-standard triple-slash `///` style — never `/* */` or `//`.

Official reference: https://dart.dev/effective-dart/documentation

### Rules

1. **First line is a self-contained one-sentence summary.** It ends with a period and appears alone in its paragraph.
2. **Additional paragraphs** are separated by a blank `///` line. Use them for details, caveats, or usage examples.
3. **Integrate parameters into prose** using `[paramName]` — do not use `@param` tags.
4. **Describe return values** with a "Returns …" sentence when the return is non-obvious.
5. **Cross-link** related types and methods with `[ClassName]` or `[methodName]`.
6. **Test descriptions** (`group()` and `test()`) must be written as plain English sentences that explain the behaviour being verified, not the implementation.


### Element-level conventions

- Classes/entidades: noun phrase (`/// A paginated list of employees from the API.`).
- Métodos com efeito colateral: verbo na 3ª pessoa (`/// Fetches and caches…`).
- Métodos que retornam valor: noun phrase ou "Returns …".
- Booleans/getters: "Whether …".
- `Future`-returning: descrever o que resolve / quando lança.
- Test `group()`: subject under test (`'LoginViewModel'`).
- Test `test()`: subject + condition + expectation (`'emits failure status when the repository returns an error'`).


## Project Overview

Flutter cross-platform app hosting **two distinct products** that share one shell:

| Produto | Escopo | Backend | Estado |
|---|---|---|---|
| **People Management** | funcionários, documentos, departamentos, locais de trabalho | `people-management-service` (.NET) + Keycloak | maduro — `packages/people_management/` desde 2026-09-04 |
| **Bill Payment** | captura de boletos, verificação, aprovação, agendamento e pagamento | `BillPayment` (.NET), fases 1–3 concluídas | ✅ UI completa até o pagamento — `packages/bill_payment/` (fase 3: seção de execução no detalhe, comprovante, reabrir falhado) |

Mais um módulo, que não é produto: **Tenant Management** (`packages/tenant_management/`) — a identidade do cliente da plataforma. É **a porta de entrada do app**: o usuário escolhe um tenant e só então chega ao Home, que mostra as funcionalidades dos produtos daquele cliente. Backend: `TenantManagement` (.NET).

**Os produtos não se conhecem, e isso é imposto por ferramenta.** Ver "Two-product architecture" abaixo antes de acrescentar qualquer coisa.


## Two-product architecture — decisions of record

Decidido em 2026-08-12/13, ao começar a UI de contas a pagar. O objetivo declarado pelo usuário: *"que o código dos dois seja o mais independente possível, para que mudanças em um não quebrem o outro"*.

### D1 — Pub workspace com pacotes, não pastas

`lib/modules/<produto>/` seria mais barato, mas o limite seria **combinado**, e combinado erode. Os produtos vivem em pacotes sob `packages/`, resolvidos por **pub workspace** (piso `sdk: ^3.6.0`): um `flutter pub get`, um `pubspec.lock` na raiz, hot reload normal.

> ⚠️ **Abaixo de 3.6 a chave `workspace:` é ignorada em silêncio** e cada pacote volta a ter lockfile próprio. Não baixe o constraint.

### D2 — O limite é imposto pelo ANALISADOR, não pelo compilador

**Pacote separado sozinho não impede import cruzado.** Num workspace todos dividem o mesmo `package_config`, então `import 'package:rufino_v2/...'` de dentro de `bill_payment` **resolve e compila** — sai apenas como `info` do lint `depend_on_referenced_packages`. Verificado com sonda deliberada, não suposto.

O que cria o limite é a promoção dessa regra a **erro** no `analysis_options.yaml` de cada pacote:

```yaml
analyzer:
  errors:
    depend_on_referenced_packages: error
```

**Consequência que precisa ser sabida:** `flutter analyze` quebra, `flutter build` sozinho **passa**. Se houver CI, a regra tem de rodar lá — é o único lugar onde ela vira garantia.

### D3′ — O critério do que entra em `rufino_core`

> **Complemento de 2026-09-04.** O critério ("se o outro produto mudar isto, este deveria se
> importar?") continua valendo, e ao aplicá-lo duas vezes de verdade ele respondeu **não** onde
> parecia óbvio que sim: o seletor de arquivo tem um contrato em cada produto (um documento com
> content type × vários filtrados por extensão), e uma abstração comum descreveria mal os dois.
> **Dois consumidores não bastam — precisa ser o mesmo contrato.** O que subiu por ter mesmo
> contrato foi `AppModule`/`HomeEntry` e o `go_router` que eles exigem.

### D3 — O critério do que entra em `rufino_core`

> *"Se o outro produto mudar isto, este deveria se importar?"* Se não, **não é core** — é do módulo.

| Entra | Não entra, nunca |
|---|---|
| `Result<T>`, `ExpectedFailure`, `HttpException` | entities, repositories, api services |
| tema e design tokens, `ThemeNotifier` | exceções de domínio de produto |
| `SecureStorage` | viewmodels, telas, rotas |
| porta `ErrorReporter` + no-op + `PiiScrubber` | qualquer coisa que cite um produto pelo nome |
| `SessionAwareHttpClient` | |
| **stack de permissão** (UMA por audiência, notifier, cache, guards) | |
| **`TenantContext` + `SelectedTenant` + `ProductGuard`** | |
| **`checkApiStatus`** (shape `{id,message}` dos BCs novos) | |
| **`CepLookupService`** (ViaCEP puro) | |
| **`SectionCard` / `InfoRow`** | |

**`SentryErrorReporter` fica no app, de propósito.** Ele lê `AppConfig`, que carrega a URL do people-management. A **porta** é do core; o **adapter concreto** é da casca. Mover o adapter arrastaria a configuração de um produto para dentro da fundação do outro.

### ~~D4~~ → **D4′ — Seletor ÚNICO de tenant, para todos os produtos**

> A D4 original dizia que cada produto teria o próprio seletor de contexto. **Foi revertida em 2026-08-14**, quando o `TenantManagement` entrou no app: o contexto passou a ser um só.

**O contexto do app é o tenant, escolhido uma vez.** Login → seleção de cliente → Home com as funcionalidades dos produtos que aquele cliente tem habilitados **e** que a pessoa pode usar.

- O tenant corrente vive em `TenantContext` (`rufino_core`) — é fundação, não módulo, justamente para que `bill_payment` leia o tenant sem depender do pacote que desenha a tela de seleção.
- **A `Company` do PeopleManagement continua sendo o cadastro local daquele produto** (ADR-002 do servidor), mas deixou de ser o que o usuário escolhe: ela é **resolvida a partir do id do tenant selecionado**, que é o mesmo Guid. Quem faz isso é `TenantSessionBridge` (`lib/core/tenant/`), que grava na mesma chave `selected_company` que os **19 ViewModels do PM já liam** — por isso trocar a porta de entrada do app não virou reescrita do produto atrás dela.
- **Depende do backfill do servidor ter preservado o Guid.** Sem isso o seletor abre vazio e o PM fica inacessível. É pré-requisito de implantação, não detalhe.
- A seleção de **empresa** e o **cadastro de empresa** saíram do app (rotas `/company` e `/company/create`, `CompanySelectionScreen`, `createCompany` fim a fim). Cliente novo nasce como **tenant**.

### ~~D7~~ → **D7′ — O cadastro nasce no back-office, não no seletor**

> A D7 original punha a única porta de cadastro em `/tenant/select`, e o back-office ficava **sem FAB** de propósito. **Foi revertida em 2026-08-18**: cadastrar cliente é trabalho de back-office, e ficava numa tela cuja função é escolher contexto.

Novo tenant nasce em **`/tenant`** (a listagem), sob `TenantPermissionGuard(tenant, create)` — que na prática só o `tenant-admin` vê, mantendo o "não faz autosserviço" da visão do BC sem regra extra na UI. **O seletor (`/tenant/select`) não oferece cadastro**: ele mostra os clientes da pessoa e a porta para o back-office, nada além. Continua valendo o que a D7 protegia — **uma porta só**, agora do lado certo: `/tenant/create` volta para `/tenant` e o redirect de quem não pode criar cai na listagem (e de lá, sem `view`, no Home).

### D8 — Edição em bloco, no lugar, como no `EmployeeProfile`

O detalhe do tenant lê em blocos e edita **inline**: o card vira formulário no lugar, com Cancelar/Salvar. Um bloco = um endpoint = um `x-requestid`. Sem diálogo e sem rota de edição — diálogo só para confirmação crítica (suspender, revogar acesso).

### ~~D5~~ — MORTA em 2026-08-15: o BillPayment tem Keycloak e o `x-user-id` não existe mais

A D5 dizia que a UI mandaria `x-user-id` enquanto o BC não tivesse token. **O backend aplicou
Keycloak em 2026-08-15**: os 55 endpoints têm `[ProtectedResource]`, o `tenantId` da rota é
validado contra o claim **`bp_tenants`**, e quem decide é o **`sub` do token** — os endpoints de
decisão perderam o `[FromHeader("x-user-id")]`. A UI manda só o `Authorization`; mandar
`x-user-id` não faz nada. Audience do resource server: **`bill-payment-api`**
(`AppConfig.billPaymentAudience`).

### D6 — A costura entre módulo e casca — ✅ **2026-09-04**

`AppModule` vive em `rufino_core` e cada produto implementa o seu
(`people_management_module.dart`, `bill_payment_module.dart`,
`tenant_management_module.dart`):

```dart
abstract class AppModule {
  String get menuTitle;
  List<RouteBase>         routes();
  List<SingleChildWidget> providers();
  List<HomeEntry>         visibleEntries(BuildContext context);
}
```

**Duas diferenças em relação ao esboço original, e as duas vieram do código:**

- É `visibleEntries(context)`, não `homeEntries()`. As entradas precisam passar por
  **duas porteiras** — produto habilitado no tenant e permissão da pessoa —, e só o módulo
  sabe qual produto e qual notifier são os seus (o `provider` resolve por tipo, e os três
  usam tipos diferentes). Com uma lista crua, essa decisão voltaria para a casca, que é de
  onde ela saiu.
- O que vem de fora chega pelo **construtor** do módulo (cliente HTTP, base url,
  `getAuthHeader`, repórter, e as capacidades de plataforma), e não como parâmetro de
  `providers()`. Quem monta essas coisas é a casca, e cada módulo precisa de um conjunto
  diferente.

**Resultado:** `app.dart` 1.103 → **550 linhas**, `home_screen.dart` 572 → **417**, e a casca
não nomeia mais nenhuma tela, repositório ou recurso de produto. Ligar ou desligar um produto
é uma linha na lista `modules`. A `TenantSessionBridge` continua na casca e recebe o
`CompanyRepository` do módulo do PM, porque costurar tenant e empresa é trabalho de quem
conhece os dois.

### Sequência de migração

| Fase | O que | Estado |
|---|---|---|
| 0 | workspace + pacotes vazios | ✅ `705689be` |
| 1 | extrair `rufino_core` | ✅ `705689be` |
| 5 | `tenant_management`: seletor único + back-office | ✅ 2026-08-14 |
| 4 | `bill_payment` nasce isolado | ✅ 2026-08-18 |
| 2 | extrair `rufino_auth` | ⬜ adiada |
| 3 | mover PM para pacote | ✅ 2026-09-04 — roteiro e decisões em [`doc/plano-migracao-people-management.md`](doc/plano-migracao-people-management.md) |
| 3b | costura `AppModule` (D6) | ✅ 2026-09-04 |

**A ordem 0 → 1 → 5 → 4 é deliberada:** entrega o código novo isolado sem tocar nos 263 arquivos que funcionam. O PM migra quando houver motivo; o estado final é o mesmo.

**A Fase 3 foi executada em 2026-09-04.** `lib/` caiu de 249 para 46 arquivos, `app.dart` de 1.103 para 760 linhas, e o pacote ficou com 197. Nenhum teste se perdeu: 2.087 passando, que são os 2.101 de antes menos os 14 do código morto removido antes de começar. **Duas decisões do plano foram revistas pelo código** — estão registradas na seção 10 do documento, e valem como precedente:

- **Um seletor de arquivo por produto, não um compartilhado.** O plano previa subir o `DocumentPicker` do `bill_payment` para `rufino_core`. Os contratos são diferentes (lá é um documento com content type; aqui são vários filtrados por extensão), e uma abstração comum descreveria mal os dois. O critério D3 continua valendo — a resposta dele aqui foi "não sobe".
- **A `TenantSessionBridge` continua dependendo de `CompanyRepository`.** O plano queria substituí-la por um par de funções exportadas; interface de repositório **é** contrato público do pacote, exatamente como a casca já usa `BillRepository`. A indireção não compraria isolamento, compraria uma camada.

### Pendências que bloqueiam a Fase 4

- [x] `AppConfig.billPaymentUrl` + `bill_payment_url` no `secrets/local_config.json` —
      feito em 2026-08-18 (`http://192.168.15.41:8100`). **Falta a chave no
      `prod_config.json`** (junto com a `tenant_management_url`, pendência antiga): o
      `assertConfigured()` exige as duas, então o build de produção não sobe sem elas.
- [x] **Como o usuário escolhe o `tenantId`** — resolvido em 2026-08-14. `GET /api/v1/me/tenants`
      devolve, a partir do e-mail do próprio token, os tenants da pessoa. O seletor é **único**
      (D4′) e vive em `packages/tenant_management/`; `bill_payment` lê o tenant corrente pelo
      `TenantContext` de `rufino_core` e **não depende** do pacote de tenants.
- [x] `AppConfig.tenantManagementUrl` — existe e é obrigatório em `assertConfigured()`.
      **Falta a chave `tenant_management_url` nos `secrets/*.json`**, senão o app não sobe.


## Tech Stack

**Language**: Dart 3.6+ / Flutter (all platforms) — o piso subiu de 3.5.2 por causa do pub workspace (D1)
**State**: ChangeNotifier + `ListenableBuilder` (MVVM)
**Routing**: `go_router`
**DI**: `provider`
**Auth**: OAuth2 + Keycloak (`oauth2`, `flutter_secure_storage`, `jwt_decoder`)
**HTTP**: `http`
**UI**: `shimmer`, `infinite_scroll_pagination`, `google_fonts`, `mask_text_input_formatter`, `file_picker`, `intl`


## Target Architecture (Flutter Official — MVVM)

The target architecture is the one officially recommended by the Flutter team, based on **MVVM** with a clear three-layer separation: **Data → Domain → UI**. Data flow is unidirectional (UDF).

Official reference: https://docs.flutter.dev/app-architecture
Reference app: https://github.com/flutter/samples/tree/main/compass_app

---

### Core Principles

1. **Separation of concerns**: each layer has a clear and exclusive responsibility.
2. **Unidirectional Data Flow (UDF)**: events flow up (UI → ViewModel → Repository), state flows down (Repository → ViewModel → UI).
3. **Single Source of Truth (SSOT)**: the data layer is the only source of truth.
4. **Constructor-based dependency injection**: dependencies flow via constructors, exposed through `provider`.
5. **Testability**: each layer can be tested in isolation using mocks.
6. **No thrown exceptions — only `Result<T>`**: errors are values, not control flow. See the Error Handling section below.

---

### Error Handling — `Result<T>` Only

**Never use `throw` or `Exception` as a cross-layer communication mechanism.** Every fallible operation must return `Result<T>` so that callers are forced by the type system to handle both outcomes.


#### Layer responsibilities

| Layer | try/catch? | Returns | Action on error |
|-------|-----------|---------|-----------------|
| Service | ✅ Yes — at the boundary | raw value / throws | catch → wrap in typed exception → let repository handle |
| Repository | ✅ Yes — wraps service calls | `Result<T>` | `on DomainException` → `Result.error(e)`; `catch e` → `Result.error(WrapperException(e))` |
| ViewModel | ❌ No | mutates state | `result.fold(onSuccess: …, onError: …)` |
| UI | ❌ No | — | reads ViewModel state; maps typed exception → localized string |


### Folder Structure

```
client/rufino_v2/
├── lib/                     SÓ a casca (46 arquivos)
│   ├── main*.dart, app.dart (760 linhas — compõe os três módulos)
│   ├── core/{config,monitoring,tenant}/       AppConfig, Sentry, TenantSessionBridge
│   ├── core/utils/                            adapters de plugin: scanner, extrator de data
│   ├── data/{services,repositories}/          auth (OAuth, redirect web) + adapters de plugin
│   └── ui/features/{auth,home,debug}/ · ui/core/widgets/
└── packages/
    ├── rufino_core/         fundação compartilhada — ver D3
    ├── tenant_management/   identidade do cliente: seletor único + back-office
    ├── people_management/   gestão de pessoas (197 arquivos)
    └── bill_payment/        contas a pagar
```

Vários arquivos de `lib/` viraram **reexport** do que subiu para `rufino_core` (permissões, exceções de auth/CEP, `SectionCard`): existem para os pontos de uso não trocarem de import. Código novo importa `package:rufino_core/rufino_core.dart` direto.

`Result<T>`, tema, `SecureStorage`, `ErrorReporter` e `SessionAwareHttpClient` **saíram de `lib/core/`** e vivem em `package:rufino_core/rufino_core.dart` — importe pelo barril, nunca por caminho relativo. Detalhes específicos de capacidades em **Code & Capability Index** e **Package Index** abaixo.

---

#### Layer Communication Rules

UI (Views + ViewModels) → Domain (Repository interfaces) → Data (Services + Implementations)

**Mandatory rules:**
- Views depend on ViewModels only — never on repositories or services.
- ViewModels depend on repositories (interfaces) only — never on services directly.
- **Repositories must never be aware of each other.** If two data types need to be combined, that coordination belongs in a use case or the ViewModel.
- Services hold no state — they are stateless wrappers around external APIs.
- The data layer never depends on the UI layer.

Violating any of these creates coupling that breaks testability and makes refactoring risky.


---

### UI Layer (`ui/`)

**View (Screen/Widget)**
- Composition of widgets that describe the interface.
- **1:1 relationship** with the ViewModel.
- Uses `ListenableBuilder` to react to ViewModel changes.
- Contains no business logic — only calls ViewModel methods.

**ViewModel**
- Extends `ChangeNotifier`.
- Converts domain data into UI state.
- Handles user interactions and delegates to repositories.
- Only dependency: repositories (or use cases when they exist).
- Expose collections via `UnmodifiableListView` — never return a mutable `List` directly.
- Always call `notifyListeners()` inside a `finally` block when paired with a loading flag — ensures the UI never gets stuck in a loading state.


### Domain Layer (`domain/`)

**Entities — Rich Domain Models (not anemic)**

Entities are **rich domain objects** that encapsulate all logic that belongs to them. They are **not** simple data holders (anemic models). Every validation, business rule, computed property, or state transition that is intrinsic to the entity must live inside the entity itself.

- Pure Dart classes with no dependency on external packages.
- Represent data formatted for UI consumption.
- Transformed from DTOs by repositories.
- **Contain all validation logic** that pertains to the entity's own data (e.g., `isValid`, field-level validators).
- **Contain all business rules** that depend only on the entity's own state (e.g., `isActive`, `canBePromoted`, `fullName`).
- **Expose computed properties** derived from internal fields instead of forcing callers to compute them.
- **Enforce invariants** in factory constructors or named constructors — an entity should never exist in an invalid state.

**Guidelines:**
- If you find yourself writing a utility function that operates solely on an entity's fields, move that logic into the entity.
- Validation logic that depends only on the entity's own data belongs in the entity (static validators for forms, instance methods for business rules).
- Logic that depends on **external state** (other entities, repositories, services) does **not** belong in the entity — that goes in a Use Case or ViewModel.

**Repository (interface)**
- Abstract contract implemented by the data layer.
- Enables swapping implementations in tests.

### Data Layer (`data/`)

**Service (API Client)**
- Wraps HTTP endpoints, returns `Future` or `Stream`.
- Holds no state.
- Returns DTOs (raw API models).

**Model (DTO)**
- Represents the exact JSON structure from the API.
- Contains `fromJson` / `toJson`.
- Never used directly by the UI.

**Repository (implementation)**
- Implements the domain interface.
- Coordinates services and converts DTOs → domain entities.
- Manages caching, retry, and fallback logic.

### Routing — `go_router`

- Declarative routes with deep link and parameter support.
- Configured in `app.dart`.
- Authentication guards via `redirect`.
- Named routes with constants to avoid magic strings.

---

### Dependency Injection — `provider`

- Dependencies created at the top of the widget tree (`MultiProvider` in `app.dart`).
- ViewModels receive repositories via constructor.
- Repositories receive services via constructor.
- Tests swap implementations with mocks easily.

### Testing Strategy

Official reference: https://docs.flutter.dev/testing/overview
Architecture testing guide: https://docs.flutter.dev/app-architecture/case-study/testing

#### Test Pyramid

- **Unit tests**: fast, no Flutter rendering, no I/O. Run on every save.
- **Widget tests**: render a single widget tree in isolation. Run on every PR.
- **Integration tests**: full app on device/emulator. Run in CI before merge.

#### Mocking Strategy — Mocktail over Mockito

Prefer **`mocktail`** for all new tests. It requires no code generation (`build_runner`) and has a simpler API.

| Concept | When to use |
|---------|-------------|
| **Mock** | Stub behavior with `when()`/`thenReturn()`. Use for external dependencies (repositories, services). |
| **Fake** | A working in-memory implementation of an interface. Preferred over mocks for repositories in widget/integration tests. |
| **Stub** | A mock that only returns a fixed value for a specific call. Subset of Mock usage. |

---

#### Folder Structure

`test/{unit,widget,integration,golden}/` espelham `lib/`. `test/testing/` contém `fakes/`, `mocks/`, `fixtures/json/`, `helpers/pump_app.dart`. Goldens commitados em `test/golden/goldens/`.

---

#### Unit Tests

**ViewModels**

Test state transitions, loading flags, and error handling. Mock the repository.

**Repositories**

Mock the API service. Test DTO → entity conversion and error mapping.

**Data Models (DTOs)**

Test JSON parsing with fixture files.

**API Services**

Mock `http.Client` to avoid real network calls.

#### Widget Tests

Wrap the widget under test with `ChangeNotifierProvider` injecting a `Fake` ViewModel.

**Key widget testing APIs:**

| API | Use |
|-----|-----|
| `find.byType(T)` | Locate widget by type |
| `find.byKey(Key)` | Locate by `ValueKey` |
| `find.text('...')` | Locate by visible text |
| `tester.pump()` | Trigger one frame rebuild |
| `tester.pumpAndSettle()` | Wait for all animations to complete |
| `tester.tap(finder)` | Simulate tap |
| `tester.enterText(finder, text)` | Simulate text input |


#### Golden Tests (Visual Regression)

Use golden tests for critical UI components to catch unintended visual regressions.

- Generate/update goldens: `flutter test --update-goldens`
- Commit golden image files to the repository
- Run on a fixed device frame/resolution for deterministic results
- Use `golden_toolkit` for multi-device and multi-theme golden tests

#### Integration Tests

Use the `integration_test` package for critical end-to-end flows. Consider **Patrol** when flows involve native components (permissions dialogs, camera, file picker).

#### Coverage

```bash
# Generate coverage report
flutter test --coverage

# Generate HTML report (requires lcov)
genhtml coverage/lcov.info -o coverage/html
```

**Targets:**

| Layer | Minimum coverage |
|-------|-----------------|
| ViewModels | 90% |
| Repositories | 85% |
| Use Cases | 90% |
| API Services | 80% |
| Widgets | 70% |

**Exclude from coverage** (add to `coverage/lcov.info` filtering):
- `*.g.dart` — generated code
- `*_module.dart` — DI/routing boilerplate
- `main*.dart` — entry points

## Error Monitoring

The app captures unexpected runtime errors on user devices and ships them to
a vendor-agnostic `ErrorReporter` (currently backed by Sentry).

### Two non-negotiable principles

1. **Only `lib/core/monitoring/sentry_error_reporter.dart` may import
   `package:sentry_flutter`.** Every other layer depends on the
   `ErrorReporter` interface so swapping vendors (Crashlytics, Datadog,
   custom endpoint) is a one-line change in `main.dart`.
2. **Repositories report at the catch boundary; ViewModels do not.** Every
   `catch (e, st)` in `lib/data/repositories/*_impl.dart` calls
   `reporter.failure(e, st)` which both captures and returns
   `Result.error(e, st)` in one shot. ViewModels just read state from
   `result.fold`.

### Decision tree — find your scenario

| You are adding… | Go to |
|---|---|
| A new exception | [§A](#a--new-exception) |
| A new repository | [§B](#b--new-repository) |
| A new method in an existing repository | [§C](#c--new-method-in-an-existing-repository) |
| A new ViewModel | [§D](#d--new-viewmodel) |
| A new PII field (DTO or context map) | [§E](#e--new-pii-field) |
| A new service or HTTP client | [§F](#f--new-service--http-client) |
| Errors outside repos (background, isolate, scheduler) | [§G](#g--errors-outside-repos) |

---

### §A — New exception

Sealed exception families live in `lib/core/errors/`. When adding a variant,
decide between **bug/network** (report — no mixin) and **user-actionable**
(silent — `with ExpectedFailure`).

| Apply `ExpectedFailure` when… | Do **not** apply it when… |
|---|---|
| Validation error (missing field, invalid CPF format) | Network failure / timeout |
| Duplicate entity (CPF/CNPJ already exists) | Server 5xx |
| Invalid credentials / wrong password | JSON parse error |
| Permission denied (403) returned to a user-driven action | Unexpected null / cast error |
| Resource not found because the user typed a wrong ID | Bug in our code |

```dart
// User-actionable — silent in Sentry, the UI shows a friendly message.
final class EmployeeAlreadyExistsException extends EmployeeException
    with ExpectedFailure {
  const EmployeeAlreadyExistsException();
}

// Bug/network — reported to Sentry with stack trace.
final class EmployeeNetworkException extends EmployeeException {
  const EmployeeNetworkException(this.cause);
  final Object cause;
}
```

`SentryErrorReporter.capture` short-circuits via `isExpectedFailure(error)` — true when the error itself is an `ExpectedFailure` **or** when its `cause` is one (repositories wrap unknown errors in `*NetworkException`, so a wrapped `SessionExpiredException`/`AccessDeniedException` must not reach Sentry disguised as a network bug).

---

### §B — New repository

Copy this template. The constructor and the catch shape are not optional.

```dart
import '../../core/errors/foo_exception.dart';
import '../../core/monitoring/error_reporter.dart';
import '../../core/result.dart';
import '../../domain/entities/foo.dart';
import '../../domain/repositories/foo_repository.dart';
import '../services/foo_api_service.dart';

class FooRepositoryImpl implements FooRepository {
  FooRepositoryImpl({required this.apiService, required this.reporter});
  final FooApiService apiService;
  final ErrorReporter reporter;

  @override
  Future<Result<Foo>> getFoo(String companyId) async {
    try {
      final dto = await apiService.getFoo(companyId);
      return Result.success(dto.toEntity());
    } on FooException catch (e, st) {
      return reporter.failure(e, st);
    } catch (e, st) {
      return reporter.failure(FooNetworkException(e), st);
    }
  }
}
```

Then:

1. In `lib/app.dart` `_buildProviders`, instantiate the repo passing
   `reporter: errorReporter` and add it to the providers list.
2. Add tests per [§ Testing checklist](#testing-checklist).

---

### §C — New method in an existing repository

Every `try { ... } catch` ends in `reporter.failure(e, st)`. Period.

❌ **Wrong** — drops the stack trace and never reports:
```dart
} catch (e) {
  return Result.error(e);
}
```

❌ **Wrong** — verbose, redundant (`reporter.failure` does both):
```dart
} catch (e, st) {
  reporter.capture(e, st);
  return Result.error(e, st);
}
```

✅ **Right** — one line, captures and returns:
```dart
} on FooException catch (e, st) {
  return reporter.failure(e, st);
} catch (e, st) {
  return reporter.failure(FooNetworkException(e), st);
}
```

**Adding context** — pass `context: {...}` when the operation has IDs that
help debug, especially for batch/long-running flows:

```dart
} catch (e, st) {
  return reporter.failure(
    BatchDocumentNetworkException(e),
    st,
    context: {'op': 'batchUpdateDate', 'companyId': companyId, 'count': items.length},
  );
}
```

**Never** put raw PII in `context` — use IDs (`employeeId`, not name/CPF).
The PII scrubber covers known keys, but the cleanest path is to never
introduce sensitive values into the context map in the first place.

---

### §D — New ViewModel

ViewModels **do not** call `reporter.capture`. The repository already
captured the error; calling it again creates duplicate Sentry events.

✅ **Right** — read state from `result.fold` and translate to UI:
```dart
result.fold(
  onSuccess: (value) {
    _data = value;
    _status = Status.loaded;
  },
  onError: (error, _) {
    _status = Status.error;
    _errorMessage = extractServerMessages(error).firstOrNull
        ?? 'Falha ao carregar.';
  },
);
notifyListeners();
```

**Rare exception** — errors that do not come from a repository (local file
parse, `compute` isolate, native SDK callback). Then ViewModel may report:

```dart
try {
  final parsed = await compute(_parseLocalFile, bytes);
  // ...
} catch (e, st) {
  _errorReporter.capture(e, st, context: {'op': 'parseLocalFile'});
  _errorMessage = 'Não foi possível ler o arquivo.';
}
```

The `ErrorReporter` is provided in the widget tree — read it via
`context.read<ErrorReporter>()` or inject through the ViewModel constructor
(prefer constructor injection for testability).

---

### §E — New PII field

Brazilian HR data is in scope. Whenever you add a field that may carry
sensitive content to a DTO body or a `reporter.failure(..., context: {...})`
call, do this **before** merging:

1. Add the key (**lowercase**, no diacritics) to `_sensitiveKeys` in
   `packages/rufino_core/lib/src/monitoring/pii_scrubber.dart`. A chave é
   procurada com `key.toLowerCase()`: entrada com maiúscula no set é entrada
   morta, nunca casa.
2. Add a case to
   `packages/rufino_core/test/monitoring/pii_scrubber_test.dart` covering
   the new key both at top level and nested.
3. If the key is now legitimately scrubbed in repos that pass it via
   `context`, no further work — `reporter.failure` already runs the scrub.

Categories already covered (consult `pii_scrubber.dart` for the full list
before adding a duplicate):

| Category | Examples |
|---|---|
| BR identifiers | `cpf`, `rg`, `cnpj`, `pispasep`, `voterid`, `militarydocument` |
| Personal info | `nome`, `fullname`, `birthdate`, `nomemae`, `nomepai` |
| Contact | `email`, `telefone`, `celular` |
| Address | `cep`, `endereco`, `street`, `complemento`, `bairro` |
| Payroll | `salario`, `salaryamount`, `wage`, `remuneracao` |
| Auth secrets | `password`, `token`, `accesstoken`, `authorization`, `apikey` |

---

### §F — New service / HTTP client

Services **receive** an `http.Client` via constructor — they do not create
one. `lib/app.dart` builds a single client wrapped by
`errorReporter.wrapHttpClient(http.Client())`, which gives every request
automatic HTTP breadcrumbs in Sentry.

✅ **Right**:
```dart
class FooApiService {
  FooApiService({required this.client, required this.baseUrl, required this.getAuthHeader});
  final http.Client client;
  final String baseUrl;
  final Future<String> Function() getAuthHeader;
  // ...
}
```

❌ **Wrong** — bypasses the wrapped client, so requests skip breadcrumbs:
```dart
class FooApiService {
  FooApiService();
  final http.Client client = http.Client();   // never do this
}
```

If you need a specialized client (custom timeout, global header), construct
it in `app.dart` and wrap it the same way:
`errorReporter.wrapHttpClient(MyCustomClient())`.

---

### §G — Errors outside repos

Background tasks, `compute` isolates, schedulers, and native SDK callbacks
need the `ErrorReporter` injected via constructor — same pattern as repos:

```dart
class DocumentScannerService {
  DocumentScannerService({required this.reporter});
  final ErrorReporter reporter;

  Future<List<ScannedDocument>> scan() async {
    try {
      // ...
    } catch (e, st) {
      reporter.capture(e, st, context: {'op': 'scanDocument'});
      rethrow;
    }
  }
}
```

For `compute`: the `ErrorReporter` is **not** serializable, so it cannot
cross the isolate boundary. The isolate function should let exceptions
propagate; catch and report on the main isolate when `await compute(...)`
completes.

---

### Testing checklist

| Scenario | Required tests |
|---|---|
| New repository | (1) `reports unexpected exception` — `reporter.capturedErrors` `hasLength(1)`. (2) `does not report ExpectedFailure` — `isEmpty`. |
| New exception with `ExpectedFailure` | One repo test confirming `capturedErrors` is empty when the API throws it. |
| New ViewModel | Inject `FakeErrorReporter`; assert `capturedErrors.isEmpty` — ViewModel must not double-report. |
| Widget test using a Provider tree | Add `Provider<ErrorReporter>.value(value: FakeErrorReporter())` to the test's `MultiProvider`. |
| New PII key in `pii_scrubber.dart` | Add a top-level + nested case to `pii_scrubber_test.dart`. |

The canonical fake is `test/testing/fakes/fake_error_reporter.dart`. It
mirrors the production short-circuit on `ExpectedFailure`, so assertions
about `capturedErrors` reflect exactly what would reach Sentry.

---

### Configuration

Compile-time keys (in `secrets/local_config.json` / `prod_config.json`):

| Key | Effect |
|---|---|
| `error_monitoring_enabled` | When `false` (default), `NoopErrorReporter` is used and no events leave the device. |
| `error_monitoring_dsn` | Vendor DSN. Required when enabled. |
| `error_monitoring_environment` | Defaults to the `environment` value (`develop` / `production`). |
| `error_monitoring_traces_sample_rate` | `"0.0"` to `"1.0"`, sent as a string. Defaults to `0.0`. |

### CI guardrail

The coupling rule must be enforced before every merge. Only one file is
allowed to import the vendor SDK:

```bash
# Linux/macOS
grep -r "package:sentry" lib/ --include="*.dart"
```

```powershell
# Windows / PowerShell
Select-String -Path lib\*.dart -Pattern "package:sentry" -Recurse
```

The single allowed hit is `lib/core/monitoring/sentry_error_reporter.dart`.
Any other match means a layer above the abstraction reached for the SDK
directly — block the merge and move the call into the reporter.

## Permission-Based UI Protection (Keycloak Authorization Services)

The app enforces **client-side permission checks** that mirror the backend's `[ProtectedResource("resource", "scope")]` model. Permissions are fetched from Keycloak Authorization Services (UMA) and cached in `PermissionNotifier`. **Every new feature that introduces UI elements tied to a protected backend endpoint must apply permission guards.**

### How It Works

1. After login, `SplashViewModel` loads permissions of **both audiences, em paralelo**.
2. Um POST por audiência ao token endpoint do Keycloak (`grant_type=urn:ietf:params:oauth:grant-type:uma-ticket`, `audience=<resource server>`, `response_mode=permissions`) devolve os pares recurso/escopo concedidos.
3. `PermissionNotifier` (em `rufino_core`) guarda o resultado **de uma audiência** e expõe `hasPermission` / `hasAnyScope`. A segunda audiência usa a **subclasse** `TenantPermissionNotifier`, porque `provider` resolve por tipo: sem tipos distintos, o último registrado responderia pelas duas.
4. Guards renderizam condicionalmente — o elemento não autorizado **some** (`SizedBox.shrink`), nunca fica desabilitado.

> ⚠️ **403 do Keycloak numa audiência significa "nenhuma permissão", não erro.** Quem não é operador da plataforma recebe 403 no `tenant-management-api`, e `PermissionApiService` traduz isso em **lista vazia**. Tratar como falha poria todo usuário de RH diante de uma tela de erro por causa do outro produto. Coberto por teste.

### Key Files

| File | Purpose |
|------|---------|
| `packages/rufino_core/lib/src/auth/permission.dart` | `Permission` + `PermissionModel` |
| `packages/rufino_core/lib/src/auth/permission_api_service.dart` | UMA RPT por audiência (403 → lista vazia) |
| `packages/rufino_core/lib/src/auth/permission_cache_service.dart` | Cache — **chave por audiência**, senão uma sobrescreve a outra |
| `packages/rufino_core/lib/src/auth/permission_repository.dart` | Contrato + implementação |
| `packages/rufino_core/lib/src/auth/permission_notifier.dart` | `PermissionNotifier` (uma audiência por instância) |
| `packages/rufino_core/lib/src/auth/permission_guard.dart` | `PermissionGuard<T>` / `ModuleGuard<T>` — o tipo escolhe a audiência; omitir resolve para o PM |
| `packages/tenant_management/lib/src/tenant_permissions.dart` | `TenantPermissionNotifier` + `TenantPermissionGuard` / `TenantModuleGuard` |
| `packages/rufino_core/lib/src/tenant/product_guard.dart` | `ProductGuard` — a **outra** porteira: o produto está habilitado neste tenant? |
| `test/testing/fakes/fake_permission_repository.dart` | Fake para testes |

Os arquivos antigos em `lib/` (`domain/entities/permission.dart`, `ui/core/widgets/permission_guard.dart`, …) permanecem como **reexport**.

### Canonical Resource & Scope Names

All resource names are **lowercase, kebab-case**. Use **exactly** these strings in `PermissionGuard` / `ModuleGuard`.

**Audiência `tenant-management-api`** — use `TenantPermissionGuard` / `TenantModuleGuard` (ou `PermissionGuard<TenantPermissionNotifier>`), nunca os guards sem tipo:

| Resource | Scopes | Papéis do realm |
|----------|--------|-----------------|
| `tenant` | `view`, `create`, `edit`, `suspend` | `view`: tenant-support e tenant-admin · resto: tenant-admin |
| `tenant-access` | `view`, `edit` | idem |
| `tenant-product` | `view`, `edit` | idem |

**Audiência `people-management-api`** — guards sem parâmetro de tipo:

| Resource | Scopes |
|----------|--------|
| `company` | `create`, `edit`, `view` |
| `debug` | `view` |
| `department` | `create`, `edit`, `view` |
| `document` | `create`, `edit`, `view`, `upload`, `webhook`, `download`, `send2sign`, `generate` |
| `document-group` | `create`, `edit`, `view` |
| `document-template` | `create`, `edit`, `view`, `upload`, `download` |
| `employee` | `create`, `edit`, `view`, `upload`, `download` |
| `position` | `create`, `edit`, `view` |
| `require-documents` | `create`, `edit`, `view` |
| `role` | `create`, `edit`, `view` |
| `workplace` | `create`, `edit`, `view` |

> When a new resource or scope is added in Keycloak, add it to this table so the app and backend stay in sync.

### Rules for New Features

1. **Identify the resource and scopes.** Check the backend controller for `[ProtectedResource("resource", "scope")]` on the endpoints your feature calls. Use the canonical table above for the correct string.

2. **Module-level visibility** — if the feature introduces a new navigation entry (menu card, nav item, route link), wrap it with `ModuleGuard`:
   ```dart
   ModuleGuard(
     resource: 'employee',
     child: _MenuCard(label: 'Funcionários', ...),
   )
   ```

3. **Action-level visibility** — wrap action buttons (FAB, edit, delete) with `PermissionGuard`:
   ```dart
   PermissionGuard(
     resource: 'employee',
     scope: 'create',
     child: FloatingActionButton(...),
   )
   ```

4. **Resource names must match the canonical table exactly.** All names are lowercase kebab-case. Never use PascalCase or camelCase (`'Document'` is wrong, `'document'` is correct).

5. **Widget tests must provide `PermissionNotifier`.** Any widget test for a screen that uses `PermissionGuard` or `ModuleGuard` must wrap the test widget tree with `ChangeNotifierProvider<PermissionNotifier>.value(...)`. Use `FakePermissionRepository` to grant the necessary permissions in `setUp()`:
   ```dart
   final fakePermRepo = FakePermissionRepository()
     ..setPermissions([
       const Permission(resource: 'employee', scopes: ['create', 'view', 'edit']),
     ]);
   permissionNotifier = PermissionNotifier(permissionRepository: fakePermRepo);
   await permissionNotifier.loadPermissions();
   ```

6. **Logout must clear permissions.** Already handled in `HomeViewModel.logout()` — no action needed unless a new logout flow is added.

7. **Never hardcode role-to-permission mappings.** All authorization decisions come from Keycloak. The app only checks what Keycloak returns — if a resource/scope is added or removed in the Keycloak dashboard, the app reflects it automatically.

---

## Session Expiry & 401/403 Handling

The app distinguishes "session died" (401) from "no permission" (403) end to end. Do not reintroduce generic handling.

- **`checkHttpStatus`** (`data/services/http_status_helper.dart`) maps **401 → `SessionExpiredException`** and **403 → `AccessDeniedException`** (both in `core/errors/auth_exception.dart`, both `ExpectedFailure`). Everything else stays `HttpException`.
- **App-wide 401 detection**: the shared `http.Client` is wrapped by `SessionAwareHttpClient` (`core/network/session_aware_http_client.dart`), which flips `AuthSessionNotifier` (`ui/features/auth/viewmodel/auth_session_notifier.dart`) on any 401. The token callbacks in `app.dart` (`flagSessionLoss`) do the same when `getCredentials()` raises `SessionExpiredException`/`NoCredentialsException`.
- **`SessionExpiredListener`** (`ui/core/widgets/session_expired_listener.dart`, mounted in the `MaterialApp.router` `builder`) shows a blocking dialog ("Sessão expirada" → "Fazer login"), then calls `AuthRepository.clearLocalSession()` + `PermissionNotifier.clear()` and navigates to `/login`. On public routes (`/`, `/login`) it resets the flag silently. The `GoRouter` `redirect` also sends any protected navigation to `/login` while the flag is set.
- **403 never logs the user out.** Screens surface it as a message: ViewModels must pass the error through `extractServerMessages` (`core/utils/error_messages.dart`), which yields `accessDeniedMessage` / `sessionExpiredMessage` for auth failures (direct or wrapped in a `*NetworkException` `cause`). Never discard the `onError` error object.
- **Proactive refresh**: both auth services treat a token inside `tokenExpiryMargin` (60s) of expiry as needing refresh; a transient network failure during refresh keeps a still-valid token instead of killing the session, and `oauth2.AuthorizationException` (rejected refresh token) becomes `SessionExpiredException`.
- Backend counterpart: the API answers **401** when Keycloak rejects the token in the UMA check (`AuthorizationResultHandler` in `PeopleManagement.API/Authorization`), **403** only for real permission denials, and **503** when Keycloak is unreachable.

---

## Document Dashboard (module)

`/document-dashboard` (home card under `ModuleGuard('document')`) is the RH triage view over `api/v1/{company}/document-dashboard` (`GET /summary` + `GET /units`). Five buckets — Vencidos, A Vencer, Pendentes, Aguardando Assinatura, Requer Validação — where **counts and list share the same server-side predicate** (`DashboardBucket.apiValue` is the query param). Rules worth preserving:

- **"A vencer" is validity-based**, not Warning-status-based: the horizon (30/60/90 days, `expiringInDays`) filters `validity` server-side; unit status 8 (Warning) is only a visual chip. **Unit status ids run 1–8** (`8 = 'A Vencer'`) — `DocumentUnit`, `BatchDocumentUnitItem` and `DashboardUnitItem` all map them.
- **State preservation:** `DocumentDashboardPage` owns the ViewModel + `ScrollController` lifecycles (same pattern as `EmployeeListPage`) — never create them inside the route builder, or filters/bucket/page/scroll are wiped on every push/pop.
- **Row navigation** uses `context.push('/employee/:id?tab=documents')`; the `/employee/:id` route maps `?tab=` (`documents` | `contracts`) to `EmployeeProfileScreen.initialTab`, so the profile lands on the Documentos tab and pop returns to the intact dashboard.
- ViewModel invariant: bucket switch and pagination reload **only the list** (`isLoadingUnits`); filter/horizon changes reload **summary + list together** so the KPI cards never disagree with the rows. Default employee filter is Ativos (status 2).

## Tenant Management (pacote `packages/tenant_management/`)

A identidade do cliente da plataforma: **a porta de entrada do app** e o back-office que a mantém. Consome o BC `TenantManagement` (`server/Services/TenantManagement/`).

| Rota | Tela | Guard |
|---|---|---|
| `/tenant/select` | Seleção de cliente — porta de entrada. **Não cadastra** (D7′) | nenhum (só autenticado) |
| `/tenant` | Back-office: busca, filtros, cursor. **FAB "Cadastrar cliente"** — única porta do cadastro (D7′) | `tenant/view` |
| `/tenant/create` | Cadastro PF/PJ + titular + produtos. Volta para `/tenant` | `tenant/create` |
| `/tenant/:id` | Detalhe: abas Cadastro (edição inline), Acessos, Produtos | `tenant/view` |

Coisas que não podem erodir:

- **O splash decide com três informações**: credencial, permissões das duas audiências e `GET /me/tenants`. Um tenant selecionável → entra direto; vários → seleção; nenhum → seleção com mensagem honesta e "Sair". O tenant guardado é **reentrado pelo servidor** (produtos e papel podem ter mudado).
- **Tenant suspenso aparece e não entra.** Sumir seria mentir sobre um cadastro que existe.
- **O guard de rota só decide com as permissões carregadas.** Na web o `go_router` abre direto na URL, sem passar pelo splash; recusar naquele instante expulsaria o operador a cada F5. Coberto por teste.
- **O cadastro responde só com o id.** O estado do convite vive no detalhe, porque `POST /tenants` devolve 200 mesmo com o provedor de identidade falhando — a falha é engolida no servidor de propósito. A UI **nunca** reporta como concedido um acesso que não chegou: banner + "Reenviar acessos" (idempotente).
- **O último responsável não mostra "Revogar"** (`TNM.TNT20`), e a tela ainda trata a recusa do servidor, porque pode estar olhando estado velho.
- **Recusa de regra ≠ falha.** `TenantRepositoryImpl` classifica 4xx com mensagem de domínio como `TenantRuleException` (`ExpectedFailure`, não vai para o Sentry) e o resto como `TenantNetworkException`.
- **Suspenso desabilita, não esconde.** Esconder é para falta de permissão; desabilitar com o motivo à vista é para estado do cadastro.
- **A PÁGINA é dona do ViewModel, nunca o builder da rota.** `tenant_pages.dart` existe só para isso. O `go_router` reexecuta o builder a cada mudança de pilha; criando o ViewModel lá dentro, cada `push`/`pop` produz uma instância nova em estado `loading` — e como o `State` da tela sobrevive ao rebuild, o `initState` que dispara o carregamento **não roda de novo**. O resultado é a tela anterior girando para sempre ao voltar. Mesma disciplina do `DocumentDashboardPage`.
- **Voltar é `pop` OU `go`, nunca só `pop`.** Estas telas chegam pelos dois caminhos: empilhadas pelo seletor e por substituição pelo menu do Home. `TenantBackButton` volta se houver pilha e vai para a rota de origem quando não houver — sem ele, quem entra pelo menu fica sem saída. O detalhe leva o botão **também nos estados de carregando e de erro**, senão uma rede lenta tranca a tela.

## People Management (pacote `packages/people_management/`)

Gestão de pessoas: funcionários, documentos, cargos e locais de trabalho. Consome o BC
`PeopleManagement` (`server/Services/PeopleManagement/`) — rotas `api/v1/{company}/...`, onde
`{company}` é **o mesmo Guid do tenant** (o backfill do servidor preservou o id), resolvido pela
`TenantSessionBridge`. Audiência de permissão: `people-management-api`.

Coisas que não podem erodir:

- **Este produto é o dono do `PermissionNotifier` base, e por isso NÃO tem subclasse.** O
  `provider` resolve por tipo: `bill_payment` e `tenant_management` precisaram de
  `BillPaymentPermissionNotifier`/`TenantPermissionNotifier` porque disputavam a mesma entrada na
  árvore. Aqui, `PermissionGuard`/`ModuleGuard` **sem parâmetro de tipo** já resolvem para a
  audiência certa — criar uma subclasse não resolveria colisão nenhuma e obrigaria a trocar os 55
  guards. Use `PeopleManagementResources` e `PeopleManagementScopes` para os nomes; string crua
  errada não quebra teste nem build, só esconde o botão.
- **Quatro capacidades chegam da casca por porta, e nenhuma delas é plugin declarado aqui**:
  `FilePickerService` (escolher arquivo e salvar em caminho), `FileSaveService` (salvar xlsx e
  bytes), `DocumentScannerService` (câmera, OCR e abrir as configurações do sistema) e
  `DocumentDateExtractor` (a data impressa no documento). As duas primeiras chegam por
  `peopleManagementRoutes(...)`; as outras pela árvore de providers.
- **`camera` é a única exceção**, declarada no `pubspec` do pacote com a justificativa: a captura
  com preview ao vivo (`DocumentScanDialog`, usada por duas telas) É a interface do produto, e
  levá-la para a casca moveria uma tela de 283 linhas para fora do pacote.
- **O barril exporta domínio, repositórios, api services, portas, rotas e as constantes.** NÃO
  exporta tela, ViewModel nem `*_api_model` — mapper de DTO não é API pública. A exceção
  documentada é `DocumentRangeItem`, que aparece na assinatura de `EmployeeRepository`.
- **Dentro do pacote os imports são relativos**, nunca o próprio barril: o self-import funciona,
  mas faz o analyzer marcar todo import direto como redundante e some com a noção de quem depende
  de quem.
- **A `Page` é dona do ViewModel; o builder da rota só a constrói** (`people_management_pages.dart`,
  2026-09-04). Nenhuma das 25 rotas cria ViewModel — conferível por
  `grep -c "ViewModel(" people_management_routes.dart`, que tem de dar **0**. O builder é
  reexecutado a cada mudança de pilha: criar o ViewModel ali fazia nascer uma instância por
  navegação, cada uma carregando de novo, e o `ChangeNotifier` anterior nunca era descartado —
  medido em **3 consultas onde cabe 1**. Mesma disciplina de `bill_payment_pages.dart`.
- **As duas telas de lote resolvem a empresa no `initState`, não no `build`.** Elas precisam do id
  da empresa, que vem de leitura assíncrona; enquanto o `Future` nascia dentro do builder, cada
  volta disparava uma leitura nova e a tela piscava para o indicador antes de reconstruir tudo. O
  `Future` é criado uma vez e guardado.
- **O teste que protege isso é de NAVEGAÇÃO** (`test/ui/route_viewmodel_lifecycle_test.dart`), com
  `peopleManagementRoutes` de verdade: entra, empilha, volta, e conta as consultas ao repositório.
  Um teste de widget que monta a tela uma vez **passa nos dois desenhos** — foi por isso que o bug
  sobreviveu tanto tempo. Ao acrescentar rota, acrescente o caso aqui.

## Bill Payment (pacote `packages/bill_payment/`)

Contas a pagar: captura, verificação, aprovação e expectativas. Consome o BC `BillPayment`
(`server/Services/BillPayment/`) — rotas `api/v1/{tenantId}/...`, com o tenant lido do
`TenantContext` via callback `getTenantId` injetado pela casca. **Terceira audiência de
permissão**: `BillPaymentPermissionNotifier` (audience `bill-payment-api`, cacheKey
`cached_permissions_bill_payment`), recarregada no splash, no `TenantSessionBridge`, no
refresh de token e limpa no logout — junto com as outras duas.

| Rota | Tela | Guard (recurso/escopo) |
|---|---|---|
| `/bill-payment/pending` | Painel diário: fila de aprovação + 3 listas de pendências + nudge de onboarding | `expectation`/`view` |
| `/bill-payment/bills` | Fila de boletos, filtro `?status=` **no servidor**, abre em Aguardando aprovação; filtros Agendados/Pagos/Falhou e a linha "pagar em" quando há data efetiva | `bill`/`view` |
| `/bill-payment/bills/import` | Importação manual: linha digitável, código Pix e/ou **anexo do boleto** (PDF/imagem) — um dos três basta | `bill`/`import` |
| `/bill-payment/bills/:id` | Aprovação: banner de risco (Seguro/Atenção/Perigo), 13 verificações, consulta oficial por inteiro, resumo com competência/descrição da IA + revalidar/negar/cancelar/aprovar (Perigo exige a caixa "assumo o risco"); pós-aprovação, a seção **Execução do pagamento** (fase 3) com status/retenção/datas da ordem, cancelar agendamento, confirmar pagamento imediato e reabrir boleto falhado | `bill`/`view` (cancelar ordem: `bill`/`cancel`; confirmar/reabrir: `bill`/`approve`) |
| `/bill-payment/bills/:id/artifact` | O documento original do boleto, em tela cheia | `bill`/`view` |
| `/bill-payment/bills/:id/receipt` | O comprovante de pagamento vindo do provedor, em tela cheia (só existe após Pago) | `bill`/`view` |
| `/bill-payment/bills/:id/email` | O e-mail que trouxe o boleto — título, remetente e corpo renderizado | `bill`/`view` |
| `/bill-payment/capture-items` (+`/:id`, `/:id/artifact`, `/:id/email`) | Quarentena: filtro server-side, claim/reprocess, documento original e o e-mail que trouxe o item | `capture-item`/`view` |
| `/bill-payment/captured-messages` | Livro-caixa: todo e-mail lido, com busca, filtros, rolagem infinita, controle de retenção e recaptura | `captured-message`/`view`·`recapture` |
| `/bill-payment/capture-sources` (+`/connect`, `/:id`) | Caixas monitoradas: stepper Entra ID, pastas, **piso temporal**, sync/rescan | `capture-source`/`view`·`manage` |
| `/bill-payment/payees` (+`/create`, `/:id`) | Beneficiários: política de valor (leitura completa + **edição no detalhe**), apelidos, bancos aceitos | `payee`/`view`·`manage` |
| `/bill-payment/payer-profile` | Perfil do pagador (1:1) — 404 = modo onboarding | `payer-profile`/`view` |
| `/bill-payment/trusted-origins` | Origens confiáveis: resolve tester + cadastro em sheet + ações na linha | `origin`/`view` |
| `/bill-payment/expectations` (+`/create`, `/:id`, `/:id/edit`) | Expectativas: cadastro, **edição** (tudo menos o beneficiário), **exclusão**, watch, ciclos, waive por ciclo | `expectation`/`view` (escrever exige `manage`) |

Coisas que não podem erodir:

- **A importação manual aceita TRÊS entradas, e uma delas basta**: linha digitável, código Pix ou o
  arquivo do boleto. O anexo sobe por `multipart/form-data` na mesma rota do JSON — quem escolhe o
  handler no servidor é o `Content-Type` —, e mandar o arquivo dentro do JSON seria base64,
  inflando um PDF de 20 MB em um terço à toa. O servidor lê os instrumentos do arquivo e o guarda
  como evidência; **arquivo ilegível sem dígitos é recusado** com a mensagem falando do arquivo,
  não pedindo a linha digitável — quem anexou um papel precisa saber que ele não foi lido.
- **`PickedDocument`/`DocumentPicker`/`LinkOpener` moram em `ui/shared/document_picker.dart`.**
  Nasceram dentro de `capture_items/capture_item_detail_screen.dart` e saíram de lá quando a
  importação de boleto também passou a anexar documento: dois consumidores é o momento de o tipo
  deixar a tela que o usou primeiro — quem importa boleto não deveria depender do arquivo da
  quarentena para nomear um callback. O barril (`bill_payment.dart`) exporta do lugar novo, então a
  API pública do módulo **não mudou** e a casca segue passando `onPickDocument` uma vez só para
  todas as telas que anexam.
- **O repositório recebe o arquivo em PRIMITIVOS** (`documentBytes`/`documentFileName`/
  `documentContentType`), não o `PickedDocument`. O record do seletor é tipo de UI, e `domain/` não
  depende de `ui/` — é a mesma fronteira que `CaptureItemRepository.attachArtifact` já respeitava.
  E o nome do arquivo **não entra no `context` do reporter**: ele costuma carregar beneficiário e
  conta, e o contexto viaja para o monitoramento de erros.
- **`checkApiStatus`, nunca `checkHttpStatus`** — o `DomainExceptionFilter` do BC emite
  `{id, message}`. Telas reagem por `domainErrorId` (`BLP.BIL02`, `BLP.CPI04`…), nunca por texto.
- **A API nunca devolve linha digitável nem payload Pix** — a tela de aprovação não os mostra
  nem sugere que existam. Contexto de report carrega só IDs.
- **O documento original é rota, não diálogo**, e as duas telas de decisão o oferecem:
  `ArtifactViewerScreen` recebe um *loader* (`Future<Result<CapturedArtifact>> Function()`) em vez
  de um repositório, porque a mesma tela serve item de quarentena e boleto — ensiná-la sobre os
  dois a tornaria o único ponto do módulo que conhece o módulo inteiro. Boleto em diálogo é
  ilegível no celular, e a rota dá o `canPop ? pop : go` de graça. O botão **some** quando
  `hasArtifact` é falso (importação manual nasce só com os dígitos); desabilitar prometeria um
  documento que nunca vai chegar. Os bytes ficam em memória e **nunca vão para disco** — o
  artefato é a prova do que o sistema viu quando decidiu.
- **A tela de e-mails capturados existe porque a quarentena não responde por quem sumiu.**
  O que a triagem descarta não deixa item — é decisão medida, não descuido —, e sem esse
  histórico a pessoa que mandou um e-mail fica sem resposta. `captured-message` e
  `capture-retention` são **recursos próprios** no Keycloak, não escopos pendurados em
  `capture-item`: fila de trabalho e histórico são coisas diferentes.
- **A recaptura segue o contrato de 2026-08-28 do servidor, e o diálogo diz a regra nova.**
  `RecaptureOutcome` carrega `artifactsReingested`, `billsCancelled` e
  `previouslyDeniedBillIds` (os nomes antigos `itemsRemoved`/`artifactsIngested` não existem
  mais no servidor — parseá-los mostraria "0 anexo(s)" para sempre). A snackbar de sucesso
  informa os boletos pendentes cancelados e **avisa** quando boletos já negados renascem para
  decisão; o diálogo de confirmação diz que boleto aguardando aprovação é **cancelado e
  recriado**, e que aprovado/agendado/pago **bloqueia** (o 409 `BLP.CMS11` chega com a mensagem
  do domínio pelo caminho genérico).
- **`ConnectOutcome` é só o id — o aviso de caixa compartilhada morreu no servidor.** O campo
  `alreadyMonitoredByAnotherAccount` saiu do contrato em 2026-08-28 (um tenant nunca fica
  sabendo do que outro configurou); a snackbar "Esta caixa já é monitorada por outra conta" foi
  removida junto. Não reintroduza o aviso: ele era o vazamento, não a funcionalidade.
- **429 tem mensagem própria no `checkApiStatus`** (`rufino_core`): os endpoints caros do BC
  têm rate limiting por pessoa e respondem 429 sem corpo de domínio; sem o fallback "Muitas
  tentativas em sequência…" o usuário veria só o texto genérico de cada tela.
- **O controle de retenção vive no topo dessa tela**, e não numa tela de configuração: quem lê a
  lista é quem decide por quanto tempo ela existe. Sem `capture-retention:manage` o prazo
  continua à vista, só não editável — esconder é para falta de permissão, mostrar sem editar é
  para quem precisa do número para interpretar a lista.
- **`hasArtifact` é booleano porque a chave de armazenamento saiu do contrato.** O download manda
  o id do recurso e o servidor resolve a chave; 404 cobre "não há arquivo" e "você não pode ver
  este item" com a mesma resposta, então a tela diz a única coisa verdadeira nos dois casos.
- **O e-mail renderiza com imagens remotas BLOQUEADAS por padrão** (`EmailViewerScreen`,
  `flutter_widget_from_html_core`): pixel de rastreamento confirma leitura ao remetente, e o
  visualizador não pode reintroduzir o vazamento que o desembrulho de links fechou no servidor.
  Carregar imagens é escolha explícita do usuário, por botão na barra. O botão "Ver e-mail" só
  existe para boleto vindo de caixa (`sourceKind == Mailbox`) — importação manual não tem e-mail.
- **`readingStatus` é o que impede a tela de mentir sobre a leitura por IA — e ele estava chegando
  e não sendo desenhado.** O campo existia no `Bill` da lista e era parseado desde sempre, mas
  `ReadingStatuses.label` e `isReadingQueued` não eram chamados em **nenhum** widget, e o
  `BillDetailDto` do servidor nem o carregava. O resultado é o defeito relatado em 2026-08-28: um
  boleto **na fila** da IA e um boleto cujo documento **não tem o que ler** ficavam idênticos na
  tela — sem "Referente a", sem "Descrição", e com o check 13 dizendo "Não se aplica / Sem leitura
  por IA" nos dois casos. Quem aprovava lia a pendência como veredito. Agora o detalhe recebe o
  campo, o Resumo traz o aviso (`_ReadingNotice`) e a lista traz o selo.
- **`ReadingStatuses.speaks` decide quem fala, e `done`/`notApplicable` calam de propósito.** Uma
  leitura concluída fala pelos campos que preencheu; "não há o que ler" é uma ausência sobre a qual
  ninguém pode agir. Só `queued` e `unavailable` viram texto na tela — e estado desconhecido vindo
  de um servidor mais novo cai no mesmo silêncio, em vez de imprimir o valor cru.
- **O check 13 lê "Aguardando" enquanto a leitura está na fila, e o servidor continua mandando
  `Skipped`.** A tradução é da UI, não do domínio: o servidor está certo em pular o check (não há o
  que comparar ainda), mas "Não se aplica" descreve um veredito. `_CheckTile` cruza
  `reasonCode == 'reading_not_available'` com `readingStatus == queued` — é o único ponto do módulo
  em que um check é reinterpretado, e existe porque pendente e inaplicável são fatos diferentes que
  não podem dividir o mesmo rótulo.
- **As linhas "Referente a"/"Descrição" do Resumo só existem quando o boleto TEM leitura por
  IA.** Ausência de linha não é defeito: é retrato que ainda não foi tirado — boleto do acervo
  antigo nasceu antes da leitura. O backfill é servidor-somente (`POST /bills/{id}/enrich`, um
  por chamada); a UI **não** expõe botão para dispará-lo — havia um ("Ler com IA") e foi
  removido a pedido do usuário em 2026-08-27.
- **O banner de risco decide a leitura da tela (ADR-015 do BC), e desde 2026-08-31 são QUATRO
  níveis + um fallback honesto**: verde Seguro, âmbar Atenção, vermelho Perigo, e **Extremo
  Perigo** (fundo cheio na cor de erro — beneficiário/origem na lista de bloqueio), sempre ACIMA
  das verificações. `RiskLevels` (`bill_payment_enums.dart`) é a única tradução — fim das
  comparações com string crua. **Nível desconhecido NUNCA desenha "Seguro"** (era o default do
  switch — um servidor mais novo mentiria verde): cai num banner neutro pedindo atualização, e
  `RiskLevels.tier` devolve 0. Aprovar Perigo OU Extremo exige a caixa "assumo o risco"
  (`BLP.BIL27`), e a **alçada por risco** (`BLP.BIL32`, 403) é espelhada na UI:
  `BillPaymentPermissionNotifier.canApproveAtRisk` lê os escopos novos
  (`approve-attention` < `approve-danger` < `approve-extreme`, hierárquicos) e o botão Aprovar
  desabilita com o motivo no Tooltip quando o boleto está acima da alçada. A lista de boletos
  ganha o chip vermelho para Perigo/Extremo. `riskLevel` nulo (boleto ainda não validado) não
  mostra banner nenhum — ausência honesta.
- **A chave Asaas é DO TENANT e entra pela seção "Conta Asaas" do Perfil do Pagador
  (2026-08-31).** O campo é `obscureText` e a chave é gravação única: vai em
  `PUT /payer-profile/asaas-account` (body `{apiKey}`), o servidor a prova no provedor e a guarda
  cifrada — ela **nunca volta** pela API, então o campo é limpo após vincular e não há "editar".
  `DELETE /payer-profile/asaas-account` remove (com confirmação — o diálogo diz que a consulta
  oficial fica indisponível). Sem chave, o badge fica "Não configurada" em tom de atenção com o
  aviso em vermelho: boleto novo nasce Perigo por `LookupAvailability` até a chave ser colada.
  A recusa do provedor chega como `BLP.PRF12`/`PRF13` com a mensagem do domínio pelo caminho
  genérico. O campo antigo `accountRef` morreu no contrato — não o reintroduza.
- **A marca de confiança do beneficiário (blacklist/whitelist) é lida em vermelho/verde e mudada
  no detalhe.** `Payee.standing` (`PayeeStandings`: Normal/Whitelisted/Blacklisted, wire names do
  Smart Enum do servidor); a lista troca o ícone para `Symbols.block` em `colorScheme.error` e
  põe o chip `Blacklist` (`BadgeTone.problem`) / `Whitelist` (positive); o detalhe ganhou a seção
  "Marca de confiança" no molde da "Situação" (badge + ações, sem modo edição), com **confirmação
  obrigatória antes de marcar blacklist** — o diálogo diz o efeito: todo boleto vira Perigo e
  aprovar exige assumir o risco. `PUT /payees/{id}/standing`. Whitelist é só selo, por decisão de
  produto — não abranda verificação nenhuma. Reason code novo `payee_blacklisted` traduzido
  começando por "BLOQUEADO" (regra dos motivos de bloqueio).
- **A varredura de `check_translations_test.dart` cobre o catálogo INTEIRO do servidor (55) e os
  13 tipos.** Estava com 48 códigos e 12 tipos — os 6 motivos do check 13 e o próprio
  `documentConsistency` nunca entraram, então um código novo podia ficar sem tradução com a suíte
  verde. Ao criar reason code no servidor, acrescente-o à lista `_serverReasonCodes` no mesmo
  commit.
- **A política de valor é lida por inteiro, e a redação dela vive na UI — não no domínio.**
  `AmountPolicy` (domínio) guarda os cinco fatos; `ui/shared/amount_policy_view.dart` é o único
  lugar que decide como eles viram texto, e serve tanto o chip da lista quanto o card do detalhe.
  O getter `summary` **saiu** do domínio: ele formatava dinheiro com `toStringAsFixed(2)`
  (`R$ 1500.00`, enquanto o resto do app usa `formatMoney` e escreve `R$ 1.500,00`) e, para
  importar o formatador compartilhado, o domínio teria de depender da UI. Antes disso ele era a
  ÚNICA renderização da política no app inteiro, e **descartava a tolerância do valor fixo e os
  dois extremos da faixa** — a tela dizia "Faixa de valores" sem número nenhum.
- **O valor fixo mostra a janela que a tolerância produz, e ela espelha `AmountPolicy.Matches`
  do servidor.** "±5%" sozinho deixa a pessoa fazendo conta para saber se o boleto do mês passa;
  `amountPolicyWindow` calcula `expected ± |expected| × tolerância/100`, que é literalmente o que
  o domínio compara. Se aquela fórmula mudar, esta muda junto — mostrar uma janela que o servidor
  recusaria é pior que não mostrar janela. Tolerância `0` é válida e sai por extenso ("o valor tem
  que bater exato"), porque "±0%" não diz o que significa.
- **`isConclusive` é exibido**, e não era. Política sem limite enfraquece a verificação de valor
  do boleto em silêncio; quem escolhe "sem limite" vê isso no cadastro em vez de descobrir quando
  um boleto errado passar.
- **`NumberField` (`ui/shared/`) é a única implementação do campo decimal.** Nasceu privado no
  formulário de cadastro e saiu de lá quando o editor do detalhe passou a coletar os mesmos
  números — dois validadores da mesma coisa divergem, e a divergência apareceria como "o cadastro
  aceitou e o detalhe recusou". **A tolerância é obrigatória no tipo Fixo** nos dois: o domínio a
  exige (`AmountPolicy.From` → `BLP.PYE07`) e o formulário a tratava como opcional, então cadastrar
  valor fixo com ela em branco voltava do servidor com "valor obrigatório" sem dizer qual campo.
- **A regra das 12 horas é do cliente também**: `BillDetail.isSnapshotStaleAt` espelha
  `Approval:MaxSnapshotAgeHours`; retrato velho desabilita Aprovar com o motivo à vista e
  oferece Revalidar. Motivo de negar/cancelar é obrigatório no form.
- **`CheckReasons` é contrato de tradução**: `check_translations.dart` traduz o **código**;
  código desconhecido cai para a `evidence` do servidor (coberto por teste que varre os 46).
- **A quarentena renderiza o que veio** — os campos financeiros chegam `null` fora de
  `Promoted`/`Unrouted` porque o servidor decide a visibilidade, nunca a tela.
- **O painel não colapsa as três listas** (`missing` / `captureFailed` / `dueSoon`): cada uma
  tem uma ação diferente. `captureFailed` navega para o item da quarentena.
- **O piso temporal (`captureSince`) nasce preenchido com 90 dias, e o campo vazio é escolha
  legítima.** A dor que ele resolve — a primeira varredura arrastando anos de caixa — atinge
  justamente quem não sabe que o campo existe, então o padrão do formulário resolve por omissão
  (`defaultCaptureSince`); quem quer o acervo inteiro limpa o campo. **O domínio continua
  aceitando nulo**: quem escolhe o padrão é a tela, não o servidor.
- **Alterar a data faz o servidor reler a caixa desde o piso novo, e a tela diz isso.** O provedor
  grava o filtro dentro do cursor, então trocar a data obriga a descartar os cursores — não é
  detalhe de implementação que dê para esconder, porque a releitura consome cota do extrator de
  visão como o `rescan`. O `helperText` do campo no detalhe é onde isso aparece.
- **O `showDatePicker` tem `lastDate: hoje`** — piso no futuro descreve uma fonte que não captura
  nada, e o servidor recusa com `BLP.CPS20`. Impedir na tela evita levar o usuário a um erro que
  já se sabe que vai acontecer.
- **A seção "Execução do pagamento" fala pela ordem, e ordem ausente é estado normal (fase 3).**
  A aprovação cria a `PaymentOrder` pelo outbox do servidor, então há uma janela observável em que
  o boleto está `Approved` e `GET /payments/by-bill/{id}` responde 404 — o
  `PaymentApiService.getByBill` traduz 404 em `null` e a tela mostra "Agendamento em
  processamento…" em vez de erro. Falha ao ler o pagamento **nunca** derruba o detalhe
  (`_loadPayment` engole o erro); o boleto está na tela de qualquer jeito. O
  `PaymentRepository` é provider **opcional** na página (`_maybeRead`), para os testes de widget
  antigos e uma casca sem o provider continuarem funcionando.
- **O aceite do boleto vencido viaja na aprovação, e a UI o coleta com caixa explícita
  (ADR-017 do BC).** `BillDetail.isOverdueAt` compara por dia; quando vencido, o sheet de
  aprovação exige marcar "sei que o pagamento sai imediatamente" e manda
  `acknowledgeImmediateExecution: true` (`BLP.BIL35` sem ele). Ordem retida em
  `AwaitingConfirmation` mostra o botão "Confirmar pagamento imediato" na seção de execução.
- **A prévia da data efetiva é INFORMATIVA e nunca bloqueia a aprovação.** O sheet de
  aprovar consulta `GET /bills/{id}/schedule-preview?date=` (`SchedulePreview`) ao abrir e a
  cada troca de data, e mostra "Pagamento será executado em \<data\>" com "(deslizou do dia
  pedido)" quando a política empurrou — a conta é do servidor (ADR-017), o cliente não a
  reimplementa. Falha/latência da prévia não desenha nada e o Autorizar segue funcionando;
  resposta obsoleta (data mudou de novo) é descartada. Prévia com `immediate: true` revela a
  caixa de aceite do vencido mesmo que o relógio local discorde.
- **Recusa `BLP.BIL35` do servidor revela a caixa NO LUGAR, sem fechar o sheet.** É o cinto
  do descompasso de relógio (UTC × local na virada do dia): a aprovação roda dentro do sheet
  (`_ApproveSheet`, widget com estado próprio — o `TextEditingController` precisa sobreviver
  à animação de saída), e quando o servidor recusa com esse código o aviso em vermelho + a
  caixa aparecem com o formulário intacto; qualquer outra recusa fecha o sheet e a mensagem
  do domínio segue pelo caminho de sempre (`errorMessage`). O ViewModel expõe
  `lastErrorCode` para o sheet reagir por código, nunca por texto de mensagem.
- **Cancelar agendamento respeita a janela de reação** (`PaymentOrderStatuses.canCancel`:
  Draft/Pending/BankProcessing) e **reabrir é só para `Failed`**
  (`BillStatuses.acceptsReopen`) — reabrir não é atalho para desfazer aprovação. Os dois pedem
  confirmação por diálogo. Status de ordem desconhecido ecoa o nome de arame, nunca é pintado
  como desfecho conhecido.
- **O comprovante é rota (`/bills/:id/receipt`), não diálogo — mesma doutrina do artefato.**
  `BillReceiptPage` reusa `ArtifactViewerScreen` com um loader que resolve a ordem e busca o
  comprovante; sem comprovante ainda, a mensagem é de regra ("ainda não tem comprovante"), não de
  rede. Os bytes ficam em memória; a URL do provedor **nunca** chega ao cliente — o servidor a
  consumiu e guardou o arquivo no storage.
- **Mesmas disciplinas do tenant_management**: Pages donas do ViewModel (`bill_payment_pages.dart`),
  rotas literais antes de `:id` (coberto por `bill_payment_routes_test.dart`), voltar =
  `canPop ? pop : go`, guard de rota libera enquanto permissões não carregaram (F5 na web),
  erro de `loadMore` mantém as linhas na tela.

## UI Design Guidelines (Material Design 3)

Official references:
- Material Design 3 spec: https://m3.material.io
- Flutter M3 components: https://docs.flutter.dev/ui/widgets/material
- Flutter typography: https://docs.flutter.dev/ui/design/text/typography
- Flutter accessibility: https://docs.flutter.dev/ui/accessibility-and-internationalization/accessibility

---

### Material 3 Setup

Enable Material 3 globally in `app.dart`. As of Flutter 3.16, M3 is the default, but always declare it explicitly.

### Cores, Tipografia, Spacing

- **Cores:** sempre `Theme.of(context).colorScheme.<role>`. Nunca hardcode. Usar mesma seed em light/dark via `ColorScheme.fromSeed`.
- **Tipografia:** sempre `Theme.of(context).textTheme.<style>`. Nunca hardcode `fontSize`. Família **Inter** via `GoogleFonts.interTextTheme()`.
- **Spacing:** sempre `AppSpacing.*` (4dp grid em `core/theme/app_spacing.dart`). Nunca valores arbitrários.

### Responsive & Adaptive Layout

Flutter targets smartphones, tablets, desktop, and web from a single codebase. Every screen must adapt to all of these. This section defines the mandatory patterns.

Official references:
- https://docs.flutter.dev/ui/adaptive-responsive
- https://docs.flutter.dev/ui/adaptive-responsive/large-screens
- https://docs.flutter.dev/ui/adaptive-responsive/safearea-mediaquery

---

#### Breakpoints

Definidos em `core/theme/app_breakpoints.dart` (mobile 600 / tablet 840 / desktop 1200). Para decisões de layout veja a tabela `LayoutBuilder` vs `MediaQuery` mais adiante.

#### SafeArea — Always Use It

Wrap Scaffold body content in `SafeArea` to avoid notches, camera cutouts, status bars, and OS navigation bars. Material `Scaffold` does **not** do this automatically for body content.

**Rules:**
- `SafeArea` modifies `MediaQuery.padding` for its children, so nested `SafeArea` widgets do **not** double-apply padding.
- Never add manual top/bottom `EdgeInsets` to compensate for system chrome — use `SafeArea` instead.

#### Content Width Limit

On large screens, full-width content becomes hard to read. **Always cap content width** for list screens and form screens:

#### Adaptive Navigation

Compact (<600dp): `NavigationBar` bottom + `AppSpacing.md`. Medium (600–840dp): `NavigationRail` collapsed + `AppSpacing.lg`. Expanded (≥840dp): `NavigationRail` extended + `AppSpacing.xl`.

Listas adaptativas: `GridView` em telas largas. Forms adaptativos: coluna única no compact, centralizado e capado em medium+.

#### Lists with a FloatingActionButton — Bottom Clearance

**Bug:** On screens with a `FloatingActionButton`, the last item in a `ListView` can be hidden behind the FAB and unreachable by tapping.

**Fix:** Add bottom padding equal to the FAB height + margin + extra room:

| FAB type | Extra bottom padding |
|----------|---------------------|
| Standard `FloatingActionButton` | `AppSpacing.md + 80` |
| `FloatingActionButton.extended` | `AppSpacing.md + 72` |

This rule applies to every `ListView`, `GridView`, or `CustomScrollView` inside a `Scaffold` that has a `FloatingActionButton`.

#### Outras regras

- Nunca trave orientação — permitir todas as orientações em todas as plataformas (foldables, iPads).
- Em desktop/web: M3 já suporta tab navigation; para widgets custom, usar `FocusableActionDetector`.

#### LayoutBuilder vs MediaQuery — Decision Table

| Scenario | Use |
|----------|-----|
| Switching top-level navigation (bottom bar ↔ rail) | `MediaQuery.sizeOf(context)` |
| Switching layout inside a scrollable list | `LayoutBuilder` |
| Form column layout (single ↔ two column) | `LayoutBuilder` |
| Reading accessibility settings (text scale, high contrast) | `MediaQuery.of(context)` (full object needed) |
| Capping content width | `ConstrainedBox(constraints: BoxConstraints(maxWidth: N))` |

---

### Component Guidelines

Usar variantes M3 padrão (`FilledButton`, `Card`, `AppBar`/`SliverAppBar`, `showDialog`, `showModalBottomSheet`, `FilterChip`/`ChoiceChip`/`InputChip`/`ActionChip`). Regras específicas do Rufino:

- **Botões:** no máximo **um** `FilledButton` por tela. Hierarquia: `FilledButton` > `FilledButton.tonal` > `OutlinedButton` > `TextButton`.
- **Cards:** padding interno via `AppSpacing`. Corner radius 12dp (default M3).
- **Text fields:** outlined por padrão; filled apenas em contextos de muito ruído visual (ex.: busca em container colorido).
- **Snackbar/Dialog:** `showDialog` apenas para confirmações críticas; preferir `showModalBottomSheet` para opções não-críticas.
- **Animations:** usar o pacote `animations` (`FadeThroughTransition`, `SharedAxisTransition`) para transições de página. Animação só com propósito (orientar, confirmar, reduzir carga cognitiva).

Em dúvidas sobre a spec genérica do M3, ver https://m3.material.io.

### Icons

Usar **Material Symbols** (`material_symbols_icons`) com variante **rounded**. Sempre parear ícone com label de texto a menos que o ação seja universal (`×`, `🔍`); para ícones isolados, usar `Semantics`.

### Accessibility

Every screen must comply with these rules — no exceptions.

#### Touch Targets
All interactive elements must be **at least 48×48dp**. Use `InkWell` or `GestureDetector` with a minimum size, or rely on M3 components which meet this by default.

#### Semantics
Label all interactive elements that do not have visible text:

#### Text Scaling
Never block text scaling. Use `MediaQuery.withClampedTextScaling` only to prevent extreme scaling while still respecting user preferences:

#### Color Contrast
- Normal text: minimum **4.5:1** (WCAG AA)
- Large text (18pt+ or 14pt+ bold): minimum **3:1**
- `ColorScheme.fromSeed` generates compliant pairings automatically. Do not override "on" colors.

#### Screen Readers
Test with TalkBack (Android) and VoiceOver (iOS). Every interactive element must have a meaningful semantic label.

### Widget Code Patterns

- Sempre usar `const` em construtores quando possível.
- Decompor widgets complexos em classes `StatelessWidget` — não em helper methods que retornam `Widget` (Flutter não consegue pular rebuild de subtrees de helpers).
- Preferir `SizedBox` a `Container` para spacing.
- Usar `LayoutBuilder` ou `MediaQuery.sizeOf(context)`, nunca `MediaQuery.of(context).size`.

### Theme

Toda configuração em `core/theme/`: `app_theme.dart` (entry point ThemeData light/dark), `app_colors.dart`, `app_spacing.dart`, `app_breakpoints.dart`, `app_text_theme.dart`, `theme_notifier.dart`.

---

## Code & Capability Index

> **Always check this index before writing a new utility, service, or exception.** If a capability is already covered, reuse it; do not introduce a parallel implementation or pull in a competing package. If a true gap exists, extend the existing module.

### Capability lookup (use these — do not reimplement)

| I need to… | Use | Notes |
|------------|-----|-------|
| Save a file (web download / native save dialog) | `data/services/file_save_service.dart` | Cross-platform; web triggers download, desktop/Android opens save-as, iOS/Linux saves to Downloads. Wraps `file_saver`. |
| Open a "Save As" dialog and write bytes | `core/utils/file_saver.dart` (+ `_stub`) | Lower-level wrapper using `file_picker`'s `saveFile`. Prefer `file_save_service.dart` unless you specifically need the dialog flow. |
| Build a `.xlsx` spreadsheet | `data/services/spreadsheet_service.dart` | Wraps `syncfusion_flutter_xlsio`. All cells written as text — preserves CPF/leading-zero formatting. |
| Merge multiple PDFs into one | `core/utils/pdf_merger.dart` | Conditional import → `_io` / `_web`. Wraps `pdf_combiner`. |
| Convert images → multi-page PDF | `core/utils/image_to_pdf_converter.dart` | Runs decode + build in `compute` isolate. Wraps `image` + `pdf`. |
| Extract text from a PDF | `core/utils/pdf_text_extractor.dart` | Page-bounded extraction. Wraps `syncfusion_flutter_pdf`. |
| Build a ZIP archive in memory | `core/utils/zip_builder.dart` | Fast compression. Wraps `archive`. |
| Scan a document (camera) + OCR | `core/utils/document_scanner_service.dart` | Platform-abstracted (`_mobile` / `_web` / `_stub`). Wraps `cunning_document_scanner`, `camera`, `google_mlkit_text_recognition`. |
| Build the combined-PDF filename for batch download | `core/utils/combine_file_namer.dart` | Mirrors backend `BatchDownloadQueries.DownloadBatchDocumentUnits` naming. |
| Fuzzy-match Brazilian names | `core/utils/fuzzy_name_matcher.dart` | Jaro-Winkler + token overlap, accent-insensitive, handles PT connectors. |
| Run many async tasks with a concurrency cap | `core/utils/concurrency.dart` | `mapWithConcurrency` — bounded worker pool, preserves input order, `Future.wait` error semantics. Used by batch fan-out (per-template queries, per-page OCR, per-file text extraction). |
| Generate a request/correlation ID | `data/services/request_id_helper.dart` | UUID v4 for `x-requestid` on mutations. Wraps `uuid`. |
| Send a multipart upload with progress | `data/services/multipart_upload_helper.dart` | Streams bytes and reports `0.0–1.0` via callback. |
| Validate an HTTP response & raise typed errors | `data/services/http_status_helper.dart` | Throws `HttpException` on non-2xx, extracts server messages, logs via `DomainErrorLogger`. |
| Read a server error message for the UI | `core/utils/error_messages.dart` | Extracts message from `HttpException` or wrappers exposing `cause`. |
| Log a domain error to disk (debug only) | `core/utils/domain_error_logger.dart` | Conditional dart:io split via `_writer` / `_writer_stub`. |
| Read/write encrypted secrets (tokens, etc.) | `core/storage/secure_storage.dart` | Wraps `flutter_secure_storage`. |
| Read/write public prefs (permission cache, etc.) | `data/services/permission_cache_service.dart` | Wraps `shared_preferences`. Do not use `shared_preferences` directly elsewhere — extend this or add a sibling cache service. |
| Authenticate via Keycloak / refresh tokens | `data/services/auth_api_service.dart` | Wraps `oauth2` + `jwt_decoder`. |
| Fetch user permissions (UMA / RPT) | `data/services/permission_api_service.dart` | Single source for Keycloak Authorization Services calls. |
| Look up a Brazilian CEP | `data/services/cep_api_service.dart` | ViaCEP wrapper. |
| Read app config / OAuth endpoints | `core/config/app_config.dart` | `--dart-define-from-file`-driven. |
| Trust self-signed certs in dev | `core/config/dev_http_overrides.dart` (+ `_stub`) | Local dev only. Never call from prod path. |
| Return a fallible result from data/domain | `core/result.dart` (`Result<T>` + `Success`/`Failure`) | Mandatory — see "Error Handling" rule. Never `throw` across layers. |
| Validar resposta de um BC **novo** (`{id, message}`) | `checkApiStatus` (`rufino_core`) | TenantManagement e BillPayment. **Não** use `checkHttpStatus` neles: ele só entende `{errors:{…}}` do PM e engoliria a mensagem. |
| Saber qual tenant está selecionado | `TenantContext` (`rufino_core`) | Fonte única do contexto do app (D4′). |
| Esconder algo que depende do produto contratado | `ProductGuard` (`rufino_core`) | Sempre **junto** com um `PermissionGuard`: produto habilitado ≠ pessoa autorizada. |
| Consultar CEP a partir de um pacote | `CepLookupService` (`rufino_core`) | `lib/data/services/cep_api_service.dart` é o adaptador do PM sobre ele. |
| Emoldurar um bloco "ler, então editar no lugar" | `SectionCard` + `InfoRow` (`rufino_core`) | Padrão do `EmployeeProfile` e do detalhe do tenant (D8). |

### Domain exception hierarchies (`core/errors/`)

One sealed family per aggregate. **Add a new variant to the existing family before creating a new exception class.**

`auth_exception.dart` (InvalidCredentials, SessionExpired, NoCredentials, NetworkAuthException) · `department_exception.dart` · `workplace_exception.dart` · `employee_exception.dart` · `document_template_exception.dart` · `document_group_exception.dart` · `require_document_exception.dart` · `permission_exception.dart` · `batch_document_exception.dart` · `batch_download_exception.dart` · `document_dashboard_exception.dart` · `cep_exception.dart`

Plus `data/services/http_exception.dart` — raised by `http_status_helper.dart`, carries `statusCode` + `serverMessages`.

### Theme tokens (`core/theme/`)

`app_colors.dart` (seed color) · `app_spacing.dart` (xs/sm/md/lg/xl/xxl/xxxl on 4dp grid) · `app_breakpoints.dart` (mobile 600 / tablet 840 / desktop 1200) · `app_theme.dart` (M3 light/dark factory using Inter via `google_fonts`) · `theme_notifier.dart` (runtime mode toggle).

**Never hardcode colors, spacing, or breakpoints — always reference these.**

### API services (`data/services/`)

One service per backend aggregate. Cross-cutting helpers (`http_exception`, `http_status_helper`, `multipart_upload_helper`, `request_id_helper`, `permission_cache_service`, `file_save_service`, `spreadsheet_service`) MUST be reused — do not inline equivalent logic in feature services.

`auth_api_service` · `permission_api_service` · `permission_cache_service` · `company_api_service` · `department_api_service` (departments + positions + roles + payment-unit/salary-type lookups) · `workplace_api_service` · `employee_api_service` (the largest — covers profile, image, contact, address, personal info, ID card, voter ID, PIS/PASEP, military doc, medical exam, dependents, contracts, documents, signing, document-unit CRUD + range ops) · `document_template_api_service` · `document_group_api_service` · `require_document_api_service` · `batch_document_api_service` · `batch_download_api_service` · `document_dashboard_api_service` · `cep_api_service`.

### Repositories

Every aggregate above has both an interface (`domain/repositories/<aggregate>_repository.dart`) and an implementation (`data/repositories/<aggregate>_repository_impl.dart`). **ViewModels depend on the interface, never the impl or service.**

### Models (DTOs) and Entities

DTOs live in `data/models/<aggregate>_api_model.dart` (+ JSON ser/deser). Domain entities live in `domain/entities/<aggregate>.dart`. Conversion is owned by the repository impl. Do not reuse a DTO as an entity or vice-versa, and do not duplicate fields between siblings — compose with nested DTOs/entities when an aggregate references another (see `employee_profile`, `document_group_with_*`).

**DocumentTemplate rules (policies).** A template's rules live in `TemplatePolicies` (`expiration`, `workload`, `period`) inside `domain/entities/document_template.dart`. **A rule is active when it is present** — `null` is how "does not apply" is expressed, and `validityInDays` / `workload` / `usePreviousPeriod` on the entity are getters derived from the rule set, never stored twice.

- **`period` = competência.** `PeriodRule` carries a `PeriodGranularity` (daily/weekly/monthly/yearly, ids matching the backend `PeriodType`: 1–4) and `usePreviousPeriod`. The 4 granularities are hardcoded in the `PeriodGranularity` enum with PT labels — the ids are the contract, the labels are presentation, so no network round-trip for four stable values. The form's Regras section has a third switch (`_PeriodRuleTile`) revealing a granularity dropdown + a retroactive switch.

- **`expiration` can be limited.** `ExpirationRule` carries an optional `maxRenewals` (`int?`): null = renews forever, a value = renews N times then stops (the API's `ExpirationPolicy` vs `ExpirationLimitedPolicy`). The expiration `_RuleTile` reveals a "Limitar renovações" switch (`_ExpirationRenewalControl`, key `rule-switch-maxRenewals`) that in turn reveals the count field; the view model's `_expirationLimited` gates it, and turning the expiration rule off clears both. `fromJson`/`toJson` carry `maxRenewals` inside the `expiration` block (null when forever).
- **Zero is not a rule.** The API rejects a rule carrying a zeroed value (`PMD.DOCT11`), so an active switch requires a value ≥ 1. Legacy templates still echo `0` back in the legacy fields; the DTO maps that to "no rule".
- **Writes send both shapes.** `DocumentTemplateRepositoryImpl._buildModel` makes `policies` the source of truth and mirrors it into `documentValidityDurationInDays` / `workloadInHours`. Sending both keeps the app correct on either side of a deploy — they cannot disagree because both come from the same rule set.
- **Reads prefer the block.** `toEntity` uses the `policies` block when the API sends it and falls back to deriving from the legacy fields when it does not.
- **The form's legacy fields are mirrors.** "Validade (dias)" and "Carga horária (h)" in the basic info section are read-only (`_RuleMirrorField`); editing happens in the Regras section through the rule switches.
- **Signature is a rule too.** The "Assinatura" switch is the fourth tile in Regras (`_SignatureRuleTile`): turning it on reveals the placement editor inline (the `_SignatureCard` list + "Adicionar Assinatura"); there is no separate "Configurações" section or standalone placements card. Turning the switch off clears the placements in the view model, so acceptance and placements can never disagree at save time (the API rejects placements without acceptance, `PMD.DOCT10`).
  - **Read and write shapes differ.** On **read**, `DocumentTemplateApiModel.fromJson` sources signature from `policies.signature`: block present = accepts, and it carries `placeSignatures` (falls back to the top-level `acceptsSignature` only when the API omits the whole `policies` block). On **write**, `toJson`/`toCreateJson` still send `acceptsSignature` + `placeSignatures` as **top-level** fields (the API's write contract is unchanged) — so signature is **not** a `TemplatePolicies` member on the entity; the model keeps its own `acceptsSignature`/`placeSignatures` fields. Reading placements from the old `templateFileInfo.placeSignatures` location was the bug where signatures created after the policy refactor vanished on GET.
  - **The placement's type is mandatory** (`PlaceSignatureData.validateType`, wired into the type dropdown). A placement without a type is serialized as `type: 0`, which the API rejects hard — `TypeSignature.FromValue(0)` throws and fails the *whole* save (not just that placement). So the dropdown must be validated like the numeric fields; an unvalidated type is how "add the first placement to an empty list" silently failed to save.

**Aggregates currently modeled** (each has DTO + entity unless noted): company / company_detail (entity-only) · workplace · department · position · role · remuneration (entity-only) · employee · employee_profile · employee_personal_info · employee_contact · employee_address (entity = `address`) · employee_id_card · employee_vote_id · employee_military_document · employee_medical_exam · employee_dependent · employee_contract · employee_social_integration_program · employee_document · document_template · document_group · document_group_with_templates · document_group_with_documents · document_range_item (DTO-only) · require_document · batch_document_unit · batch_download · document_dashboard · period · permission · selection_option (entity-only) · personal_info_options (entity-only) · signing_option (entity-only) · scanned_document (entity-only) · bulk_upload_match (entity-only) · cep_lookup (DTO-only).

**Entidades do `tenant_management`** (no pacote, não em `lib/`): `Tenant` (+ `TenantAddress`, `TenantContact`, `TenantMembership`, `TenantProductInfo`) · `MyTenant` · `TenantSummary` / `TenantPage` / `TenantListFilter` · `TaxId` (validação de CPF/CNPJ com DV) · `RegisterTenantInput`. Os valores de arame dos Smart Enums do servidor vivem em `TenantKinds` / `TenantStatuses` / `MembershipRoles` / `ProvisioningStatuses`; os códigos de produto ficam em `TenantProducts` (`rufino_core`), porque a casca e os dois produtos os leem.

---

## Package Index

> **Before adding a dependency, check whether one of the packages below already covers the use case.** If it does, use it. Do not introduce a competing package (e.g. don't add `dio` — `http` is the standard; don't add `riverpod` — `provider` + `ChangeNotifier` is the standard; don't add `intl` for masking — `mask_text_input_formatter` is already in use).

### State / DI / Routing
| Package | Use for |
|---------|---------|
| `provider` | Dependency injection + ChangeNotifier consumption. |
| `nested` | Used internally by provider's `MultiProvider`. Do not consume directly. |
| `go_router` | All routing (declarative routes, deep links, redirect guards). |

### Auth & Storage
| Package | Use for |
|---------|---------|
| `oauth2` | OAuth2 token flows against Keycloak. Wrapped by `auth_api_service.dart`. |
| `flutter_secure_storage` | Encrypted persistence for tokens. Wrapped by `core/storage/secure_storage.dart`. |
| `jwt_decoder` | Decoding JWT payloads (claims, company extraction). |
| `shared_preferences` | Non-secret persistence (permission cache). Wrapped by `permission_cache_service.dart`. |

### Networking
| Package | Use for |
|---------|---------|
| `http` | All HTTP calls. Do **not** add `dio`, `chopper`, or `retrofit`. |
| `web` | Browser interop (download triggers, etc.). |

### UI / Theme / Forms
| Package | Use for |
|---------|---------|
| `google_fonts` | Inter font family for the entire `TextTheme`. |
| `material_symbols_icons` | All icons. Use the **rounded** variant. |
| `shimmer` | Loading placeholders. |
| `mask_text_input_formatter` | Masked inputs (CPF, CNPJ, phone, CEP). Do **not** add `intl` or custom formatters for this. |
| `flutter_json_view` | Debug/dev JSON viewers only. |

### PDF & Documents
| Package | Use for |
|---------|---------|
| `syncfusion_flutter_pdf` | PDF text extraction (`pdf_text_extractor.dart`). |
| `syncfusion_flutter_pdfviewer` | In-app PDF preview. |
| `pdf` | Generating PDFs from images (`image_to_pdf_converter.dart`). |
| `pdf_combiner` | Merging PDFs (`pdf_merger.dart`). |
| `image` | Decoding image bytes before PDF assembly. |

### Spreadsheet & Files
| Package | Use for |
|---------|---------|
| `syncfusion_flutter_xlsio` | Generating `.xlsx` files (wrapped by `spreadsheet_service.dart`). |
| `file_saver` | Cross-platform file save (wrapped by `file_save_service.dart`). |
| `file_picker` | Picking files from disk and the lower-level save dialog (wrapped by `core/utils/file_saver.dart`). |
| `archive` | ZIP creation (wrapped by `zip_builder.dart`). |

### Document Scanning & OCR
| Package | Use for |
|---------|---------|
| `cunning_document_scanner` | Native document scanner (mobile). |
| `camera` | Camera capture fallback (web). |
| `google_mlkit_text_recognition` | OCR on scanned pages (mobile). |

### Utilities
| Package | Use for |
|---------|---------|
| `uuid` | Generating v4 UUIDs (request IDs). Wrapped by `request_id_helper.dart`. |

### Dev / Test
| Package | Use for |
|---------|---------|
| `flutter_test` | Standard Flutter test runner. |
| `flutter_lints` | Lint rules. |
| `mocktail` | All mocks/fakes/stubs. Do **not** add `mockito` (no codegen-based mocks). |


## Key Entry Points

- `lib/main.dart` — app bootstrap
- `lib/app_module.dart` — root DI + route registration *(legacy)*
- `lib/app_widget.dart` — MaterialApp with Teal theme *(legacy)*

---

## Environment Config

Secrets live in `secrets/` (not committed):
- `local_config.json` — local Keycloak + API endpoints
- `prod_config.json` — Azure Keycloak + API endpoints

Chaves obrigatórias (`AppConfig.assertConfigured` falha rápido sem elas): `end_session_endpoint`, `identifier`, `people_management_url`, **`tenant_management_url`** (aceita `host:porta` — HTTPS — ou origem completa `http://host:porta`, porque o BC roda em HTTP no desenvolvimento), e o par do fluxo de auth em uso.

## Deployment (Android / Google Play)

CI deploys to the Play Store via `.github/workflows/deploy-rufino-android.yml`
(triggered manually or on push to `main` under `client/rufino_v2/`). The
release build is signed with a real upload key: `android/app/build.gradle.kts`
reads `android/key.properties` when present (created by CI from secrets) and
falls back to debug keys locally. `prod_config.json` is recreated in CI from
the `PLAY_STORE_CONFIG_JSON` secret. See the workflow header for the full list
of required GitHub secrets and where to obtain each in the Google Play Console.

---

## Common Commands

```bash
flutter run          # Run locally
flutter build web    # Build for web
flutter test         # Run tests
flutter analyze      # Static analysis
```
