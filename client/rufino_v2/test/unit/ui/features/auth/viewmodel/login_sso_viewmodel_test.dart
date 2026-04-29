import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/core/errors/auth_exception.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/login_sso_viewmodel.dart';

import '../../../../../testing/fakes/fake_auth_repository.dart';

void main() {
  late FakeAuthRepository repo;
  late LoginSsoViewModel vm;

  setUp(() {
    repo = FakeAuthRepository();
    vm = LoginSsoViewModel(authRepository: repo);
  });

  group('LoginSsoViewModel', () {
    test('starts in initial state', () {
      expect(vm.status, LoginSsoStatus.initial);
      expect(vm.lastError, isNull);
      expect(vm.isLoading, isFalse);
    });

    test('transitions to success when the repository login succeeds',
        () async {
      await vm.submit();

      expect(vm.status, LoginSsoStatus.success);
      expect(vm.lastError, isNull);
    });

    test('transitions to failure with the typed error when login fails',
        () async {
      repo.setLoginError(const InvalidCredentialsException());

      await vm.submit();

      expect(vm.status, LoginSsoStatus.failure);
      expect(vm.lastError, isA<InvalidCredentialsException>());
    });

    test('resetError clears the failure state', () async {
      repo.setLoginError(const InvalidCredentialsException());
      await vm.submit();

      vm.resetError();

      expect(vm.status, LoginSsoStatus.initial);
      expect(vm.lastError, isNull);
    });

    test('submit ignores re-entry while a request is already in flight',
        () async {
      final first = vm.submit();
      final second = vm.submit();

      await Future.wait([first, second]);

      expect(vm.status, LoginSsoStatus.success);
    });
  });
}
