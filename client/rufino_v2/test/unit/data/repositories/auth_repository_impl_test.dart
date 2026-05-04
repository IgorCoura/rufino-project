import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:rufino_v2/core/errors/auth_exception.dart';
import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/data/repositories/auth_repository_impl.dart';

import '../../../testing/fakes/fake_error_reporter.dart';
import '../../../testing/mocks/mocks.dart';

void main() {
  late MockAuthApiService mockService;
  late FakeErrorReporter reporter;
  late AuthRepositoryImpl repository;

  setUp(() {
    mockService = MockAuthApiService();
    reporter = FakeErrorReporter();
    repository = AuthRepositoryImpl(
      authApiService: mockService,
      reporter: reporter,
    );
  });

  group('AuthRepositoryImpl login', () {
    test('returns success when service login succeeds', () async {
      when(() => mockService.login(
              username: any(named: 'username'),
              password: any(named: 'password')))
          .thenAnswer((_) async {});

      final result = await repository.login(
        username: 'user',
        password: 'pass',
      );

      expect(result, isA<Success<void>>());
    });

    test('returns InvalidCredentialsException when service throws it',
        () async {
      when(() => mockService.login(
              username: any(named: 'username'),
              password: any(named: 'password')))
          .thenThrow(const InvalidCredentialsException());

      final result = await repository.login(
        username: 'user',
        password: 'wrong',
      );

      result.fold(
        onSuccess: (_) => fail('expected error'),
        onError: (error, _) =>
            expect(error, isA<InvalidCredentialsException>()),
      );
    });

    test('returns NetworkAuthException when service throws a generic error',
        () async {
      when(() => mockService.login(
              username: any(named: 'username'),
              password: any(named: 'password')))
          .thenThrow(Exception('socket error'));

      final result = await repository.login(
        username: 'user',
        password: 'pass',
      );

      result.fold(
        onSuccess: (_) => fail('expected error'),
        onError: (error, _) => expect(error, isA<NetworkAuthException>()),
      );
    });

    test('reports unexpected NetworkAuthException to the error reporter',
        () async {
      when(() => mockService.login(
              username: any(named: 'username'),
              password: any(named: 'password')))
          .thenThrow(Exception('socket error'));

      await repository.login(username: 'user', password: 'pass');

      expect(reporter.capturedErrors, hasLength(1));
      expect(
        reporter.capturedErrors.first.error,
        isA<NetworkAuthException>(),
      );
    });

    test(
      'does not report InvalidCredentialsException because it is an '
      'expected user-actionable failure',
      () async {
        when(() => mockService.login(
                username: any(named: 'username'),
                password: any(named: 'password')))
            .thenThrow(const InvalidCredentialsException());

        await repository.login(username: 'user', password: 'wrong');

        expect(reporter.capturedErrors, isEmpty);
      },
    );
  });
}
