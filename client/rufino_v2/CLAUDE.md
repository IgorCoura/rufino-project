# Rufino Client — CLAUDE.md

> ⚠️ **MANDATORY — Tests required on every code change**
>
> Every new feature, refactor, or bug fix **must** include tests. After writing or changing any code:
> 1. Write unit tests for ViewModels, repositories, and data models.
> 2. Write widget tests for new screens.
> 3. Run `flutter test` before considering the task done. All tests must pass.
>
> No code change is complete without passing tests. This is non-negotiable.

---

## Language Convention

> **All code must be written in English**: class names, method names, variable names, file names, comments, and commit messages.
> The only exception is user-facing text visible in the app UI (labels, messages, tooltips, button text, etc.), which must remain in **Brazilian Portuguese** as it is the language of the end users.

---

## Code Documentation

All public APIs and any non-trivial private member **must** have a doc comment. Use the Dart-standard triple-slash `///` style — never `/* */` or `//`.

Official reference: https://dart.dev/effective-dart/documentation

---

### Rules

1. **First line is a self-contained one-sentence summary.** It ends with a period and appears alone in its paragraph.
2. **Additional paragraphs** are separated by a blank `///` line. Use them for details, caveats, or usage examples.
3. **Integrate parameters into prose** using `[paramName]` — do not use `@param` tags.
4. **Describe return values** with a "Returns …" sentence when the return is non-obvious.
5. **Cross-link** related types and methods with `[ClassName]` or `[methodName]`.
6. **Test descriptions** (`group()` and `test()`) must be written as plain English sentences that explain the behaviour being verified, not the implementation.

---

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

---

### Templates

**Class:**
```dart
/// Manages authentication state and credential storage for the current session.
///
/// Wraps the OAuth2 client and [SecureStorage] to provide a safe,
/// typed API for login, logout, and token refresh. All operations
/// return [Result] — exceptions are never propagated to callers.
class AuthApiService { … }
```

**Repository interface method:**
```dart
/// Attempts to log in with the given [username] and [password].
///
/// Returns [Result.success] with `null` on success.
/// Returns [Result.error] with an [InvalidCredentialsException] if the
/// credentials are rejected, or a [NetworkAuthException] if the request fails.
Future<Result<void>> login({required String username, required String password});
```

**ViewModel method:**
```dart
/// Validates the form and submits the login request to [AuthRepository].
///
/// Sets [status] to [LoginStatus.inProgress] while the request is in flight,
/// then to [LoginStatus.success] or [LoginStatus.failure] depending on the
/// outcome. Notifies listeners after each transition.
/// Does nothing if [username] or [password] is empty.
Future<void> submit() async { … }
```

**Test:**
```dart
group('LoginViewModel', () {
  test('transitions to success status after a valid login', () async { … });
  test('transitions to failure status when the repository returns an error', () async { … });
  test('does nothing when username or password is empty', () async { … });
});
```

---

## Project Overview

Flutter cross-platform app for HR/people management (employees, documents, departments, workplaces). Backend is a .NET service (`people-management-service`) with Keycloak OAuth2 auth.

---

## Tech Stack

- **Language**: Dart 3.5.2+
- **Framework**: Flutter (iOS, Android, Web, Windows, macOS, Linux)
- **State Management**: ChangeNotifier + `ListenableBuilder` (MVVM — Flutter recommended)
- **Routing**: `go_router` (Flutter team maintained)
- **DI**: `provider` (Flutter team recommended)
- **Auth**: OAuth2 + Keycloak (`oauth2`, `flutter_secure_storage`, `jwt_decoder`)
- **HTTP**: `http`
- **UI extras**: `shimmer`, `infinite_scroll_pagination`, `google_fonts`, `mask_text_input_formatter`, `audioplayers`, `file_picker`, `intl`

---

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

```
// core/result.dart — already defined, do not redefine
sealed class Result<T> { … }
final class Success<T> extends Result<T> { … }
final class Failure<T> extends Result<T> { … }
```

---

#### Rules

1. **Services (data layer boundary)** are the **only** place where `try/catch` is allowed.
   They catch raw exceptions thrown by third-party libraries (HTTP, OAuth2, JWT, etc.)
   and convert them into typed domain exceptions that are stored inside `Result.error(…)`.

2. **Typed domain exceptions** (e.g. `AuthException` subtypes) are **payloads** inside
   `Result.error(e)` — they are **never thrown**. They exist so the UI can do a type-safe
   `switch` on the error kind.

3. **Repositories** call services inside `try/catch`, catch `DomainException` first, then
   catch everything else and wrap as a generic network/unknown error. They always return
   `Result<T>` and never re-throw.

4. **ViewModels** call repositories and fold the `Result<T>`. They never use `try/catch`.

5. **UI (Screens/Widgets)** inspect ViewModel state properties. They never use `try/catch`
   or interact with exceptions directly.

---

#### Layer responsibilities

| Layer | try/catch? | Returns | Action on error |
|-------|-----------|---------|-----------------|
| Service | ✅ Yes — at the boundary | raw value / throws | catch → wrap in typed exception → let repository handle |
| Repository | ✅ Yes — wraps service calls | `Result<T>` | `on DomainException` → `Result.error(e)`; `catch e` → `Result.error(WrapperException(e))` |
| ViewModel | ❌ No | mutates state | `result.fold(onSuccess: …, onError: …)` |
| UI | ❌ No | — | reads ViewModel state; maps typed exception → localized string |

---

#### Correct pattern

