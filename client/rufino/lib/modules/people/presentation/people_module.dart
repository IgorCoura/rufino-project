import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/app_module.dart';
import 'package:rufino/modules/people/presentation/domain/services/people_management_service.dart';
import 'package:rufino/modules/people/presentation/employees_list/bloc/employees_list_bloc.dart';
import 'package:rufino/modules/people/presentation/employees_list/employees_list_page.dart';

class PeopleModule extends Module {
  @override
  void binds(i) {
    i.add<PeopleManagementService>(PeopleManagementService.new);

    i.add<EmployeesListBloc>(EmployeesListBloc.new);
  }

  @override
  List<Module> get imports => [AppModule()];

  @override
  void routes(r) {
    r.child('/list', child: (context) => EmployeesListPage());
  }
}
