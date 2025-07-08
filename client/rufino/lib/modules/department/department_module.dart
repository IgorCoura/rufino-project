import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/app_module.dart';
import 'package:rufino/modules/department/department_edit/bloc/department_edit_bloc.dart';
import 'package:rufino/modules/department/department_edit/department_edit_page.dart';
import 'package:rufino/modules/department/departments/bloc/departments_bloc.dart';
import 'package:rufino/modules/department/departments/departments_page.dart';
import 'package:rufino/modules/department/domain/services/department_service.dart';
import 'package:rufino/modules/department/domain/services/position_service.dart';
import 'package:rufino/modules/department/domain/services/role_service.dart';
import 'package:rufino/modules/department/position_edit/bloc/position_edit_bloc.dart';
import 'package:rufino/modules/department/position_edit/position_edit_page.dart';
import 'package:rufino/modules/department/role_edit/bloc/role_edit_bloc.dart';
import 'package:rufino/modules/department/role_edit/role_edit_page.dart';

class DepartmentModule extends Module {
  @override
  void binds(i) {
    // SERVICES
    i.add<DepartmentService>(DepartmentService.new);
    i.add<PositionService>(PositionService.new);
    i.add<RoleService>(RoleService.new);
    // BLOCs
    i.add<DepartmentsBloc>(DepartmentsBloc.new);
    i.add<DepartmentEditBloc>(DepartmentEditBloc.new);
    i.add<PositionEditBloc>(PositionEditBloc.new);
    i.add<RoleEditBloc>(RoleEditBloc.new);
  }

  @override
  List<Module> get imports => [AppModule()];

  @override
  void routes(r) {
    r.child('/', child: (context) => DepartmentsPage());
    r.child(
      '/edit/:id',
      child: (context) => DepartmentEditPage(
        r.args.params['id'],
      ),
    );

    r.child(
      '/position/edit/:departmentId/:id',
      child: (context) => PositionEditPage(
        r.args.params['id'],
        r.args.params['departmentId'],
      ),
    );

    r.child(
      '/role/edit/:positionId/:id',
      child: (context) => RoleEditPage(
        r.args.params['id'],
        r.args.params['positionId'],
      ),
    );
  }
}