```dart
// ✅ Service — only layer allowed to throw (to the repository above it)
Future<List<String>> getCompanyIds() async {
  final credentials = await getCredentials(); // may throw AuthException
  try {
    final decoded = JwtDecoder.decode(credentials.accessToken);
    final raw = decoded['companies'];
    if (raw == null) return [];
    return (raw as List<dynamic>).map((e) => e.toString()).toList();
  } catch (_) {
    throw const SessionExpiredException(); // typed, caught by repo
  }
}

// ✅ Repository — catches everything, always returns Result
@override
Future<Result<List<String>>> getCompanyIds() async {
  try {
    final ids = await authApiService.getCompanyIds();
    return Result.success(ids);
  } on AuthException catch (e) {
    return Result.error(e);               // pass typed exception through
  } catch (e) {
    return Result.error(NetworkAuthException(e)); // wrap unknown errors
  }
}

// ✅ ViewModel — fold only, no try/catch
final result = await _authRepository.getCompanyIds();
result.fold(
  onSuccess: (ids) => _companyIds = ids,
  onError: (error) {
    _lastError = error is AuthException ? error : NetworkAuthException(error);
    _status = Status.failure;
  },
);

// ✅ UI — switch on typed exception to produce localized string
String _errorText(AuthException error) => switch (error) {
  InvalidCredentialsException() => 'Usuário ou senha incorretos.',
  SessionExpiredException()     => 'Sessão expirada. Faça login novamente.',
  NoCredentialsException()      => 'Nenhuma credencial encontrada.',
  NetworkAuthException()        => 'Falha de conexão. Verifique sua internet.',
};
```

#### Anti-patterns — never do these

```dart
// ❌ Throwing from a repository
Future<List<String>> getCompanyIds() async {
  throw Exception('failed'); // violates Result-only contract
}

// ❌ try/catch in a ViewModel
try {
  final ids = await _repo.getCompanyIds();
} catch (e) {
  _error = e.toString(); // errors must come via Result, not exceptions
}

// ❌ Generic error string hardcoded in ViewModel (PT belongs in the UI)
_errorMessage = 'Falha na autenticação. Verifique seu usuário e senha.';

// ❌ Untyped error payload in Result
return Result.error(Exception('Session expired')); // use SessionExpiredException()
```

---

#### Adding new domain error types

When a new feature needs its own error taxonomy, create a sealed class in `lib/core/errors/`:

```
lib/core/errors/
├── auth_exception.dart        # AuthException subtypes
└── employee_exception.dart    # EmployeeException subtypes (future)
```

Each sealed class follows the same shape as `AuthException`:

```dart
sealed class EmployeeException implements Exception {
  const EmployeeException();
}

final class EmployeeNotFoundException extends EmployeeException {
  const EmployeeNotFoundException(this.id);
  final String id;
}
```

---

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

Dependencies are **strictly one-directional** — each layer only depends on the layer directly below it.

```
UI (Views + ViewModels)
        ↓ depends on
Domain (Repositories — interfaces)
        ↓ depends on
Data (Services + Repository implementations)
```

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

```dart
class EmployeeListScreen extends StatelessWidget {
  const EmployeeListScreen({super.key, required this.viewModel});
  final EmployeeListViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: viewModel,
      builder: (context, _) {
        if (viewModel.isLoading) return const CircularProgressIndicator();
        return ListView.builder(
          itemCount: viewModel.employees.length,
          itemBuilder: (_, i) => EmployeeTile(employee: viewModel.employees[i]),
        );
      },
    );
  }
}
```

**ViewModel**
- Extends `ChangeNotifier`.
- Converts domain data into UI state.
- Handles user interactions and delegates to repositories.
- Only dependency: repositories (or use cases when they exist).
- Expose collections via `UnmodifiableListView` — never return a mutable `List` directly.
- Always call `notifyListeners()` inside a `finally` block when paired with a loading flag — ensures the UI never gets stuck in a loading state.

```dart
class EmployeeListViewModel extends ChangeNotifier {
  EmployeeListViewModel({required EmployeeRepository employeeRepository})
      : _employeeRepository = employeeRepository;

  final EmployeeRepository _employeeRepository;

  List<Employee> _employees = [];

  // ❌ Mutable — callers can push() directly into _employees
  // List<Employee> get employees => _employees;

  // ✅ Immutable view
  UnmodifiableListView<Employee> get employees => UnmodifiableListView(_employees);

  bool _isLoading = false;
  bool get isLoading => _isLoading;

  Future<void> loadEmployees() async {
    _isLoading = true;
    notifyListeners();

    try {
      final result = await _employeeRepository.getEmployees();
      result.fold(
        onSuccess: (data) => _employees = data,
        onError: (_) => _employees = [],
      );
    } finally {
      _isLoading = false;
      notifyListeners();  // always called, even if an exception escapes
    }
  }
}
```

---

#### Form Screens

Form screens follow four mandatory rules. Violating any of them is an architectural error.

**Rule 1 — ViewModel owns `TextEditingController`**

Controllers are created, held, and disposed by the ViewModel — never by the `State` class.

```dart
// ❌ NEVER do this in a StatefulWidget
class _WorkplaceFormScreenState extends State<WorkplaceFormScreen> {
  final _nameController = TextEditingController();
  final _zipCodeController = TextEditingController();
  // ... 7 more controllers

  @override
  void dispose() {
    _nameController.dispose();
    _zipCodeController.dispose();
    super.dispose();
  }
}
```

