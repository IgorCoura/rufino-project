import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_app/src/home/page/home_page.dart';
import 'package:rufino_app/src/login/pages/bloc/login_bloc.dart';
import 'package:rufino_app/src/login/pages/login_page.dart';
import 'package:rufino_app/src/stock/Module/stock_module.dart';

class AppModule extends Module {
  @override
  List<Bind<Object>> get binds => [
        Bind.singleton((i) => LoginBloc()),
      ];

  @override
  List<ModularRoute> get routes => [
        ChildRoute("/", child: (context, args) => LoginPage()),
        ChildRoute("/home", child: (context, args) => HomePage()),
        ModuleRoute("/stock", module: StockModule()),
      ];
}
