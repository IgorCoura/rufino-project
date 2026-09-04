import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';

import '../fakes/fakes.dart';

void main() {
  group('NoopErrorReporter', () {
    const reporter = NoopErrorReporter();

    test('initializes without touching anything', () async {
      await expectLater(reporter.init(), completes);
    });

    test('swallows every capture, breadcrumb and user change', () {
      expect(
        () {
          reporter.capture(Exception('boom'), StackTrace.current);
          reporter.capture(Exception('boom'), null, context: {'a': 1});
          reporter.addBreadcrumb('opened screen', category: 'nav');
          reporter.setUser(userId: 'u-1', companyId: 'co-1');
          reporter.clearUser();
        },
        returnsNormally,
      );
    });

    test('hands back the same http client it was given, unwrapped', () {
      final base = FakeHttpClient.status(200);

      expect(identical(reporter.wrapHttpClient(base), base), isTrue);
    });

    test('supplies a navigator observer that can be attached to a router', () {
      expect(reporter.navigatorObserver, isA<NavigatorObserver>());
    });

    test('is const constructible, so it can be a compile-time default', () {
      expect(const NoopErrorReporter(), isA<ErrorReporter>());
    });
  });

  group('ErrorReporter.failure', () {
    test('returns a failure carrying the very error and stack it reported',
        () {
      final reporter = RecordingErrorReporter();
      final error = StateError('boom');
      final stackTrace = StackTrace.current;

      final result = reporter.failure<int>(error, stackTrace);

      expect(result.isError, isTrue);
      expect(result.errorOrNull, same(error));
      expect(
        result.fold(onSuccess: (_) => null, onError: (_, st) => st),
        same(stackTrace),
      );
    });

    test('reports the error on the way out', () {
      final reporter = RecordingErrorReporter();
      final error = StateError('boom');

      reporter.failure<int>(error, StackTrace.current);

      expect(reporter.captured.single.error, same(error));
    });

    test('does not report a user-actionable failure', () {
      final reporter = RecordingErrorReporter();

      final result = reporter.failure<int>(
        const SessionExpiredException(),
        StackTrace.current,
      );

      expect(result.isError, isTrue);
      expect(reporter.captured, isEmpty);
      expect(reporter.offered, hasLength(1));
    });

    test('is typed to the value the caller was going to return', () {
      final reporter = RecordingErrorReporter();

      final result =
          reporter.failure<List<String>>(StateError('boom'), StackTrace.current);

      expect(result, isA<Result<List<String>>>());
    });
  });
}