```dart
// ✅ ViewModel owns and disposes all controllers
class WorkplaceFormViewModel extends ChangeNotifier {
  final nameController = TextEditingController();
  final zipCodeController = TextEditingController();

  @override
  void dispose() {
    nameController.dispose();
    zipCodeController.dispose();
    super.dispose();
  }
}

// Screen is stateless (or minimal StatefulWidget for FormKey + listener only):
class WorkplaceFormScreen extends StatelessWidget {
  final WorkplaceFormViewModel viewModel;
  // ...
  // TextField(controller: viewModel.nameController)
}
```

---

**Rule 2 — Validation logic belongs to ViewModel**

Validator lambdas must not contain any logic. The screen delegates to ViewModel methods.

```dart
// ❌ Validation inline in the screen
TextFormField(
  validator: (value) {
    if (value == null || value.trim().isEmpty) return 'Não pode ser vazio.';
    return null;
  },
)
```

```dart
// ✅ ViewModel exposes validator methods
class WorkplaceFormViewModel extends ChangeNotifier {
  String? validateRequired(String? value) {
    if (value == null || value.trim().isEmpty) return 'Não pode ser vazio.';
    return null;
  }

  String? validateEmail(String? value) { /* ... */ }
}

// Screen just delegates:
TextFormField(validator: viewModel.validateRequired)
```

---

**Rule 3 — `_onSave()` must be a thin delegate**

Because the ViewModel owns the controllers, `controller.text` is always current. `save()` reads directly from its own controllers — the screen must not pass values in.

```dart
// ❌ Screen orchestrates data flow
void _onSave() {
  if (_formKey.currentState?.validate() != true) return;
  viewModel
    ..setName(_nameController.text)
    ..setZipCode(_zipCodeController.text)
    // ... 7 more setters
    ..save();
}
```

```dart
// ✅ ViewModel.save() reads its own controllers
Future<void> save() async {
  final name = nameController.text.trim();
  final zipCode = zipCodeController.text.trim();
  // build entity, call repository
}

// Screen:
void _onSave() {
  if (_formKey.currentState?.validate() != true) return;
  viewModel.save(); // nothing else
}
```

---

**Rule 4 — Standard `_onViewModelChanged()` listener**

Every form screen uses the same listener shape. Do not invent variations.

```dart
// Screen is a minimal StatefulWidget: FormKey + listener only.
@override
void initState() {
  super.initState();
  widget.viewModel.addListener(_onViewModelChanged);
  if (widget.workplaceId != null) {
    widget.viewModel.loadWorkplace(widget.workplaceId!);
  }
}

@override
void dispose() {
  widget.viewModel.removeListener(_onViewModelChanged);
  super.dispose();
}

void _onViewModelChanged() {
  if (!mounted) return;
  switch (widget.viewModel.status) {
    case WorkplaceFormStatus.saved:
      context.pop();
    case WorkplaceFormStatus.error:
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(widget.viewModel.errorMessage ?? 'Erro')),
      );
    default:
      break;
  }
}
```

---

#### Async Operations — Command Pattern

For async operations that need to expose loading/success/error states to the UI, use the **Command pattern** instead of raw boolean flags.

**Why**: A plain `_isLoading` boolean only covers two states. Command covers four: idle, running, completed (ok), completed (error) — and is observable via `ListenableBuilder`.

**Base class** (add once to `lib/core/`):
```dart
/// Wraps an async operation and exposes its execution state.
///
/// Extend or instantiate via [Command0], [Command1], etc.
abstract class Command<T> extends ChangeNotifier {
  bool _running = false;
  Result<T>? _result;

  bool get running => _running;
  bool get completed => _result is Success;
  bool get error => _result is Failure;
  Result<T>? get result => _result;

  Future<void> _execute(Future<Result<T>> Function() action) async {
    if (_running) return;
    _running = true;
    _result = null;
    notifyListeners();
    try {
      _result = await action();
    } finally {
      _running = false;
      notifyListeners();
    }
  }
}

/// Command with no arguments.
class Command0<T> extends Command<T> {
  Command0(this._action);
  final Future<Result<T>> Function() _action;
  Future<void> execute() => _execute(_action);
}

/// Command with one argument.
class Command1<T, A> extends Command<T> {
  Command1(this._action);
  final Future<Result<T>> Function(A) _action;
  Future<void> execute(A arg) => _execute(() => _action(arg));
}
```

**ViewModel — replace `_isLoading` flag with `Command`:**
```dart
class HomeViewModel extends ChangeNotifier {
  HomeViewModel({required BookingRepository bookingRepository})
      : _bookingRepository = bookingRepository {
    load = Command0(_load);
  }

  final BookingRepository _bookingRepository;
  late final Command0<List<Booking>> load;
  List<Booking> _bookings = [];
  UnmodifiableListView<Booking> get bookings => UnmodifiableListView(_bookings);

  Future<Result<List<Booking>>> _load() async {
    final result = await _bookingRepository.getBookings();
    result.fold(
      onSuccess: (data) => _bookings = data,
      onError: (_) => _bookings = [],
    );
    notifyListeners();
    return result;
  }
}
```

**View — listen to the Command, not the ViewModel directly:**
```dart
ListenableBuilder(
  listenable: viewModel.load,
  builder: (context, child) {
    if (viewModel.load.running) return const CircularProgressIndicator();
    if (viewModel.load.error) {
      return ErrorWidget(onRetry: viewModel.load.execute);
    }
    return child!;
  },
  child: BookingList(bookings: viewModel.bookings),
)
```

