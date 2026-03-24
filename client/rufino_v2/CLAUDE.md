# Rufino Client — CLAUDE.md

+> **MANDATORY — Tests required on every code change.**
Every feature, refactor, or bug fix **must** include tests (unit for ViewModels/repos/models, widget for screens). Run `flutter test` before considering the task done. Non-negotiable.

## Language Convention

**All code in English** (classes, methods, variables, files, comments, commits). Only exception: **user-facing UI text in Brazilian Portuguese**.

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

| Element | Opening phrase | Example |
|---------|---------------|---------|
| Class / Entity | Noun phrase describing one instance | `/// A paginated list of employees from the API.` |
| Method with side effect | Third-person verb | `/// Fetches and caches the list of companies from the remote API.` |
| Method that returns a value | Noun phrase or "Returns …" | `/// Returns the currently selected [Company], or null if none is selected.` |
| Boolean property / getter | "Whether …" | `/// Whether the form submission is currently in progress.` |
| `Future`-returning method | Describe what resolves | `/// Resolves with the decoded JWT payload, or throws [SessionExpiredException] if the token is invalid.` |
| Repository interface method | Contract description (what, not how) | `/// Persists the given [company] as the active company for the current session.` |
| Test `group()` | Subject under test | `'LoginViewModel'` |
| Test `test()` | Full sentence: subject + condition + expectation | `'emits failure status when the repository returns an error'` |


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

```
lib/
├── main.dart                    # Entry point (prod)
├── main_dev.dart                # Entry point (dev)
├── app.dart                     # MaterialApp + go_router + provider setup
│
├── core/                        # Shared across all features
│   ├── network/                 # HTTP client, interceptors, error handling
│   ├── storage/                 # flutter_secure_storage wrapper
│   ├── theme/                   # ThemeData, colors, typography
│   ├── widgets/                 # Reusable widgets (AppButton, AppTextField...)
│   ├── result.dart              # Result<T, E> type for error handling
│   └── utils/                   # Helpers, formatters, extensions
│
├── data/                        # Data layer (shared across features)
│   ├── services/                # HTTP clients per domain (AuthApiService, EmployeeApiService...)
│   ├── models/                  # DTOs — API response models (JSON → Dart)
│   └── repositories/            # Concrete repository implementations
│
├── domain/                      # Domain layer
│   ├── entities/                # Pure domain models (no API logic)
│   └── repositories/            # Repository interfaces/abstractions
│
└── ui/                          # Presentation layer
    ├── core/
    │   └── widgets/             # Branded shared components
    └── features/                # One folder per feature
        ├── auth/
        │   ├── viewmodel/
        │   │   └── login_viewmodel.dart
        │   └── widgets/
        │       ├── login_screen.dart
        │       └── login_form.dart
        ├── employee/
        │   ├── viewmodel/
        │   │   ├── employee_list_viewmodel.dart
        │   │   └── employee_detail_viewmodel.dart
        │   └── widgets/
        │       ├── employee_list_screen.dart
        │       └── employee_detail_screen.dart
        ├── company/
        ├── department/
        ├── workplace/
        └── home/
```

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

**ViewModel:** Extends `ChangeNotifier`. Expose collections via `UnmodifiableListView`. Always `notifyListeners()` in `finally` block when paired with loading flag.

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

```
test/
├── unit/
│   ├── ui/
│   │   └── features/
│   │       ├── employee/
│   │       │   └── employee_list_viewmodel_test.dart
│   │       └── auth/
│   │           └── login_viewmodel_test.dart
│   ├── domain/
│   │   └── usecases/
│   │       └── get_employees_usecase_test.dart
│   └── data/
│       ├── repositories/
│       │   └── employee_repository_test.dart
│       ├── services/
│       │   └── employee_api_service_test.dart
│       └── models/
│           └── employee_model_test.dart
├── widget/
│   └── features/
│       ├── employee/
│       │   └── employee_list_screen_test.dart
│       └── auth/
│           └── login_screen_test.dart
├── integration/
│   └── flows/
│       └── employee_management_flow_test.dart
├── golden/
│   ├── goldens/                  # committed golden image files
│   └── features/
│       └── employee/
│           └── employee_list_screen_golden_test.dart
└── testing/                      # shared test utilities (not tests themselves)
    ├── fakes/
    │   ├── fake_employee_repository.dart
    │   └── fake_auth_repository.dart
    ├── mocks/
    │   └── mocks.dart
    ├── fixtures/
    │   └── json/
    │       ├── employee_list_response.json
    │       └── employee_detail_response.json
    └── helpers/
        └── pump_app.dart         # helper to pump widget with providers
```

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

