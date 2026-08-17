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

Flutter cross-platform app for HR/people management (employees, documents, departments, workplaces). Backend is a .NET service (`people-management-service`) with Keycloak OAuth2 auth.


## Tech Stack

**Language**: Dart 3.5.2+ / Flutter (all platforms)
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

`lib/main*.dart`, `lib/app.dart`, `lib/core/{network,storage,theme,widgets,utils}/`, `lib/core/result.dart`, `lib/data/{services,models,repositories}/`, `lib/domain/{entities,repositories}/`, `lib/ui/core/widgets/`, `lib/ui/features/<feature>/{viewmodel,widgets}/`. Detalhes específicos de capacidades em **Code & Capability Index** e **Package Index** abaixo.

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

1. Add the key (lowercase, no diacritics) to `_sensitiveKeys` in
   `lib/core/monitoring/pii_scrubber.dart`.
2. Add a case to `test/unit/core/monitoring/pii_scrubber_test.dart` covering
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

1. After login, `SplashViewModel` calls `permissionNotifier.loadPermissions()`.
2. A single POST to the Keycloak token endpoint (`grant_type=urn:ietf:params:oauth:grant-type:uma-ticket`, `audience=people-management-api`, `response_mode=permissions`) returns all granted resource/scope pairs.
3. `PermissionNotifier` (a `ChangeNotifier` provided app-wide) caches the result and exposes `hasPermission(resource, scope)` and `hasAnyScope(resource)`.
4. UI widgets use `PermissionGuard` or `ModuleGuard` to conditionally render — unauthorized elements are **completely hidden** (`SizedBox.shrink`), never disabled.

### Key Files

| File | Purpose |
|------|---------|
| `lib/domain/entities/permission.dart` | `Permission` entity (resource + scopes) |
| `lib/domain/repositories/permission_repository.dart` | Repository interface |
| `lib/data/services/permission_api_service.dart` | Keycloak UMA RPT request |
| `lib/data/repositories/permission_repository_impl.dart` | Repository implementation |
| `lib/ui/features/auth/viewmodel/permission_notifier.dart` | `PermissionNotifier` — holds state, exposes `hasPermission` / `hasAnyScope` / `clear` |
| `lib/ui/core/widgets/permission_guard.dart` | `PermissionGuard` and `ModuleGuard` widgets |
| `test/testing/fakes/fake_permission_repository.dart` | Fake for tests |

### Canonical Resource & Scope Names

All resource names are **lowercase, kebab-case**. Use **exactly** these strings in `PermissionGuard` / `ModuleGuard`:

| Resource | Scopes |
|----------|--------|
| `company` | `create`, `edit`, `view` |
| `debug` | `view` |
| `department` | `create`, `edit`, `view` |
| `document` | `create`, `edit`, `view`, `upload`, `webhook`, `download`, `send2sign`, `generate`, `approve`, `reject`, `deprecate`, `mark-not-applicable` |
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