**When to use Command vs. plain `_isLoading`:**

| Scenario | Use |
|----------|-----|
| Single operation per screen (load + save) | Command |
| Multiple independent async operations on the same screen | Multiple Commands |
| Simple boolean toggle (e.g. checkbox) | Plain state field |

---

### Domain Layer (`domain/`)

**Entities**
- Pure Dart classes with no dependency on external packages.
- Represent data formatted for UI consumption.
- Transformed from DTOs by repositories.

```dart
class Employee {
  const Employee({required this.id, required this.name, required this.email});
  final String id;
  final String name;
  final String email;
}
```

**Repository (interface)**
- Abstract contract implemented by the data layer.
- Enables swapping implementations in tests.

```dart
abstract class EmployeeRepository {
  Future<Result<List<Employee>>> getEmployees();
  Future<Result<Employee>> getEmployee(String id);
}
```

**Use Cases** *(optional — introduce only when ALL criteria are met)*

Introduce a use case when the logic meets **at least one** of these conditions:
1. Requires merging or coordinating data from **multiple repositories**.
2. Logic is **exceedingly complex** and would make the ViewModel hard to test.
3. The same logic is **reused** by multiple ViewModels.

Do not add a domain layer preemptively — start with logic in the ViewModel and refactor only when the above conditions appear (YAGNI).

---

### Data Layer (`data/`)

**Service (API Client)**
- Wraps HTTP endpoints, returns `Future` or `Stream`.
- Holds no state.
- Returns DTOs (raw API models).

```dart
class EmployeeApiService {
  EmployeeApiService({required this.client, required this.baseUrl});
  final http.Client client;
  final String baseUrl;

  Future<List<EmployeeApiModel>> getEmployees() async {
    final response = await client.get(Uri.parse('$baseUrl/employees'));
    // parse and return list of DTOs
  }
}
```

**Model (DTO)**
- Represents the exact JSON structure from the API.
- Contains `fromJson` / `toJson`.
- Never used directly by the UI.

**Repository (implementation)**
- Implements the domain interface.
- Coordinates services and converts DTOs → domain entities.
- Manages caching, retry, and fallback logic.

```dart
class EmployeeRepositoryImpl implements EmployeeRepository {
  EmployeeRepositoryImpl({required this.apiService});
  final EmployeeApiService apiService;

  @override
  Future<Result<List<Employee>>> getEmployees() async {
    try {
      final dtos = await apiService.getEmployees();
      return Result.success(dtos.map((d) => d.toEntity()).toList());
    } catch (e) {
      return Result.error(e);
    }
  }
}
```

---

### Routing — `go_router`

- Declarative routes with deep link and parameter support.
- Configured in `app.dart`.
- Authentication guards via `redirect`.
- Named routes with constants to avoid magic strings.

```dart
final router = GoRouter(
  initialLocation: '/login',
  redirect: (context, state) {
    final isAuthenticated = context.read<AuthViewModel>().isAuthenticated;
    if (!isAuthenticated) return '/login';
    return null;
  },
  routes: [
    GoRoute(path: '/login', builder: (_, __) => const LoginScreen()),
    GoRoute(path: '/employees', builder: (_, __) => const EmployeeListScreen()),
  ],
);
```

---

### Dependency Injection — `provider`

- Dependencies created at the top of the widget tree (`MultiProvider` in `app.dart`).
- ViewModels receive repositories via constructor.
- Repositories receive services via constructor.
- Tests swap implementations with mocks easily.

```dart
MultiProvider(
  providers: [
    // Data layer
    Provider(create: (_) => EmployeeApiService(client: http.Client(), baseUrl: config.apiUrl)),
    Provider<EmployeeRepository>(
      create: (ctx) => EmployeeRepositoryImpl(apiService: ctx.read()),
    ),
    // ViewModels
    ChangeNotifierProvider(
      create: (ctx) => EmployeeListViewModel(employeeRepository: ctx.read()),
    ),
  ],
  child: MaterialApp.router(routerConfig: router),
)
```

---

### Testing Strategy

Official reference: https://docs.flutter.dev/testing/overview
Architecture testing guide: https://docs.flutter.dev/app-architecture/case-study/testing

---

#### Test Pyramid

```
         /\
        /  \  Integration tests  (5–10%)
       /----\  — critical user flows end-to-end
      /      \
     /        \  Widget tests  (15–20%)
    /----------\  — individual screens and components
   /            \
  /              \  Unit tests  (70–80%)
 /________________\  — ViewModels, repositories, use cases, utils
```

- **Unit tests**: fast, no Flutter rendering, no I/O. Run on every save.
- **Widget tests**: render a single widget tree in isolation. Run on every PR.
- **Integration tests**: full app on device/emulator. Run in CI before merge.

---

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

```dart
void main() {
  late MockEmployeeRepository mockRepository;
  late EmployeeListViewModel viewModel;

  setUp(() {
    mockRepository = MockEmployeeRepository();
    viewModel = EmployeeListViewModel(employeeRepository: mockRepository);
  });

  test('loadEmployees emits loading then success state', () async {
    when(() => mockRepository.getEmployees())
        .thenAnswer((_) async => Result.success([fakeEmployee]));

    expect(viewModel.isLoading, false);

    final future = viewModel.loadEmployees();
    expect(viewModel.isLoading, true);

    await future;

    expect(viewModel.isLoading, false);
    expect(viewModel.employees, [fakeEmployee]);
  });

  test('loadEmployees handles repository error', () async {
    when(() => mockRepository.getEmployees())
        .thenAnswer((_) async => Result.error(Exception('network error')));

    await viewModel.loadEmployees();

    expect(viewModel.employees, isEmpty);
    expect(viewModel.hasError, true);
  });
}
```

