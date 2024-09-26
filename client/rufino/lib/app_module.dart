import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/domain/services/auth_service.dart';
import 'package:rufino/domain/services/company_service.dart';
import 'package:rufino/modules/auth/auth_module.dart';
import 'package:rufino/modules/home/home_module.dart';
import 'package:rufino/modules/people/presentation/people_module.dart';

class AppModule extends Module {
  @override
  void binds(i) {}
  @override
  void exportedBinds(i) {
    i.addSingleton<AuthService>(AuthService.new);
    i.addSingleton<CompanyService>(CompanyService.new);
  }

  @override
  void routes(r) {
    r.module('/', module: AuthModule());
    r.module('/home', module: HomeModule());
    r.module('/people', module: PeopleModule());
  }
}
