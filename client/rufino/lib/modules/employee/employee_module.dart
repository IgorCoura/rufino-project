import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/app_module.dart';
import 'package:rufino/modules/employee/domain/services/people_management_service.dart';
import 'package:rufino/modules/employee/presentation/archive_category/archive_category_page.dart';
import 'package:rufino/modules/employee/presentation/archive_category/bloc/archive_category_bloc.dart';
import 'package:rufino/modules/employee/presentation/document_template/bloc/document_template_bloc.dart';
import 'package:rufino/modules/employee/presentation/document_template/document_template_page.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/bloc/employee_profile_bloc.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/dependents_list_component/bloc/dependents_list_component_bloc.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/employee_profile_page.dart';
import 'package:rufino/modules/employee/presentation/employees_list/bloc/employees_list_bloc.dart';
import 'package:rufino/modules/employee/presentation/employees_list/employees_list_page.dart';

class PeopleModule extends Module {
  @override
  void binds(i) {
    i.add<PeopleManagementService>(PeopleManagementService.new);
    i.add<EmployeesListBloc>(EmployeesListBloc.new);
    i.add<EmployeeProfileBloc>(EmployeeProfileBloc.new);
    i.add<ArchiveCategoryBloc>(ArchiveCategoryBloc.new);
    i.add<DependentsListComponentBloc>(DependentsListComponentBloc.new);
    i.add<DocumentTemplateBloc>(DocumentTemplateBloc.new);
  }

  @override
  List<Module> get imports => [AppModule()];

  @override
  void routes(r) {
    r.child('/list', child: (context) => EmployeesListPage());
    r.child('/archive-category', child: (context) => ArchiveCategoryPage());
    r.child('/document-template', child: (context) => DocumentTemplatePage());
    r.child('/profile/:id',
        child: (context) => EmployeeProfilePage(
              employeeId: r.args.params['id'],
            ));
  }
}