**Repositories**

Mock the API service. Test DTO → entity conversion and error mapping.

```dart
test('getEmployees parses response and returns domain entities', () async {
  when(() => mockApiService.getEmployees())
      .thenAnswer((_) async => [EmployeeApiModel.fromJson(fixture('employee_list_response.json'))]);

  final result = await repository.getEmployees();

  expect(result.isSuccess, true);
  expect(result.value?.first.name, 'John Doe');
});
```

**Data Models (DTOs)**

Test JSON parsing with fixture files.

```dart
test('EmployeeApiModel.fromJson parses all fields correctly', () {
  final json = jsonDecode(fixture('employee_detail_response.json'));
  final model = EmployeeApiModel.fromJson(json);

  expect(model.id, 'abc-123');
  expect(model.name, 'John Doe');
});
```

**API Services**

Mock `http.Client` to avoid real network calls.

```dart
test('getEmployees sends GET to /employees and returns DTOs', () async {
  when(() => mockClient.get(Uri.parse('$baseUrl/employees')))
      .thenAnswer((_) async => http.Response(fixture('employee_list_response.json'), 200));

  final result = await service.getEmployees();

  expect(result, isA<List<EmployeeApiModel>>());
});
```

---

#### Widget Tests

Wrap the widget under test with `ChangeNotifierProvider` injecting a `Fake` ViewModel.

```dart
void main() {
  testWidgets('shows loading indicator while loading', (tester) async {
    final fakeViewModel = FakeEmployeeListViewModel()..setLoading(true);

    await tester.pumpWidget(
      ChangeNotifierProvider<EmployeeListViewModel>.value(
        value: fakeViewModel,
        child: const MaterialApp(home: EmployeeListScreen()),
      ),
    );

    expect(find.byType(CircularProgressIndicator), findsOneWidget);
  });

  testWidgets('shows employee list when data is loaded', (tester) async {
    final fakeViewModel = FakeEmployeeListViewModel()
      ..setEmployees([Employee(id: '1', name: 'John Doe', email: 'john@test.com')]);

    await tester.pumpWidget(/* ... */);
    await tester.pump();

    expect(find.text('John Doe'), findsOneWidget);
  });
}
```

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

---

#### Golden Tests (Visual Regression)

Use golden tests for critical UI components to catch unintended visual regressions.

```dart
testWidgets('EmployeeListScreen matches golden', (tester) async {
  await tester.pumpWidget(/* widget with fake data */);
  await tester.pumpAndSettle();

  await expectLater(
    find.byType(EmployeeListScreen),
    matchesGoldenFile('goldens/employee_list_screen.png'),
  );
});
```

- Generate/update goldens: `flutter test --update-goldens`
- Commit golden image files to the repository
- Run on a fixed device frame/resolution for deterministic results
- Use `golden_toolkit` for multi-device and multi-theme golden tests

---

#### Integration Tests

Use the `integration_test` package for critical end-to-end flows. Consider **Patrol** when flows involve native components (permissions dialogs, camera, file picker).

```dart
void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('user can log in and view employee list', (tester) async {
    app.main();
    await tester.pumpAndSettle();

    await tester.enterText(find.byKey(const ValueKey('email_field')), 'user@rufino.com');
    await tester.enterText(find.byKey(const ValueKey('password_field')), 'password');
    await tester.tap(find.byKey(const ValueKey('login_button')));
    await tester.pumpAndSettle();

    expect(find.byType(EmployeeListScreen), findsOneWidget);
  });
}
```

Run integration tests:

```bash
flutter test integration_test/flows/employee_management_flow_test.dart
```

---

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

---

#### CI Pipeline (GitHub Actions)

```yaml
name: CI

on:
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: subosito/flutter-action@v2
        with:
          flutter-version: '3.x'
          channel: stable
      - run: flutter pub get
      - run: flutter analyze
      - run: flutter test --coverage
      - name: Upload coverage
        uses: codecov/codecov-action@v4
        with:
          file: ./coverage/lcov.info
```

PRs must pass `flutter analyze` + all tests before merge.

---

#### Recommended Packages

| Package | Purpose |
|---------|---------|
| `flutter_test` | Core widget testing (bundled with SDK) |
| `mocktail` | Mocking without code generation |
| `golden_toolkit` | Multi-device golden tests |
| `patrol` | Integration tests with native component support |
| `http` | Injectable `http.Client` for service mocking |

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

```dart
MaterialApp.router(
  theme: ThemeData(
    useMaterial3: true,
    colorScheme: ColorScheme.fromSeed(
      seedColor: const Color(0xFF00695C), // Rufino teal seed
      brightness: Brightness.light,
    ),
    textTheme: GoogleFonts.interTextTheme(),
  ),
  darkTheme: ThemeData(
    useMaterial3: true,
    colorScheme: ColorScheme.fromSeed(
      seedColor: const Color(0xFF00695C),
      brightness: Brightness.dark,
    ),
    textTheme: GoogleFonts.interTextTheme(),
  ),
  themeMode: ThemeMode.system,
  routerConfig: router,
)
```

---

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

```dart
// Correct
color: Theme.of(context).colorScheme.primary

// Never do this
color: const Color(0xFF00695C)
color: Colors.teal
```