- **"A vencer" is validity-based**, not Warning-status-based: the horizon (30/60/90 days, `expiringInDays`) filters `validity` server-side; unit status 8 (Warning) is only a visual chip. **Unit status ids run 1–9** (`8 = 'A Vencer'`, `9 = 'Vencido'`) — `DocumentUnit`, `BatchDocumentUnitItem`, `BatchDownloadUnit` and `DashboardUnitItem` all map them. The "Vencidos" bucket is now status 9 plus the OK/Warning units whose validity already passed but the depreciation job has not run yet.
- **"Pendentes" não conta a renovação em voo.** A substituta criada por "Renovar" fica de fora enquanto a unidade que ela renova ainda cobre (OK / A Vencer / Não Aplicável) — o documento já aparece em "A Vencer" pela substituída, e contá-la duas vezes faria o número de pendências subir a cada renovação feita no prazo. Quando a substituída vence, a renovação passa a contar. É filtro do servidor; o cliente não sabe nem precisa saber.
- **State preservation:** `DocumentDashboardPage` owns the ViewModel + `ScrollController` lifecycles (same pattern as `EmployeeListPage`) — never create them inside the route builder, or filters/bucket/page/scroll are wiped on every push/pop.
- **Row navigation** uses `context.push('/employee/:id?tab=documents')`; the `/employee/:id` route maps `?tab=` (`documents` | `contracts`) to `EmployeeProfileScreen.initialTab`, so the profile lands on the Documentos tab and pop returns to the intact dashboard.
- ViewModel invariant: bucket switch and pagination reload **only the list** (`isLoadingUnits`); filter/horizon changes reload **summary + list together** so the KPI cards never disagree with the rows. Default employee filter is Ativos (status 2).
- **O filtro "Funcionários" lista os cinco status, id 1 (Pendentes) incluído.** Funcionário ainda em admissão já acumula pendência de documento — os `RequireDocuments` do servidor escutam eventos com `Status.Pending` —, então esconder o status 1 tirava da triagem justamente o recém-contratado. A opção nasceu faltando na lista hardcoded (`_employeeStatusOptions`), não por decisão: nada no servidor nem no ViewModel restringe o valor. Cuidado ao mexer: o label "Pendentes" colide com o do card de KPI (documentos pendentes), e é por isso que os itens do dropdown têm key própria (`employee-status-option-<id>`) — teste que busque por texto pega o card errado.

## Documentos em Lote (escopo por funcionário, grupo e documento)

`/batch-document` consulta `GET api/v1/{company}/batch-document/pending-units` e `/missing-employees`. **Os três eixos de escopo são independentes e opcionais** — só funcionário, só grupo, grupo+documento, ou nada (a empresa inteira). O template já foi segmento de rota obrigatório; hoje é query param.

- **O seletor de funcionário é um diálogo de busca (`_EmployeePickerDialog`), NÃO um `Autocomplete`.** O `optionsBuilder` do `Autocomplete` aceita `Future`, mas quando a busca resolve depois de o overlay de opções já ter sido escondido o framework estoura `_zOrderIndex != null` (`OverlayPortal.hide`, `overlay.dart`) — foi exatamente o que aconteceu na primeira versão. O diálogo é dono do próprio ciclo de vida e não cai nesse estado. Busca **no submit**, não por tecla: uma requisição por intenção.

- **Uma requisição, não N.** O modo "Todos" fazia fan-out por template e **somava o `totalCount` de cada resposta** — `pageCount` e a paginação nunca fecharam. `_activeTemplateIds`, `allTemplatesId` e os `mapWithConcurrency` de leitura foram removidos; `null` nos dropdowns significa "todos" e o servidor filtra. O fan-out **sobrevive só na escrita** (`batchCreateDocumentUnits`), porque o command é por template.
- **`EmployeeRepository` é injetado no ViewModel**, não consumido por outro repositório: quem combina dois agregados é o ViewModel. `searchEmployees` devolve lista vazia no erro — o seletor degrada para "sem resultados", nunca para tela de erro.
- **Capacidade é da seleção, não da primeira linha.** `canGenerateSelected` / `canSignSelected` exigem que **todas** as unidades selecionadas suportem a ação; `canSignStaged` olha os arquivos staged, porque "Enviar para Assinar" age sobre o staged e não sobre a seleção. Antes vinha de `pendingUnits.first`, o que já mentia no modo "Todos" quando os templates divergiam.
- **A linha mostra `grupo · documento`** nas duas variantes (wide e narrow). Sem template fixo a lista mistura documentos e o nome do funcionário sozinho não diz o que está pendente.
- **Trocar qualquer eixo limpa seleção, staged e página** (`_resetScopedState`): as unidades saem da lista, e manter arquivos staged enviaria para linhas que o usuário não vê mais.
- **"Criar Docs Faltantes" exige grupo ou documento** (`canCreateMissing`) — pendência nasce sempre de um template, e "todos os templates da empresa" não é operação válida. Cada linha do diálogo é o par **funcionário × template** (chave `'employeeId::templateId'`), então `batchCreateDocumentUnits` recebe os pares e agrupa por template.
- **A lista carrega sem escopo escolhido**: `initState` chama `loadGroupsAndTemplates` e depois `loadPendingUnits`. Testes de widget precisam **rolar** (`drag(-300)`) antes de tocar na barra de ações — a seção de escopo empurrou-a para fora da viewport padrão.

