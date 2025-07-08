import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/app_module.dart';
import 'package:rufino/modules/company/company_edit/bloc/company_edit_bloc.dart';
import 'package:rufino/modules/company/company_edit/company_edit_page.dart';
import 'package:rufino/modules/company/company_selection/bloc/company_selection_bloc.dart';
import 'package:rufino/modules/company/company_selection/company_selection_page.dart';
import 'package:rufino/modules/company/domain/services/company_service.dart';

class CompanyModule extends Module {
  @override
  void binds(i) {
    // SERVICES
    i.addSingleton<CompanyService>(CompanyService.new);
    // BLOCs
    i.add<CompanySelectionBloc>(CompanySelectionBloc.new);
    i.add<CompanyEditBloc>(CompanyEditBloc.new);
  }

  @override
  List<Module> get imports => [AppModule()];

  @override
  void routes(r) {
    r.child('/', child: (context) => CompanySelectionPage());
    r.child('/create', child: (context) => CompanyEditPage());
    r.child('/edit/{id}', child: (context) => CompanyEditPage());
    r.child(
      '/edit/:id',
      child: (context) => CompanyEditPage(
        companyId: r.args.params['id'],
      ),
    );
  }
}