## UI Design Guidelines (Material Design 3)

Official references:
- Material Design 3 spec: https://m3.material.io
- Flutter M3 components: https://docs.flutter.dev/ui/widgets/material
- Flutter typography: https://docs.flutter.dev/ui/design/text/typography
- Flutter accessibility: https://docs.flutter.dev/ui/accessibility-and-internationalization/accessibility

---

### Material 3 Setup

Enable Material 3 globally in `app.dart`. As of Flutter 3.16, M3 is the default, but always declare it explicitly.

### Color System

**Never hardcode colors.** Always consume colors from `Theme.of(context).colorScheme`.

#### Semantic Color Roles (M3)

| Role | Usage |
|------|-------|
| `primary` | Key actions, FAB, active state, filled buttons |
| `onPrimary` | Content placed on top of `primary` |
| `secondary` | Less prominent components, filter chips |
| `tertiary` | Contrasting accents, special emphasis |
| `surface` | Backgrounds, cards, sheets |
| `onSurface` | Text and icons on surface backgrounds |
| `surfaceContainer` | Cards, dialogs, navigation components |
| `surfaceContainerHigh` | Higher-elevated cards and components |
| `error` | Error states, validation feedback |
| `onError` | Content on top of `error` |
| `outline` | Borders, dividers |
| `outlineVariant` | Subtle dividers, decorative borders |

#### Light and Dark Theme

Use the same seed color for both themes — `ColorScheme.fromSeed` generates harmonious palettes for each brightness automatically. Dark theme uses surface tones (not opacity overlays) for elevation depth.

#### Accessibility — Contrast

`ColorScheme.fromSeed` ensures "on" color pairs (e.g., `onPrimary` over `primary`) meet **WCAG AA** (4.5:1 for normal text, 3:1 for large text). Never override these with custom colors that break contrast.

### Typography

**Never hardcode `fontSize`.** Always use `Theme.of(context).textTheme`.

#### M3 Type Scale

| Style | Usage |
|-------|-------|
| `displayLarge / Medium / Small` | Hero text, splash screens, large numbers |
| `headlineLarge / Medium / Small` | Section titles, page headings |
| `titleLarge / Medium / Small` | AppBar titles, card titles, list group headers |
| `bodyLarge / Medium / Small` | Paragraphs, descriptions, list content |
| `labelLarge / Medium / Small` | Buttons, tabs, chips, captions |

#### Google Fonts

Apply a single font family to the entire `TextTheme` via `GoogleFonts.<family>TextTheme()`. For the Rufino app, use **Inter** as the base font — it is highly legible for data-dense HR interfaces.

### Spacing System

All spacing values must follow the **4dp grid**. Define them as constants in `core/theme/app_spacing.dart` and never use arbitrary values.

| Value | Usage |
|-------|-------|
| `4dp` | Spacing between icon and label, tight internal gaps |
| `8dp` | Internal component spacing, between related elements |
| `16dp` | Page horizontal padding (mobile), between list items |
| `24dp` | Page padding (tablet/desktop), between sections |
| `32dp+` | Between major layout sections |

### Responsive & Adaptive Layout

Flutter targets smartphones, tablets, desktop, and web from a single codebase. Every screen must adapt to all of these. This section defines the mandatory patterns.

Official references:
- https://docs.flutter.dev/ui/adaptive-responsive
- https://docs.flutter.dev/ui/adaptive-responsive/large-screens
- https://docs.flutter.dev/ui/adaptive-responsive/safearea-mediaquery

---

#### Three-Step Framework: Abstract → Measure → Branch

1. **Abstract** — identify widgets that change shape across sizes (navigation, dialogs, list layouts, form widths).
2. **Measure** — pick the right tool to read available space.
3. **Branch** — swap the layout at defined breakpoints.

---

#### Breakpoints (Material 3 Window Size Classes)

Define once in `core/theme/app_breakpoints.dart`:

| Class | Width range | Typical device | Layout style |
|-------|------------|----------------|-------------|
| Compact | < 600dp | Smartphone portrait | Single column |
| Medium | 600–840dp | Tablet / large phone landscape | Two columns possible |
| Expanded | 840–1200dp | Desktop, tablet landscape | Multi-column / side panels |
| Large | ≥ 1200dp | Wide desktop / maximised browser | Wide multi-column, max-width cap |

#### Measuring Available Space

Two tools — choose based on scope:

| Tool | When to use | Why |
|------|-------------|-----|
| `MediaQuery.sizeOf(context)` | Full-screen layout decisions (navigation, page structure) | Reads the whole app window; only triggers rebuild on size changes |
| `LayoutBuilder` | Local widget constraints (a card, a form column, a list) | Reads parent constraints, not the whole window; correct for widgets in scroll views or columns |

#### SafeArea — Always Use It

Wrap Scaffold body content in `SafeArea` to avoid notches, camera cutouts, status bars, and OS navigation bars. Material `Scaffold` does **not** do this automatically for body content.

**Rules:**
- `SafeArea` modifies `MediaQuery.padding` for its children, so nested `SafeArea` widgets do **not** double-apply padding.
- Never add manual top/bottom `EdgeInsets` to compensate for system chrome — use `SafeArea` instead.

#### Content Width Limit

On large screens, full-width content becomes hard to read. **Always cap content width** for list screens and form screens:

#### Adaptive Navigation Pattern

Switch the navigation component based on available width:

| Width | Navigation | Page horizontal padding |
|-------|-----------|------------------------|
| < 600dp (compact) | `NavigationBar` (bottom) | `AppSpacing.md` (16dp) |
| 600–840dp (medium) | `NavigationRail` collapsed | `AppSpacing.lg` (24dp) |
| ≥ 840dp (expanded) | `NavigationRail` extended | `AppSpacing.xl` (32dp) |

#### List Screens — Adaptive Layout

Prefer `GridView` over `ListView` on larger screens so space is used efficiently:

#### Form Screens — Adaptive Layout

On small screens: single-column scrollable form.
On medium+ screens: center the form and cap its width.

#### Lists with a FloatingActionButton — Bottom Clearance

**Bug:** On screens with a `FloatingActionButton`, the last item in a `ListView` can be hidden behind the FAB and unreachable by tapping.

**Fix:** Add bottom padding equal to the FAB height + margin + extra room:

| FAB type | Extra bottom padding |
|----------|---------------------|
| Standard `FloatingActionButton` | `AppSpacing.md + 80` |
| `FloatingActionButton.extended` | `AppSpacing.md + 72` |

This rule applies to every `ListView`, `GridView`, or `CustomScrollView` inside a `Scaffold` that has a `FloatingActionButton`.

#### Never Lock Orientation

Do **not** lock the app to portrait. Allow all orientations on all platforms. Foldable Android devices and iPads are used in landscape constantly; locking causes letterboxing.

#### Desktop / Web — Input Handling

On desktop and web, users interact with mouse and keyboard.

**Keyboard focus and tab traversal:**
Built-in M3 components support tab navigation out of the box. For custom interactive widgets, use `FocusableActionDetector`.

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

#### Buttons — Hierarchy

Always match button emphasis to action importance. A single screen should have **at most one** `FilledButton`.

| Component | Emphasis | When to use |
|-----------|----------|-------------|
| `FilledButton` | Highest | Primary action (save, confirm, submit) |
| `FilledButton.tonal` | High | Secondary primary action |
| `OutlinedButton` | Medium | Important but non-destructive secondary action |
| `TextButton` | Low | Tertiary actions, cancel, navigation links |
| `IconButton` | Variable | Icon-only actions; use `.filled` or `.tonal` variants for emphasis |

#### Cards — Variants

| Variant | Use |
|---------|-----|
| `Card()` | Primary content, elevated with shadow — for focal list items |
| `Card.filled()` | Grouped content, less emphasis — for info sections |
| `Card.outlined()` | Content needing clear boundaries — for selectable items |

Cards must use `AppSpacing` for their internal padding. Default corner radius is 12dp (M3 default).

#### AppBar — Variants