#### Light and Dark Theme

Use the same seed color for both themes — `ColorScheme.fromSeed` generates harmonious palettes for each brightness automatically. Dark theme uses surface tones (not opacity overlays) for elevation depth.

#### Accessibility — Contrast

`ColorScheme.fromSeed` ensures "on" color pairs (e.g., `onPrimary` over `primary`) meet **WCAG AA** (4.5:1 for normal text, 3:1 for large text). Never override these with custom colors that break contrast.

---

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

```dart
// Correct
Text('Funcionários', style: Theme.of(context).textTheme.titleLarge)

// Never do this
Text('Funcionários', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold))
```

#### Google Fonts

Apply a single font family to the entire `TextTheme` via `GoogleFonts.<family>TextTheme()`. For the Rufino app, use **Inter** as the base font — it is highly legible for data-dense HR interfaces.

```dart
textTheme: GoogleFonts.interTextTheme(),
```

To override specific styles:
```dart
textTheme: GoogleFonts.interTextTheme().copyWith(
  displayLarge: GoogleFonts.inter(fontWeight: FontWeight.w700, fontSize: 57),
),
```

---

### Spacing System

All spacing values must follow the **4dp grid**. Define them as constants in `core/theme/app_spacing.dart` and never use arbitrary values.

```dart
// core/theme/app_spacing.dart
abstract final class AppSpacing {
  static const double xs  = 4;
  static const double sm  = 8;
  static const double md  = 16;
  static const double lg  = 24;
  static const double xl  = 32;
  static const double xxl = 48;
  static const double xxxl = 64;
}
```

| Value | Usage |
|-------|-------|
| `4dp` | Spacing between icon and label, tight internal gaps |
| `8dp` | Internal component spacing, between related elements |
| `16dp` | Page horizontal padding (mobile), between list items |
| `24dp` | Page padding (tablet/desktop), between sections |
| `32dp+` | Between major layout sections |

```dart
// Correct
Padding(padding: const EdgeInsets.all(AppSpacing.md), child: ...)
const SizedBox(height: AppSpacing.sm)

// Never do this
Padding(padding: const EdgeInsets.all(15), child: ...)
const SizedBox(height: 10)
```

---

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

```dart
abstract final class AppBreakpoints {
  /// Compact: < 600dp — smartphones in portrait.
  static const double compact = 600;

  /// Medium: 600–840dp — tablets, large phones in landscape.
  static const double medium = 840;

  /// Expanded: ≥ 840dp — desktops, wide tablets, web.
  static const double expanded = 840;

  /// Large: ≥ 1200dp — wide desktop / maximised browser.
  static const double large = 1200;
}
```

| Class | Width range | Typical device | Layout style |
|-------|------------|----------------|-------------|
| Compact | < 600dp | Smartphone portrait | Single column |
| Medium | 600–840dp | Tablet / large phone landscape | Two columns possible |
| Expanded | 840–1200dp | Desktop, tablet landscape | Multi-column / side panels |
| Large | ≥ 1200dp | Wide desktop / maximised browser | Wide multi-column, max-width cap |

---

#### Measuring Available Space

Two tools — choose based on scope:

| Tool | When to use | Why |
|------|-------------|-----|
| `MediaQuery.sizeOf(context)` | Full-screen layout decisions (navigation, page structure) | Reads the whole app window; only triggers rebuild on size changes |
| `LayoutBuilder` | Local widget constraints (a card, a form column, a list) | Reads parent constraints, not the whole window; correct for widgets in scroll views or columns |

```dart
// ✅ Full-screen decision — use MediaQuery.sizeOf
final width = MediaQuery.sizeOf(context).width;
final isExpanded = width >= AppBreakpoints.expanded;

// ✅ Local widget decision — use LayoutBuilder
LayoutBuilder(
  builder: (context, constraints) {
    final isWide = constraints.maxWidth >= AppBreakpoints.medium;
    return isWide ? _TwoColumnForm() : _SingleColumnForm();
  },
)

// ❌ Never use MediaQuery.of(context).size — it rebuilds on ANY MediaQuery change
if (MediaQuery.of(context).size.width >= 600) { ... }
```

---

#### SafeArea — Always Use It

Wrap Scaffold body content in `SafeArea` to avoid notches, camera cutouts, status bars, and OS navigation bars. Material `Scaffold` does **not** do this automatically for body content.

```dart
// ✅ Correct — content never hides behind system chrome
Scaffold(
  body: SafeArea(
    child: YourContent(),
  ),
)

// ✅ Selective — header extends under notch, body is protected
Scaffold(
  body: Column(
    children: [
      HeroHeader(), // intentionally full-bleed
      Expanded(
        child: SafeArea(
          top: false, // header already handled top
          child: ContentList(),
        ),
      ),
    ],
  ),
)
```

**Rules:**
- `SafeArea` modifies `MediaQuery.padding` for its children, so nested `SafeArea` widgets do **not** double-apply padding.
- Never add manual top/bottom `EdgeInsets` to compensate for system chrome — use `SafeArea` instead.

---

#### Content Width Limit

On large screens, full-width content becomes hard to read. **Always cap content width** for list screens and form screens:

```dart
// Forms — max 600dp, centered
Center(
  child: ConstrainedBox(
    constraints: const BoxConstraints(maxWidth: 600),
    child: formContent,
  ),
)

// List/detail screens — max 960dp
Center(
  child: ConstrainedBox(
    constraints: const BoxConstraints(maxWidth: 960),
    child: listContent,
  ),
)
```

