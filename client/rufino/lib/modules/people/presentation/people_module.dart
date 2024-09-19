import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/app_module.dart';
import 'package:rufino/modules/people/presentation/employees_list/employees_list_page.dart';

class PeopleModule extends Module {
  @override
  void binds(i) {
    //i.add<HomeBloc>(HomeBloc.new);
  }

  @override
  List<Module> get imports => [AppModule()];

  @override
  void routes(r) {
    r.child('/list', child: (context) => EmployeesListPage());
  }
}
