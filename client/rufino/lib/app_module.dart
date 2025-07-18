import 'package:flutter_modular/flutter_modular.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:rufino/domain/services/auth_service.dart';
import 'package:rufino/domain/services/company_global_service.dart';
import 'package:rufino/modules/auth/auth_module.dart';
import 'package:rufino/modules/company/company_module.dart';
import 'package:rufino/modules/department/department_module.dart';
import 'package:rufino/modules/home/home_module.dart';
import 'package:rufino/modules/employee/employee_module.dart';
import 'package:rufino/modules/workplace/workplace_module.dart';

class AppModule extends Module {
  @override
  void binds(i) {}
  @override
  void exportedBinds(i) {
    i.addSingleton<FlutterSecureStorage>(FlutterSecureStorage.new);
    i.addSingleton<AuthService>(AuthService.new);
    i.addSingleton<CompanyGlobalService>(CompanyGlobalService.new);
  }

  @override
  void routes(r) {
    r.module('/', module: AuthModule());
    r.module('/home', module: HomeModule());
    r.module('/employee', module: PeopleModule());
    r.module('/company', module: CompanyModule());
    r.module('/workplace', module: WorkplaceModule());
    r.module('/department', module: DepartmentModule());
  }
}
