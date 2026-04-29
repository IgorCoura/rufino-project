import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:rufino_v2/core/errors/auth_exception.dart';
import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/data/repositories/auth_code_repository_impl.dart';

import '../../../testing/mocks/mocks.dart';

void main() {
  late MockAuthCodeApiService mockService;
  late AuthCodeRepositoryImpl repository;

  setUp(() {
    mockService = MockAuthCodeApiService();
    repository =
        AuthCodeRepositoryImpl(authCodeApiService: mockService);
  });

  group('AuthCodeRepositoryImpl', () {
    test('login returns success when the service login completes', () async {
      when(() => mockService.login()).thenAnswer((_) async {});

      final result =
          await repository.login(username: '', password: '');

      expect(result, isA<Success<void>>());
    });

    test('login forwards InvalidCredentialsException unchanged', () async {
      when(() => mockService.login())
          .thenThrow(const InvalidCredentialsException());

      final result =
          await repository.login(username: '', password: '');

      result.fold(
        onSuccess: (_) => fail('expected error'),
        onError: (error) =>
            expect(error, isA<InvalidCredentialsException>()),
      );
    });

    test('login wraps non-AuthException in NetworkAuthException', () async {
      when(() => mockService.login())
          .thenThrow(Exception('boom'));

      final result =
          await repository.login(username: '', password: '');

      result.fold(
        onSuccess: (_) => fail('expected error'),
        onError: (error) => expect(error, isA<NetworkAuthException>()),
      );
    });

    test('hasValidCredentials reflects the service result', () async {
      when(() => mockService.hasValidCredentials())
          .thenAnswer((_) async => true);

      final result = await repository.hasValidCredentials();

      result.fold(
        onSuccess: (value) => expect(value, isTrue),
        onError: (_) => fail('expected success'),
      );
    });

    test('logout returns success even if the service throws', () async {
      when(() => mockService.logout()).thenThrow(Exception('boom'));

      final result = await repository.logout();

      result.fold(
        onSuccess: (_) => fail('expected error'),
        onError: (error) => expect(error, isA<NetworkAuthException>()),
      );
    });
  });
}
