import 'dart:async';

import 'package:rufino/modules/auth/domain/enums/authentication_status.dart';

class AuthenticationService {
  final _controller = StreamController<AuthenticationStatus>();

  Stream<AuthenticationStatus> get status async* {
    await Future<void>.delayed(const Duration(seconds: 1));
    yield AuthenticationStatus.unauthenticated;
    yield* _controller.stream;
  }

  Future<bool> logIn({
    required String username,
    required String password,
  }) async {
    await Future.delayed(const Duration(milliseconds: 300), () {});
    if (username == "admin" && password == "admin") {
      _controller.add(AuthenticationStatus.authenticated);
      return true;
    } else {
      _controller.add(AuthenticationStatus.unauthenticated);
      return false;
    }
  }

  void logOut() {
    _controller.add(AuthenticationStatus.unauthenticated);
  }

  void dispose() => _controller.close();
}
