import 'package:flutter/foundation.dart';

import '../../../../core/errors/auth_exception.dart';
import '../../../../domain/repositories/auth_repository.dart';

enum LoginStatus { initial, inProgress, success, failure }

class LoginViewModel extends ChangeNotifier {
  LoginViewModel({required AuthRepository authRepository})
      : _authRepository = authRepository;

  final AuthRepository _authRepository;

  String _username = '';
  String _password = '';
  LoginStatus _status = LoginStatus.initial;
  AuthException? _lastError;

  String get username => _username;
  String get password => _password;
  LoginStatus get status => _status;
  AuthException? get lastError => _lastError;
  bool get isLoading => _status == LoginStatus.inProgress;

  void onUsernameChanged(String value) {
    _username = value;
    if (_status == LoginStatus.failure) {
      _status = LoginStatus.initial;
      _lastError = null;
      notifyListeners();
    }
  }

  void onPasswordChanged(String value) {
    _password = value;
    if (_status == LoginStatus.failure) {
      _status = LoginStatus.initial;
      _lastError = null;
      notifyListeners();
    }
  }

  Future<void> submit() async {
    if (_username.isEmpty || _password.isEmpty) return;
    _status = LoginStatus.inProgress;
    _lastError = null;
    notifyListeners();

    final result = await _authRepository.login(
      username: _username,
      password: _password,
    );

    result.fold(
      onSuccess: (_) => _status = LoginStatus.success,
      onError: (error) {
        _status = LoginStatus.failure;
        _lastError = error is AuthException ? error : NetworkAuthException(error);
      },
    );
    notifyListeners();
  }

  void resetError() {
    if (_status == LoginStatus.failure) {
      _status = LoginStatus.initial;
      _lastError = null;
      notifyListeners();
    }
  }
}