### Dividir a digitalização

O diálogo de fim de digitalização (`_showScanSessionDialog`) tem uma quarta ação — **Dividir** — que corta uma pilha digitalizada em documentos de tamanho igual. O caso de uso é o RH escanear a pilha inteira de uma vez (uma ficha de 2 páginas por funcionário) em vez de digitalizar um funcionário por vez.

- **Dividir só aparece com UM documento de duas ou mais páginas** na sessão (`canSplit`). Com dois ou mais, o usuário já traçou as fronteiras à mão usando "Digitalizar Mais" e cortar por cima delas apagaria essa decisão; com uma página só não há o que cortar. É por isso que Dividir e Digitalizar Mais só aparecem juntos no primeiro documento.
- **A divisão precisa dar número inteiro.** Sobra de página significa que a pilha não era o que o usuário pensava, e adivinhar onde as páginas ímpares entram anexaria página ao funcionário errado. `validatePagesPerDocument` (`core/utils/page_splitter.dart`) recusa vazio, não-inteiro, `< 1`, maior que o total e resto — e é a mesma função que alimenta a prévia ao vivo ("5 documentos de 2 páginas") no `TextFormField`.
- **`_resolveScanSession` resolve a divisão num laço próprio**, não no laço de `_scanDocument`: lá `continue` **reabre o scanner**, então cancelar a divisão dispararia uma nova digitalização em vez de voltar à pergunta. Cancelar desfaz só o corte; Descartar (no diálogo pós-divisão) joga a sessão inteira fora, mesmo sentido do Descartar que já existia.
- **O ViewModel não é tocado.** `processScannedDocuments` já trata cada entrada de `List<List<Uint8List>>` como um documento independente (PDF, OCR, match difuso, reserva da unidade em `assignedUnitIds`), então dividir é só entregar mais entradas. Nome de arquivo (`scan_..._001.pdf`) e o contador do `_BulkProcessingDialog` acertam sozinhos. `splitIntoEqualParts` usa `sublist` — as partes referenciam as mesmas páginas, sem copiar bytes.
- **A sessão de digitalização é estado local da tela** (`scannedDocuments` em `_scanDocument`), não do VM. A regra mora em `page_splitter.dart`, puro e testado em unit; a tela só chama. Se um dia a sessão subir para o ViewModel, é refactor à parte.

## Outdated Document Snapshot (aviso ao gerar)

O PDF é montado no backend a partir de um **snapshot** dos dados do funcionário gravado no `Content` da unidade quando a data foi atualizada. O cadastro muda depois, o snapshot não. Antes de gerar, o app pergunta ao backend se ele ainda bate.

- **Um único serviço/repositório para as duas telas** — `data/services/document_content_api_service.dart` + `DocumentContentRepository` (`checkOutdated` / `refresh`). Não duplicar em `employee_api_service` ou `batch_document_api_service`: o endpoint é o mesmo, o consumo é dos dois lados.
- **`checkFailed` nunca vira aviso.** `DocumentContentStatus.needsWarning` é `isOutdated && !checkFailed` — o servidor marca a verificação como inconclusiva quando um bloco de dado não carregou, e avisar aí levaria o usuário a sobrescrever um snapshot bom. Pela mesma razão, **falha na própria chamada do check não bloqueia a geração**: os ViewModels devolvem conjunto vazio no `onError`.
- **`showOutdatedContentDialog`** (`ui/core/widgets/outdated_content_dialog.dart`) é compartilhado. Lista **todos** os documentos da operação e marca os desatualizados individualmente (badge "Desatualizado"; ícone `priority_high` no layout compacto) — ver os que estão OK é o que torna os marcados legíveis. Retorna `OutdatedContentAction { cancel, continueAnyway, refreshAndContinue }`.
- **`allowRefresh` separa as duas telas.** Perfil: `true` → Cancelar / Gerar com os dados atuais / **Atualizar e gerar** (esta dentro de `PermissionGuard('document','edit')`). Lote: `false` → só Cancelar / Gerar assim mesmo; **atualizar no lote é decisão de produto — o usuário edita cada documento individualmente**. Por isso `BatchDocumentViewModel` tem só `checkOutdatedContent()`, sem refresh.
- **Cobre gerar E gerar+assinar**, nos dois lados: é o mesmo `Content`. **Download não entra** — entrega arquivo já existente, não lê o snapshot. No perfil, o aviso do `generate_sign` aparece **antes** do diálogo de data-limite.
- **Refresh não move a data.** O backend reusa a data já gravada na unidade; o cliente não reenvia data nenhuma. Só o perfil chama, e só para as unidades efetivamente divergentes.