---

#### Adaptive Navigation Pattern

Switch the navigation component based on available width:

```dart
// In the root Scaffold (e.g., home screen or shell route)
final width = MediaQuery.sizeOf(context).width;
final isCompact  = width < AppBreakpoints.compact;
final isExpanded = width >= AppBreakpoints.expanded;

return Scaffold(
  body: Row(
    children: [
      if (!isCompact)
        NavigationRail(
          extended: isExpanded,   // show labels when wide
          destinations: destinations,
          selectedIndex: selectedIndex,
          onDestinationSelected: onDestinationSelected,
        ),
      Expanded(child: currentPage),
    ],
  ),
  bottomNavigationBar: isCompact
      ? NavigationBar(
          destinations: destinations,
          selectedIndex: selectedIndex,
          onDestinationSelected: onDestinationSelected,
        )
      : null,
);
```

| Width | Navigation | Page horizontal padding |
|-------|-----------|------------------------|
| < 600dp (compact) | `NavigationBar` (bottom) | `AppSpacing.md` (16dp) |
| 600–840dp (medium) | `NavigationRail` collapsed | `AppSpacing.lg` (24dp) |
| ≥ 840dp (expanded) | `NavigationRail` extended | `AppSpacing.xl` (32dp) |

---

#### List Screens — Adaptive Layout

Prefer `GridView` over `ListView` on larger screens so space is used efficiently:

```dart
LayoutBuilder(
  builder: (context, constraints) {
    // On wide screens, use a grid; on narrow screens, a list
    if (constraints.maxWidth >= AppBreakpoints.medium) {
      return GridView.builder(
        padding: const EdgeInsets.all(AppSpacing.md),
        gridDelegate: const SliverGridDelegateWithMaxCrossAxisExtent(
          maxCrossAxisExtent: 320, // each card is at most 320dp wide
          mainAxisSpacing: AppSpacing.sm,
          crossAxisSpacing: AppSpacing.sm,
          childAspectRatio: 3,
        ),
        itemCount: items.length,
        itemBuilder: (context, index) => ItemCard(item: items[index]),
      );
    }
    return ListView.separated(
      padding: const EdgeInsets.all(AppSpacing.md),
      itemCount: items.length,
      separatorBuilder: (_, __) => const SizedBox(height: AppSpacing.sm),
      itemBuilder: (context, index) => ItemCard(item: items[index]),
    );
  },
)
```

---

#### Form Screens — Adaptive Layout

On small screens: single-column scrollable form.
On medium+ screens: center the form and cap its width.

```dart
LayoutBuilder(
  builder: (context, constraints) {
    final isWide = constraints.maxWidth >= AppBreakpoints.medium;
    Widget form = Form(
      key: formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: formFields,
      ),
    );
    if (isWide) {
      form = Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 600),
          child: form,
        ),
      );
    }
    return SingleChildScrollView(
      padding: EdgeInsets.symmetric(
        horizontal: isWide ? AppSpacing.xl : AppSpacing.md,
        vertical: AppSpacing.md,
      ),
      child: form,
    );
  },
)
```

---

#### Lists with a FloatingActionButton — Bottom Clearance

**Bug:** On screens with a `FloatingActionButton`, the last item in a `ListView` can be hidden behind the FAB and unreachable by tapping.

**Fix:** Add bottom padding equal to the FAB height + margin + extra room:

```dart
// ✅ Standard FAB (56dp) + 16dp margin + 8dp room = 80dp
ListView.separated(
  padding: const EdgeInsets.fromLTRB(
    AppSpacing.md, AppSpacing.md, AppSpacing.md, AppSpacing.md + 80,
  ),
  ...
)

// ✅ Extended FAB (48dp) + 16dp margin + 8dp room = 72dp
ListView(
  padding: const EdgeInsets.fromLTRB(
    AppSpacing.md, AppSpacing.md, AppSpacing.md, AppSpacing.md + 72,
  ),
  ...
)
```

| FAB type | Extra bottom padding |
|----------|---------------------|
| Standard `FloatingActionButton` | `AppSpacing.md + 80` |
| `FloatingActionButton.extended` | `AppSpacing.md + 72` |

This rule applies to every `ListView`, `GridView`, or `CustomScrollView` inside a `Scaffold` that has a `FloatingActionButton`.

---

#### Never Lock Orientation

Do **not** lock the app to portrait. Allow all orientations on all platforms. Foldable Android devices and iPads are used in landscape constantly; locking causes letterboxing.

```dart
// ❌ Never do this
SystemChrome.setPreferredOrientations([DeviceOrientation.portraitUp]);

// ✅ Support all orientations — design layouts that work in both
```

---

#### Desktop / Web — Input Handling

On desktop and web, users interact with mouse and keyboard. Add these behaviors to custom interactive elements:

**Mouse cursor and hover:**
```dart
MouseRegion(
  cursor: SystemMouseCursors.click,
  onEnter: (_) => setState(() => _hovered = true),
  onExit:  (_) => setState(() => _hovered = false),
  child: GestureDetector(onTap: onTap, child: widget),
)
```

**Keyboard focus and tab traversal:**
Built-in M3 components support tab navigation out of the box. For custom interactive widgets, use `FocusableActionDetector`.

**Visual density** — tighten UI on desktop where mouse precision is higher:
```dart
ThemeData(
  visualDensity: VisualDensity.adaptivePlatformDensity,
  // or for fine-grained control:
  // visualDensity: VisualDensity(horizontal: -1, vertical: -1),
)
```

