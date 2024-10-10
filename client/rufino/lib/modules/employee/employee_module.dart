import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/app_module.dart';
import 'package:rufino/modules/employee/domain/services/people_management_service.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/bloc/employee_profile_bloc.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/employee_profile_page.dart';
import 'package:rufino/modules/employee/presentation/employees_list/bloc/employees_list_bloc.dart';
import 'package:rufino/modules/employee/presentation/employees_list/employees_list_page.dart';

class PeopleModule extends Module {
  @override
  void binds(i) {
    i.add<PeopleManagementService>(PeopleManagementService.new);
    i.add<EmployeesListBloc>(EmployeesListBloc.new);
    i.add<EmployeeProfileBloc>(EmployeeProfileBloc.new);
  }

  @override
  List<Module> get imports => [AppModule()];

  @override
  void routes(r) {
    r.child('/list', child: (context) => EmployeesListPage());
    r.child('/profile/:id',
        child: (context) => EmployeeProfilePage(
              employeeId: r.args.params['id'],
            ));
  }
}