## Status das unidades e as quatro ações (perfil do funcionário)

Os ids 1–9 vêm do servidor e o cliente só rotula. A distinção que importa na tela é **Obsoleto (3)** vs **Vencido (9)**: as duas são documentos que saíram de vigência, e o que as separa é já existir substituto — só o 9 é falta de cobertura agora.

- **Rotule sempre por `statusLabel`, NUNCA por `statusName`.** O servidor manda o nome do smart enum em inglês (`"Pending"`, `"NotApplicable"`, `"RequiresDocument"`, `"AwaitingSignature"`), e o `statusName` só existe como último fallback **dentro** do `statusLabel`. A linha da unidade e o tile do documento renderizavam o campo cru e mostravam inglês na tela. As fixtures de teste usavam nomes já em português, o que escondeu o bug — por isso o widget test do status 6 usa `statusName: 'NotApplicable'` e assere `'Não Aplicável'`.
- **Os rótulos moram em `domain/entities/document_status_labels.dart`, e traduzem por id OU por nome.** Uma função por escala — `documentUnitStatusLabel` (1–9), `documentStatusLabel` (1–7) e `documentComplianceStatusLabel` (0/1/2) — e todo `statusLabel` de entidade delega para elas (`employee_document`, `document_group_with_documents`, `document_dashboard`, `batch_document_unit`, `batch_download`). Duas razões: o mesmo mapa estava copiado em quatro entidades e já tinha divergido; e **rotular só pelo id deixava o inglês chegar à tela sempre que o id não batia** — id vazio, formato inesperado ou status novo no servidor caíam direto no `statusName`. Agora o id é a chave primária e o nome do enum é uma segunda chave para o mesmo rótulo. As escalas **não compartilham mapa**: id `1` é `Pending` na unidade e `RequiresDocument` no documento.
- **Não existe botão de criar unidade avulsa.** Foi removido junto com `createDocumentUnit` (repo/serviço/ViewModel). Pendência nasce do evento de admissão, de **Renovar**, ou de depreciar/invalidar a vigente — os caminhos do servidor deixam a substituta no lugar, então a lista recarregada mostra a nova pendente.
- **Quatro ações, todas com diálogo de confirmação** (`_confirmUnitStatusChange`, chaves `unit-renew-confirm` / `unit-deprecate-confirm` / `unit-invalidate-confirm` / `unit-not-applicable-confirm`): mudam o que o documento prova, nenhuma é operação de um toque.
- **A regra de habilitação mora na entidade**, não no widget — `DocumentUnit.canBeRenewed` (`OK`, `Warning` ou `Expired`), `canBeDeprecated` (só `OK`), `canBeInvalidated` (`Pending`, `OK` ou `NotApplicable`), `canBeMarkedNotApplicable` (só `Pending`). Depreciada e vencida **nunca** são invalidáveis: são a prova do período coberto, e a API recusa (`PMD.DOC24`).
- **Renovar ≠ depreciar.** As duas deixam uma pendente no lugar, mas **depreciar derruba a cobertura na hora** e renovar a mantém: a unidade renovada continua valendo até a substituta ser entregue, e o documento continua "A Vencer"/"Vencido" em vez de virar "Falta Entregar". É a ação certa para trocar de ciclo; depreciar é para tirar de vigência um documento que não deveria mais valer. Só renovar consome um ciclo de validade do template — e **o teto nunca recusa a ação**: esgotados os ciclos o servidor continua renovando, só que a unidade nova vem **sem data de validade** e o documento para de vencer. (Houve uma versão em que o teto recusava, `PMD.DOC26`; ela deixava a unidade vencida sem nenhuma ação possível na tela. O código foi aposentado.)
- **Renovar é a única saída de uma unidade `Vencida`.** Vencida não é depreciável nem invalidável, e o vencimento **não cria mais a substituta sozinho** — sem Renovar, o documento vencido fica sem nenhuma ação possível na tela. Vale também antes de vencer (`OK`/`Warning`): é assim que o RH providencia o substituto no aviso, e é o que torna alcançável o "Agendar envio" (que exige unidade pendente).
- **A substituta se identifica na linha** com o chip "Renovação" (`unit-renewal-badge`), a partir de `DocumentUnit.isRenewal` (`replacesDocumentUnitId` vindo do servidor). Sem ele a substituta é indistinguível de uma pendência qualquer, e a linha não explica por que apareceu ao lado de um documento que ainda vale.
- **Invalidar é a única saída de uma unidade `NotApplicable`** — e a tela do invalidar muda de texto nesse caso ("Voltar a exigir documento", não "Invalidar documento"): não há erro a desfazer, o documento simplesmente voltou a ser exigido, e a pendente substituta do servidor devolve ao RH o que preencher. Dispensar é decisão administrativa, não prova de cobertura — por isso é invalidável e depreciada/vencida não são.
- **Renovar, depreciar e invalidar ficam FORA do bloco `if (unit.isPending)`** — valem para a unidade em vigência, que é o caso mais comum, e é o que faz o invalidar aparecer também na `NotApplicable`. Scopes: `create` (renovar — é criar a próxima unidade do documento, mesmo escopo do `POST /document`, sem escopo novo no Keycloak), `deprecate` e `reject`.
- **Os erros de regra do servidor aparecem na tela**: os dois ViewModels passam o erro por `extractServerMessages` em vez de mensagem fixa, porque `PMD.DOC23`/`PMD.DOC24` explicam por que a ação foi recusada.

