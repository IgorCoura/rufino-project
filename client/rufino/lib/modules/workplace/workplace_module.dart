import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/app_module.dart';
import 'package:rufino/modules/workplace/domain/services/workplace_service.dart';
import 'package:rufino/modules/workplace/presentation/workplace_edit/bloc/workplace_edit_bloc.dart';
import 'package:rufino/modules/workplace/presentation/workplace_edit/workplace_edit_page.dart';
import 'package:rufino/modules/workplace/presentation/workplaces/bloc/workplace_bloc.dart';
import 'package:rufino/modules/workplace/presentation/workplaces/workplaces_page.dart';

class WorkplaceModule extends Module {
  @override
  void binds(i) {
    i.add<WorkplaceService>(WorkplaceService.new);
    i.add<WorkplaceBloc>(WorkplaceBloc.new);
    i.add<WorkplaceEditBloc>(WorkplaceEditBloc.new);
  }

  @override
  List<Module> get imports => [AppModule()];

  @override
  void routes(r) {
    r.child('/', child: (context) => WorkplacesPage());
    r.child(
      '/edit/:id',
      child: (context) => WorkplaceEditPage(
        r.args.params['id'],
      ),
    );
  }
}
