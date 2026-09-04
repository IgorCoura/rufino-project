import 'package:flutter/widgets.dart';
import 'package:http/http.dart' as http;

import 'error_reporter.dart';

/// An [ErrorReporter] that does nothing.
///
/// Used in tests, in builds where monitoring is disabled, and as the default
/// when `error_monitoring_enabled` is `false`.
class NoopErrorReporter implements ErrorReporter {
  const NoopErrorReporter();

  @override
  Future<void> init() async {}

  @override
  void capture(
    Object error,
    StackTrace? stackTrace, {
    Map<String, Object?>? context,
  }) {}

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
  NavigatorObserver get navigatorObserver => _NoopNavigatorObserver();
}

class _NoopNavigatorObserver extends NavigatorObserver {}