## Agendar Envio para Assinatura (perfil do funcionário)

Na seção Documentos do perfil, o diálogo "Gerar Documento" de uma unidade pendente tem uma terceira ação — **"Agendar envio"** (`generate-dialog-schedule-sign`) — ao lado de "Gerar arquivo" e "Gerar e enviar para assinatura". O documento só é gerado e enviado ao funcionário **na data escolhida**.

- **Agendar não passa pelo aviso de snapshot desatualizado.** O PDF só é montado na data do disparo, então quem vale é o cadastro daquele momento — avisar agora sobre um dado que ainda vai mudar seria informação errada. Os outros dois caminhos continuam passando por `_confirmSnapshotFreshness`.
- **A data do envio vem pré-preenchida** com `EmployeeDocument.suggestedSignatureScheduleDate` (o vencimento da cobertura atual, calculado no servidor), então o caso comum — renovar exatamente no dia em que o documento vence — é uma confirmação. Sem sugestão, o campo nasce vazio.
- **Depende de existir unidade pendente**, e é por isso que **Renovar** vem antes no fluxo: no aviso de vencimento não há pendente nenhuma até o RH renovar, e sem ela o agendamento (que exige `Pending`, `PMD.DOC15`) não tem em que agir.
- **`_ScheduleSignDialog` é `StatefulWidget` e possui os próprios controllers.** Não construa controllers no caller e descarte-os depois do `await showDialog`: o diálogo continua vivo durante a animação de saída, e o validador do prazo lê o controller da data de envio — descartar de fora quebra o rebuild (`build scope unexpectedly does not contain that widget`).
- **Validação espelha a API:** `DocumentUnit.validateScheduleSendDate` (hoje ou depois, `PMD.DOC21`) e `DocumentUnit.validateSignDeadline(value, sendOn)` (posterior ao envio, `PMD.DOC22`). O prazo é contado **do envio**, então os atalhos `+3/+5/+10 dias` somam sobre a data do envio, não sobre hoje. Quando a data de envio está inválida, o validador do prazo só checa formato — o outro campo já reporta o problema, e marcar os dois em vermelho pelo mesmo motivo confunde.
- **Na linha da unidade:** chip "Envio agendado: dd/MM/yyyy" quando `unit.isSignatureScheduled`, e a ação "Cancelar agendamento" sob `PermissionGuard('document','send2sign')`. A unidade **continua Pendente** enquanto agendada — o agendamento é intenção, não envio, e por isso não há status novo.
- **Só o caminho de gerar é agendável** — agendar upload exigiria guardar o arquivo até a data. `EmployeeRepository.scheduleSendToSign` manda datas puras (`yyyy-MM-dd`), diferente de `generateAndSendToSign`, que manda instante ISO.

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
| Cortar uma pilha de páginas digitalizadas em documentos iguais | `core/utils/page_splitter.dart` | `splitIntoEqualParts` (sublists, não copia bytes) + `validatePagesPerDocument` (validator de formulário, recusa resto). Ver "Dividir a digitalização". |
| Run many async tasks with a concurrency cap | `core/utils/concurrency.dart` | `mapWithConcurrency` — bounded worker pool, preserves input order, `Future.wait` error semantics. Used by batch fan-out (per-template queries, per-page OCR, per-file text extraction). |
| Generate a request/correlation ID | `data/services/request_id_helper.dart` | UUID v4 for `x-requestid` on mutations. Wraps `uuid`. |
| Send a multipart upload with progress | `data/services/multipart_upload_helper.dart` | Streams bytes and reports `0.0–1.0` via callback. |
| Validate an HTTP response & raise typed errors | `data/services/http_status_helper.dart` | Throws `HttpException` on non-2xx, extracts server messages, logs via `DomainErrorLogger`. |
| Saber se o snapshot de um documento envelheceu / regravá-lo | `data/services/document_content_api_service.dart` | `checkOutdated` + `refresh`. Usado pelo perfil E pelo lote — não replicar em serviço de feature. Ver "Outdated Document Snapshot". |
| Read a server error message for the UI | `core/utils/error_messages.dart` | Extracts message from `HttpException` or wrappers exposing `cause`. |
| Rotular um status de documento, unidade ou grupo em português | `domain/entities/document_status_labels.dart` | Fonte única das três escalas; traduz por id **ou** pelo nome do smart enum. Entidades delegam no `statusLabel` — nunca escreva um `switch` de status novo. |
| Log a domain error to disk (debug only) | `core/utils/domain_error_logger.dart` | Conditional dart:io split via `_writer` / `_writer_stub`. |
| Read/write encrypted secrets (tokens, etc.) | `core/storage/secure_storage.dart` | Wraps `flutter_secure_storage`. |
| Read/write public prefs (permission cache, etc.) | `data/services/permission_cache_service.dart` | Wraps `shared_preferences`. Do not use `shared_preferences` directly elsewhere — extend this or add a sibling cache service. |
| Authenticate via Keycloak / refresh tokens | `data/services/auth_api_service.dart` | Wraps `oauth2` + `jwt_decoder`. |
| Fetch user permissions (UMA / RPT) | `data/services/permission_api_service.dart` | Single source for Keycloak Authorization Services calls. |
| Look up a Brazilian CEP | `data/services/cep_api_service.dart` | ViaCEP wrapper. |
| Read app config / OAuth endpoints | `core/config/app_config.dart` | `--dart-define-from-file`-driven. |
| Trust self-signed certs in dev | `core/config/dev_http_overrides.dart` (+ `_stub`) | Local dev only. Never call from prod path. |
| Return a fallible result from data/domain | `core/result.dart` (`Result<T>` + `Success`/`Failure`) | Mandatory — see "Error Handling" rule. Never `throw` across layers. |

