import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/app_module.dart';
import 'package:rufino/modules/employee/presentation/document_group/bloc/document_group_bloc.dart';
import 'package:rufino/modules/employee/presentation/document_group/document_group_page.dart';
import 'package:rufino/modules/employee/services/document_group_service.dart';
import 'package:rufino/modules/employee/services/document_service.dart';
import 'package:rufino/modules/employee/services/document_template_service.dart';
import 'package:rufino/modules/employee/services/people_management_service.dart';
import 'package:rufino/modules/employee/presentation/archive_category/archive_category_page.dart';
import 'package:rufino/modules/employee/presentation/archive_category/bloc/archive_category_bloc.dart';
import 'package:rufino/modules/employee/presentation/document_template/bloc/document_template_bloc.dart';
import 'package:rufino/modules/employee/presentation/document_template/document_template_page.dart';
import 'package:rufino/modules/employee/presentation/document_template_list/bloc/document_template_list_bloc.dart';
import 'package:rufino/modules/employee/presentation/document_template_list/document_template_list_page.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/bloc/employee_profile_bloc.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/dependents_list_component/bloc/dependents_list_component_bloc.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/documents_component/bloc/documents_component_bloc.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/employee_profile_page.dart';
import 'package:rufino/modules/employee/presentation/employees_list/bloc/employees_list_bloc.dart';
import 'package:rufino/modules/employee/presentation/employees_list/employees_list_page.dart';
import 'package:rufino/modules/employee/presentation/require_document/bloc/require_document_bloc.dart';
import 'package:rufino/modules/employee/presentation/require_document/require_document_page.dart';
import 'package:rufino/modules/employee/presentation/require_document_list/bloc/require_document_list_bloc.dart';
import 'package:rufino/modules/employee/presentation/require_document_list/require_document_list_page.dart';

class PeopleModule extends Module {
  @override
  void binds(i) {
    //SERVICES
    i.add<PeopleManagementService>(PeopleManagementService.new);
    i.add<DocumentService>(DocumentService.new);
    i.add<DocumentTemplateService>(DocumentTemplateService.new);
    i.add<DocumentGroupService>(DocumentGroupService.new);

    //BLOCs
    i.add<EmployeesListBloc>(EmployeesListBloc.new);
    i.add<EmployeeProfileBloc>(EmployeeProfileBloc.new);
    i.add<ArchiveCategoryBloc>(ArchiveCategoryBloc.new);
    i.add<DependentsListComponentBloc>(DependentsListComponentBloc.new);
    i.add<DocumentTemplateBloc>(DocumentTemplateBloc.new);
    i.add<DocumentTemplateListBloc>(DocumentTemplateListBloc.new);
    i.add<RequireDocumentListBloc>(RequireDocumentListBloc.new);
    i.add<RequireDocumentBloc>(RequireDocumentBloc.new);
    i.add<DocumentsComponentBloc>(DocumentsComponentBloc.new);
    i.add<DocumentGroupBloc>(DocumentGroupBloc.new);
  }

  @override
  List<Module> get imports => [AppModule()];

  @override
  void routes(r) {
    r.child('/', child: (context) => EmployeesListPage());
    r.child('/archive-category', child: (context) => ArchiveCategoryPage());
    r.child('/document-template',
        child: (context) => DocumentTemplateListPage());

    r.child('/document-template/:id',
        child: (context) => DocumentTemplatePage(
              documentTemplateId: r.args.params['id'],
            ));
    r.child('/profile/:id',
        child: (context) => EmployeeProfilePage(
              employeeId: r.args.params['id'],
            ));
    r.child(
      '/require-documents',
      child: (context) => RequireDocumentListPage(),
    );
    r.child(
      '/require-documents/:id',
      child: (context) => RequireDocumentPage(
        requireDocumentId: r.args.params['id'],
      ),
    );
    r.child(
      '/document-group/',
      child: (context) => DocumentGroupPage(),
    );
  }
}
