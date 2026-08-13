import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/core/errors/auth_exception.dart';
import 'package:rufino_core/rufino_core.dart';
import '../../../testing/fakes/fake_error_reporter.dart';
/// Mirrors the `*NetworkException` wrapper pattern used by repositories.
class _WrapperException implements Exception {
  const _WrapperException(this.cause);
  final Object cause;
}

class _CauselessException implements Exception {
  const _CauselessException();
}

void main() {
  group('isExpectedFailure', () {
    test('is true for an ExpectedFailure instance', () {
      expect(isExpectedFailure(const SessionExpiredException()), isTrue);
    });

    test('is true for a wrapper whose cause is an ExpectedFailure', () {
      const wrapped = _WrapperException(AccessDeniedException());
      expect(isExpectedFailure(wrapped), isTrue);
    });

    test('is false for a wrapper whose cause is not an ExpectedFailure', () {
      const wrapped = _WrapperException('boom');
      expect(isExpectedFailure(wrapped), isFalse);
    });

    test('is false for an exception without a cause field', () {
      expect(isExpectedFailure(const _CauselessException()), isFalse);
    });
  });

  group('FakeErrorReporter', () {
    test('does not capture a wrapper whose cause is an ExpectedFailure', () {
      final reporter = FakeErrorReporter();

      reporter.capture(
        const _WrapperException(SessionExpiredException()),
        StackTrace.current,
      );

      expect(reporter.capturedErrors, isEmpty);
    });

    test('still captures a wrapper carrying an unexpected cause', () {
      final reporter = FakeErrorReporter();

      reporter.capture(
        const _WrapperException('socket closed'),
        StackTrace.current,
      );

      expect(reporter.capturedErrors, hasLength(1));
    });
  });
}
