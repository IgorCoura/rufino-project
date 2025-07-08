import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/app_module.dart';
import 'package:rufino/modules/auth/presentation/initial/bloc/authentication_bloc.dart';
import 'package:rufino/modules/auth/presentation/initial/initial_page.dart';
import 'package:rufino/modules/auth/presentation/login/bloc/login_bloc.dart';
import 'package:rufino/modules/auth/presentation/login/login_page.dart';

class AuthModule extends Module {
  @override
  void binds(i) {
    i.add<AuthenticationBloc>(AuthenticationBloc.new);
    i.add<LoginBloc>(LoginBloc.new);
  }

  @override
  List<Module> get imports => [AppModule()];

  @override
  void routes(r) {
    r.child('/', child: (context) => const InitialPage());
    r.child('/login', child: (context) => const LoginPage());
  }
}
