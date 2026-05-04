import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart' as http_testing;
import 'package:rufino_v2/core/errors/expected_failure.dart';

import '../../../testing/fakes/fake_error_reporter.dart';

class _BugException implements Exception {
  const _BugException();
}

class _ExpectedException with ExpectedFailure implements Exception {
  const _ExpectedException();
}

void main() {
  group('FakeErrorReporter', () {
    test('records unexpected errors with stack and context', () {
      final reporter = FakeErrorReporter();
      final stack = StackTrace.current;

      reporter.capture(
        const _BugException(),
        stack,
        context: {'op': 'fetch', 'companyId': 'co-1'},
      );

      expect(reporter.capturedErrors, hasLength(1));
      expect(reporter.capturedErrors.first.error, isA<_BugException>());
      expect(reporter.capturedErrors.first.stackTrace, equals(stack));
      expect(reporter.capturedErrors.first.context?['op'], 'fetch');
    });

    test('drops errors that mix in ExpectedFailure', () {
      final reporter = FakeErrorReporter();

      reporter.capture(const _ExpectedException(), StackTrace.current);

      expect(reporter.capturedErrors, isEmpty);
    });

    test('setUser and clearUser update tracked identity', () {
      final reporter = FakeErrorReporter();

      reporter.setUser(userId: 'u-1', companyId: 'co-1');
      expect(reporter.lastUserId, 'u-1');
      expect(reporter.lastCompanyId, 'co-1');
      expect(reporter.userCleared, isFalse);

      reporter.clearUser();
      expect(reporter.lastUserId, isNull);
      expect(reporter.userCleared, isTrue);
    });

    test('addBreadcrumb appends to breadcrumbs list', () {
      final reporter = FakeErrorReporter();

      reporter.addBreadcrumb('navigated', category: 'navigation');

      expect(reporter.breadcrumbs, hasLength(1));
      expect(reporter.breadcrumbs.first.message, 'navigated');
      expect(reporter.breadcrumbs.first.category, 'navigation');
    });

    test('wrapHttpClient is a passthrough that does not wrap the base', () {
      final reporter = FakeErrorReporter();
      final http.Client base = http_testing.MockClient((_) async {
        return http.Response('', 200);
      });

      final wrapped = reporter.wrapHttpClient(base);

      expect(identical(wrapped, base), isTrue);
    });
  });
}
