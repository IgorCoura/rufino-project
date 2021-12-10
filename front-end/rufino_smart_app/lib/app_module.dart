import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_smart_app/Login/page/login_screen.dart';
import 'package:rufino_smart_app/home_page.dart';

class AppModule extends Module {
  @override
  List<Bind> get binds => [];

  @override
  List<ModularRoute> get routes => [
        ChildRoute("/", child: (context, args) => const LoginScreen()),
      ];
}
