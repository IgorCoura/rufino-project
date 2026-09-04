import 'package:flutter/widgets.dart';
import 'package:http/http.dart' as http;
import 'package:rufino_core/rufino_core.dart';

/// A recording [ErrorReporter] for tests.
///
/// Mirrors the production short-circuit on `ExpectedFailure`, so an assertion
/// about [capturedErrors] reflects exactly what would reach the monitor.
class FakeErrorReporter implements ErrorReporter {
  /// Everything that would have been reported.
  final List<
      ({
        Object error,
        StackTrace? stackTrace,
        Map<String, Object?>? context
      })> capturedErrors = [];

  @override
  Future<void> init() async {}

  @override
  void capture(
    Object error,
    StackTrace? stackTrace, {
    Map<String, Object?>? context,
  }) {
    if (isExpectedFailure(error)) return;
    capturedErrors
        .add((error: error, stackTrace: stackTrace, context: context));
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