### Domain exception hierarchies (`core/errors/`)

One sealed family per aggregate. **Add a new variant to the existing family before creating a new exception class.**

`auth_exception.dart` (InvalidCredentials, SessionExpired, NoCredentials, NetworkAuthException) · `department_exception.dart` · `workplace_exception.dart` · `employee_exception.dart` · `document_template_exception.dart` · `document_group_exception.dart` · `require_document_exception.dart` · `permission_exception.dart` · `batch_document_exception.dart` · `batch_download_exception.dart` · `document_dashboard_exception.dart` · `document_content_exception.dart` · `cep_exception.dart`

Plus `data/services/http_exception.dart` — raised by `http_status_helper.dart`, carries `statusCode` + `serverMessages`.

### Theme tokens (`core/theme/`)

`app_colors.dart` (seed color) · `app_spacing.dart` (xs/sm/md/lg/xl/xxl/xxxl on 4dp grid) · `app_breakpoints.dart` (mobile 600 / tablet 840 / desktop 1200) · `app_theme.dart` (M3 light/dark factory using Inter via `google_fonts`) · `theme_notifier.dart` (runtime mode toggle).

**Never hardcode colors, spacing, or breakpoints — always reference these.**

### API services (`data/services/`)

One service per backend aggregate. Cross-cutting helpers (`http_exception`, `http_status_helper`, `multipart_upload_helper`, `request_id_helper`, `permission_cache_service`, `file_save_service`, `spreadsheet_service`) MUST be reused — do not inline equivalent logic in feature services.

