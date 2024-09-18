import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/auth/domain/services/auth_service.dart';
import 'package:rufino/modules/auth/domain/services/user_service.dart';
import 'package:rufino/modules/auth/presentation/initial/bloc/authentication_bloc.dart';
import 'package:rufino/modules/auth/presentation/initial/initial_page.dart';
import 'package:rufino/modules/auth/presentation/login/bloc/login_bloc.dart';
import 'package:rufino/modules/auth/presentation/login/login_page.dart';

class AuthModule extends Module {
  @override
  void binds(i) {
    i.add<AuthenticationService>(AuthenticationService.new);
    i.add<UserService>(UserService.new);
    i.addLazySingleton<AuthenticationBloc>(AuthenticationBloc.new);
    i.addLazySingleton<LoginBloc>(LoginBloc.new);
  }

  @override
  void routes(r) {
    r.child('/', child: (context) => const InitialPage());
    r.child('/login', child: (context) => LoginPage());
  }
}
