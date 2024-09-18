import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/auth/auth_module.dart';
import 'package:rufino/modules/home/pages/home_page.dart';

class AppModule extends Module {
  @override
  void binds(i) {}

  @override
  void routes(r) {
    r.module('/', module: AuthModule());
    r.child('/home', child: (context) => const HomePage());
  }
}
