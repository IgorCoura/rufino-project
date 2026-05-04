import 'package:flutter/widgets.dart';
import 'package:http/http.dart' as http;
import 'package:sentry_flutter/sentry_flutter.dart';

import '../config/app_config.dart';
import '../errors/expected_failure.dart';
import '../errors/http_exception.dart';
import 'error_reporter.dart';
import 'pii_scrubber.dart';

/// An [ErrorReporter] backed by `sentry_flutter`.
///
/// This is the only file in the app that imports `package:sentry_flutter`.
/// Every other layer depends on the [ErrorReporter] interface, so swapping
/// to a different vendor (Crashlytics, Datadog, custom endpoint) only
/// requires adding a new implementation file and changing one line in
/// `main.dart`.
///
/// Privacy: structured context maps, breadcrumb data and failed-request
/// response bodies are passed through [scrubJson] / [scrubAndTruncateBody]
/// before reaching the SDK, and `sendDefaultPii` is left at its secure
/// default.
class SentryErrorReporter implements ErrorReporter {
  SentryErrorReporter();

  @override
  Future<void> init() async {
    await SentryFlutter.init((options) {
      options.dsn = AppConfig.errorMonitoringDsn;
      options.environment = AppConfig.errorMonitoringEnvironment;
      options.tracesSampleRate = AppConfig.errorMonitoringTracesSampleRate;
      options.sendDefaultPii = false;
      options.tracePropagationTargets
        ..clear()
        ..addAll([
          if (AppConfig.peopleManagementUrl.isNotEmpty)
            AppConfig.peopleManagementUrl,
        ]);
    });
  }

  @override
  void capture(
    Object error,
    StackTrace? stackTrace, {
    Map<String, Object?>? context,
  }) {
    if (error is ExpectedFailure) return;
    final safeContext = context != null ? scrubJson(context) : null;
    final httpError = _findHttpException(error);
    final httpContext = httpError != null ? _httpContext(httpError) : null;
    Sentry.captureException(
      error,
      stackTrace: stackTrace,
      withScope: (scope) {
        if (safeContext != null && safeContext.isNotEmpty) {
          scope.setContexts('app', safeContext);
        }
        if (httpContext != null) {
          scope.setContexts('http', httpContext);
        }
      },
    );
  }

  @override
  void addBreadcrumb(
    String message, {
    String? category,
    Map<String, Object?>? data,
  }) {
    final safeData = data != null ? scrubJson(data) : null;
    Sentry.addBreadcrumb(
      Breadcrumb(
        message: message,
        category: category,
        data: safeData,
      ),
    );
  }

  @override
  void setUser({required String? userId, String? companyId}) {
    Sentry.configureScope((scope) {
      scope.setUser(
        SentryUser(
          id: userId,
          data: companyId != null ? {'companyId': companyId} : null,
        ),
      );
    });
  }

  @override
  void clearUser() {
    Sentry.configureScope((scope) => scope.setUser(null));
  }

  @override
  http.Client wrapHttpClient(http.Client base) {
    return SentryHttpClient(client: base, captureFailedRequests: false);
  }

  @override
  NavigatorObserver get navigatorObserver => SentryNavigatorObserver();
}

/// Returns the [HttpException] carried by [error], unwrapping the `cause`
/// field of any wrapper exception (e.g. `EmployeeNetworkException`) one
/// level deep — mirrors how `extractServerMessages` reaches the underlying
/// HTTP error.
HttpException? _findHttpException(Object error) {
  if (error is HttpException) return error;
  try {
    final cause = (error as dynamic).cause;
    if (cause is HttpException) return cause;
  } catch (_) {}
  return null;
}

Map<String, Object?> _httpContext(HttpException error) {
  final ctx = <String, Object?>{
    'status_code': error.statusCode,
    if (error.requestMethod != null) 'method': error.requestMethod,
    if (error.requestUrl != null) 'url': error.requestUrl,
    if (error.serverMessages.isNotEmpty)
      'server_messages': error.serverMessages,
  };
  final body = error.responseBody;
  if (body != null && body.isNotEmpty) {
    ctx['response_body'] = scrubAndTruncateBody(body);
  }
  return ctx;
}
