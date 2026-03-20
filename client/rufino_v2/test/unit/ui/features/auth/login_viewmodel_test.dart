import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/login_viewmodel.dart';

import '../../../../testing/mocks/mocks.dart';

void main() {
  late MockAuthRepository mockRepository;
  late LoginViewModel viewModel;

  setUp(() {
    mockRepository = MockAuthRepository();
    viewModel = LoginViewModel(authRepository: mockRepository);
  });

  tearDown(() => viewModel.dispose());

  group('LoginViewModel', () {
    test('initial state is correct', () {
      expect(viewModel.status, LoginStatus.initial);
      expect(viewModel.isLoading, false);
      expect(viewModel.lastError, isNull);
    });

    test('submit transitions to inProgress then success', () async {
      when(() => mockRepository.login(username: any(named: 'username'), password: any(named: 'password')))
          .thenAnswer((_) async => const Result.success(null));

      viewModel.onUsernameChanged('user');
      viewModel.onPasswordChanged('password');

      final statuses = <LoginStatus>[];
      viewModel.addListener(() => statuses.add(viewModel.status));

      await viewModel.submit();

      expect(statuses, contains(LoginStatus.inProgress));
      expect(viewModel.status, LoginStatus.success);
    });

    test('submit transitions to failure on error', () async {
      when(() => mockRepository.login(username: any(named: 'username'), password: any(named: 'password')))
          .thenAnswer((_) async => Result.error(Exception('auth error')));

      viewModel.onUsernameChanged('user');
      viewModel.onPasswordChanged('wrong');

      await viewModel.submit();

      expect(viewModel.status, LoginStatus.failure);
      expect(viewModel.lastError, isNotNull);
    });

    test('submit does nothing when credentials are empty', () async {
      await viewModel.submit();
      verifyNever(() => mockRepository.login(username: any(named: 'username'), password: any(named: 'password')));
      expect(viewModel.status, LoginStatus.initial);
    });

    test('resetError clears failure status', () async {
      when(() => mockRepository.login(username: any(named: 'username'), password: any(named: 'password')))
          .thenAnswer((_) async => Result.error(Exception('error')));

      viewModel.onUsernameChanged('user');
      viewModel.onPasswordChanged('pass');
      await viewModel.submit();

      expect(viewModel.status, LoginStatus.failure);
      viewModel.resetError();
      expect(viewModel.status, LoginStatus.initial);
      expect(viewModel.lastError, isNull);
    });
  });
}
