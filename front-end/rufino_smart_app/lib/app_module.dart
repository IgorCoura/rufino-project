import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_smart_app/Login/bloc/login_bloc.dart';
import 'package:rufino_smart_app/epi/page/epi_home_page.dart';
import 'package:rufino_smart_app/home/page/home_page.dart';
import 'epi/page/epi_employee_page.dart';
import 'fogot_password/page/forgot_password_page.dart';
import 'login/page/login_page.dart';

class AppModule extends Module {
  @override
  List<Bind> get binds => [Bind.singleton((i) => LoginBloc())];

  @override
  List<ModularRoute> get routes => [
        ChildRoute("/", child: (context, args) => LoginPage()),
        ChildRoute("/home", child: (context, args) => HomePage()),
        ChildRoute("/epihome", child: (context, args) => EpiHomePage()),
        ChildRoute("/epiemployee", child: (context, args) => EpiEmployeePage()),
        ChildRoute("/forgotpassword",
            child: (context, args) => const ForgotPasswordPage()),
      ];
}
