import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';

/// An exception that carries a `cause` but is not itself expected — the shape
/// repositories use when they wrap an unknown error before reporting it.
class _WrappingException implements Exception {
  const _WrappingException(this.cause);

  final Object cause;
}

/// An exception whose `cause` getter blows up when read.
class _HostileException implements Exception {
  Object get cause => throw StateError('cause is not readable');
}

void main() {
  group('isExpectedFailure', () {
    test('recognizes an exception that mixes in ExpectedFailure', () {
      expect(isExpectedFailure(const SessionExpiredException()), isTrue);
      expect(isExpectedFailure(const AccessDeniedException()), isTrue);
      expect(isExpectedFailure(const InvalidCredentialsException()), isTrue);
      expect(isExpectedFailure(const NoCredentialsException()), isTrue);
    });

    test('rejects an ordinary exception', () {
      expect(isExpectedFailure(Exception('boom')), isFalse);
      expect(isExpectedFailure(StateError('boom')), isFalse);
    });

    test('rejects a value that is not an exception at all', () {
      expect(isExpectedFailure('plain string'), isFalse);
      expect(isExpectedFailure(42), isFalse);
    });

    test('recognizes an expected failure hidden inside a wrapper cause', () {
      const wrapped = _WrappingException(SessionExpiredException());

      expect(isExpectedFailure(wrapped), isTrue);
    });

    test('recognizes it through the auth transport wrapper the app ships', () {
      const wrapped = NetworkAuthException(InvalidCredentialsException());

      expect(isExpectedFailure(wrapped), isTrue);
    });

    test('rejects a wrapper whose cause is an ordinary error', () {
      final wrapped = _WrappingException(Exception('socket closed'));

      expect(isExpectedFailure(wrapped), isFalse);
    });

    test('looks exactly one cause deep, so a doubly wrapped failure is not '
        'recognized', () {
      const inner = _WrappingException(SessionExpiredException());
      const outer = _WrappingException(inner);

      expect(isExpectedFailure(outer), isFalse);
    });

    test('rejects an exception whose cause cannot be read instead of '
        'rethrowing', () {
      expect(isExpectedFailure(_HostileException()), isFalse);
    });

    test('rejects a permission fetch failure, which is a bug and must reach '
        'the crash dashboard', () {
      expect(
        isExpectedFailure(const PermissionFetchException('HTTP 500')),
        isFalse,
      );
    });
  });
}