`auth_api_service` · `permission_api_service` · `permission_cache_service` · `company_api_service` · `department_api_service` (departments + positions + roles + payment-unit/salary-type lookups) · `workplace_api_service` · `employee_api_service` (the largest — covers profile, image, contact, address, personal info, ID card, voter ID, PIS/PASEP, military doc, medical exam, dependents, contracts, documents, signing, document-unit CRUD + range ops) · `document_template_api_service` · `document_group_api_service` · `require_document_api_service` · `batch_document_api_service` · `batch_download_api_service` · `document_dashboard_api_service` · `document_content_api_service` (snapshot: check + refresh, compartilhado entre perfil e lote) · `cep_api_service`.

### Repositories

Every aggregate above has both an interface (`domain/repositories/<aggregate>_repository.dart`) and an implementation (`data/repositories/<aggregate>_repository_impl.dart`). **ViewModels depend on the interface, never the impl or service.**

### Models (DTOs) and Entities

DTOs live in `data/models/<aggregate>_api_model.dart` (+ JSON ser/deser). Domain entities live in `domain/entities/<aggregate>.dart`. Conversion is owned by the repository impl. Do not reuse a DTO as an entity or vice-versa, and do not duplicate fields between siblings — compose with nested DTOs/entities when an aggregate references another (see `employee_profile`, `document_group_with_*`).

**DocumentTemplate rules (policies).** A template's rules live in `TemplatePolicies` (`expiration`, `workload`, `period`, `newContractDeprecation`) inside `domain/entities/document_template.dart`. **A rule is active when it is present** — `null` is how "does not apply" is expressed, and `validityInDays` / `workload` / `usePreviousPeriod` on the entity are getters derived from the rule set, never stored twice.

- **`period` = competência.** `PeriodRule` carries a `PeriodGranularity` (daily/weekly/monthly/yearly, ids matching the backend `PeriodType`: 1–4) and `usePreviousPeriod`. The 4 granularities are hardcoded in the `PeriodGranularity` enum with PT labels — the ids are the contract, the labels are presentation, so no network round-trip for four stable values. The form's Regras section has a third switch (`_PeriodRuleTile`) revealing a granularity dropdown + a retroactive switch.

