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

### Domain exception hierarchies (`core/errors/`)

One sealed family per aggregate. **Add a new variant to the existing family before creating a new exception class.**

`auth_exception.dart` (InvalidCredentials, SessionExpired, NoCredentials, NetworkAuthException) · `department_exception.dart` · `workplace_exception.dart` · `employee_exception.dart` · `document_template_exception.dart` · `document_group_exception.dart` · `require_document_exception.dart` · `permission_exception.dart` · `batch_document_exception.dart` · `batch_download_exception.dart` · `cep_exception.dart`

Plus `data/services/http_exception.dart` — raised by `http_status_helper.dart`, carries `statusCode` + `serverMessages`.

### Theme tokens (`core/theme/`)

`app_colors.dart` (seed color) · `app_spacing.dart` (xs/sm/md/lg/xl/xxl/xxxl on 4dp grid) · `app_breakpoints.dart` (mobile 600 / tablet 840 / desktop 1200) · `app_theme.dart` (M3 light/dark factory using Inter via `google_fonts`) · `theme_notifier.dart` (runtime mode toggle).

**Never hardcode colors, spacing, or breakpoints — always reference these.**

### API services (`data/services/`)

One service per backend aggregate. Cross-cutting helpers (`http_exception`, `http_status_helper`, `multipart_upload_helper`, `request_id_helper`, `permission_cache_service`, `file_save_service`, `spreadsheet_service`) MUST be reused — do not inline equivalent logic in feature services.

`auth_api_service` · `permission_api_service` · `permission_cache_service` · `company_api_service` · `department_api_service` (departments + positions + roles + payment-unit/salary-type lookups) · `workplace_api_service` · `employee_api_service` (the largest — covers profile, image, contact, address, personal info, ID card, voter ID, PIS/PASEP, military doc, medical exam, dependents, contracts, documents, signing, document-unit CRUD + range ops) · `document_template_api_service` · `document_group_api_service` · `require_document_api_service` · `batch_document_api_service` · `batch_download_api_service` · `cep_api_service`.

### Repositories

Every aggregate above has both an interface (`domain/repositories/<aggregate>_repository.dart`) and an implementation (`data/repositories/<aggregate>_repository_impl.dart`). **ViewModels depend on the interface, never the impl or service.**

### Models (DTOs) and Entities

DTOs live in `data/models/<aggregate>_api_model.dart` (+ JSON ser/deser). Domain entities live in `domain/entities/<aggregate>.dart`. Conversion is owned by the repository impl. Do not reuse a DTO as an entity or vice-versa, and do not duplicate fields between siblings — compose with nested DTOs/entities when an aggregate references another (see `employee_profile`, `document_group_with_*`).

**Aggregates currently modeled** (each has DTO + entity unless noted): company / company_detail (entity-only) · workplace · department · position · role · remuneration (entity-only) · employee · employee_profile · employee_personal_info · employee_contact · employee_address (entity = `address`) · employee_id_card · employee_vote_id · employee_military_document · employee_medical_exam · employee_dependent · employee_contract · employee_social_integration_program · employee_document · document_template · document_group · document_group_with_templates · document_group_with_documents · document_range_item (DTO-only) · require_document · batch_document_unit · batch_download · period · permission · selection_option (entity-only) · personal_info_options (entity-only) · signing_option (entity-only) · scanned_document (entity-only) · bulk_upload_match (entity-only) · cep_lookup (DTO-only).

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

---

## Common Commands

```bash
flutter run          # Run locally
flutter build web    # Build for web
flutter test         # Run tests
flutter analyze      # Static analysis
```