---

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

```dart
// Primary — one per screen
FilledButton(onPressed: onSave, child: const Text('Salvar'))

// Secondary
OutlinedButton(onPressed: onCancel, child: const Text('Cancelar'))

// Tertiary
TextButton(onPressed: onLearnMore, child: const Text('Saiba mais'))
```

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

```dart
TextField(
  decoration: InputDecoration(
    labelText: 'Nome',
    border: const OutlineInputBorder(),
    prefixIcon: const Icon(Icons.person_outline),
  ),
)
```

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

---

### Icons

Use **Material Symbols** (`material_symbols_icons` package) with the `rounded` variant as the default — it is friendlier and more consistent across component sizes.

```dart
import 'package:material_symbols_icons/symbols.dart';

// Preferred — rounded variant, consistent visual weight
Icon(Symbols.person_rounded)
Icon(Symbols.folder_rounded)
Icon(Symbols.arrow_back_rounded)
```

**Sizing conventions:**

| Context | Size |
|---------|------|
| Navigation items | 24dp |
| Button icons | 18dp |
| Leading icons in ListTile | 24dp |
| Prominent focal icons | 48dp |

Always pair icons with a text label unless the action is universally understood (close `×`, search `🔍`). Use `Semantics` to label icon-only buttons.

---

### Accessibility

Every screen must comply with these rules — no exceptions.

#### Touch Targets
All interactive elements must be **at least 48×48dp**. Use `InkWell` or `GestureDetector` with a minimum size, or rely on M3 components which meet this by default.

```dart
// If using a custom interactive widget
SizedBox(
  width: 48,
  height: 48,
  child: InkWell(onTap: onTap, child: icon),
)
```

#### Semantics
Label all interactive elements that do not have visible text:

```dart
Semantics(
  label: 'Editar funcionário',
  button: true,
  child: IconButton(icon: const Icon(Symbols.edit_rounded), onPressed: onEdit),
)
```

#### Text Scaling
Never block text scaling. Use `MediaQuery.withClampedTextScaling` only to prevent extreme scaling while still respecting user preferences:

```dart
MediaQuery.withClampedTextScaling(
  minScaleFactor: 1.0,
  maxScaleFactor: 1.4,
  child: child,
)
```

#### Color Contrast
- Normal text: minimum **4.5:1** (WCAG AA)
- Large text (18pt+ or 14pt+ bold): minimum **3:1**
- `ColorScheme.fromSeed` generates compliant pairings automatically. Do not override "on" colors.

#### Screen Readers
Test with TalkBack (Android) and VoiceOver (iOS). Every interactive element must have a meaningful semantic label.

---

### Widget Code Patterns

#### Always Use `const` Constructors

```dart
// Correct — Flutter skips rebuild for const subtrees
const SizedBox(height: AppSpacing.md)
const Icon(Symbols.person_rounded)

// Avoid — unnecessary allocation on every rebuild
SizedBox(height: AppSpacing.md)
```

#### Decompose Complex Widgets Into Classes

Extract widgets into their own `StatelessWidget` class — not helper methods that return `Widget`. Flutter can skip rebuilding const subtrees from extracted classes.

```dart
// Correct — extracted StatelessWidget
class EmployeeListTile extends StatelessWidget {
  const EmployeeListTile({super.key, required this.employee});
  final Employee employee;

  @override
  Widget build(BuildContext context) => ListTile(/* ... */);
}

// Avoid — method returning Widget (no rebuild optimization)
Widget _buildTile(Employee employee) => ListTile(/* ... */);
```

#### Prefer `SizedBox` Over `Container` for Spacing

```dart
// Correct — lightweight, clear intent
const SizedBox(width: AppSpacing.sm)
const SizedBox(height: AppSpacing.md)

// Use Padding for spacing around content
const Padding(
  padding: EdgeInsets.symmetric(horizontal: AppSpacing.md),
  child: content,
)

// Container only when decoration is needed
Container(
  decoration: BoxDecoration(
    color: Theme.of(context).colorScheme.surfaceContainer,
    borderRadius: BorderRadius.circular(12),
  ),
  child: content,
)
```

#### Use `LayoutBuilder` or `MediaQuery.sizeOf`, Not `MediaQuery.of().size`

```dart
// ✅ Full-screen layout decision — MediaQuery.sizeOf (only triggers on size change)
final isExpanded = MediaQuery.sizeOf(context).width >= AppBreakpoints.expanded;

// ✅ Local widget layout — LayoutBuilder (reads parent constraints)
LayoutBuilder(
  builder: (context, constraints) {
    final isWide = constraints.maxWidth >= AppBreakpoints.medium;
    return isWide ? WideLayout() : NarrowLayout();
  },
)

// ❌ Never — triggers rebuild on ANY MediaQuery property change
if (MediaQuery.of(context).size.width >= 600) { ... }
```

---

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

```dart
abstract final class AppTheme {
  static ThemeData light() => ThemeData(
    useMaterial3: true,
    colorScheme: ColorScheme.fromSeed(
      seedColor: AppColors.seed,
      brightness: Brightness.light,
    ),
    textTheme: AppTextTheme.build(),
  );

  static ThemeData dark() => ThemeData(
    useMaterial3: true,
    colorScheme: ColorScheme.fromSeed(
      seedColor: AppColors.seed,
      brightness: Brightness.dark,
    ),
    textTheme: AppTextTheme.build(),
  );
}
```

---

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