- **`expiration` can be limited.** `ExpirationRule` carries an optional `maxRenewals` (`int?`): null = expires forever, a value = expires N times and then stops expiring (the API's `ExpirationPolicy` vs `ExpirationLimitedPolicy`). O teto limita **vencimentos**, não o que o RH pode fazer: passado ele, renovar continua permitido e as unidades novas nascem sem validade. The expiration `_RuleTile` reveals a "Limitar renovações" switch (`_ExpirationRenewalControl`, key `rule-switch-maxRenewals`) that in turn reveals the count field; the view model's `_expirationLimited` gates it, and turning the expiration rule off clears both. `fromJson`/`toJson` carry `maxRenewals` inside the `expiration` block (null when forever).
- **`newContractDeprecation` is presence-only.** `NewContractDeprecationRule` carries **no data** — the API's block is sent and returned empty (`{}` = on, `null` = off). It is a class rather than a `bool` so the rule set stays uniform (every rule active when present) and a future parameter has somewhere to land. In the form it is a `_ToggleRuleTile` (key `rule-switch-newContractDeprecation`): switch only, **no field to reveal** and nothing to clear when turned off — that is what separates it from `_RuleTile`. Backend effect: on the employee's next admission, documents from this template that were already delivered get deprecated.
- **Zero is not a rule.** The API rejects a rule carrying a zeroed value (`PMD.DOCT11`), so an active switch requires a value ≥ 1. Legacy templates still echo `0` back in the legacy fields; the DTO maps that to "no rule".
- **Writes send both shapes.** `DocumentTemplateRepositoryImpl._buildModel` makes `policies` the source of truth and mirrors it into `documentValidityDurationInDays` / `workloadInHours`. Sending both keeps the app correct on either side of a deploy — they cannot disagree because both come from the same rule set.
- **Reads prefer the block.** `toEntity` uses the `policies` block when the API sends it and falls back to deriving from the legacy fields when it does not.
- **The form's legacy fields are mirrors.** "Validade (dias)" and "Carga horária (h)" in the basic info section are read-only (`_RuleMirrorField`); editing happens in the Regras section through the rule switches.
- **Signature is a rule too.** The "Assinatura" switch is the fourth tile in Regras (`_SignatureRuleTile`): turning it on reveals the placement editor inline (the `_SignatureCard` list + "Adicionar Assinatura"); there is no separate "Configurações" section or standalone placements card. Turning the switch off clears the placements in the view model, so acceptance and placements can never disagree at save time (the API rejects placements without acceptance, `PMD.DOCT10`).
  - **Read and write shapes differ.** On **read**, `DocumentTemplateApiModel.fromJson` sources signature from `policies.signature`: block present = accepts, and it carries `placeSignatures` (falls back to the top-level `acceptsSignature` only when the API omits the whole `policies` block). On **write**, `toJson`/`toCreateJson` still send `acceptsSignature` + `placeSignatures` as **top-level** fields (the API's write contract is unchanged) — so signature is **not** a `TemplatePolicies` member on the entity; the model keeps its own `acceptsSignature`/`placeSignatures` fields. Reading placements from the old `templateFileInfo.placeSignatures` location was the bug where signatures created after the policy refactor vanished on GET.
  - **The placement's type is mandatory** (`PlaceSignatureData.validateType`, wired into the type dropdown). A placement without a type is serialized as `type: 0`, which the API rejects hard — `TypeSignature.FromValue(0)` throws and fails the *whole* save (not just that placement). So the dropdown must be validated like the numeric fields; an unvalidated type is how "add the first placement to an empty list" silently failed to save.

**Aggregates currently modeled** (each has DTO + entity unless noted): company / company_detail (entity-only) · workplace · department · position · role · remuneration (entity-only) · employee · employee_profile · employee_personal_info · employee_contact · employee_address (entity = `address`) · employee_id_card · employee_vote_id · employee_military_document · employee_medical_exam · employee_dependent · employee_contract · employee_social_integration_program · employee_document · document_template · document_group · document_group_with_templates · document_group_with_documents · document_range_item (DTO-only) · require_document · batch_document_unit · batch_download · document_dashboard · document_content_status · period · permission · selection_option (entity-only) · personal_info_options (entity-only) · signing_option (entity-only) · scanned_document (entity-only) · bulk_upload_match (entity-only) · cep_lookup (DTO-only).

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
