import 'package:flutter/widgets.dart';
import 'package:http/http.dart' as http;
import 'package:rufino_v2/core/errors/expected_failure.dart';
import 'package:rufino_v2/core/monitoring/error_reporter.dart';

/// A recording [ErrorReporter] for tests.
///
/// Captures every call to [capture] in [capturedErrors] for assertions, and
/// mirrors the production short-circuit on `error is ExpectedFailure` so
/// tests can verify that user-actionable failures are not reported.
class FakeErrorReporter implements ErrorReporter {
  final List<({Object error, StackTrace? stackTrace, Map<String, Object?>? context})>
      capturedErrors = [];

  final List<({String message, String? category, Map<String, Object?>? data})>
      breadcrumbs = [];

  String? lastUserId;
  String? lastCompanyId;
  bool userCleared = false;

  @override
  Future<void> init() async {}

  @override
  void capture(
    Object error,
    StackTrace? stackTrace, {
    Map<String, Object?>? context,
  }) {
    if (error is ExpectedFailure) return;
    capturedErrors.add((error: error, stackTrace: stackTrace, context: context));
  }

  @override
  void addBreadcrumb(
    String message, {
    String? category,
    Map<String, Object?>? data,
  }) {
    breadcrumbs.add((message: message, category: category, data: data));
  }

  @override
  void setUser({required String? userId, String? companyId}) {
    lastUserId = userId;
    lastCompanyId = companyId;
    userCleared = false;
  }

  @override
  void clearUser() {
    lastUserId = null;
    lastCompanyId = null;
    userCleared = true;
  }

  @override
  http.Client wrapHttpClient(http.Client base) => base;

  @override
  NavigatorObserver get navigatorObserver => NavigatorObserver();
}
