import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/app_module.dart';
import 'package:rufino/modules/auth/presentation/company_selection/bloc/company_selection_bloc.dart';
import 'package:rufino/modules/auth/presentation/company_selection/company_selection_page.dart';
import 'package:rufino/modules/auth/presentation/initial/bloc/authentication_bloc.dart';
import 'package:rufino/modules/auth/presentation/initial/initial_page.dart';
import 'package:rufino/modules/auth/presentation/login/bloc/login_bloc.dart';
import 'package:rufino/modules/auth/presentation/login/login_page.dart';

class AuthModule extends Module {
  @override
  void binds(i) {
    i.addLazySingleton<AuthenticationBloc>(AuthenticationBloc.new);
    i.addLazySingleton<LoginBloc>(LoginBloc.new);
    i.addLazySingleton<CompanySelectionBloc>(CompanySelectionBloc.new);
  }

  @override
  List<Module> get imports => [AppModule()];

  @override
  void routes(r) {
    r.child('/', child: (context) => const InitialPage());
    r.child('/login', child: (context) => const LoginPage());
    r.child('/company-selection', child: (context) => CompanySelectionPage());
  }
}
