import 'package:flutter/widgets.dart';
import 'package:http/http.dart' as http;

import '../result.dart';

/// A provider-agnostic facade for capturing runtime errors and breadcrumbs.
///
/// Only one concrete implementation imports a vendor SDK; the rest of the
/// app depends on this interface so that swapping the backend (Sentry,
/// Crashlytics, a custom HTTP endpoint, etc.) is a one-line change in
/// `main.dart` and does not ripple through repositories, ViewModels, or
/// widgets.
///
/// Capture flow:
/// - Repositories call [capture] from each `catch (e, st)` block before
///   returning `Result.error(e, st)`.
/// - Implementations short-circuit when `error is ExpectedFailure` so that
///   user-actionable errors (e.g. invalid credentials) are not reported.
/// - HTTP and navigation breadcrumbs are added automatically through
///   [wrapHttpClient] and [navigatorObserver].
abstract class ErrorReporter {
  /// Initializes the underlying SDK.
  ///
  /// Must be awaited in `main()` before [runApp].
  Future<void> init();

  /// Reports an unexpected [error] together with its [stackTrace].
  ///
  /// [context] is attached as additional structured data on the report.
  /// Implementations must drop the report when `error is ExpectedFailure`.
  void capture(
    Object error,
    StackTrace? stackTrace, {
    Map<String, Object?>? context,
  });

  /// Records a breadcrumb that will be attached to the next captured event.
  void addBreadcrumb(
    String message, {
    String? category,
    Map<String, Object?>? data,
  });

  /// Associates subsequent events with the currently logged-in user.
  ///
  /// Pass `null` for [userId] together with [clearUser] when logging out.
  void setUser({required String? userId, String? companyId});

  /// Clears the user context so subsequent events are anonymous.
  void clearUser();

  /// Wraps the app's [http.Client] to record HTTP breadcrumbs.
  ///
  /// Returns [base] unchanged when monitoring is disabled.
  http.Client wrapHttpClient(http.Client base);

  /// A [NavigatorObserver] to attach to the router for navigation breadcrumbs.
  ///
  /// Returns a no-op observer when monitoring is disabled.
  NavigatorObserver get navigatorObserver;
}

/// Reporting helpers for the repository layer.
extension ErrorReporterFailure on ErrorReporter {
  /// Reports [error] with [stackTrace] and returns a `Result.error` carrying
  /// the same pair, in a single call.
  ///
  /// Use this from `catch (e, st)` blocks in repository implementations to
  /// avoid duplicating the capture-then-return pattern at every call site.
  Result<T> failure<T>(
    Object error,
    StackTrace stackTrace, {
    Map<String, Object?>? context,
  }) {
    capture(error, stackTrace, context: context);
    return Result.error(error, stackTrace);
  }
}
