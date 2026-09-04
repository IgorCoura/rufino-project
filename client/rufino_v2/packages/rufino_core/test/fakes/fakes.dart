import 'package:flutter/widgets.dart';
import 'package:http/http.dart' as http;
import 'package:rufino_core/rufino_core.dart';

/// In-memory [SecureStorage] for tests.
///
/// Keeps what production keeps — a key/value map — and additionally records
/// the keys that were deleted, so a test can assert that a corrupt payload was
/// actually evicted and not merely ignored in memory.
class FakeSecureStorage implements SecureStorage {
  /// Everything currently persisted.
  final Map<String, String> values = {};

  /// The keys passed to [delete], in call order.
  final List<String> deletedKeys = [];

  @override
  Future<void> write({required String key, required String value}) async {
    values[key] = value;
  }

  @override
  Future<String?> read({required String key}) async => values[key];

  @override
  Future<void> delete({required String key}) async {
    deletedKeys.add(key);
    values.remove(key);
  }

  @override
  Future<bool> containsKey({required String key}) async =>
      values.containsKey(key);
}

/// In-memory [PermissionRepository] for tests.
///
/// A fake rather than a mock: the notifier's hydrate-then-fetch flow depends
/// on the cache and the remote answer agreeing with each other over several
/// calls, which stubbing one call at a time does not express well.
class FakePermissionRepository implements PermissionRepository {
  /// What [fetchPermissions] answers with when [remoteError] is `null`.
  List<Permission> remotePermissions = const [];

  /// When set, [fetchPermissions] returns it as a failure instead.
  Object? remoteError;

  /// What [getCachedPermissions] answers with.
  List<Permission>? cached;

  /// How many times [fetchPermissions] was called.
  int fetchCount = 0;

  /// How many times [clearCachedPermissions] was called.
  int clearCount = 0;

  @override
  Future<Result<List<Permission>>> fetchPermissions() async {
    fetchCount++;
    final error = remoteError;
    if (error != null) return Result.error(error, StackTrace.current);
    return Result.success(remotePermissions);
  }

  @override
  Future<List<Permission>?> getCachedPermissions() async => cached;

  @override
  Future<void> cachePermissions(List<Permission> permissions) async {
    cached = permissions;
  }

  @override
  Future<void> clearCachedPermissions() async {
    clearCount++;
    cached = null;
  }
}

/// A recording [ErrorReporter] for tests.
///
/// Mirrors the production short-circuit on [isExpectedFailure] so a test can
/// verify that user-actionable failures never reach the crash dashboard.
class RecordingErrorReporter implements ErrorReporter {
  /// Everything that would have been reported.
  final List<({Object error, StackTrace? stackTrace})> captured = [];

  /// Everything passed to [capture], including expected failures.
  final List<Object> offered = [];

  @override
  Future<void> init() async {}

  @override
  void capture(
    Object error,
    StackTrace? stackTrace, {
    Map<String, Object?>? context,
  }) {
    offered.add(error);
    if (isExpectedFailure(error)) return;
    captured.add((error: error, stackTrace: stackTrace));
  }

  @override
  void addBreadcrumb(
    String message, {
    String? category,
    Map<String, Object?>? data,
  }) {}

  @override
  void setUser({required String? userId, String? companyId}) {}

  @override
  void clearUser() {}

  @override
  http.Client wrapHttpClient(http.Client base) => base;

  @override
  NavigatorObserver get navigatorObserver => NavigatorObserver();
}

/// An [http.Client] that answers every request from [respond] and remembers
/// what it was asked, including whether it was closed.
///
/// [http.BaseClient] is subclassed instead of implemented so the convenience
/// verbs (`get`, `post`, ...) keep their real behaviour and the test exercises
/// the same `send` path production does.
class FakeHttpClient extends http.BaseClient {
  /// Creates a client answering with what [respond] returns.
  FakeHttpClient(this.respond);

  /// Creates a client answering every request with [statusCode] and [body].
  FakeHttpClient.status(int statusCode, {String body = ''})
      : respond = ((_) async => http.Response(body, statusCode));

  /// Builds the response for a request.
  final Future<http.Response> Function(http.BaseRequest request) respond;

  /// Every request the client was handed, in order.
  final List<http.BaseRequest> requests = [];

  /// Whether [close] was called.
  bool closed = false;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    requests.add(request);
    final response = await respond(request);
    return http.StreamedResponse(
      Stream.value(response.bodyBytes),
      response.statusCode,
      request: request,
      headers: response.headers,
      reasonPhrase: response.reasonPhrase,
    );
  }

  @override
  void close() {
    closed = true;
  }
}

/// A [Permission] on [resource] granting [scopes].
Permission grant(String resource, List<String> scopes) =>
    Permission(resource: resource, scopes: scopes);

/// A [SelectedTenant] with sensible defaults, overridable per test.
SelectedTenant tenant({
  String id = 'tenant-1',
  String kind = 'Company',
  String legalName = 'Rufino Servicos LTDA',
  String tradeName = 'Rufino',
  String status = 'Active',
  String role = 'Owner',
  List<String> activeProducts = const [TenantProducts.peopleManagement],
}) {
  return SelectedTenant(
    id: id,
    kind: kind,
    legalName: legalName,
    tradeName: tradeName,
    status: status,
    role: role,
    activeProducts: activeProducts,
  );
}
