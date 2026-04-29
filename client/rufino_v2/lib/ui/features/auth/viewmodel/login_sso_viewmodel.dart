import 'package:flutter/foundation.dart';

import '../../../../core/errors/auth_exception.dart';
import '../../../../domain/repositories/auth_repository.dart';

enum LoginSsoStatus { initial, inProgress, success, failure }

/// View-model for the SSO (Authorization Code Flow) login screen.
///
/// There is no username/password form — [submit] simply asks the
/// repository to launch the system browser and waits for the result.
class LoginSsoViewModel extends ChangeNotifier {
  LoginSsoViewModel({required AuthRepository authRepository})
      : _authRepository = authRepository;

  final AuthRepository _authRepository;

  LoginSsoStatus _status = LoginSsoStatus.initial;
  AuthException? _lastError;

  LoginSsoStatus get status => _status;
  AuthException? get lastError => _lastError;
  bool get isLoading => _status == LoginSsoStatus.inProgress;

  Future<void> submit() async {
    if (_status == LoginSsoStatus.inProgress) return;
    _status = LoginSsoStatus.inProgress;
    _lastError = null;
    notifyListeners();

    try {
      final result = await _authRepository.login(username: '', password: '');
      result.fold(
        onSuccess: (_) => _status = LoginSsoStatus.success,
        onError: (error) {
          _status = LoginSsoStatus.failure;
          _lastError =
              error is AuthException ? error : NetworkAuthException(error);
        },
      );
    } finally {
      notifyListeners();
    }
  }

  void resetError() {
    if (_status == LoginSsoStatus.failure) {
      _status = LoginSsoStatus.initial;
      _lastError = null;
      notifyListeners();
    }
  }
}
