import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_smart_app/Login/bloc/login_bloc.dart';
import 'package:rufino_smart_app/login/bloc/login_event.dart';
import 'package:rufino_smart_app/ppe_manager/bloc/ppe_manager_bloc.dart';
import 'package:rufino_smart_app/ppe_manager/page/ppe_manager_employee_page.dart';
import 'package:rufino_smart_app/ppe_manager/page/ppe_manager_home_page.dart';
import 'package:rufino_smart_app/home/page/home_page.dart';
import 'package:rufino_smart_app/storage/Module/storage_module.dart';
import 'package:rufino_smart_app/storage/page/storage_home_old_page.dart';
import 'login/page/forgot_password_page.dart';
import 'login/page/login_page.dart';

class AppModule extends Module {
  @override
  List<Bind> get binds => [
        Bind.singleton((i) => LoginBloc()),
        Bind.singleton((i) => PpeManagerBloc())
      ];

  @override
  List<ModularRoute> get routes => [
        ChildRoute("/", child: (context, args) => LoginPage()),
        ChildRoute("/home", child: (context, args) => HomePage()),
        ChildRoute("/ppemanagerhome", child: (context, args) => HomePage()),
        ChildRoute("/ppemanageremployee/:id",
            child: (context, args) =>
                PpeManagerEmployeePage(workerId: args.params['id'])),
        ChildRoute("/forgotpassword",
            child: (context, args) => const ForgotPasswordPage()),
        ModuleRoute("/storage", module: StorageModule()),
      ];
}
