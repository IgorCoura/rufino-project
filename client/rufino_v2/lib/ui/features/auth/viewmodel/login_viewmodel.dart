import 'package:flutter/material.dart';

import '../../../../core/errors/auth_exception.dart';
import '../../../../domain/repositories/auth_repository.dart';

enum LoginStatus { initial, inProgress, success, failure }

class LoginViewModel extends ChangeNotifier {
  LoginViewModel({required AuthRepository authRepository})
      : _authRepository = authRepository {
    usernameController.addListener(_onCredentialChanged);
    passwordController.addListener(_onCredentialChanged);
  }

  final AuthRepository _authRepository;

  final usernameController = TextEditingController();
  final passwordController = TextEditingController();

  LoginStatus _status = LoginStatus.initial;
  AuthException? _lastError;

  LoginStatus get status => _status;
  AuthException? get lastError => _lastError;
  bool get isLoading => _status == LoginStatus.inProgress;

  void _onCredentialChanged() {
    if (_status == LoginStatus.failure) {
      _status = LoginStatus.initial;
      _lastError = null;
      notifyListeners();
    }
  }

  Future<void> submit() async {
    if (usernameController.text.isEmpty || passwordController.text.isEmpty) return;
    _status = LoginStatus.inProgress;
    _lastError = null;
    notifyListeners();

    try {
      final result = await _authRepository.login(
        username: usernameController.text,
        password: passwordController.text,
      );
      result.fold(
        onSuccess: (_) => _status = LoginStatus.success,
        onError: (error) {
          _status = LoginStatus.failure;
          _lastError = error is AuthException ? error : NetworkAuthException(error);
        },
      );
    } finally {
      notifyListeners();
    }
  }

  void resetError() {
    if (_status == LoginStatus.failure) {
      _status = LoginStatus.initial;
      _lastError = null;
      notifyListeners();
    }
  }

  @override
  void dispose() {
    usernameController.dispose();
    passwordController.dispose();
    super.dispose();
  }
}
