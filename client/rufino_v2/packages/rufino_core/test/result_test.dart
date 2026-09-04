import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';

/// The error contract every repository in the app answers with.
///
/// If `fold` ever ran the wrong branch, or `valueOrNull` leaked a value out of
/// a failure, every screen would silently render stale or empty data instead
/// of an error — so the branches are asserted one by one rather than through a
/// round trip.
void main() {
  group('Success', () {
    test('reports itself as a success and not as an error', () {
      const result = Result<int>.success(7);

      expect(result.isSuccess, isTrue);
      expect(result.isError, isFalse);
    });

    test('exposes the wrapped value and no error', () {
      const result = Result<String>.success('ok');

      expect(result.valueOrNull, 'ok');
      expect(result.errorOrNull, isNull);
    });

    test('runs only the success branch of fold and returns its value', () {
      const result = Result<int>.success(21);
      var errorBranchRan = false;

      final doubled = result.fold(
        onSuccess: (value) => value * 2,
        onError: (_, __) {
          errorBranchRan = true;
          return 0;
        },
      );

      expect(doubled, 42);
      expect(errorBranchRan, isFalse);
    });

    test('is the runtime type produced by the success factory', () {
      expect(const Result<int>.success(1), isA<Success<int>>());
      expect(const Success(1).value, 1);
    });

    test('still folds into the success branch when the value is null', () {
      const Result<String?> result = Result<String?>.success(null);

      expect(result.isSuccess, isTrue);
      expect(result.fold(onSuccess: (_) => 'success', onError: (_, __) => 'error'),
          'success');
    });

    test('cannot be told apart from a failure by valueOrNull alone when the '
        'value is null', () {
      const Result<String?> success = Result<String?>.success(null);
      final Result<String?> failure = Result<String?>.error(Exception('boom'));

      expect(success.valueOrNull, isNull);
      expect(failure.valueOrNull, isNull);
      expect(success.isSuccess, isNot(failure.isSuccess));
    });
  });

  group('Failure', () {
    test('reports itself as an error and not as a success', () {
      final result = Result<int>.error(Exception('boom'));

      expect(result.isError, isTrue);
      expect(result.isSuccess, isFalse);
    });

    test('exposes the error and no value', () {
      final error = Exception('boom');
      final result = Result<int>.error(error);

      expect(result.errorOrNull, same(error));
      expect(result.valueOrNull, isNull);
    });

    test('runs only the error branch of fold, handing over error and stack',
        () {
      final error = Exception('boom');
      final stackTrace = StackTrace.current;
      final result = Result<int>.error(error, stackTrace);
      var successBranchRan = false;

      final seen = result.fold(
        onSuccess: (_) {
          successBranchRan = true;
          return null;
        },
        onError: (e, st) => (e, st),
      );

      expect(seen, (error, stackTrace));
      expect(successBranchRan, isFalse);
    });

    test('hands a null stack trace to fold when none was captured', () {
      final result = Result<int>.error(Exception('boom'));

      final stackTrace = result.fold(
        onSuccess: (_) => StackTrace.empty,
        onError: (_, st) => st,
      );

      expect(stackTrace, isNull);
    });

    test('is the runtime type produced by the error factory', () {
      expect(Result<int>.error(Exception('boom')), isA<Failure<int>>());
    });

    test('accepts any object as the error, not only exceptions', () {
      const result = Result<int>.error('plain string failure');

      expect(result.errorOrNull, 'plain string failure');
    });
  });

  group('Result', () {
    test('matches exhaustively in a switch without a default branch', () {
      String describe(Result<int> result) => switch (result) {
            Success(:final value) => 'success:$value',
            Failure(:final error) => 'failure:$error',
          };

      expect(describe(const Result<int>.success(3)), 'success:3');
      expect(describe(const Result<int>.error('nope')), 'failure:nope');
    });
  });
}