| Variant | Use |
|---------|-----|
| `AppBar` | Standard fixed height. For simple pages |
| `SliverAppBar.medium()` | Medium collapsing (~112dp). For content-rich screens |
| `SliverAppBar.large()` | Large collapsing (~152dp). For primary feature screens (employee list, home) |

#### Text Fields

Use **outlined** text fields (M3 recommendation) by default.

Use **filled** fields only in contexts where outlined borders add too much visual noise (e.g., search bars inside colored containers).

#### Dialogs, Bottom Sheets, Snackbars

| Component | Use | Notes |
|-----------|-----|-------|
| `showDialog` | Critical confirmations, destructive action warnings | Blocks interaction — use sparingly |
| `showModalBottomSheet` | Non-critical options, secondary actions, filters | Set `isScrollControlled: true` for tall content |
| `ScaffoldMessenger.showSnackBar` | Brief feedback, undo actions | Auto-dismiss, max one at a time, non-blocking |

#### Chips

| Variant | Use |
|---------|-----|
| `FilterChip` | Filtering lists (by department, status, role) |
| `ChoiceChip` | Single-select from a set (active/inactive toggle) |
| `InputChip` | Selected items in multi-select fields |
| `ActionChip` | Shortcut actions related to current content |

---

### Animation and Motion

Use the `animations` package for M3-compliant page and container transitions.

| Scenario | Widget / Pattern |
|----------|-----------------|
| Simple property change | `AnimatedContainer`, `AnimatedOpacity` (implicit) |
| Shared element between pages | `Hero` |
| Page transitions | `FadeThroughTransition`, `SharedAxisTransition` (from `animations` package) |
| List item appear/disappear | `AnimatedList` |
| Complex, choreographed | `AnimationController` + `AnimatedBuilder` (explicit) |

**Duration guidance (M3 motion tokens):**

| Type | Duration | Use |
|------|----------|-----|
| Short | 100–200ms | Small, simple transitions (tooltip, chip) |
| Medium | 250–400ms | Standard screen transitions, expanding cards |
| Long | 450–600ms | Complex layout changes, full-screen transitions |

Never use animation purely for decoration. Every animation must serve a purpose: orient the user, confirm an action, or reduce cognitive load.

### Icons

Use **Material Symbols** (`material_symbols_icons` package) with the `rounded` variant as the default — it is friendlier and more consistent across component sizes.

**Sizing conventions:**

| Context | Size |
|---------|------|
| Navigation items | 24dp |
| Button icons | 18dp |
| Leading icons in ListTile | 24dp |
| Prominent focal icons | 48dp |

Always pair icons with a text label unless the action is universally understood (close `×`, search `🔍`). Use `Semantics` to label icon-only buttons.

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

#### Always Use `const` Constructors

#### Decompose Complex Widgets Into Classes

Extract widgets into their own `StatelessWidget` class — not helper methods that return `Widget`. Flutter can skip rebuilding const subtrees from extracted classes.

#### Prefer `SizedBox` Over `Container` for Spacing

#### Use `LayoutBuilder` or `MediaQuery.sizeOf`, Not `MediaQuery.of().size`

### Theme File Structure

All theme configuration lives in `core/theme/`:

```
core/theme/
├── app_theme.dart          # ThemeData factory (light + dark)
├── app_colors.dart         # Seed color constant and any custom color extensions
├── app_spacing.dart        # AppSpacing constants (4dp grid)
├── app_breakpoints.dart    # AppBreakpoints constants
└── app_text_theme.dart     # GoogleFonts TextTheme configuration (if complex)
```

`app_theme.dart` is the single entry point:

## Legacy Architecture (to be migrated)

| Aspect               | Current                        | Target                       |
|----------------------|-------------------------------|------------------------------|
| State management     | BLoC (`flutter_bloc`)          | ChangeNotifier + ViewModel   |
| Routing              | `flutter_modular`              | `go_router`                  |
| DI                   | `flutter_modular`              | `provider`                   |
| Structure            | `modules/<feature>/`           | `ui/features/<feature>/`     |
| Data                 | mixed inside modules           | `data/` (services + repos)   |
| Domain               | partial global `domain/`       | `domain/` per clear entity   |
| Contracts (interfaces)| absent                        | `domain/repositories/`       |

---

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
